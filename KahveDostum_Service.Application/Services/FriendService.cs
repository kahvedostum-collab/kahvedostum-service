using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;

namespace KahveDostum_Service.Application.Services;

public class FriendService(IUnitOfWork unitOfWork) : IFriendService
{
    private readonly IUnitOfWork _uow = unitOfWork;

    public async Task SendFriendRequestAsync(int currentUserId, SendFriendRequestDto request)
    {
        if (currentUserId == request.ToUserId)
            throw new InvalidOperationException("Kendinize arkadaşlık isteği gönderemezsiniz.");

        var targetUser = await _uow.Users.GetByIdAsync(request.ToUserId);
        if (targetUser is null)
            throw new InvalidOperationException("Hedef kullanıcı bulunamadı.");

        var alreadyFriends = await _uow.Friendships.AreFriendsAsync(currentUserId, request.ToUserId);
        if (alreadyFriends)
            throw new InvalidOperationException("Zaten arkadaşsınız.");

        var hasPending = await _uow.FriendRequests.HasPendingRequestBetweenAsync(currentUserId, request.ToUserId);
        if (hasPending)
            throw new InvalidOperationException("Zaten aranızda bekleyen bir istek var.");

        var friendRequest = new FriendRequest
        {
            FromUserId = currentUserId,
            ToUserId = request.ToUserId,
            Status = FriendRequestStatus.Pending
        };

        await _uow.FriendRequests.AddAsync(friendRequest);
        await _uow.SaveChangesAsync();
    }

    public async Task CancelFriendRequestAsync(int currentUserId, int requestId)
    {
        var req = await _uow.FriendRequests.GetByIdAsync(requestId)
                  ?? throw new InvalidOperationException("İstek bulunamadı.");

        if (req.FromUserId != currentUserId)
            throw new InvalidOperationException("Sadece kendi gönderdiğiniz isteği iptal edebilirsiniz.");

        if (req.Status != FriendRequestStatus.Pending)
            throw new InvalidOperationException("Bu istek zaten sonuçlanmış.");

        req.Status = FriendRequestStatus.Canceled;
        req.RespondedAt = DateTime.UtcNow;

        _uow.FriendRequests.Update(req);
        await _uow.SaveChangesAsync();
    }

    public async Task RespondToFriendRequestAsync(int currentUserId, int requestId, bool accept)
    {
        var req = await _uow.FriendRequests.GetPendingRequestForReceiverAsync(requestId, currentUserId)
                  ?? throw new InvalidOperationException("Bekleyen istek bulunamadı.");

        if (accept)
        {
            req.Status = FriendRequestStatus.Accepted;
            req.RespondedAt = DateTime.UtcNow;

            // iki yönlü friendship oluştur
            var f1 = new Friendship
            {
                UserId = currentUserId,
                FriendUserId = req.FromUserId
            };

            var f2 = new Friendship
            {
                UserId = req.FromUserId,
                FriendUserId = currentUserId
            };

            await _uow.Friendships.AddAsync(f1);
            await _uow.Friendships.AddAsync(f2);
        }
        else
        {
            req.Status = FriendRequestStatus.Rejected;
            req.RespondedAt = DateTime.UtcNow;
        }

        _uow.FriendRequests.Update(req);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<FriendRequestDto>> GetIncomingRequestsAsync(int currentUserId)
    {
        var list = await _uow.FriendRequests.GetIncomingPendingRequestsAsync(currentUserId);

        return list.Select(fr => new FriendRequestDto
        {
            RequestId = fr.Id,
            FromUserId = fr.FromUserId,
            FromUserName = fr.FromUser.UserName,
            FromUserEmail = fr.FromUser.Email,
            CreatedAt = fr.CreatedAt
        }).ToList();
    }

    public async Task<List<FriendRequestDto>> GetOutgoingRequestsAsync(int currentUserId)
    {
        var list = await _uow.FriendRequests.GetOutgoingPendingRequestsAsync(currentUserId);

        return list.Select(fr => new FriendRequestDto
        {
            RequestId = fr.Id,
            FromUserId = fr.ToUserId,
            FromUserName = fr.ToUser.UserName,
            FromUserEmail = fr.ToUser.Email,
            CreatedAt = fr.CreatedAt
        }).ToList();
    }

    public async Task<List<FriendDto>> GetFriendsAsync(int currentUserId)
    {
        var friendships = await _uow.Friendships.GetFriendshipsForUserAsync(currentUserId);

        return friendships.Select(f => new FriendDto
        {
            UserId = f.FriendUserId,
            UserName = f.FriendUser.UserName,
            Email = f.FriendUser.Email
        }).ToList();
    }

    public async Task RemoveFriendAsync(int currentUserId, int friendUserId)
    {
        if (currentUserId == friendUserId)
            throw new InvalidOperationException("Kendinizi arkadaşlıktan çıkaramazsınız.");

        var f1 = await _uow.Friendships.GetFriendshipAsync(currentUserId, friendUserId);
        var f2 = await _uow.Friendships.GetFriendshipAsync(friendUserId, currentUserId);

        if (f1 is null && f2 is null)
            throw new InvalidOperationException("Şu anda arkadaş değilsiniz.");

        if (f1 is not null)
            _uow.Friendships.Remove(f1);

        if (f2 is not null)
            _uow.Friendships.Remove(f2);

        await _uow.SaveChangesAsync();
    }
}
