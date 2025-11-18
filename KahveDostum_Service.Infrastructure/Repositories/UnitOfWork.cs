using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class UnitOfWork(
    AppDbContext context,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository
) : IUnitOfWork
{
    private readonly AppDbContext _context = context;

    public IUserRepository Users { get; } = userRepository;
    public IRefreshTokenRepository RefreshTokens { get; } = refreshTokenRepository;

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}