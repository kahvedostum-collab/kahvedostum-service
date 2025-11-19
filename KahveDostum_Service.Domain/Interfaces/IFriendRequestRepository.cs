using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface IFriendRequestRepository : IRepository<FriendRequest>
{
    Task<bool> HasPendingRequestBetweenAsync(int userId1, int userId2);
    Task<List<FriendRequest>> GetIncomingPendingRequestsAsync(int userId);
    Task<List<FriendRequest>> GetOutgoingPendingRequestsAsync(int userId);
    Task<FriendRequest?> GetPendingRequestForReceiverAsync(int requestId, int receiverUserId);
}