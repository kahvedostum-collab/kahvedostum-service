using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Interfaces;
namespace KahveDostum_Service.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;

    public UserService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task UpdateAvatarPathAsync(int userId, string? avatarPath)
    {
        var user = await _uow.Users.GetByIdAsync(userId)
                   ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        user.PhotoUrl = avatarPath;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
    }

    public async Task<MeResponseDto> GetMeAsync(int userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId)
                   ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        return new MeResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            UserName = user.UserName,
            Email = user.Email,
            Bio = user.Bio,
            AvatarUrl = string.IsNullOrEmpty(user.PhotoUrl)
                ? null
                : "/api/User/Avatar"
        };
    }
    
    public async Task UpdateProfileAsync(int userId, UpdateProfileRequestDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(userId)
                   ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        // Username uniqueness
        var usernameExists = await _uow.Users.AnyAsync(
            x => x.UserName == dto.UserName && x.Id != userId
        );
        if (usernameExists)
            throw new ArgumentException("Bu kullanıcı adı zaten kullanılıyor.");

        // Email uniqueness
        var emailExists = await _uow.Users.AnyAsync(
            x => x.Email == dto.Email && x.Id != userId
        );
        if (emailExists)
            throw new ArgumentException("Bu e-posta adresi zaten kullanılıyor.");

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.UserName = dto.UserName.Trim();
        user.Email = dto.Email.Trim();
        user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
    }

}
