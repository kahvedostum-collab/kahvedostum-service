using KahveDostum_Service.Application.Dtos;

namespace KahveDostum_Service.Application.Interfaces;

public interface ICafeService
{
    Task<CafeDto> CreateCafeAsync(CafeDto dto);
    Task<List<CafeDto>> GetAllAsync();
    Task<List<ActiveUserDto>> GetActiveUsersAsync(int cafeId);
}