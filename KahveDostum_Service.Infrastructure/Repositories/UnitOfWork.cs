using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;

namespace KahveDostum_Service.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(
        AppDbContext context,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IFriendRequestRepository friendRequestRepository,
        IFriendshipRepository friendshipRepository,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IMessageReceiptRepository messageReceiptRepository,
        ICafeRepository cafeRepository,
        IUserSessionRepository sessionRepository,
        ICompanyRepository companyRepository,
        IReceiptRepository receiptRepository,
        IReceiptLineRepository receiptLineRepository,
        IReceiptOcrResultRepository receiptOcrResultRepository,
        ICafeActivationTokenRepository cafeActivationTokenRepository)
    {
        _context = context;

        Users = userRepository;
        RefreshTokens = refreshTokenRepository;

        FriendRequests = friendRequestRepository;
        Friendships = friendshipRepository;

        Conversations = conversationRepository;
        Messages = messageRepository;
        MessageReceipts = messageReceiptRepository;

        Companies = companyRepository;
        Cafes = cafeRepository;

        UserSessions = sessionRepository;

        Receipts = receiptRepository;
        ReceiptLines = receiptLineRepository;

        ReceiptOcrResults = receiptOcrResultRepository;

        CafeActivationTokens = cafeActivationTokenRepository;
    }

    public IUserRepository Users { get; }
    public IRefreshTokenRepository RefreshTokens { get; }

    public IFriendRequestRepository FriendRequests { get; }
    public IFriendshipRepository Friendships { get; }

    public IConversationRepository Conversations { get; }
    public IMessageRepository Messages { get; }
    public IMessageReceiptRepository MessageReceipts { get; }

    public ICompanyRepository Companies { get; }
    public ICafeRepository Cafes { get; }

    public IUserSessionRepository UserSessions { get; }

    public IReceiptRepository Receipts { get; }
    public IReceiptLineRepository ReceiptLines { get; }

    public IReceiptOcrResultRepository ReceiptOcrResults { get; }

    public ICafeActivationTokenRepository CafeActivationTokens { get; }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
