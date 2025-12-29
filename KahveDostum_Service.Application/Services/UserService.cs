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
            AvatarUrl = user.PhotoUrl
        };
    }
    
    public async Task UpdateAvatarUrlAsync(int userId, string avatarUrl)
    {
        var user = await _uow.Users.GetByIdAsync(userId)
                   ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        user.PhotoUrl = avatarUrl;
        _uow.Users.Update(user);

        await _uow.SaveChangesAsync();
    }

}