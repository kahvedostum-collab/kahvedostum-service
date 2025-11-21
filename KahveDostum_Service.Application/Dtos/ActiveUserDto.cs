namespace KahveDostum_Service.Application.Dtos;

public class ActiveUserDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = default!;
    public string? PhotoUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime ExpiresAt { get; set; }
}