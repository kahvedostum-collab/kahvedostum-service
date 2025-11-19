using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface IMessageRepository : IRepository<Message>
{
    Task<Message?> GetMessageWithReceiptsAsync(int messageId);
}