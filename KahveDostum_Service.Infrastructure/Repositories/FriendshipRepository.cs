using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class FriendshipRepository(AppDbContext context)
    : Repository<Friendship>(context), IFriendshipRepository
{
    private readonly AppDbContext _context = context;

    public async Task<bool> AreFriendsAsync(int userId, int otherUserId)
    {
        return await _context.Friendships.AnyAsync(f =>
            f.UserId == userId && f.FriendUserId == otherUserId);
    }

    public async Task<List<Friendship>> GetFriendshipsForUserAsync(int userId)
    {
        return await _context.Friendships
            .Include(f => f.FriendUser)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<Friendship?> GetFriendshipAsync(int userId, int friendUserId)
    {
        return await _context.Friendships
            .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendUserId == friendUserId);
    }
}