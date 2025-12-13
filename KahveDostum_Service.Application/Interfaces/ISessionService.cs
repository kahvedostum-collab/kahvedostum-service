using KahveDostum_Service.Application.Dtos;

namespace KahveDostum_Service.Application.Interfaces;

public interface ISessionService
{
    Task<SessionDto> StartSessionAsync(int userId, int cafeId);
    Task<SessionDto> StartSessionByTokenAsync(int userId, string token);

}