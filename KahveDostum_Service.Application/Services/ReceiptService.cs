using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Helpers;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;

namespace KahveDostum_Service.Application.Services;

public class ReceiptService : IReceiptService
{
    private const string DefaultBucket = "receipts";
    private const int PresignSeconds = 300;

    private readonly IUnitOfWork _uow;
    private readonly ICafeTokenService _tokenService;
    private readonly IObjectStorage _storage;
    private readonly IOcrJobPublisher _publisher;

    public ReceiptService(
        IUnitOfWork uow,
        ICafeTokenService tokenService,
        IObjectStorage storage,
        IOcrJobPublisher publisher)
    {
        _uow = uow;
        _tokenService = tokenService;
        _storage = storage;
        _publisher = publisher;
    }

    // 1) INIT: receipt oluştur + presigned url dön
    // Init: sadece upload oturumu açar. CafeId burada set edilmez.
    public async Task<ReceiptInitResponseDto> InitAsync(int userId)
    {
        // Her fiş için uniq channel
        var channelKey = Guid.NewGuid().ToString("N");

        var receipt = new Receipt
        {
            UserId = userId,
            // CafeId = null (OCR sonrası belirlenecek)
            Status = ReceiptStatus.INIT,
            CreatedAt = DateTime.UtcNow,
            ChannelKey = channelKey
        };

        await _uow.Receipts.AddAsync(receipt);
        await _uow.SaveChangesAsync();

        var bucket = DefaultBucket;
        var objectKey = $"uploads/{DateTime.UtcNow:yyyy.MM.dd}/{receipt.Id}.jpg";

        var uploadUrl = await _storage.PresignPutAsync(
            bucket: bucket,
            objectKey: objectKey,
            expirySeconds: PresignSeconds,
            ct: CancellationToken.None);

        receipt.Bucket = bucket;
        receipt.ObjectKey = objectKey;
        // UploadedAt burada set edilmez; upload bittikten sonra Complete'de set edilecek

        await _uow.SaveChangesAsync();

        return new ReceiptInitResponseDto
        {
            ReceiptId = receipt.Id,
            ChannelKey = channelKey,
            Bucket = bucket,
            ObjectKey = objectKey,
            UploadUrl = uploadUrl
        };
    }

    // 2) COMPLETE:
    //  - Upload bitti -> Status = UPLOADED
    //  - Job kuyruğa verildi -> Status = PROCESSING
    public async Task<ReceiptCompleteResponseDto> CompleteAsync(int userId, int receiptId, ReceiptCompleteRequestDto dto)
    {
        var receipt = await _uow.Receipts.GetByIdAsync(receiptId);
        if (receipt == null)
            throw new ArgumentException("Fiş bulunamadı.");

        if (receipt.UserId != userId)
            throw new ArgumentException("Bu fişe erişim yok.");

        // Zaten ilerlemiş durumdaysa tekrar başlatma
        if (receipt.Status is ReceiptStatus.PROCESSING or ReceiptStatus.DONE)
        {
            return new ReceiptCompleteResponseDto
            {
                ReceiptId = receipt.Id,
                Status = receipt.Status.ToString()
            };
        }

        // İstersen override imkanı
        receipt.Bucket = dto.Bucket ?? receipt.Bucket ?? DefaultBucket;
        receipt.ObjectKey = dto.ObjectKey ?? receipt.ObjectKey;

        if (string.IsNullOrWhiteSpace(receipt.ObjectKey))
            throw new ArgumentException("ObjectKey boş. Önce init + upload yapmalısın.");

        // 1️⃣ Upload tamamlandı → UPLOADED
        receipt.Status = ReceiptStatus.UPLOADED;
        receipt.UploadedAt ??= DateTime.UtcNow;

        await _uow.SaveChangesAsync(); // DB'de gerçekten UPLOADED olarak görünür

        // 2️⃣ Job hazırla
        var jobId = Guid.NewGuid().ToString("N");
        receipt.OcrJobId = jobId;

        var job = new OcrJobMessage
        {
            JobId = jobId,
            ReceiptId = receipt.Id,
            UserId = receipt.UserId,
            Bucket = receipt.Bucket!,
            ObjectKey = receipt.ObjectKey!,
            ChannelKey = receipt.ChannelKey
        };

        // 3️⃣ Job kuyruğa ver
        await _publisher.PublishAsync(job);

        // 4️⃣ Job kuyruğa başarıyla verildiyse → PROCESSING
        receipt.Status = ReceiptStatus.PROCESSING;
        await _uow.SaveChangesAsync();

        return new ReceiptCompleteResponseDto
        {
            ReceiptId = receipt.Id,
            Status = receipt.Status.ToString() // "PROCESSING"
        };
    }

    // 3) STATUS: receipt + varsa son ocr result
    public async Task<ReceiptStatusResponseDto> GetStatusAsync(int userId, int receiptId)
    {
        var receipt = await _uow.Receipts
            .GetByIdAsync(receiptId);

        if (receipt == null || receipt.UserId != userId)
            throw new ArgumentException("Fiş bulunamadı.");

        var ocrResult = await _uow.ReceiptOcrResults
            .GetByReceiptIdAsync(receiptId);

        return new ReceiptStatusResponseDto
        {
            ReceiptId = receipt.Id,
            Status = receipt.Status.ToString(),
            Result = ocrResult
        };
    }

    public async Task<List<ReceiptListItemDto>> GetMyReceiptsAsync(int userId)
    {
        var receipts = await _uow.Receipts.GetByUserIdAsync(userId);

        return receipts
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReceiptListItemDto
            {
                Id = r.Id,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                UploadedAt = r.UploadedAt,
                ReceiptDate = r.ReceiptDate,
                Total = r.Total,
                CafeId = r.CafeId
            })
            .ToList();
    }

    // Eski scan akışı (manual input) aynen kalsın
    public async Task<CafeTokenDto> ScanReceiptAsync(int userId, CreateReceiptDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TaxNumber))
            throw new ArgumentException("Vergi numarası zorunludur.");

        if (string.IsNullOrWhiteSpace(dto.Address))
            throw new ArgumentException("Fiş adresi zorunludur.");

        if (string.IsNullOrWhiteSpace(dto.Total))
            throw new ArgumentException("Fiş tutarı zorunludur.");

        if (dto.ReceiptDate == default)
            throw new ArgumentException("Fiş tarihi zorunludur.");

        var normalizedAddress = AddressNormalizer.Normalize(dto.Address);

        Cafe? cafe = await _uow.Cafes.GetByTaxNumberAsync(dto.TaxNumber)
                     ?? await _uow.Cafes.GetByNormalizedAddressAsync(normalizedAddress);

        if (cafe == null)
            throw new ArgumentException("Bu fiş tanımlı ve aktif bir kafeye ait değil.");

        var receiptHash = ReceiptHashHelper.Generate(
            dto.TaxNumber,
            dto.Total,
            dto.ReceiptDate,
            dto.ReceiptNo,
            dto.RawText);

        var exists = await _uow.Receipts.ExistsByHashAsync(receiptHash);
        if (exists)
            throw new ArgumentException("Bu fiş daha önce kullanılmış.");

        var receipt = new Receipt
        {
            UserId = userId,
            CafeId = cafe.Id,
            RawText = dto.RawText,
            ReceiptNo = dto.ReceiptNo,
            Total = dto.Total,
            ReceiptDate = dto.ReceiptDate,
            ReceiptHash = receiptHash,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Receipts.AddAsync(receipt);
        await _uow.SaveChangesAsync();

        return await _tokenService.GenerateTokenAsync(userId, cafe.Id, receipt.Id);
    }
}
