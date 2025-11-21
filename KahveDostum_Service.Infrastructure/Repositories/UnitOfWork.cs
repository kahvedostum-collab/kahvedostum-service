using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;

namespace KahveDostum_Service.Infrastructure.Repositories;


public class UnitOfWork(
    AppDbContext context,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IFriendRequestRepository friendRequestRepository,
    IFriendshipRepository friendshipRepository,
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IMessageReceiptRepository messageReceiptRepository,
    ICafeRepository cafeRepository,
    IUserSessionRepository sessionRepository
) : IUnitOfWork
{
    private readonly AppDbContext _context = context;

    public IUserRepository Users { get; } = userRepository;
    public IRefreshTokenRepository RefreshTokens { get; } = refreshTokenRepository;

    public IFriendRequestRepository FriendRequests { get; } = friendRequestRepository;
    public IFriendshipRepository Friendships { get; } = friendshipRepository;
    public IConversationRepository Conversations { get; } = conversationRepository;
    public IMessageRepository Messages { get; } = messageRepository;
    public IMessageReceiptRepository MessageReceipts { get; } = messageReceiptRepository;

    public ICafeRepository Cafes { get; } = cafeRepository;
    public IUserSessionRepository UserSessions { get; } = sessionRepository;

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}