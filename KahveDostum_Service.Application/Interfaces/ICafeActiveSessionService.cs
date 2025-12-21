using KahveDostum_Service.Application.Dtos;

namespace KahveDostum_Service.Application.Interfaces;

public interface ICafeActiveSessionService
{
    bool Grant(int cafeId, int userId, int receiptId, TimeSpan ttl);
    bool IsActive(int cafeId, int userId);
    List<CafeActiveUserDto> GetActiveUsers(int cafeId);
    void RevokeExpired();
}

