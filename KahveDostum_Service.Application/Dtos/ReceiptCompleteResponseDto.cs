namespace KahveDostum_Service.Application.Dtos;

public class ReceiptCompleteResponseDto
{
    public int ReceiptId { get; set; }
    public string Status { get; set; } = "PROCESSING";
    public string JobId { get; set; } = default!;
}