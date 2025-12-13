namespace KahveDostum_Service.Domain.Entities;

public class Receipt
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int? CafeId { get; set; }

    public string? Brand { get; set; }
    public string? ReceiptNo { get; set; }

    public string Total { get; set; } = default!;
    public string? RawText { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }

    // FİŞ TARİHİ (OCR)
    public DateTime ReceiptDate { get; set; }

    // SERVER TIME
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // TEKİL FİŞ HASH
    public string ReceiptHash { get; set; } = default!;

    public User User { get; set; } = default!;
    public Cafe? Cafe { get; set; }

    public ICollection<CafeActivationToken> ActivationTokens { get; set; }
        = new List<CafeActivationToken>();

    public ICollection<ReceiptLine> Lines { get; set; }
        = new List<ReceiptLine>();
}
