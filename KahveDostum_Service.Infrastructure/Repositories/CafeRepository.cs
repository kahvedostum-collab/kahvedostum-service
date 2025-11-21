using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class CafeRepository : ICafeRepository
{
    private readonly AppDbContext _context;
    public CafeRepository(AppDbContext context) => _context = context;

    public Task<Cafe?> GetByIdAsync(int id)
        => _context.Cafes.FirstOrDefaultAsync(c => c.Id == id);

    public Task<List<Cafe>> GetAllAsync()
        => _context.Cafes.Where(c => c.IsActive).ToListAsync();

    public async Task AddAsync(Cafe cafe)
        => await _context.Cafes.AddAsync(cafe);
}