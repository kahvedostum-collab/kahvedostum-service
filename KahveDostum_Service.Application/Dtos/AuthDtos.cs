namespace KahveDostum_Service.Application.Dtos;

public class RegisterRequestDto
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class LoginRequestDto
{
    public string UserNameOrEmail { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class RefreshRequestDto
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}

public class LoginResultDto
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}
public class LogoutRequestDto
{
    public string RefreshToken { get; set; } = default!;
}
public class RegisterResultDto : LoginResultDto
{
    public string? AvatarUploadUrl { get; set; }
    public string? AvatarObjectKey { get; set; }
    public string? AvatarBucket { get; set; }
}