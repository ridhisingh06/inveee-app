using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace invmgmt.web.Repositories
{
    public class RequestRepository : IRequestRepository
    {
        private readonly AppDbContext _context;

        public RequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Request?> GetByIdAsync(int id)
        {
            return await _context.Requests.FindAsync(id);
        }

        public async Task<Request?> GetByIdWithItemsAsync(int id)
        {
            return await _context.Requests
                .Include(r => r.RequestItems)
                    .ThenInclude(ri => ri.Item)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Request>> GetUserRequestsAsync(int userId, int pageNumber, int pageSize)
        {
            return await _context.Requests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Request>> GetPendingRequestsAsync(int pageNumber, int pageSize)
        {
            return await _context.Requests
                .Where(r => r.Status == RequestStatus.PendingWithIssuer || r.Status == RequestStatus.Pending)
                .Include(r => r.User)
                .Include(r => r.RequestItems)
                    .ThenInclude(ri => ri.Item)
                .OrderBy(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> HasActiveRequestAsync(int userId)
        {
            // Business rule:
            // - Block new requests while any existing request is waiting with issuer/admin or approved.
            // - Allow new requests only after NOT_ISSUED, REJECTED, or RECEIVED.
            // Note: keep Pending in the block list for backwards compatibility with older records.
            var activeStatuses = new[] { RequestStatus.PendingWithIssuer, RequestStatus.PendingAdminApproval, RequestStatus.Approved, RequestStatus.Pending };
            return await _context.Requests
                .AnyAsync(r => r.UserId == userId && activeStatuses.Contains(r.Status));
        }

        public async Task AddRequestAsync(Request request)
        {
            await _context.Requests.AddAsync(request);
        }

        public async Task AddRequestItemsAsync(IEnumerable<RequestItem> items)
        {
            await _context.RequestItems.AddRangeAsync(items);
        }

        public async Task UpdateRequestAsync(Request request)
        {
            _context.Requests.Update(request);
        }

        public async Task<bool> ItemsExistAsync(IEnumerable<int> itemIds)
        {
            var existingItemsCount = await _context.Items
                .Where(i => itemIds.Contains(i.Id))
                .CountAsync();
            return existingItemsCount == itemIds.Count();
        }

        public void DeleteRequest(Request request)
        {
            _context.Requests.Remove(request);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
