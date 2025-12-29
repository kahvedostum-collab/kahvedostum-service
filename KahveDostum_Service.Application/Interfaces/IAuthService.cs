using KahveDostum_Service.Application.Dtos;

namespace KahveDostum_Service.Application.Interfaces;

public interface IAuthService
{
    Task<RegisterResultDto> RegisterAsync(RegisterRequestDto request);
    Task<LoginResultDto> LoginAsync(LoginRequestDto request);
    Task<LoginResultDto> RefreshAsync(RefreshRequestDto request);
    Task LogoutAsync(LogoutRequestDto request);
}