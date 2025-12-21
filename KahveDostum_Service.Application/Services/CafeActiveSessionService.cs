using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;

namespace KahveDostum_Service.Application.Services;

public class CafeActiveSessionService : ICafeActiveSessionService
{
    private readonly List<CafeActiveUserDto> _users = new();

    public bool Grant(int cafeId, int userId, int receiptId, TimeSpan ttl)
    {
        if (_users.Any(x => x.ReceiptId == receiptId))
            return false; // fiş tek kullanımlık

        _users.Add(new CafeActiveUserDto
        {
            CafeId = cafeId,
            UserId = userId,
            ReceiptId = receiptId,
            JoinedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        });

        return true;
    }

    public bool IsActive(int cafeId, int userId)
        => _users.Any(x =>
            x.CafeId == cafeId &&
            x.UserId == userId &&
            x.ExpiresAt > DateTime.UtcNow);

    public List<CafeActiveUserDto> GetActiveUsers(int cafeId)
        => _users
            .Where(x => x.CafeId == cafeId && x.ExpiresAt > DateTime.UtcNow)
            .ToList();

    public void RevokeExpired()
        => _users.RemoveAll(x => x.ExpiresAt <= DateTime.UtcNow);
}