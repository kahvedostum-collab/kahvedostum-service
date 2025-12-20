namespace KahveDostum_Service.Domain.Entities;

public class Receipt
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int? CafeId { get; set; }

    // ===== Upload/Pipeline alanları (YENİ) =====
    public ReceiptStatus Status { get; set; } = ReceiptStatus.INIT;

    public string? Bucket { get; set; }
    public string? ObjectKey { get; set; }
    public string? OcrJobId { get; set; }          // rabbit job correlation
    public string? RejectReason { get; set; }      // doğrulama reddi vs

    public double? ClientLat { get; set; }
    public double? ClientLng { get; set; }

    public DateTime? UploadedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string ChannelKey { get; set; } = Guid.NewGuid().ToString("N");

    // ===== OCR sonucu fiş alanları (SENDE VAR) =====
    public string? Brand { get; set; }
    public string? ReceiptNo { get; set; }

    public string? Total { get; set; }    public string? RawText { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }

    // FİŞ TARİHİ (OCR)
    public DateTime? ReceiptDate { get; set; }
    // SERVER TIME
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // TEKİL FİŞ HASH
    public string? ReceiptHash { get; set; }
    public User User { get; set; } = default!;
    public Cafe? Cafe { get; set; }

    public ICollection<CafeActivationToken> ActivationTokens { get; set; } = new List<CafeActivationToken>();
    public ICollection<ReceiptLine> Lines { get; set; } = new List<ReceiptLine>();
}