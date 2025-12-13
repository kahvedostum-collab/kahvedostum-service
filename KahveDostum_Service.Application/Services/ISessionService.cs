using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;

namespace KahveDostum_Service.Application.Services;

public class SessionService : ISessionService
{
    private readonly IUnitOfWork _uow;
    private readonly ICafeTokenService _tokenService;

    public SessionService(
        IUnitOfWork uow,
        ICafeTokenService tokenService)
    {
        _uow = uow;
        _tokenService = tokenService;
    }

    public async Task<SessionDto> StartSessionAsync(int userId, int cafeId)
    {
        var now = DateTime.UtcNow;

        // Kullanıcının eski aktif session'larını kapat
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

    public async Task<SessionDto> StartSessionByTokenAsync(int userId, string token)
    {
        // Token doğrulama burada
        var cafeId = await _tokenService.ValidateTokenAsync(token);

        // Aynı mantıkla session aç
        return await StartSessionAsync(userId, cafeId);
    }
}