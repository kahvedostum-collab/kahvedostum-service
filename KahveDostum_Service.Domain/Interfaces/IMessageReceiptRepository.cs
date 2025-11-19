using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface IMessageReceiptRepository : IRepository<MessageReceipt>
{
    Task<MessageReceipt?> GetReceiptAsync(int messageId, int userId);
}