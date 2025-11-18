using System.Security.Cryptography;
using System.Text;
using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace KahveDostum_Service.Application.Services;

public class AuthService(
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IConfiguration configuration
) : IAuthService
{
    private readonly IUnitOfWork _uow = unitOfWork;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IConfiguration _configuration = configuration;

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var exists = await _uow.Users.ExistsByUserNameOrEmailAsync(request.UserName, request.Email);
        if (exists)
            throw new InvalidOperationException("Kullanıcı adı veya email zaten kayıtlı.");

        CreatePasswordHash(request.Password, out var hash, out var salt);

        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        refreshToken.UserId = user.Id;

        await _uow.RefreshTokens.AddAsync(refreshToken);
        await _uow.SaveChangesAsync();

        return BuildAuthResponse(accessToken, refreshToken.Token, refreshToken.ExpiresAt);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _uow.Users.GetByUserNameOrEmailAsync(request.UserNameOrEmail);
        if (user is null)
            throw new UnauthorizedAccessException("Kullanıcı bulunamadı.");

        if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            throw new UnauthorizedAccessException("Şifre hatalı.");

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        refreshToken.UserId = user.Id;

        await _uow.RefreshTokens.AddAsync(refreshToken);
        await _uow.SaveChangesAsync();

        return BuildAuthResponse(accessToken, refreshToken.Token, refreshToken.ExpiresAt);
    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshRequestDto request)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
            throw new InvalidOperationException("Geçersiz access token.");

        var userIdClaim = principal.Claims.FirstOrDefault(c =>
            c.Type == System.Security.Claims.ClaimTypes.NameIdentifier ||
            c.Type == JwtRegisteredClaimNames.Sub);

        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            throw new InvalidOperationException("Geçersiz kullanıcı bilgisi.");

        var user = await _uow.Users.GetByIdAsync(userId)
                   ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        var existingRefresh = await _uow.RefreshTokens.GetValidTokenAsync(user.Id, request.RefreshToken);
        if (existingRefresh is null)
            throw new InvalidOperationException("Geçersiz veya süresi dolmuş refresh token.");

        existingRefresh.IsRevoked = true;
        _uow.RefreshTokens.Update(existingRefresh);

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        newRefreshToken.UserId = user.Id;

        await _uow.RefreshTokens.AddAsync(newRefreshToken);
        await _uow.SaveChangesAsync();

        return BuildAuthResponse(newAccessToken, newRefreshToken.Token, newRefreshToken.ExpiresAt);
    }

    private AuthResponseDto BuildAuthResponse(string accessToken, string refreshToken, DateTime refreshExpiresAt)
    {
        var minutes = int.Parse(_configuration["Jwt:AccessTokenMinutes"]! as string);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(minutes),
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        };
    }

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
    {
        using var hmac = new HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }
}
