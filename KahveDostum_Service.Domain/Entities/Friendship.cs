namespace KahveDostum_Service.Domain.Entities;

public class Friendship
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int FriendUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = default!;
    public User FriendUser { get; set; } = default!;
}