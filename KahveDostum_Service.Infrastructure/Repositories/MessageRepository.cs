using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class MessageRepository(AppDbContext context)
    : Repository<Message>(context), IMessageRepository
{
    private readonly AppDbContext _context = context;

    public Task<Message?> GetMessageWithReceiptsAsync(int messageId)
    {
        return _context.Messages
            .Include(m => m.Receipts)
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }
}