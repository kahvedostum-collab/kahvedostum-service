namespace KahveDostum_Service.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;

    public byte[] PasswordHash { get; set; } = default!;
    public byte[] PasswordSalt { get; set; } = default!;
    public string? PhotoUrl { get; set; }
    public string? Bio { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}