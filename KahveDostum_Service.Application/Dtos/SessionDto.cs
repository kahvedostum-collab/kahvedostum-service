namespace KahveDostum_Service.Application.Dtos;

public class SessionDto
{
    public int SessionId { get; set; }
    public int UserId { get; set; }
    public int CafeId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class StartSessionRequestDto
{
    public int CafeId { get; set; }
}