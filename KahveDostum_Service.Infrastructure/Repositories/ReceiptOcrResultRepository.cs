using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class ReceiptOcrResultRepository : Repository<ReceiptOcrResult>, IReceiptOcrResultRepository
{
    public ReceiptOcrResultRepository(AppDbContext context) : base(context) { }

    public async Task<ReceiptOcrResult?> GetByReceiptIdAsync(int receiptId)
    {
        return await Context.ReceiptOcrResults
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReceiptId == receiptId);
    }
}