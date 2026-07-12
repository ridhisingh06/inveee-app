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
        private readonly Microsoft.Extensions.Logging.ILogger<RequestRepository> _logger;

        public RequestRepository(AppDbContext context, Microsoft.Extensions.Logging.ILogger<RequestRepository> logger)
        {
            _context = context;
            _logger = logger;
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
            var activeItemStatuses = new[] { Models.Enums.RequestItemStatus.PendingWithIssuer, Models.Enums.RequestItemStatus.PendingAdminApproval, Models.Enums.RequestItemStatus.Approved };

            // First: detect and correct any requests whose recorded Request.Status is stale
            // (e.g. still PendingWithIssuer while all underlying items are terminal). This
            // ensures dashboards driven by item statuses and the request-level status stay in sync.
            var candidates = await _context.Requests
                .Where(r => r.UserId == userId && activeStatuses.Contains(r.Status))
                .Include(r => r.RequestItems)
                .ToListAsync();

            var changed = false;
            foreach (var req in candidates)
            {
                var recalculated = RecalculateStatusFromItems(req.RequestItems.Select(ri => ri.Status));
                if (recalculated != req.Status)
                {
                    _logger.LogInformation("Correcting stale request status: RequestId={RequestId} {OldStatus} -> {NewStatus}", req.Id, req.Status, recalculated);
                    req.Status = recalculated;
                    req.UpdatedAt = System.DateTime.UtcNow;
                    _context.Requests.Update(req);
                    changed = true;
                }
            }
            if (changed)
            {
                await _context.SaveChangesAsync();
            }

            // Now evaluate active condition using both request-level and item-level states.
            return await _context.Requests
                .AnyAsync(r => r.UserId == userId && (
                    activeStatuses.Contains(r.Status)
                    || r.RequestItems.Any(ri => activeItemStatuses.Contains(ri.Status))
                ));
        }

        private static RequestStatus RecalculateStatusFromItems(IEnumerable<Models.Enums.RequestItemStatus> itemStatusesEnum)
        {
            var itemStatuses = itemStatusesEnum.ToList();
            if (itemStatuses.Count == 0) return RequestStatus.PendingWithIssuer;

            if (itemStatuses.Any(status => status == Models.Enums.RequestItemStatus.PendingWithIssuer)) return RequestStatus.PendingWithIssuer;
            if (itemStatuses.Any(status => status == Models.Enums.RequestItemStatus.PendingAdminApproval)) return RequestStatus.PendingAdminApproval;
            if (itemStatuses.All(status => status == Models.Enums.RequestItemStatus.NotIssued)) return RequestStatus.NotIssued;
            if (itemStatuses.All(status => status == Models.Enums.RequestItemStatus.Received || status == Models.Enums.RequestItemStatus.NotIssued || status == Models.Enums.RequestItemStatus.Rejected))
            {
                if (itemStatuses.All(status => status == Models.Enums.RequestItemStatus.Received || status == Models.Enums.RequestItemStatus.NotIssued)) return RequestStatus.Received;
                return RequestStatus.Rejected;
            }
            if (itemStatuses.All(status => status == Models.Enums.RequestItemStatus.Approved || status == Models.Enums.RequestItemStatus.NotIssued || status == Models.Enums.RequestItemStatus.Received)) return RequestStatus.Approved;
            return RequestStatus.Rejected;
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
