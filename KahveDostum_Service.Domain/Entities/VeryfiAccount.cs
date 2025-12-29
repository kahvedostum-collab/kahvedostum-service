namespace KahveDostum_Service.Domain.Entities;

public class VeryfiAccount
{
    public int Id { get; set; }

    public string BaseUrl { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string ApiKey { get; set; } = default!;

    // Kaç kez kullanıldığını sayan sayaç
    public int UsedCount { get; set; }

    // Bu hesabın limiti (senin örneğinde 100)
    public int UsageLimit { get; set; } = 100;

    // Hesap aktif mi?
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}