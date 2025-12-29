using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Infrastructure.Data;
using KahveDostum_Service.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly MinioOptions _minio;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VerifyReceiptWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;  
    private IConnection? _connection;
    private IChannel? _channel;

    private const string JobsQueueName = "ocr.jobs";
    private const string JobsRoutingKey = "jobs";
    private const string DlqQueueName = "ocr.jobs.dlq";
    private const string DlqRoutingKey = "jobs.dlq";

    private const ushort PrefetchCount = 50;
    private const int MaxAttempts = 3;
    private const string AttemptHeaderKey = "attempt";

    // Concurrency: aynı anda en fazla 50 job
    private const int MaxConcurrentJobs = 50;
    private readonly SemaphoreSlim _concurrencySemaphore = new(MaxConcurrentJobs, MaxConcurrentJobs);

    // RabbitMQ channel için tek şerit (ACK + Publish)
    private readonly SemaphoreSlim _channelSemaphore = new(1, 1);

   public VerifyReceiptWorker(
       IOptions<RabbitOptions> opt,
       IOptions<MinioOptions> minio,
       IHttpClientFactory httpClientFactory,
       IServiceScopeFactory scopeFactory,
       ILogger<VerifyReceiptWorker> logger)
   {
       _opt = opt.Value;
       _minio = minio.Value;
       _httpClientFactory = httpClientFactory;
       _scopeFactory = scopeFactory;
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

        // Prefetch
        await _channel.BasicQosAsync(0, PrefetchCount, false, cancellationToken: stoppingToken);

        _logger.LogInformation(
            "VerifyReceiptWorker started. Listening queue={Queue}",
            JobsQueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            _logger.LogCritical("📥 VERIFY JOB MESSAGE RECEIVED");

            // MaxConcurrentJobs kadar job aynı anda
            await _concurrencySemaphore.WaitAsync(stoppingToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    try
                    {
                        await HandleJobAsync(ea, stoppingToken);

                        // İş başarıyla bittiyse ACK
                        await SafeAckAsync(ea.DeliveryTag, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "VerifyReceiptWorker message handling error");

                        try
                        {
                            await HandleErrorAsync(ea, ex, stoppingToken);
                        }
                        catch (Exception inner)
                        {
                            _logger.LogError(inner, "HandleErrorAsync failed");
                        }
                    }
                }
                finally
                {
                    _concurrencySemaphore.Release();
                }
            }, stoppingToken);
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

        // 4) DB'den uygun Veryfi hesabını çek
        VeryfiAccount account;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            account = await db.VeryfiAccounts
                .Where(a => a.IsActive && a.UsedCount < a.UsageLimit)
                .OrderBy(a => a.UsedCount)
                .ThenBy(a => a.Id)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Kullanılabilir Veryfi hesabı bulunamadı.");

            // Http çağrısından SONRA sayaç arttırmak için account referansını dışarı taşıyoruz
        }

        // 5) HttpClient
        var client = _httpClientFactory.CreateClient("veryfi");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{account.BaseUrl.TrimEnd('/')}/documents");

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        request.Headers.Add("CLIENT-ID", account.ClientId);

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "apikey",
            $"{account.Username}:{account.ApiKey}");

        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        _logger.LogInformation("🌐 Calling Veryfi for receiptId={ReceiptId} using VeryfiAccountId={AccountId}", msg.ReceiptId, account.Id);

        var response = await client.SendAsync(request, ct);
        var respBody = await response.Content.ReadAsStringAsync(ct);

        _logger.LogCritical("Veryfi response status={StatusCode}", (int)response.StatusCode);
        //_logger.LogCritical("Veryfi response body={Body}", respBody);

        response.EnsureSuccessStatusCode(); // ✅ 200, 201 vs aldığımızdan emin olduk

        // 6) Başarılı response geldiyse sayaç arttır
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var accToUpdate = await db.VeryfiAccounts
                .FirstOrDefaultAsync(a => a.Id == account.Id, ct);

            if (accToUpdate != null)
            {
                accToUpdate.UsedCount += 1;
                accToUpdate.LastUsedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "VeryfiAccountId={AccountId} usage incremented. UsedCount={UsedCount}/{Limit}",
                    accToUpdate.Id, accToUpdate.UsedCount, accToUpdate.UsageLimit);
            }
        }

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
        catch
        {
            // ignore
        }

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

        // Max deneme sayısını geçtiyse DLQ'ya at
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

            // Önce DLQ'ya publish, sonra ACK (duplicate > loss)
            await SafePublishAsync(DlqRoutingKey, dlqBody, props, ct);
            await SafeAckAsync(ea.DeliveryTag, ct);

            _logger.LogCritical("❌ Sent job to DLQ after {Attempt} attempts corrId={CorrelationId}",
                attempt, corrId);

            return;
        }

        // Retry
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

        // Önce tekrar kuyruğa publish, sonra ACK
        var retryBody = ea.Body.ToArray();
        await SafePublishAsync(JobsRoutingKey, retryBody, retryProps, ct);
        await SafeAckAsync(ea.DeliveryTag, ct);

        _logger.LogWarning(
            "🔁 Republished job for retry nextAttempt={Attempt} corrId={CorrelationId}",
            attempt + 1, corrId);
    }

    // ---- RESULT PUBLISH ----
    private async Task PublishResultAsync(OcrResultMessage result, CancellationToken ct)
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

        await SafePublishAsync(_opt.ResultsRoutingKey, body, props, ct);
    }

    // ---- SAFE ACK ----
    private async Task SafeAckAsync(ulong deliveryTag, CancellationToken ct)
    {
        if (_channel is null) return;

        await _channelSemaphore.WaitAsync(ct);
        try
        {
            await _channel.BasicAckAsync(
                deliveryTag: deliveryTag,
                multiple: false,
                cancellationToken: ct);
        }
        finally
        {
            _channelSemaphore.Release();
        }
    }

    // ---- SAFE PUBLISH ----
    private async Task SafePublishAsync(string routingKey, byte[] body, BasicProperties props, CancellationToken ct)
    {
        if (_channel is null) return;

        await _channelSemaphore.WaitAsync(ct);
        try
        {
            await _channel.BasicPublishAsync(
                exchange: _opt.Exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);
        }
        finally
        {
            _channelSemaphore.Release();
        }
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
    
    private async Task<VeryfiAccount> GetAvailableVeryfiAccountAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // En az kullanılan, limiti dolmamış ve aktif olan hesabı seç
        var account = await db.VeryfiAccounts
            .Where(a => a.IsActive && a.UsedCount < a.UsageLimit)
            .OrderBy(a => a.UsedCount)
            .ThenBy(a => a.Id)
            .FirstOrDefaultAsync(ct);

        if (account == null)
        {
            throw new InvalidOperationException("Kullanılabilir Veryfi hesabı bulunamadı (UsageLimit dolu).");
        }

        return account;
    }

    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null)
                await _channel.CloseAsync(cancellationToken: cancellationToken);
        }
        catch { }

        try
        {
            if (_connection is not null)
                await _connection.CloseAsync(cancellationToken: cancellationToken);
        }
        catch { }

        await base.StopAsync(cancellationToken);
    }
}
