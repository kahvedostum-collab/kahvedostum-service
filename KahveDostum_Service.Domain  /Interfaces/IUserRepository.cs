using KahveDostum_Service.Domain.Entities;

namespace KahveDostum_Service.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUserNameOrEmailAsync(string userNameOrEmail);
    Task<bool> ExistsByUserNameOrEmailAsync(string userName, string email);
}