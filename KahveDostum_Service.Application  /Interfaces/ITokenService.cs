using System.Security.Claims;
using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}