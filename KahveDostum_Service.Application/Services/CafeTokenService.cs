using System.Security.Cryptography;
using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;

namespace KahveDostum_Service.Application.Services;

public class CafeTokenService : ICafeTokenService
{
    private readonly IUnitOfWork _uow;

    public CafeTokenService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<CafeTokenDto> GenerateTokenAsync(int userId, int cafeId, int? receiptId = null)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        var entity = new CafeActivationToken
        {
            CafeId = cafeId,
            IssuedByUserId = userId,
            ReceiptId = receiptId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        await _uow.CafeActivationTokens.AddAsync(entity);
        await _uow.SaveChangesAsync();

        return new CafeTokenDto
        {
            Token = token,
            ExpiresAt = entity.ExpiresAt
        };
    }

    public async Task<int> ValidateTokenAsync(string token)
    {
        var entity = await _uow.CafeActivationTokens.GetByTokenAsync(token);

        if (entity == null)
            throw new InvalidOperationException("Geçersiz token.");

        if (entity.IsUsed)
            throw new InvalidOperationException("Token daha önce kullanılmış.");

        if (entity.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Token süresi dolmuş.");

        entity.IsUsed = true;
        entity.UsedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();

        return entity.CafeId;
    }
}