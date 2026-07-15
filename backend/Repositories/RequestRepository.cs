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
            return await _context.Requests
                .Include(r => r.RequestItems)
                .Include(r => r.IssueLogs)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> IsEditableAsync(int requestId)
        {
            return await _context.Requests
                .Where(r => r.Id == requestId)
                .AnyAsync(r => r.Status == RequestStatus.PendingWithIssuer && !r.IssueLogs.Any());
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
            // ── Business Rule ────────────────────────────────────────────────────────
            // A user is BLOCKED from creating a new request only when they have one in
            // a non-terminal, in-progress state:
            //   • PendingWithIssuer   — waiting for issuer to process
            //   • PendingAdminApproval — waiting for admin to approve
            //   • Approved (ReadyToReceive) — approved but user hasn't confirmed receipt
            //
            // A user is NOT blocked when the latest request is in a terminal state:
            //   • Received  — fully completed workflow
            //   • Rejected  — fully or partially rejected
            //   • NotIssued — issuer rejected all items
            //   • Pending   — legacy status treated as terminal if corrected below
            // ────────────────────────────────────────────────────────────────────────
            var activeStatuses = new[]
            {
                RequestStatus.PendingWithIssuer,
                RequestStatus.PendingAdminApproval,
                RequestStatus.Approved,   // ReadyToReceive — blocks until user clicks Receive
                RequestStatus.Pending     // legacy alias
            };

            // Stale-status correction: if a request is recorded as active but its items
            // have all reached terminal states, correct the request-level status so the
            // user isn't permanently blocked.
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
                    _logger.LogInformation(
                        "Correcting stale request status: RequestId={RequestId} {OldStatus} -> {NewStatus}",
                        req.Id, req.Status, recalculated);
                    req.Status     = recalculated;
                    req.UpdatedAt  = System.DateTime.UtcNow;
                    _context.Requests.Update(req);
                    changed = true;
                }
            }
            if (changed)
                await _context.SaveChangesAsync();

            // After corrections, re-query using only the active statuses.
            return await _context.Requests
                .AnyAsync(r => r.UserId == userId && activeStatuses.Contains(r.Status));
        }

        private static RequestStatus RecalculateStatusFromItems(IEnumerable<Models.Enums.RequestItemStatus> itemStatusesEnum)
        {
            var itemStatuses = itemStatusesEnum.ToList();
            if (itemStatuses.Count == 0) return RequestStatus.PendingWithIssuer;

            // Still waiting for issuer
            if (itemStatuses.Any(s => s == Models.Enums.RequestItemStatus.PendingWithIssuer))
                return RequestStatus.PendingWithIssuer;

            // Still waiting for admin approval
            if (itemStatuses.Any(s => s == Models.Enums.RequestItemStatus.PendingAdminApproval))
                return RequestStatus.PendingAdminApproval;

            // Every item was rejected by the issuer — nothing to approve
            if (itemStatuses.All(s => s == Models.Enums.RequestItemStatus.NotIssued))
                return RequestStatus.NotIssued;

            // All items are in terminal states (Received, NotIssued, Rejected)
            var terminalStatuses = new[]
            {
                Models.Enums.RequestItemStatus.Received,
                Models.Enums.RequestItemStatus.NotIssued,
                Models.Enums.RequestItemStatus.Rejected
            };
            if (itemStatuses.All(s => terminalStatuses.Contains(s)))
            {
                // If at least one was received, the request completed (Received)
                if (itemStatuses.Any(s => s == Models.Enums.RequestItemStatus.Received))
                    return RequestStatus.Received;
                return RequestStatus.Rejected;
            }

            // ✅ Approved: all remaining non-terminal items are Approved; some may be NotIssued.
            // This covers Scenario 1 & 2 — partial issue where some items got NotIssued
            // by the issuer and the rest were approved by admin.
            var approvedTerminalStatuses = new[]
            {
                Models.Enums.RequestItemStatus.Approved,
                Models.Enums.RequestItemStatus.NotIssued,
                Models.Enums.RequestItemStatus.Received
            };
            if (itemStatuses.All(s => approvedTerminalStatuses.Contains(s))
                && itemStatuses.Any(s => s == Models.Enums.RequestItemStatus.Approved))
                return RequestStatus.Approved;

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

        /// <summary>
        /// Explicitly removes a RequestItem row from the DbSet.
        /// This is required when updating a request because calling
        /// _context.Requests.Update(request) re-marks everything as Modified,
        /// which would override any Remove() tracking on child entities.
        /// By explicitly removing from the DbSet first we ensure the DELETE
        /// statement is emitted regardless of the subsequent Update() call.
        /// </summary>
        public void RemoveRequestItem(RequestItem item)
        {
            _context.RequestItems.Remove(item);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
