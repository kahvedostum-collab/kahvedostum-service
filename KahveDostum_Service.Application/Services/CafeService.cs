using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Interfaces;

namespace KahveDostum_Service.Application.Services;

public class CafeService : ICafeService
{
    private readonly IUnitOfWork _uow;

    public CafeService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<CafeDto> CreateCafeAsync(CafeDto dto)
    {
        var cafe = new Domain.Entities.Cafe
        {
            Name = dto.Name,
            Address = dto.Address,
            Description = dto.Description,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };

        await _uow.Cafes.AddAsync(cafe);
        await _uow.SaveChangesAsync();

        dto.Id = cafe.Id;
        return dto;
    }

    public async Task<List<CafeDto>> GetAllAsync()
    {
        var cafes = await _uow.Cafes.GetAllAsync();
        return cafes.Select(c => new CafeDto
        {
            Id = c.Id,
            Name = c.Name,
            Address = c.Address,
            Description = c.Description,
            Latitude = c.Latitude,
            Longitude = c.Longitude
        }).ToList();
    }

    public async Task<List<ActiveUserDto>> GetActiveUsersAsync(int cafeId)
    {
        var sessions = await _uow.UserSessions.GetActiveSessionsByCafeIdAsync(cafeId);

        return sessions.Select(s => new ActiveUserDto
        {
            UserId = s.UserId,
            UserName = s.User.UserName,
            PhotoUrl = s.User.PhotoUrl,
            Bio = s.User.Bio,
            ExpiresAt = s.ExpiresAt
        }).ToList();
    }
}