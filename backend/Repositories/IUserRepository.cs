using invmgmt.web.Models;

namespace invmgmt.web.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task AddUserRoleAsync(UserRole userRole);
        Task<bool> AnyAdminExistsAsync();
    }
}
