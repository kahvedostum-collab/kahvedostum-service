using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class ReceiptRepository 
    : Repository<Receipt>, IReceiptRepository
{
    public ReceiptRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<Receipt>> GetByUserIdAsync(int userId)
    {
        return await Context.Receipts
            .Include(r => r.Lines)
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }
    
    public async Task<bool> HasRecentReceiptAsync(
        int userId,
        int cafeId,
        DateTime sinceUtc)
    {
        return await Context.Receipts.AnyAsync(r =>
            r.UserId == userId &&
            r.CafeId == cafeId &&
            r.CreatedAt >= sinceUtc);
    }
    
    public async Task<bool> ExistsByHashAsync(string receiptHash)
    {
        return await Context.Receipts
            .AnyAsync(r => r.ReceiptHash == receiptHash);
    }

}