using invmgmt.web.Models;

namespace invmgmt.web.Repositories
{
    public interface IPersonnelRepository
    {
        Task<Personnel?> GetByIdAsync(int id);
        Task<(IEnumerable<Personnel> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
        Task<bool> ICNumberExistsAsync(string? icNumber, int? excludeId = null);
        Task<bool> IdCardNumberExistsAsync(string? idCardNumber, int? excludeId = null);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
        Task AddAsync(Personnel personnel);
        Task UpdateAsync(Personnel personnel);
        Task DeleteAsync(Personnel personnel);
        Task SaveChangesAsync();
    }
}
