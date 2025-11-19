namespace KahveDostum_Service.Domain.Entities;

public enum FriendRequestStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Canceled = 3
}

public class FriendRequest
{
    public int Id { get; set; }

    public int FromUserId { get; set; }
    public int ToUserId { get; set; }

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public User FromUser { get; set; } = default!;
    public User ToUser { get; set; } = default!;
}