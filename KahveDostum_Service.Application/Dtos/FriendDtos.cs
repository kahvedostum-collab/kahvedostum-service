namespace KahveDostum_Service.Application.Dtos;

public class SendFriendRequestDto
{
    public int ToUserId { get; set; }
}

public class RespondFriendRequestDto
{
    public bool Accept { get; set; }
}

public class FriendDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
}

public class FriendRequestDto
{
    public int RequestId { get; set; }
    public int FromUserId { get; set; }
    public string FromUserName { get; set; } = default!;
    public string FromUserEmail { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}