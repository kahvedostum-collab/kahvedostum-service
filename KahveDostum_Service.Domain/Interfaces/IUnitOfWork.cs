namespace KahveDostum_Service.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    IFriendRequestRepository FriendRequests { get; }  
    IFriendshipRepository Friendships { get; }       

    Task<int> SaveChangesAsync();
}