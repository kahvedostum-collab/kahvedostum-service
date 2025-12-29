namespace KahveDostum_Service.Application.Dtos;

public class ReceiptListItemDto
{
    public int Id { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UploadedAt { get; set; }

    // Eğer OCR/manuel tarafta doluyorsa:
    public DateTime? ReceiptDate { get; set; }
    public string? Total { get; set; }

    // İstersen ekstra:
    public int? CafeId { get; set; }
}
