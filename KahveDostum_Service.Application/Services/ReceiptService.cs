using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Helpers;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;

namespace KahveDostum_Service.Application.Services;

public class ReceiptService : IReceiptService
{
    private readonly IUnitOfWork _uow;
    private readonly ICafeTokenService _tokenService;

    public ReceiptService(IUnitOfWork uow, ICafeTokenService tokenService)
    {
        _uow = uow;
        _tokenService = tokenService;
    }

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

        // KAFE BUL
        Cafe? cafe = await _uow.Cafes.GetByTaxNumberAsync(dto.TaxNumber)
                     ?? await _uow.Cafes.GetByNormalizedAddressAsync(normalizedAddress);

        if (cafe == null)
            throw new ArgumentException(
                "Bu fiş tanımlı ve aktif bir kafeye ait değil.");

        // HASH OLUŞTUR
        var receiptHash = ReceiptHashHelper.Generate(
            dto.TaxNumber,
            dto.Total,
            dto.ReceiptDate,
            dto.ReceiptNo,
            dto.RawText);


        // AYNI FİŞ KONTROLÜ (ASLA GEÇEMEZ)
        var exists = await _uow.Receipts.ExistsByHashAsync(receiptHash);
        if (exists)
            throw new ArgumentException(
                "Bu fiş daha önce kullanılmış.");

        // RECEIPT KAYDI
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

        // TOKEN
        return await _tokenService.GenerateTokenAsync(
            userId,
            cafe.Id,
            receipt.Id);
    }

    
    

}
