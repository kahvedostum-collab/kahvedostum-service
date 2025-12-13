using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface ICafeRepository
{
    Task<Cafe?> GetByIdAsync(int id);
    Task<List<Cafe>> GetAllAsync();
    Task AddAsync(Cafe cafe);
    Task<Cafe?> GetByTaxNumberAsync(string taxNumber);
    Task<Cafe?> GetByNormalizedAddressAsync(string normalizedAddress);
}