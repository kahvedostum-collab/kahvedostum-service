using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Repositories;

public class UserRepository(AppDbContext context)
    : Repository<User>(context), IUserRepository
{
    private readonly AppDbContext _context = context;
    
    public Task<User?> GetByUserNameOrEmailAsync(string userNameOrEmail)
    {
        return _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u =>
                u.UserName == userNameOrEmail || u.Email == userNameOrEmail);
    }

    public Task<bool> ExistsByUserNameOrEmailAsync(string userName, string email)
    {
        return _context.Users
            .AnyAsync(u => u.UserName == userName || u.Email == email);
    }
    
    public async Task<(bool userNameExists, bool emailExists)> CheckUserConflictsAsync(string userName, string email)
    {
        var userNameExists = await _context.Users.AnyAsync(u => u.UserName == userName);
        var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
        return (userNameExists, emailExists);
    }
}