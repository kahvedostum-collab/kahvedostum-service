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

    public async Task<LoginResultDto> RegisterAsync(RegisterRequestDto request)
    {
        // kullanıcı adı ve email kontrolü
        var (userNameExists, emailExists) = await _uow.Users.CheckUserConflictsAsync(request.UserName, request.Email);

        if (userNameExists && emailExists)
            throw new InvalidOperationException("Bu kullanıcı adı ve email zaten kayıtlı.");

        if (userNameExists)
            throw new InvalidOperationException("Bu kullanıcı adı zaten alınmış.");

        if (emailExists)
            throw new InvalidOperationException("Bu email adresi zaten kayıtlı.");

        // 2) Password Hash
        CreatePasswordHash(request.Password, out var hash, out var salt);

        // 3) User oluşturma
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        // 4) Token üretimi
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        refreshToken.UserId = user.Id;

        await _uow.RefreshTokens.AddAsync(refreshToken);
        await _uow.SaveChangesAsync();

        // 5) Response
        return new LoginResultDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token
        };
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequestDto request)
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

        return new LoginResultDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token
        };
    }

    public async Task LogoutAsync(LogoutRequestDto request)
    {
        var token = await _uow.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (token == null)
            throw new InvalidOperationException("Token bulunamadı.");

        _uow.RefreshTokens.Remove(token);
        await _uow.SaveChangesAsync();
    }

    public async Task<LoginResultDto> RefreshAsync(RefreshRequestDto request)
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
            throw new InvalidOperationException("Geçersiz refresh token.");

        existingRefresh.IsRevoked = true;
        _uow.RefreshTokens.Update(existingRefresh);

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        newRefreshToken.UserId = user.Id;

        await _uow.RefreshTokens.AddAsync(newRefreshToken);
        await _uow.SaveChangesAsync();

        return new LoginResultDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token
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
