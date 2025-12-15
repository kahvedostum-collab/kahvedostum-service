namespace KahveDostum_Service.Application.Dtos;

public class ReceiptStatusResponseDto
{
    public int ReceiptId { get; set; }
    public string Status { get; set; } = default!; // INIT/PROCESSING/DONE/FAILED
    public object? Result { get; set; }            
}