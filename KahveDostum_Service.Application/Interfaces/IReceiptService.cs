using KahveDostum_Service.Application.Dtos;

namespace KahveDostum_Service.Application.Interfaces;

public interface IReceiptService
{
    Task<ReceiptInitResponseDto> InitAsync(int userId, ReceiptInitRequestDto dto);
    Task<ReceiptCompleteResponseDto> CompleteAsync(int userId, int receiptId, ReceiptCompleteRequestDto dto);
    Task<ReceiptStatusResponseDto> GetStatusAsync(int userId, int receiptId);

    Task<CafeTokenDto> ScanReceiptAsync(int userId, CreateReceiptDto dto);
}