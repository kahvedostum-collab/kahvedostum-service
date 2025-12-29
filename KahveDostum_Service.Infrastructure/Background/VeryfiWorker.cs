using System.Net.Http;
using System.Text;
using System.Text.Json;
using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace KahveDostum_Service.Infrastructure.Background;

public sealed class VerifyReceiptWorker : BackgroundService
{
    private readonly RabbitOptions _opt;
    private readonly VeryfiOptions _veryfi;
    private readonly MinioOptions _minio;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VerifyReceiptWorker> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string JobsQueueName = "ocr.jobs";
    private const string JobsRoutingKey = "jobs";
    private const string DlqQueueName = "ocr.jobs.dlq";
    private const string DlqRoutingKey = "jobs.dlq";

    private const ushort PrefetchCount = 50;
    private const int MaxAttempts = 3;
    private const string AttemptHeaderKey = "attempt";

    public VerifyReceiptWorker(
        IOptions<RabbitOptions> opt,
        IOptions<VeryfiOptions> veryfi,
        IOptions<MinioOptions> minio,
        IHttpClientFactory httpClientFactory,
        ILogger<VerifyReceiptWorker> logger)
    {
        _opt = opt.Value;
        _veryfi = veryfi.Value;
        _minio = minio.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogCritical("🔥🔥 VERIFY RECEIPT WORKER BAŞLIYOR 🔥🔥");
        _logger.LogInformation(
            "VerifyReceiptWorker connecting to RabbitMQ {Host}:{Port} vhost={VHost}",
            _opt.Host, _opt.Port, _opt.VHost);

        var factory = new ConnectionFactory
        {
            HostName = _opt.Host,
            Port = _opt.Port,
            UserName = _opt.User,
            Password = _opt.Pass,
            VirtualHost = _opt.VHost
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken: stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // EXCHANGE
        await _channel.ExchangeDeclareAsync(
            exchange: _opt.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        // JOBS QUEUE
        await _channel.QueueDeclareAsync(
            queue: JobsQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: JobsQueueName,
            exchange: _opt.Exchange,
            routingKey: JobsRoutingKey,
            arguments: null,
            cancellationToken: stoppingToken);

        // RESULTS QUEUE
        await _channel.QueueDeclareAsync(
            queue: _opt.ResultsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: _opt.ResultsQueue,
            exchange: _opt.Exchange,
            routingKey: _opt.ResultsRoutingKey,
            arguments: null,
            cancellationToken: stoppingToken);

        // DLQ
        await _channel.QueueDeclareAsync(
            queue: DlqQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: DlqQueueName,
            exchange: _opt.Exchange,
            routingKey: DlqRoutingKey,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, PrefetchCount, false, cancellationToken: stoppingToken);

        _logger.LogInformation(
            "VerifyReceiptWorker started. Listening queue={Queue}",
            JobsQueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            _logger.LogCritical("📥 VERIFY JOB MESSAGE RECEIVED");

            try
            {
                await HandleJobAsync(ea, stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifyReceiptWorker message handling error");
                try
                {
                    await HandleErrorAsync(ea, ex, stoppingToken);
                }
                catch { }
            }
        };

        await _channel.BasicConsumeAsync(
            queue: JobsQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    // ---- JOB İŞLEME ----
    private async Task HandleJobAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        _logger.LogInformation("VerifyReceiptWorker job json: {Json}", json);

        var attempt = GetAttempt(ea.BasicProperties);

        var msg = JsonSerializer.Deserialize<OcrJobMessage>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (msg is null || msg.ReceiptId <= 0 ||
            string.IsNullOrWhiteSpace(msg.Bucket) ||
            string.IsNullOrWhiteSpace(msg.ObjectKey))
        {
            _logger.LogCritical("❌ INVALID JOB MESSAGE");
            return;
        }

        if (string.IsNullOrWhiteSpace(msg.JobId))
            msg.JobId = Guid.NewGuid().ToString("N");

        _logger.LogCritical(
            "🚀 START JOB jobId={JobId} receiptId={ReceiptId} bucket={Bucket} object={ObjectKey} attempt={Attempt}",
            msg.JobId, msg.ReceiptId, msg.Bucket, msg.ObjectKey, attempt);

        var started = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
// 1) MinIO'dan dosyayı indir
var fileBytes = await DownloadFromMinioAsync(msg.Bucket, msg.ObjectKey, ct);

// 1) MinIO'dan dosya indirildi: fileBytes

// 2) Base64'e çevir
var fileDataBase64 = Convert.ToBase64String(fileBytes);

// 3) Veryfi payload’u
var payload = new
{
    file_data = fileDataBase64,
    file_name = Path.GetFileName(msg.ObjectKey),
    categories = Array.Empty<string>(),
    tags = Array.Empty<string>(),
    compute = true,
    country = "TR",
    document_type = "receipt"
};

var payloadJson = JsonSerializer.Serialize(payload);

// 4) Dokümandaki C# örneğinin birebir HttpClient versiyonu
using var client = new HttpClient();

// URL: appsettings’ten geliyor
var request = new HttpRequestMessage(
    HttpMethod.Post,
    $"{_veryfi.BaseUrl.TrimEnd('/')}/documents");

// Accept: application/json
request.Headers.Accept.Clear();
request.Headers.Accept.Add(
    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

// CLIENT-ID: appsettings
request.Headers.Add("CLIENT-ID", _veryfi.ClientId);

// Authorization: apikey username:apikey
// Burada STRONGLY-TYPED Authorization header kullanıyoruz
request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
    "apikey",
    $"{_veryfi.Username}:{_veryfi.ApiKey}"
);

// Body: application/json
request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
// İstersen charset'i de dokümandaki gibi kapatabilirsin:
// request.Content.Headers.ContentType!.CharSet = "";

_logger.LogInformation("🌐 Calling Veryfi for receiptId={ReceiptId}", msg.ReceiptId);

// Request gönder
var response = await client.SendAsync(request, ct);
var respBody = await response.Content.ReadAsStringAsync(ct);

_logger.LogCritical("Veryfi response status={StatusCode}", (int)response.StatusCode);
_logger.LogCritical("Veryfi response body={Body}", respBody);

// 2xx değilse fırlat, retry mekanizması çalışsın
response.EnsureSuccessStatusCode();

// JSON'u sakla
var root = JsonSerializer.Deserialize<JsonElement>(respBody);

var finished = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

var result = new OcrResultMessage
{
    JobId = msg.JobId,
    ReceiptId = msg.ReceiptId,
    ChannelKey = msg.ChannelKey,
    Status = "DONE",
    Attempt = attempt,
    Started = started,
    Finished = finished,
    Bucket = msg.Bucket,
    ObjectKey = msg.ObjectKey,
    Payload = root
};

await PublishResultAsync(result, ct);

_logger.LogCritical(
    "✅ JOB DONE jobId={JobId} receiptId={ReceiptId} elapsed={Elapsed}ms",
    result.JobId, result.ReceiptId, result.Elapsed);


    }

    // ---- ATTEMPT OKUMA ----
    private int GetAttempt(IReadOnlyBasicProperties? props)
    {
        try
        {
            if (props?.Headers != null &&
                props.Headers.TryGetValue(AttemptHeaderKey, out var val) &&
                val is byte[] raw)
            {
                var s = Encoding.UTF8.GetString(raw);
                if (int.TryParse(s, out var attempt))
                    return attempt;
            }
        }
        catch { }

        return 1;
    }

    // ---- HATA / RETRY / DLQ ----
    private async Task HandleErrorAsync(BasicDeliverEventArgs ea, Exception ex, CancellationToken ct)
    {
        if (_channel is null) return;

        var attempt = GetAttempt(ea.BasicProperties);
        var corrId = ea.BasicProperties?.CorrelationId ?? string.Empty;

        _logger.LogError(ex,
            "Verify job failed attempt={Attempt} corrId={CorrelationId}",
            attempt, corrId);

        if (attempt >= MaxAttempts)
        {
            var dlqPayload = new
            {
                status = "FAILED",
                error = ex.Message,
                attempt,
                body = Encoding.UTF8.GetString(ea.Body.ToArray()),
                failedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var dlqJson = JsonSerializer.Serialize(dlqPayload);
            var dlqBody = Encoding.UTF8.GetBytes(dlqJson);

            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                CorrelationId = corrId
            };

            await _channel.BasicPublishAsync(
                exchange: _opt.Exchange,
                routingKey: DlqRoutingKey,
                mandatory: false,
                basicProperties: props,
                body: dlqBody,
                cancellationToken: ct);

            await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: ct);

            _logger.LogCritical("❌ Sent job to DLQ after {Attempt} attempts corrId={CorrelationId}",
                attempt, corrId);

            return;
        }

        var headers = ea.BasicProperties?.Headers != null
            ? new Dictionary<string, object>(ea.BasicProperties.Headers)
            : new Dictionary<string, object>();

        headers[AttemptHeaderKey] = Encoding.UTF8.GetBytes((attempt + 1).ToString());

        var retryProps = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            CorrelationId = corrId,
            Headers = headers
        };

        await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: ct);

        await _channel.BasicPublishAsync(
            exchange: _opt.Exchange,
            routingKey: JobsRoutingKey,
            mandatory: false,
            basicProperties: retryProps,
            body: ea.Body,
            cancellationToken: ct);

        _logger.LogWarning(
            "🔁 Republished job for retry nextAttempt={Attempt} corrId={CorrelationId}",
            attempt + 1, corrId);
    }

    // ---- RESULT PUBLISH ----
    private async Task PublishResultAsync(Application.Dtos.OcrResultMessage result, CancellationToken ct)
    {
        if (_channel is null) return;

        var json = JsonSerializer.Serialize(result);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            CorrelationId = result.JobId
        };

        await _channel.BasicPublishAsync(
            exchange: _opt.Exchange,
            routingKey: _opt.ResultsRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }

    // ---- MinIO ----
    private async Task<byte[]> DownloadFromMinioAsync(string bucket, string objectKey, CancellationToken ct)
    {
        var minioBuilder = new MinioClient()
            .WithEndpoint(_minio.Endpoint)
            .WithCredentials(_minio.AccessKey, _minio.SecretKey);

        if (_minio.Secure)
        {
            minioBuilder = minioBuilder.WithSSL();
        }

        var client = minioBuilder.Build();

        await using var ms = new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(ms));

        try
        {
            _logger.LogInformation("📥 MinIO'dan dosya indiriliyor bucket={Bucket}, object={Object}",
                bucket, objectKey);

            await client.GetObjectAsync(args, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "MinIO'dan dosya indirilirken hata oluştu. bucket={Bucket}, object={Object}",
                bucket, objectKey);
            throw;
        }

        return ms.ToArray();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try { if (_channel is not null) await _channel.CloseAsync(cancellationToken: cancellationToken); } catch { }
        try { if (_connection is not null) await _connection.CloseAsync(cancellationToken: cancellationToken); } catch { }

        await base.StopAsync(cancellationToken);
    }
}
