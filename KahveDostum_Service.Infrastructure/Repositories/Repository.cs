using System.Linq.Expressions;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class Repository<T>(AppDbContext context) : IRepository<T> where T : class
{
    protected readonly AppDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(int id) => await DbSet.FindAsync(id);
    public async Task AddAsync(T entity) => await DbSet.AddAsync(entity);
    public void Update(T entity) => DbSet.Update(entity);
    public void Remove(T entity) => DbSet.Remove(entity);
    public IQueryable<T> Query() => DbSet.AsQueryable();
    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        => DbSet.FirstOrDefaultAsync(predicate);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => DbSet.AnyAsync(predicate);
}