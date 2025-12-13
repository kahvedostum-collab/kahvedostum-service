using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface IReceiptLineRepository : IRepository<ReceiptLine>
{
    Task<List<ReceiptLine>> GetByReceiptIdAsync(int receiptId);
}