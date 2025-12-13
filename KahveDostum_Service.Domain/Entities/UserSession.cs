namespace KahveDostum_Service.Domain.Entities;

public enum SessionStatus
{
    Active = 1,
    Expired = 2
}

public class UserSession
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = default!;

    public int CafeId { get; set; }
    public Cafe Cafe { get; set; } = default!;

    // Hangi token ile aktif oldu
    public int? ActivationTokenId { get; set; }
    public CafeActivationToken? ActivationToken { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Active;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
