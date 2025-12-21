namespace KahveDostum_Service.Application.Dtos;

public class CafeActiveUserDto
{
    public int UserId { get; set; }
    public int CafeId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ReceiptId { get; set; }
}