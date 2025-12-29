using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class ReceiptRepository : Repository<Receipt>, IReceiptRepository
{
    public ReceiptRepository(AppDbContext context) : base(context)
    {
    }

    // Liste: performans için Lines include etmeden
    public async Task<List<Receipt>> GetByUserIdAsync(int userId)
    {
        return await Context.Receipts
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    // Detay: Lines dahil
    public async Task<Receipt?> GetWithLinesAsync(int receiptId, int userId)
    {
        return await Context.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.UserId == userId);
    }

    // INIT/PROCESSING sayma, sadece DONE say.
    public async Task<bool> HasRecentReceiptAsync(int userId, int cafeId, DateTime sinceUtc)
    {
        return await Context.Receipts.AnyAsync(r =>
            r.UserId == userId &&
            r.CafeId.HasValue && r.CafeId.Value == cafeId &&
            r.CreatedAt >= sinceUtc &&
            r.Status == ReceiptStatus.DONE
        );
    }

    // Duplicate hash kontrolü: hash boş/null gelirse false
    public async Task<bool> ExistsByHashAsync(string receiptHash)
    {
        if (string.IsNullOrWhiteSpace(receiptHash))
            return false;

        return await Context.Receipts.AnyAsync(r => r.ReceiptHash == receiptHash);
    }
}