using KahveDostum_Service.Application.Dtos;
namespace KahveDostum_Service.Application.Interfaces;

public interface IUserService
{
    Task<MeResponseDto> GetMeAsync(int userId);
    Task UpdateAvatarPathAsync(int userId, string? avatarPath);
    Task UpdateProfileAsync(int userId, UpdateProfileRequestDto dto);

}