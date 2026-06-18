using invmgmt.web.Models;

namespace invmgmt.web.Repositories
{
    public interface IRegistrationRepository
    {
        Task<RegistrationRequest?> GetByIdAsync(int id);
        Task<RegistrationRequest?> GetByEmailAsync(string email);
        Task<bool> ExistsPendingAsync(string email);
        Task AddRequestAsync(RegistrationRequest request);
        Task<IEnumerable<RegistrationRequest>> GetPendingRequestsAsync();
        Task SaveChangesAsync();
    }
}
