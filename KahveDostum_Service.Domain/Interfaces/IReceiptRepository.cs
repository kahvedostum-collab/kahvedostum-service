using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface IReceiptRepository : IRepository<Receipt>
{
    Task<List<Receipt>> GetByUserIdAsync(int userId);
    Task<bool> HasRecentReceiptAsync(int userId, int cafeId, DateTime sinceUtc);
    Task<bool> ExistsByHashAsync(string receiptHash);

}