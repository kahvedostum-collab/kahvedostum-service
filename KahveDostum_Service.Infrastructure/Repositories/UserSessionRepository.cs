using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class UserSessionRepository : IUserSessionRepository
{
    private readonly AppDbContext _context;

    public UserSessionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<UserSession>> GetActiveSessionsByUserIdAsync(int userId)
        => _context.UserSessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .ToListAsync();

    public Task<List<UserSession>> GetActiveSessionsByCafeIdAsync(int cafeId)
        => _context.UserSessions
            .Include(s => s.User)
            .Where(s => s.CafeId == cafeId &&
                        s.Status == SessionStatus.Active &&
                        s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

    public async Task AddAsync(UserSession session)
        => await _context.UserSessions.AddAsync(session);
}