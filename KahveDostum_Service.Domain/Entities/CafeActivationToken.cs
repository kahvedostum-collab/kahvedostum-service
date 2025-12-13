namespace KahveDostum_Service.Domain.Entities;

public class CafeActivationToken
{
    public int Id { get; set; }

    public int CafeId { get; set; }
    public Cafe Cafe { get; set; } = default!;

    // Tokenı hangi fiş üretti
    public int? ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    // Tokenı üreten kullanıcı (fişi okutan)
    public int IssuedByUserId { get; set; }

    public string Token { get; set; } = default!;

    public bool IsUsed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}