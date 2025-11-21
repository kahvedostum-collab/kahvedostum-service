using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;

namespace KahveDostum_Service.Application.Services;

public class SessionService : ISessionService
{
    private readonly IUnitOfWork _uow;

    public SessionService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<SessionDto> StartSessionAsync(int userId, int cafeId)
    {
        var now = DateTime.UtcNow;

        var oldSessions = await _uow.UserSessions.GetActiveSessionsByUserIdAsync(userId);
        foreach (var session in oldSessions)
            session.Status = SessionStatus.Expired;

        var newSession = new UserSession
        {
            UserId = userId,
            CafeId = cafeId,
            Status = SessionStatus.Active,
            StartedAt = now,
            ExpiresAt = now.AddHours(1)
        };

        await _uow.UserSessions.AddAsync(newSession);
        await _uow.SaveChangesAsync();

        return new SessionDto
        {
            SessionId = newSession.Id,
            CafeId = cafeId,
            UserId = userId,
            StartedAt = newSession.StartedAt,
            ExpiresAt = newSession.ExpiresAt
        };
    }
}