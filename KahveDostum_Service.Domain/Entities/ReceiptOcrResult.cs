using KahveDostum_Service.Domain.Entities;

public class ReceiptOcrResult
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }
    public Receipt Receipt { get; set; } = default!;

    public string JobId { get; set; } = default!;
    public string Status { get; set; } = default!; // DONE / FAILED

    public string? RawText { get; set; }
    public string? PayloadJson { get; set; }
    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}