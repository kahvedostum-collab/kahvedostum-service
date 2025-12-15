using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface IReceiptOcrResultRepository : IRepository<ReceiptOcrResult>
{
    Task<ReceiptOcrResult?> GetByReceiptIdAsync(int receiptId);
}