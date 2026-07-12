using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace invmgmt.web.Repositories
{
    /// <summary>
    /// Repository for RequestItem entity
    /// Implements partial issuing/approval workflow operations
    /// </summary>
    public class RequestItemRepository : IRequestItemRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RequestItemRepository> _logger;

        public RequestItemRepository(AppDbContext context, ILogger<RequestItemRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RequestItem?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.RequestItems
                .Include(ri => ri.Item)
                    .ThenInclude(i => i.Category)
                .Include(ri => ri.Item)
                    .ThenInclude(i => i.InventoryStock)
                .Include(ri => ri.Request)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(ri => ri.Id == id);
        }

        public async Task<IEnumerable<RequestItem>> GetByRequestIdAsync(int requestId)
        {
            return await _context.RequestItems
                .Where(ri => ri.RequestId == requestId)
                .Include(ri => ri.Item)
                    .ThenInclude(i => i.Category)
                .Include(ri => ri.Item)
                    .ThenInclude(i => i.InventoryStock)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<RequestItem?> GetByIdWithInventoryAsync(int id)
        {
            return await _context.RequestItems
                .Include(ri => ri.Item)
                    .ThenInclude(i => i.InventoryStock)
                .FirstOrDefaultAsync(ri => ri.Id == id);
        }

        public async Task AddAsync(RequestItem item)
        {
            await _context.RequestItems.AddAsync(item);
            _logger.LogInformation("RequestItem added: RequestId={RequestId}, ItemId={ItemId}", 
                item.RequestId, item.ItemId);
        }

        public async Task AddRangeAsync(IEnumerable<RequestItem> items)
        {
            await _context.RequestItems.AddRangeAsync(items);
            _logger.LogInformation("RequestItems added: Count={Count}", items.Count());
        }

        public async Task UpdateIssuerQuantitiesAsync(
            int requestItemId,
            int issuerIssuedQuantity,
            int issuerRejectedQuantity,
            int issuedByUserId,
            DateTime issuedDate)
        {
            var requestItem = await _context.RequestItems.FindAsync(requestItemId);
            if (requestItem == null)
            {
                _logger.LogWarning("RequestItem not found for issuer update: {RequestItemId}", requestItemId);
                throw new InvalidOperationException($"RequestItem {requestItemId} not found.");
            }

            // Update issuer quantities
            requestItem.IssuerIssuedQuantity = issuerIssuedQuantity;
            requestItem.IssuerRejectedQuantity = issuerRejectedQuantity;
            requestItem.IssuedDate = issuedDate;
            requestItem.IssuedBy = issuedByUserId;
            requestItem.Status = RequestItemStatus.PendingAdminApproval;

            // Increment concurrency token for optimistic locking
            requestItem.ConcurrencyToken++;

            _context.RequestItems.Update(requestItem);

            _logger.LogInformation(
                "RequestItem issuer quantities updated: Id={Id}, Issued={Issued}, Rejected={Rejected}, Status={Status}",
                requestItemId, issuerIssuedQuantity, issuerRejectedQuantity, requestItem.Status);
        }

        public async Task UpdateAdminQuantitiesAsync(
            int requestItemId,
            int adminApprovedQuantity,
            int adminRejectedQuantity,
            int approvedByUserId,
            DateTime approvedDate)
        {
            var requestItem = await _context.RequestItems.FindAsync(requestItemId);
            if (requestItem == null)
            {
                _logger.LogWarning("RequestItem not found for admin approval: {RequestItemId}", requestItemId);
                throw new InvalidOperationException($"RequestItem {requestItemId} not found.");
            }

            // Update admin quantities
            requestItem.AdminApprovedQuantity = adminApprovedQuantity;
            requestItem.AdminRejectedQuantity = adminRejectedQuantity;
            requestItem.ApprovedDate = approvedDate;
            requestItem.ApprovedBy = approvedByUserId;
            requestItem.Status = RequestItemStatus.Approved;

            // Increment concurrency token
            requestItem.ConcurrencyToken++;

            _context.RequestItems.Update(requestItem);

            _logger.LogInformation(
                "RequestItem admin quantities updated: Id={Id}, Approved={Approved}, Rejected={Rejected}, Status={Status}",
                requestItemId, adminApprovedQuantity, adminRejectedQuantity, requestItem.Status);
        }

        public async Task UpdateReceivedAsync(
            int requestItemId,
            int receivedQuantity,
            DateTime receivedDate)
        {
            var requestItem = await _context.RequestItems.FindAsync(requestItemId);
            if (requestItem == null)
            {
                _logger.LogWarning("RequestItem not found for receive update: {RequestItemId}", requestItemId);
                throw new InvalidOperationException($"RequestItem {requestItemId} not found.");
            }

            // Update received quantity
            requestItem.ReceivedQuantity = receivedQuantity;
            requestItem.ReceivedDate = receivedDate;
            requestItem.Status = RequestItemStatus.Received;

            // Increment concurrency token
            requestItem.ConcurrencyToken++;

            _context.RequestItems.Update(requestItem);

            _logger.LogInformation(
                "RequestItem received: Id={Id}, ReceivedQuantity={ReceivedQuantity}, Status={Status}",
                requestItemId, receivedQuantity, requestItem.Status);

            // After marking an item received, ensure the parent Request.Status is consistent.
            try
            {
                var requestId = requestItem.RequestId;
                var itemStatuses = await _context.RequestItems
                    .Where(ri => ri.RequestId == requestId)
                    .Select(ri => ri.Status)
                    .ToListAsync();

                // Recalculate request-level status mirroring RequestsController.RecalculateRequestStatus
                RequestStatus newStatus;
                if (itemStatuses.Count == 0) newStatus = RequestStatus.PendingWithIssuer;
                else if (itemStatuses.Any(s => s == RequestItemStatus.PendingWithIssuer)) newStatus = RequestStatus.PendingWithIssuer;
                else if (itemStatuses.Any(s => s == RequestItemStatus.PendingAdminApproval)) newStatus = RequestStatus.PendingAdminApproval;
                else if (itemStatuses.All(s => s == RequestItemStatus.NotIssued)) newStatus = RequestStatus.NotIssued;
                else if (itemStatuses.All(s => s == RequestItemStatus.Received || s == RequestItemStatus.NotIssued || s == RequestItemStatus.Rejected))
                {
                    if (itemStatuses.All(s => s == RequestItemStatus.Received || s == RequestItemStatus.NotIssued)) newStatus = RequestStatus.Received;
                    else newStatus = RequestStatus.Rejected;
                }
                else if (itemStatuses.All(s => s == RequestItemStatus.Approved || s == RequestItemStatus.NotIssued || s == RequestItemStatus.Received)) newStatus = RequestStatus.Approved;
                else newStatus = RequestStatus.Rejected;

                var request = await _context.Requests.FindAsync(requestId);
                if (request != null && request.Status != newStatus)
                {
                    _logger.LogInformation("Updating request status after receive: RequestId={RequestId} {OldStatus} -> {NewStatus}", request.Id, request.Status, newStatus);
                    request.Status = newStatus;
                    request.UpdatedAt = DateTime.UtcNow;
                    _context.Requests.Update(request);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while recalculating request status after marking item received: RequestItemId={RequestItemId}", requestItemId);
            }
        }

        public async Task<IEnumerable<RequestItem>> GetPendingWithIssuerAsync(int pageNumber, int pageSize)
        {
            return await _context.RequestItems
                .Where(ri => ri.Status == RequestItemStatus.PendingWithIssuer)
                .Include(ri => ri.Item)
                    .ThenInclude(i => i.Category)
                .Include(ri => ri.Item)
                    .ThenInclude(i => i.InventoryStock)
                .Include(ri => ri.Request)
                    .ThenInclude(r => r.User)
                .OrderBy(ri => ri.Request.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<RequestItem>> GetPendingWithAdminAsync(int pageNumber, int pageSize)
        {
            return await _context.RequestItems
                .Where(ri => ri.Status == RequestItemStatus.PendingAdminApproval)
                .Include(ri => ri.Item)
                    .ThenInclude(i => i.Category)
                .Include(ri => ri.Request)
                    .ThenInclude(r => r.User)
                .OrderBy(ri => ri.Request.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetPendingWithIssuerCountAsync()
        {
            return await _context.RequestItems
                .Where(ri => ri.Status == RequestItemStatus.PendingWithIssuer)
                .CountAsync();
        }

        public async Task<int> GetPendingWithAdminCountAsync()
        {
            return await _context.RequestItems
                .Where(ri => ri.Status == RequestItemStatus.PendingAdminApproval)
                .CountAsync();
        }

        public async Task<bool> AllItemsIssuedAsync(int requestId)
        {
            // Check if all items in the request have been issued (not PendingWithIssuer)
            var pendingCount = await _context.RequestItems
                .Where(ri => ri.RequestId == requestId && ri.Status == RequestItemStatus.PendingWithIssuer)
                .CountAsync();

            return pendingCount == 0;
        }

        public async Task<bool> AllItemsApprovedAsync(int requestId)
        {
            // Check if all items in the request have been approved (not PendingAdminApproval)
            var pendingCount = await _context.RequestItems
                .Where(ri => ri.RequestId == requestId && ri.Status == RequestItemStatus.PendingAdminApproval)
                .CountAsync();

            return pendingCount == 0;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
