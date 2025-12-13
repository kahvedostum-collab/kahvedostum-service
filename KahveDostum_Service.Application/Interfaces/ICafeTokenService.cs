using KahveDostum_Service.Application.Dtos;

namespace KahveDostum_Service.Application.Interfaces;

public interface ICafeTokenService
{
    Task<CafeTokenDto> GenerateTokenAsync(int userId, int cafeId, int? receiptId = null);
    Task<int> ValidateTokenAsync(string token);
}