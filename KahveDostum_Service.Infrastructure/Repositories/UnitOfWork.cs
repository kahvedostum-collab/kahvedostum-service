using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class UnitOfWork(
    AppDbContext context,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IFriendRequestRepository friendRequestRepository,
    IFriendshipRepository friendshipRepository
) : IUnitOfWork
{
    private readonly AppDbContext _context = context;

    public IUserRepository Users { get; } = userRepository;
    public IRefreshTokenRepository RefreshTokens { get; } = refreshTokenRepository;

    public IFriendRequestRepository FriendRequests { get; } = friendRequestRepository;
    public IFriendshipRepository Friendships { get; } = friendshipRepository;

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}