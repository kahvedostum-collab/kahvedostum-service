using System.Text.Json;
using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Helpers;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Application.Interfaces;
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
    public async Task<ReceiptInitResponseDto> InitAsync(int userId, ReceiptInitRequestDto dto)
    {
        if (dto.CafeId.HasValue)
        {
            var cafe = await _uow.Cafes.GetByIdAsync(dto.CafeId.Value);
            if (cafe == null)
                throw new ArgumentException("Cafe bulunamadı.");
        }

        // 🔴 Her fiş için uniq channel
        var channelKey = Guid.NewGuid().ToString("N");

        var receipt = new Receipt
        {
            UserId = userId,
            CafeId = dto.CafeId,
            ClientLat = dto.Lat,
            ClientLng = dto.Lng,
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
        receipt.UploadedAt = DateTime.UtcNow;

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



    // 2) COMPLETE: upload bitti -> job bas + status PROCESSING
    public async Task<ReceiptCompleteResponseDto> CompleteAsync(int userId, int receiptId, ReceiptCompleteRequestDto dto)
    {
        var receipt = await _uow.Receipts.GetByIdAsync(receiptId);
        if (receipt == null)
            throw new ArgumentException("Fiş bulunamadı.");

        if (receipt.UserId != userId)
            throw new ArgumentException("Bu fişe erişim yok.");

        if (receipt.Status is ReceiptStatus.PROCESSING or ReceiptStatus.DONE)
        {
            return new ReceiptCompleteResponseDto
            {
                ReceiptId = receipt.Id,
                Status = receipt.Status.ToString()
            };
        }

        receipt.Bucket = dto.Bucket ?? receipt.Bucket ?? DefaultBucket;
        receipt.ObjectKey = dto.ObjectKey ?? receipt.ObjectKey;

        if (string.IsNullOrWhiteSpace(receipt.ObjectKey))
            throw new ArgumentException("ObjectKey boş. Önce init + upload yapmalısın.");

        receipt.Status = ReceiptStatus.PROCESSING;
        receipt.UploadedAt ??= DateTime.UtcNow;

        var jobId = Guid.NewGuid().ToString("N");
        receipt.OcrJobId = jobId;

        await _uow.SaveChangesAsync();

        var job = new OcrJobMessage
        {
            JobId = jobId,
            ReceiptId = receipt.Id,
            UserId = receipt.UserId,
            CafeId = receipt.CafeId,
            Bucket = receipt.Bucket!,
            ObjectKey = receipt.ObjectKey!,
            ClientLat = receipt.ClientLat,
            ClientLng = receipt.ClientLng,
            ChannelKey = receipt.ChannelKey    // 🔴 buradan worker’a gidecek
        };

        await _publisher.PublishAsync(job);

        return new ReceiptCompleteResponseDto
        {
            ReceiptId = receipt.Id,
            Status = "PROCESSING"
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
