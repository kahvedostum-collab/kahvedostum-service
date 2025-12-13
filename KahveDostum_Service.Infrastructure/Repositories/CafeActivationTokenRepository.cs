using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class CafeActivationTokenRepository
    : Repository<CafeActivationToken>, ICafeActivationTokenRepository
{
    public CafeActivationTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<CafeActivationToken?> GetByTokenAsync(string token)
    {
        return await Context.CafeActivationTokens
            .FirstOrDefaultAsync(t => t.Token == token);
    }
}