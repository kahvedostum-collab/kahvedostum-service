namespace KahveDostum_Service.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    IFriendRequestRepository FriendRequests { get; }
    IFriendshipRepository Friendships { get; }

    ICompanyRepository Companies { get; }
    ICafeRepository Cafes { get; }

    IUserSessionRepository UserSessions { get; }

    IReceiptRepository Receipts { get; }
    IReceiptLineRepository ReceiptLines { get; }

    IReceiptOcrResultRepository ReceiptOcrResults { get; }   // ✅ EKLE

    ICafeActivationTokenRepository CafeActivationTokens { get; }

    IConversationRepository Conversations { get; }
    IMessageRepository Messages { get; }
    IMessageReceiptRepository MessageReceipts { get; }

    Task<int> SaveChangesAsync();
}