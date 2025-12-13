using KahveDostum_Service.Application.Dtos;

namespace KahveDostum_Service.Application.Interfaces;

public interface IReceiptService
{
    Task<CafeTokenDto> ScanReceiptAsync(int userId, CreateReceiptDto dto);
}