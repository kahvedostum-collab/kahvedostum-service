using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Infrastructure.Data;
using KahveDostum_Service.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Http;
using System.Net.Http.Json;

namespace KahveDostum_Service.Infrastructure.Background;

public sealed class ResultsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitOptions _opt;
    private readonly RealtimeOptions _rtOpt;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ResultsConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public ResultsConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitOptions> opt,
        IOptions<RealtimeOptions> rtOpt,
        IHttpClientFactory httpClientFactory,
        ILogger<ResultsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _opt = opt.Value;
        _rtOpt = rtOpt.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private sealed class OcrResultMessage
    {
        public string? JobId { get; set; }
        public int ReceiptId { get; set; }
        public string? ChannelKey { get; set; }  

        public string? Status { get; set; }       // DONE / FAILED
        public JsonElement? Payload { get; set; }
        public string? RawText { get; set; }
        public string? Error { get; set; }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ResultsConsumer connecting to RabbitMQ {Host}:{Port} vhost={VHost}",
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

        await _channel.ExchangeDeclareAsync(
            exchange: _opt.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

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

        await _channel.BasicQosAsync(0, 5, false, cancellationToken: stoppingToken);

        _logger.LogInformation(
            "ResultsConsumer started. Listening queue={Queue}",
            _opt.ResultsQueue);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("ResultsConsumer message: {Json}", json);

                var msg = JsonSerializer.Deserialize<OcrResultMessage>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (msg is null || msg.ReceiptId <= 0)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var receipt = await db.Receipts
                    .Include(r => r.Lines)
                    .FirstOrDefaultAsync(r => r.Id == msg.ReceiptId, stoppingToken);

                if (receipt == null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
                    return;
                }

                // Ham raw text
                if (!string.IsNullOrWhiteSpace(msg.RawText))
                    receipt.RawText = msg.RawText;

                // Payload içinden brand / total / tarih vs çek (varsa)
                string? brand = null;
                string? total = null;
                string? receiptNo = null;
                string? address = null;
                DateTime? receiptDate = null;

                if (msg.Payload.HasValue && msg.Payload.Value.ValueKind == JsonValueKind.Object)
                {
                    var root = msg.Payload.Value;

                    if (root.TryGetProperty("brand", out var brandProp))
                        brand = brandProp.GetString();

                    if (root.TryGetProperty("total", out var totalProp))
                        total = totalProp.GetString();

                    if (root.TryGetProperty("receiptNo", out var noProp))
                        receiptNo = noProp.GetString();

                    if (root.TryGetProperty("address", out var addrProp))
                        address = addrProp.GetString();

                    if (root.TryGetProperty("date", out var dateProp))
                    {
                        var dateStr = dateProp.GetString();
                        if (!string.IsNullOrWhiteSpace(dateStr) &&
                            DateTime.TryParse(dateStr, out var dt))
                        {
                            receiptDate = dt;
                        }
                    }

                    // Tekil hash üretmek istersen burada hesaplayabilirsin (brand+total+date vs)
                }

                if (string.Equals(msg.Status, "DONE", StringComparison.OrdinalIgnoreCase))
                {
                    receipt.Status = ReceiptStatus.DONE;
                    receipt.ProcessedAt = DateTime.UtcNow;

                    if (!string.IsNullOrWhiteSpace(brand))
                        receipt.Brand = brand;
                    if (!string.IsNullOrWhiteSpace(total))
                        receipt.Total = total;
                    if (!string.IsNullOrWhiteSpace(receiptNo))
                        receipt.ReceiptNo = receiptNo;
                    if (!string.IsNullOrWhiteSpace(address))
                        receipt.Address = address;
                    if (receiptDate.HasValue)
                        receipt.ReceiptDate = receiptDate;
                }
                else
                {
                    receipt.Status = ReceiptStatus.FAILED;
                    receipt.ProcessedAt = DateTime.UtcNow;
                    receipt.RejectReason = msg.Error ?? "OCR FAILED";
                }

                // ReceiptOcrResult kaydı
                var ocrResult = await db.ReceiptOcrResults
                    .FirstOrDefaultAsync(x => x.ReceiptId == receipt.Id, cancellationToken: stoppingToken);

                if (ocrResult == null)
                {
                    ocrResult = new ReceiptOcrResult
                    {
                        ReceiptId = receipt.Id,
                        JobId = msg.JobId ?? receipt.OcrJobId ?? string.Empty,
                        Status = msg.Status ?? "UNKNOWN",
                        RawText = msg.RawText,
                        PayloadJson = msg.Payload?.GetRawText(),
                        Error = msg.Error
                    };
                    await db.ReceiptOcrResults.AddAsync(ocrResult, stoppingToken);
                }
                else
                {
                    ocrResult.Status = msg.Status ?? ocrResult.Status;
                    ocrResult.RawText = msg.RawText ?? ocrResult.RawText;
                    ocrResult.PayloadJson = msg.Payload?.GetRawText() ?? ocrResult.PayloadJson;
                    ocrResult.Error = msg.Error ?? ocrResult.Error;
                }

                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Receipt {Id} updated. Status={Status}", receipt.Id, receipt.Status);

                // 🔴 Realtime'a haber ver (SignalR)
                try
                {
                    await NotifyRealtimeAsync(msg, receipt, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Realtime notify failed for receipt {Id}",
                        receipt.Id);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResultsConsumer error");

                try
                {
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken: stoppingToken);
                }
                catch { }
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _opt.ResultsQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task NotifyRealtimeAsync(OcrResultMessage msg, Receipt receipt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_rtOpt.BaseUrl))
        {
            _logger.LogWarning("RealtimeOptions.BaseUrl boş, realtime notify atlanıyor.");
            return;
        }

        // channelKey yoksa DB'den al (msg.ChannelKey boş gelebilir)
        var channelKey = msg.ChannelKey;
        if (string.IsNullOrWhiteSpace(channelKey))
            channelKey = receipt.ChannelKey;

        if (string.IsNullOrWhiteSpace(channelKey))
        {
            _logger.LogWarning("Receipt {Id} için ChannelKey yok, realtime notify atlanıyor.", receipt.Id);
            return;
        }

        var client = _httpClientFactory.CreateClient("realtime");
        client.BaseAddress = new Uri(_rtOpt.BaseUrl.TrimEnd('/'));

        if (!string.IsNullOrWhiteSpace(_rtOpt.InternalApiKey))
        {
            client.DefaultRequestHeaders.Add("X-Internal-ApiKey", _rtOpt.InternalApiKey);
        }

        var dto = new
        {
            receiptId = receipt.Id,
            channelKey = channelKey,
            status = receipt.Status.ToString(),       // DONE / FAILED
            rejectReason = receipt.RejectReason,
            total = receipt.Total,
            brand = receipt.Brand
        };

        var response = await client.PostAsJsonAsync("/internal/receipts/status-changed", dto, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Realtime notified for receipt {Id}", receipt.Id);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try { if (_channel is not null) await _channel.CloseAsync(cancellationToken: cancellationToken); } catch { }
        try { if (_connection is not null) await _connection.CloseAsync(cancellationToken: cancellationToken); } catch { }

        await base.StopAsync(cancellationToken);
    }
}
