using KahveDostum_Service.Application.Dtos;

namespace KahveDostum_Service.Application.Interfaces;

public interface IFriendService
{
    Task SendFriendRequestAsync(int currentUserId, SendFriendRequestDto request);
    Task CancelFriendRequestAsync(int currentUserId, int requestId);
    Task RespondToFriendRequestAsync(int currentUserId, int requestId, bool accept);

    Task<List<FriendRequestDto>> GetIncomingRequestsAsync(int currentUserId);
    Task<List<FriendRequestDto>> GetOutgoingRequestsAsync(int currentUserId);
    Task<List<FriendDto>> GetFriendsAsync(int currentUserId);
    Task RemoveFriendAsync(int currentUserId, int friendUserId);
}