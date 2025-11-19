using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class MessageReceiptRepository(AppDbContext context)
    : Repository<MessageReceipt>(context), IMessageReceiptRepository
{
    private readonly AppDbContext _context = context;

    public Task<MessageReceipt?> GetReceiptAsync(int messageId, int userId)
    {
        return _context.MessageReceipts
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);
    }
}