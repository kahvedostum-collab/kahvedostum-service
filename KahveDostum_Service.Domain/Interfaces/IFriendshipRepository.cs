using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface IFriendshipRepository : IRepository<Friendship>
{
    Task<bool> AreFriendsAsync(int userId, int otherUserId);
    Task<List<Friendship>> GetFriendshipsForUserAsync(int userId);
    Task<Friendship?> GetFriendshipAsync(int userId, int friendUserId);
}