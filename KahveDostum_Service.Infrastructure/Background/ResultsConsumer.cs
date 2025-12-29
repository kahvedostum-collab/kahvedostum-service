using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Globalization;
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

    // VerifyReceiptWorker’dan gelen result mesajı DTO’su
    private sealed class OcrResultMessage
    {
        public string? JobId { get; set; }
        public int ReceiptId { get; set; }
        public string? ChannelKey { get; set; }

        public string? Status { get; set; }       // DONE / FAILED
        public JsonElement? Payload { get; set; } // Veryfi JSON burada
        public string? RawText { get; set; }
        public string? Error { get; set; }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("RESULTS CONSUMER EXECUTEASYNC ÇALIŞTI");
        _logger.LogCritical("RESULTS CONSUMER EXECUTEASYNC ÇALIŞTI");
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
            _logger.LogCritical("🔥🔥 OCR RESULT MESSAGE RECEIVED 🔥🔥");

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("ResultsConsumer message: {Json}", json);
                _logger.LogCritical("📦 RAW JSON = {Json}", json);

                var msg = JsonSerializer.Deserialize<OcrResultMessage>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogCritical(
                    "🧠 DESERIALIZED => ReceiptId={ReceiptId}, Status={Status}, ChannelKey={ChannelKey}",
                    msg?.ReceiptId,
                    msg?.Status,
                    msg?.ChannelKey
                );

                if (msg is null || msg.ReceiptId <= 0)
                {
                    _logger.LogCritical("❌ MESSAGE INVALID");
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
                    _logger.LogCritical("❌ RECEIPT DB'DE YOK receiptId={Id}", msg.ReceiptId);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
                    return;
                }

                _logger.LogCritical(
                    "✅ RECEIPT BULUNDU receiptId={Id} currentStatus={Status}",
                    receipt.Id,
                    receipt.Status
                );

                // -----------------------------------------------------------------
                // 1) Veryfi payload'ından alanları çek
                // -----------------------------------------------------------------
                string? brand = null;          // vendor.name
                string? totalStr = null;       // string olarak saklanacak
                decimal? totalAmount = null;   // numeric
                string? receiptNo = null;      // invoice_number
                string? address = null;        // vendor.address
                DateTime? receiptDate = null;  // date
                string? taxNumber = null;      // vendor.vat_number
                string? vendorCategory = null; // vendor.category
                string? ocrText = null;        // ocr_text

                if (msg.Payload.HasValue && msg.Payload.Value.ValueKind == JsonValueKind.Object)
                {
                    var root = msg.Payload.Value;

                    // ocr_text
                    if (root.TryGetProperty("ocr_text", out var ocrProp))
                        ocrText = ocrProp.GetString();

                    // total
                    if (root.TryGetProperty("total", out var totalProp))
                    {
                        if (totalProp.ValueKind == JsonValueKind.Number &&
                            totalProp.TryGetDecimal(out var dec))
                        {
                            totalAmount = dec;
                            totalStr = dec.ToString(CultureInfo.InvariantCulture);
                        }
                        else if (totalProp.ValueKind == JsonValueKind.String)
                        {
                            var s = totalProp.GetString();
                            totalStr = s;
                            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec2))
                            {
                                totalAmount = dec2;
                            }
                        }
                    }

                    // tarih
                    if (root.TryGetProperty("date", out var dateProp))
                    {
                        var dateStr = dateProp.GetString();
                        if (!string.IsNullOrWhiteSpace(dateStr) &&
                            DateTime.TryParse(dateStr, out var dt))
                        {
                            receiptDate = dt;
                        }
                    }

                    // invoice_number -> fiş no
                    if (root.TryGetProperty("invoice_number", out var invProp))
                        receiptNo = invProp.GetString();

                    // vendor objesi
                    if (root.TryGetProperty("vendor", out var vendorProp) &&
                        vendorProp.ValueKind == JsonValueKind.Object)
                    {
                        if (vendorProp.TryGetProperty("name", out var nameProp))
                            brand = nameProp.GetString();

                        if (vendorProp.TryGetProperty("address", out var addrProp))
                            address = addrProp.GetString();

                        if (vendorProp.TryGetProperty("vat_number", out var vatProp))
                            taxNumber = vatProp.GetString();

                        if (vendorProp.TryGetProperty("category", out var catProp))
                            vendorCategory = catProp.GetString();
                    }
                }

                // Raw text
                if (!string.IsNullOrWhiteSpace(ocrText))
                    receipt.RawText = ocrText;
                else if (!string.IsNullOrWhiteSpace(msg.RawText))
                    receipt.RawText = msg.RawText;

                // -----------------------------------------------------------------
                // 2) VALIDATION Kuralları
                // -----------------------------------------------------------------
                var rejectReasons = new List<string>();
                bool isValid = true;

                // coffee mi?
                if (string.IsNullOrWhiteSpace(vendorCategory) ||
                    !vendorCategory.Contains("coffee", StringComparison.OrdinalIgnoreCase))
                {
                    isValid = false;
                    rejectReasons.Add("Fiş bir kahve işletmesine ait görünmüyor.");
                }

                // total var mı?
                if (totalAmount is null || totalAmount <= 0)
                {
                    isValid = false;
                    rejectReasons.Add("Fişin toplam tutarı okunamadı.");
                }

                // adres
                if (string.IsNullOrWhiteSpace(address))
                {
                    isValid = false;
                    rejectReasons.Add("İşletme adresi bulunamadı.");
                }

                // vergi numarası
                if (string.IsNullOrWhiteSpace(taxNumber))
                {
                    isValid = false;
                    rejectReasons.Add("İşletmenin vergi numarası bulunamadı.");
                }

                // -----------------------------------------------------------------
                // 3) Cafe bul / yoksa oluştur
                // -----------------------------------------------------------------
                Cafe? cafe = null;

                if (!string.IsNullOrWhiteSpace(taxNumber))
                {
                    cafe = await db.Cafes
                        .FirstOrDefaultAsync(c => c.TaxNumber == taxNumber, stoppingToken);

                    if (cafe == null)
                    {
                        // Yeni cafe oluştur
                        cafe = new Cafe
                        {
                            Name = !string.IsNullOrWhiteSpace(brand) ? brand : "Bilinmeyen Cafe",
                            TaxNumber = taxNumber,
                            Address = address ?? string.Empty,
                            NormalizedAddress = NormalizeAddress(address),
                            Description = "Veryfi fişinden otomatik oluşturuldu.",
                            Latitude = null,     // İleride ClientLat/ClientLng ile doldurabilirsin
                            Longitude = null,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        await db.Cafes.AddAsync(cafe, stoppingToken);
                        await db.SaveChangesAsync(stoppingToken); // Id almak için

                        _logger.LogInformation(
                            "🆕 Yeni cafe oluşturuldu. CafeId={CafeId}, TaxNumber={TaxNumber}, Name={Name}",
                            cafe.Id, cafe.TaxNumber, cafe.Name);
                    }
                }

                // -----------------------------------------------------------------
                // 4) Receipt güncelle (DONE / FAILED)
                // -----------------------------------------------------------------
                if (string.Equals(msg.Status, "DONE", StringComparison.OrdinalIgnoreCase) && isValid)
                {
                    _logger.LogCritical("✅✅ DONE & VALID receiptId={Id}", receipt.Id);

                    receipt.Status = ReceiptStatus.DONE;
                    receipt.ProcessedAt = DateTime.UtcNow;

                    if (!string.IsNullOrWhiteSpace(brand))
                        receipt.Brand = brand;
                    if (!string.IsNullOrWhiteSpace(totalStr))
                        receipt.Total = totalStr;
                    if (!string.IsNullOrWhiteSpace(receiptNo))
                        receipt.ReceiptNo = receiptNo;
                    if (!string.IsNullOrWhiteSpace(address))
                        receipt.Address = address;
                    if (receiptDate.HasValue)
                        receipt.ReceiptDate = receiptDate;

                    // Cafe set et
                    if (cafe != null)
                        receipt.CafeId = cafe.Id;
                }
                else
                {
                    _logger.LogCritical(
                        "⚠️ FAILED OR INVALID receiptId={Id} status={Status}",
                        receipt.Id,
                        msg.Status
                    );

                    receipt.Status = ReceiptStatus.FAILED;
                    receipt.ProcessedAt = DateTime.UtcNow;

                    var reason = msg.Error;
                    if (rejectReasons.Count > 0)
                    {
                        var validationReason = string.Join(" | ", rejectReasons);
                        reason = string.IsNullOrWhiteSpace(reason)
                            ? validationReason
                            : $"{reason} | {validationReason}";
                    }

                    receipt.RejectReason = reason ?? "OCR / VALIDATION FAILED";
                }

                // -----------------------------------------------------------------
                // 5) ReceiptOcrResult kaydı
                // -----------------------------------------------------------------
                var ocrResult = await db.ReceiptOcrResults
                    .FirstOrDefaultAsync(x => x.ReceiptId == receipt.Id, cancellationToken: stoppingToken);

                var payloadJson = msg.Payload?.GetRawText();

                if (ocrResult == null)
                {
                    ocrResult = new ReceiptOcrResult
                    {
                        ReceiptId = receipt.Id,
                        JobId = msg.JobId ?? receipt.OcrJobId ?? string.Empty,
                        Status = msg.Status ?? "UNKNOWN",
                        RawText = receipt.RawText,
                        PayloadJson = payloadJson,
                        Error = receipt.RejectReason
                    };
                    await db.ReceiptOcrResults.AddAsync(ocrResult, stoppingToken);
                }
                else
                {
                    ocrResult.Status = msg.Status ?? ocrResult.Status;
                    ocrResult.RawText = receipt.RawText ?? ocrResult.RawText;
                    ocrResult.PayloadJson = payloadJson ?? ocrResult.PayloadJson;
                    ocrResult.Error = receipt.RejectReason ?? ocrResult.Error;
                }

                await db.SaveChangesAsync(stoppingToken);
                _logger.LogCritical(
                    "💾 DB SAVE OK receiptId={Id} newStatus={Status}",
                    receipt.Id,
                    receipt.Status
                );

                _logger.LogInformation("Receipt {Id} updated. Status={Status}", receipt.Id, receipt.Status);

                // 🔴 Cafe aktifliği ver (sadece DONE ise ve CafeId varsa)
                if (receipt.Status == ReceiptStatus.DONE && receipt.CafeId.HasValue)
                {
                    try
                    {
                        await GrantCafeActiveAsync(receipt, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Cafe active grant failed for receipt {ReceiptId}",
                            receipt.Id);
                    }
                }

                // 🔴 Realtime'a haber ver (SignalR)
                try
                {
                    _logger.LogCritical(
                        "📡 REALTIME NOTIFY ÇAĞRILIYOR receiptId={Id} status={Status}",
                        receipt.Id,
                        receipt.Status
                    );
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

    // Address normalize (basit)
    private static string NormalizeAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        return address
            .Trim()
            .ToLowerInvariant()
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    // Cafe aktiflik verme – senin eski kodun (hiç değiştirmedim)
    private async Task GrantCafeActiveAsync(Receipt receipt, CancellationToken ct)
    {
        if (!receipt.CafeId.HasValue)
            return;

        var client = _httpClientFactory.CreateClient("realtime");
        client.BaseAddress = new Uri(_rtOpt.BaseUrl.TrimEnd('/'));

        if (!string.IsNullOrWhiteSpace(_rtOpt.InternalApiKey))
        {
            client.DefaultRequestHeaders.Add(
                "X-Internal-ApiKey",
                _rtOpt.InternalApiKey
            );
        }

        var dto = new
        {
            CafeId = receipt.CafeId.Value,
            UserId = receipt.UserId,
            ReceiptId = receipt.Id
        };

        var response = await client.PostAsJsonAsync(
            "/internal/cafe/grant-active",
            dto,
            ct
        );

        response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Cafe active granted. Receipt={ReceiptId}, Cafe={CafeId}, User={UserId}",
            receipt.Id, receipt.CafeId.Value, receipt.UserId);
    }

    // Realtime notify – senin eski kodun (ufak oynamadan kopyaladım)
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

        var url = "/internal/receipts/status-changed";
        _logger.LogCritical("➡️ POST {Base}{Url}", client.BaseAddress, url);
        _logger.LogCritical("➡️ DTO: {Dto}", JsonSerializer.Serialize(dto));

        var response = await client.PostAsJsonAsync(url, dto, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogCritical("⬅️ Realtime RESP Status={StatusCode}", (int)response.StatusCode);
        _logger.LogCritical("⬅️ Realtime RESP Body={Body}", body);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Realtime notify failed. Status={(int)response.StatusCode}. Body={body}");
        }

        _logger.LogInformation("Realtime notified for receipt {Id}", receipt.Id);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try { if (_channel is not null) await _channel.CloseAsync(cancellationToken: cancellationToken); } catch { }
        try { if (_connection is not null) await _connection.CloseAsync(cancellationToken: cancellationToken); } catch { }

        await base.StopAsync(cancellationToken);
    }
}
