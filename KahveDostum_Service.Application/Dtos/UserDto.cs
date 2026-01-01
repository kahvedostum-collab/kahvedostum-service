namespace KahveDostum_Service.Application.Dtos;

public class UserDto
{
    
}

public class MeResponseDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;

    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateProfileRequestDto
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;

    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;

    public string? Bio { get; set; }
}
