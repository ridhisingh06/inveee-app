using invmgmt.web.Data;
using invmgmt.web.Models;
using Microsoft.EntityFrameworkCore;

namespace invmgmt.web.Repositories
{
    public class RegistrationRepository : IRegistrationRepository
    {
        private readonly AppDbContext _context;

        public RegistrationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RegistrationRequest?> GetByIdAsync(int id)
        {
            return await _context.RegistrationRequests.FindAsync(id);
        }

        public async Task<RegistrationRequest?> GetByEmailAsync(string email)
        {
            return await _context.RegistrationRequests.FirstOrDefaultAsync(r => r.Email == email);
        }

        public async Task<bool> ExistsPendingAsync(string email)
        {
            return await _context.RegistrationRequests
                .AnyAsync(r => r.Email == email && r.Status == RegistrationStatus.Pending);
        }

        public async Task AddRequestAsync(RegistrationRequest request)
        {
            await _context.RegistrationRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<RegistrationRequest>> GetPendingRequestsAsync()
        {
            return await _context.RegistrationRequests
                .Where(r => r.Status == RegistrationStatus.Pending)
                .Include(r => r.Department)
                .Include(r => r.Role)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
