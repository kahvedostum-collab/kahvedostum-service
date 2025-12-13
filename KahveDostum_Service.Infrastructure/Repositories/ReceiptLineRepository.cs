using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class ReceiptLineRepository 
    : Repository<ReceiptLine>, IReceiptLineRepository
{
    public ReceiptLineRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<ReceiptLine>> GetByReceiptIdAsync(int receiptId)
    {
        return await Context.ReceiptLines
            .Where(l => l.ReceiptId == receiptId)
            .ToListAsync();
    }
}