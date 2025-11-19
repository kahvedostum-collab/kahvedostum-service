using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class FriendRequestRepository(AppDbContext context)
    : Repository<FriendRequest>(context), IFriendRequestRepository
{
    private readonly AppDbContext _context = context;

    public async Task<bool> HasPendingRequestBetweenAsync(int userId1, int userId2)
    {
        return await _context.FriendRequests.AnyAsync(fr =>
            fr.Status == FriendRequestStatus.Pending &&
            (
                (fr.FromUserId == userId1 && fr.ToUserId == userId2) ||
                (fr.FromUserId == userId2 && fr.ToUserId == userId1)
            ));
    }

    public async Task<List<FriendRequest>> GetIncomingPendingRequestsAsync(int userId)
    {
        return await _context.FriendRequests
            .Include(fr => fr.FromUser)
            .Where(fr => fr.ToUserId == userId && fr.Status == FriendRequestStatus.Pending)
            .OrderByDescending(fr => fr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FriendRequest>> GetOutgoingPendingRequestsAsync(int userId)
    {
        return await _context.FriendRequests
            .Include(fr => fr.ToUser)
            .Where(fr => fr.FromUserId == userId && fr.Status == FriendRequestStatus.Pending)
            .OrderByDescending(fr => fr.CreatedAt)
            .ToListAsync();
    }

    public async Task<FriendRequest?> GetPendingRequestForReceiverAsync(int requestId, int receiverUserId)
    {
        return await _context.FriendRequests
            .FirstOrDefaultAsync(fr =>
                fr.Id == requestId &&
                fr.ToUserId == receiverUserId &&
                fr.Status == FriendRequestStatus.Pending);
    }
}