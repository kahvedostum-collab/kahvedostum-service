using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface ICafeActivationTokenRepository 
    : IRepository<CafeActivationToken>
{
    Task<CafeActivationToken?> GetByTokenAsync(string token);
}