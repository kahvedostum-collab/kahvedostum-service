namespace KahveDostum_Service.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    IFriendRequestRepository FriendRequests { get; }  
    IFriendshipRepository Friendships { get; }       
    IConversationRepository Conversations { get; }      
    IMessageRepository Messages { get; }                
    IMessageReceiptRepository MessageReceipts { get; }  
    Task<int> SaveChangesAsync();
}