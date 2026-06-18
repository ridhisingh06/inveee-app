using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace invmgmt.web.Controllers
{
    [Route("api/issuer")]
    [ApiController]
    [Authorize(Roles = "ISSUER")]
    public class IssuerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<IssuerController> _logger;

        public IssuerController(AppDbContext context, ILogger<IssuerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/issuer/requests
        /// Returns paginated list of REQUESTED requests for the Issuer to process.
        /// Status flow: REQUESTED (User) → ISSUED (Issuer) → APPROVED (Admin)
        /// </summary>
        [HttpGet("requests")]
        public async Task<IActionResult> GetRequestedRequests(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Issuer fetching REQUESTED requests (page={Page}, size={Size})", pageNumber, pageSize);

                var query = _context.Requests
                    .AsNoTracking()
                    .Where(r => r.Status == RequestStatus.PendingWithIssuer);

                var total = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(total / (double)pageSize);

                var data = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        id = r.Id,
                        userId = r.UserId,
                        username = r.User != null ? r.User.Username : string.Empty,
                        email = r.User != null ? r.User.Email : string.Empty,
                        status = r.Status.ToString(),
                        createdAt = r.CreatedAt,
                        updatedAt = r.UpdatedAt,
                        items = r.RequestItems.Select(ri => new
                        {
                            id = ri.Id,
                            itemId = ri.ItemId,
                            itemName = ri.Item != null ? ri.Item.Name : string.Empty,
                            quantityRequested = ri.QuantityRequested
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new { total, totalPages, currentPage = pageNumber, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching requested requests");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/issuer/stats
        /// Returns counts for issuer dashboard stats cards.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var requestedCount  = await _context.Requests.CountAsync(r => r.Status == RequestStatus.PendingWithIssuer);
                var issuedCount     = await _context.Requests.CountAsync(r => r.Status == RequestStatus.PendingAdminApproval);
                var totalStockItems = await _context.Items.CountAsync(i => i.IsActive);
                return Ok(new { requestedCount, issuedCount, totalStockItems });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching issuer stats");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/issuer/request/{id}/issue
        /// Transitions a REQUESTED request to ISSUED, reduces stock.
        /// </summary>
        [HttpPut("request/{id}/issue")]
        public async Task<IActionResult> IssueRequest(int id)
            => await IssueInternal(id);

        /// <summary>
        /// POST /api/issuer/issue/{id}  (legacy route kept for backwards compat)
        /// </summary>
        [HttpPost("issue/{id}")]
        public async Task<IActionResult> IssueLegacy(int id)
            => await IssueInternal(id);

        private async Task<IActionResult> IssueInternal(int id)
        {
            try
            {
                _logger.LogInformation("Issuer issuing request: RequestId={Id}", id);

                await using var transaction = await _context.Database.BeginTransactionAsync();
                var issuerId = User.GetUserId();
                var request = await _context.Requests
                    .Include(r => r.RequestItems)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                    return NotFound(new { message = "Request not found" });

                // Workflow: only REQUESTED → ISSUED is allowed at Issuer stage
                if (request.Status != RequestStatus.PendingWithIssuer)
                    return BadRequest(new { message = $"Only requests pending with issuer can be issued. Current status: {request.Status}" });

                var oldStatus = request.Status;
                foreach (var item in request.RequestItems)
                {
                    var stock = await _context.InventoryStocks
                        .FirstOrDefaultAsync(s => s.ItemId == item.ItemId);

                    if (stock == null)
                        return BadRequest(new { message = $"Stock record not found for ItemId={item.ItemId}" });

                    var qty = item.QuantityRequested;
                    if (stock.AvailableQuantity < qty)
                        return BadRequest(new { message = $"Insufficient stock for ItemId={item.ItemId}. Available: {stock.AvailableQuantity}, Required: {qty}" });

                    stock.AvailableQuantity -= qty;
                    item.QuantityIssued   = qty;
                    item.QuantityApproved = qty;
                }

                request.Status    = RequestStatus.PendingAdminApproval;
                request.UpdatedAt = DateTime.UtcNow;
                _context.IssueLogs.Add(new IssueLog
                {
                    RequestId = request.Id,
                    IssuedBy = issuerId,
                    IssuedDate = DateTime.UtcNow
                });
                _context.ApprovalLogs.Add(new ApprovalLog
                {
                    RequestId = request.Id,
                    ApprovedBy = issuerId,
                    Status = $"{oldStatus} -> {request.Status}",
                    Remarks = "Issued by issuer; pending admin approval",
                    ActionDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Request issued successfully: RequestId={Id}", id);
                return Ok(new { message = "Items issued successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error issuing request: RequestId={Id}", id);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/issuer/request/{id}/reject
        /// Issuer can reject a REQUESTED request.
        /// </summary>
        [HttpPut("request/{id}/reject")]
        public async Task<IActionResult> RejectRequest(int id)
            => await RejectInternal(id);

        /// <summary>
        /// POST /api/issuer/reject/{id}  (legacy route)
        /// </summary>
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectLegacy(int id)
            => await RejectInternal(id);

        private async Task<IActionResult> RejectInternal(int id)
        {
            try
            {
                _logger.LogInformation("Issuer rejecting request: RequestId={Id}", id);

                await using var transaction = await _context.Database.BeginTransactionAsync();
                var issuerId = User.GetUserId();
                var request = await _context.Requests.FindAsync(id);
                if (request == null)
                    return NotFound(new { message = "Request not found" });

                if (request.Status != RequestStatus.PendingWithIssuer)
                    return BadRequest(new { message = $"Only requests pending with issuer can be marked not issued. Current status: {request.Status}" });

                var oldStatus = request.Status;
                request.Status    = RequestStatus.NotIssued;
                request.UpdatedAt = DateTime.UtcNow;
                _context.ApprovalLogs.Add(new ApprovalLog
                {
                    RequestId = request.Id,
                    ApprovedBy = issuerId,
                    Status = $"{oldStatus} -> {request.Status}",
                    Remarks = "Not issued by issuer",
                    ActionDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Request marked not issued by issuer: RequestId={Id}", id);
                return Ok(new { message = "Request marked as not issued" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request: RequestId={Id}", id);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}
