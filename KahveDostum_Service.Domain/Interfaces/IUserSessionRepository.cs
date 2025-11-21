using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface IUserSessionRepository
{
    Task<List<UserSession>> GetActiveSessionsByUserIdAsync(int userId);
    Task<List<UserSession>> GetActiveSessionsByCafeIdAsync(int cafeId);
    Task AddAsync(UserSession session);
}