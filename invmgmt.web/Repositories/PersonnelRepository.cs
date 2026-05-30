using invmgmt.web.Data;
using invmgmt.web.Models;
using Microsoft.EntityFrameworkCore;

namespace invmgmt.web.Repositories
{
    public class PersonnelRepository : IPersonnelRepository
    {
        private readonly AppDbContext _context;

        public PersonnelRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Personnel?> GetByIdAsync(int id)
            => await _context.Personnel.FindAsync(id);

        public async Task<(IEnumerable<Personnel> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
        {
            var query = _context.Personnel.AsNoTracking().OrderByDescending(p => p.CreatedAt);
            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
            => await _context.Personnel
                .AnyAsync(p => p.Email.ToLower() == email.ToLower()
                            && (excludeId == null || p.Id != excludeId));

        public async Task AddAsync(Personnel personnel)
            => await _context.Personnel.AddAsync(personnel);

        public Task UpdateAsync(Personnel personnel)
        {
            _context.Personnel.Update(personnel);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Personnel personnel)
        {
            _context.Personnel.Remove(personnel);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
