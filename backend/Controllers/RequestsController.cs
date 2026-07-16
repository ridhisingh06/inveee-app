using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.Services;
using invmgmt.web.Utils;
using System;

namespace invmgmt.web.Controllers;

[Route("api/requests")]
[ApiController]
[Authorize(Roles = "USER,ISSUER,ADMIN")]
public sealed class RequestsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRequestService _requestService;
    private readonly IOrderSummaryService _orderSummaryService;
    private readonly ILogger<RequestsController> _logger;
    private readonly IConfiguration _configuration;

    public RequestsController(AppDbContext context, IRequestService requestService, IOrderSummaryService orderSummaryService, ILogger<RequestsController> logger, IConfiguration configuration)
    {
        _context = context;
        _requestService = requestService;
        _orderSummaryService = orderSummaryService;
        _logger = logger;
        _configuration = configuration;
    }

    // ==========================================
    // USER: Create + list + view + receive
    // ==========================================

    /// <summary>
    /// POST /api/requests
    /// User submits a request -> status = PendingWithIssuer, items stored in RequestItems.
    /// Rule: user cannot create a new request if any existing request is waiting with issuer/admin or approved.
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequestFromCartDto dto)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _requestService.CreateRequestAsync(userId, dto);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return CreatedAtAction(nameof(GetById), new { id = result.RequestId }, new { id = result.RequestId, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.Create");
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// GET /api/requests
    /// Role-based list:
    /// - USER  : own requests
    /// - ISSUER: all PendingWithIssuer requests
    /// - ADMIN : all PendingAdminApproval requests
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // Support either `page` or `pageNumber` (frontend/requirement differences).
            if (page.HasValue && page.Value > 0) pageNumber = page.Value;

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 100) pageSize = 100; // prevent accidental huge responses

            RequestStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var parsed))
                    return BadRequest(new { message = $"Invalid status '{status}'." });
                statusFilter = parsed;
            }

            var query = _context.Requests.AsNoTracking();
            var isUserRole = User.IsInRole("USER");
            var isIssuerRole = User.IsInRole("ISSUER");
            var isAdminRole = User.IsInRole("ADMIN");

            if (isUserRole)
            {
                var userId = User.GetUserId();
                query = query.Where(r => r.UserId == userId);

                if (statusFilter.HasValue)
                {
                    var requestedStatus = statusFilter.Value;
                    query = requestedStatus switch
                    {
                        RequestStatus.PendingWithIssuer => query.Where(r => r.Status == requestedStatus || r.RequestItems.Any(ri => ri.Status == RequestItemStatus.PendingWithIssuer)),
                        RequestStatus.NotIssued => query.Where(r => r.Status == requestedStatus || r.RequestItems.Any(ri => ri.Status == RequestItemStatus.NotIssued)),
                        RequestStatus.PendingAdminApproval => query.Where(r => r.Status == requestedStatus || r.RequestItems.Any(ri => ri.Status == RequestItemStatus.PendingAdminApproval)),
                        RequestStatus.Approved => query.Where(r => r.Status == requestedStatus || r.RequestItems.Any(ri => ri.Status == RequestItemStatus.Approved)),
                        RequestStatus.Rejected => query.Where(r => r.Status == requestedStatus || r.RequestItems.Any(ri => ri.Status == RequestItemStatus.Rejected)),
                        _ => query.Where(r => r.Status == requestedStatus)
                    };
                }

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
                        status = r.Status.ToString(),
                        createdAt = r.CreatedAt,
                        updatedAt = r.UpdatedAt,
                        items = r.RequestItems
                            .OrderBy(ri => ri.Id)
                            .Select(ri => new
                            {
                                id = ri.Id,
                                itemId = ri.ItemId,
                                itemName = ri.Item != null ? (ri.Item.Name ?? string.Empty) : string.Empty,
                                quantityRequested = ri.QuantityRequested,
                                quantityIssued = ri.QuantityIssued,
                                issuerIssuedQuantity = ri.IssuerIssuedQuantity != 0 ? ri.IssuerIssuedQuantity : ri.QuantityIssued,
                                issuerRejectedQuantity = ri.IssuerRejectedQuantity,
                                adminApprovedQuantity = ri.AdminApprovedQuantity != 0 ? ri.AdminApprovedQuantity : ri.QuantityApproved,
                                adminRejectedQuantity = ri.AdminRejectedQuantity,
                                receivedQuantity = ri.ReceivedQuantity,
                                status = ri.Status.ToString()
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(new { total, totalPages, currentPage = pageNumber, data });
            }

            if (isIssuerRole)
            {
                // ISSUERs may query PendingWithIssuer (items waiting for them)
                // OR Approved (items already admin-approved, for dispatch tracking).
                // Any other status is outside their workflow scope.
                var issuerAllowedStatuses = new[]
                {
                    RequestStatus.PendingWithIssuer,
                    RequestStatus.Approved
                };

                if (statusFilter.HasValue && !issuerAllowedStatuses.Contains(statusFilter.Value))
                    return BadRequest(new { message = $"ISSUER can only query status=PendingWithIssuer or status=Approved. Got: '{status}'." });

                if (!statusFilter.HasValue || statusFilter.Value == RequestStatus.PendingWithIssuer)
                {
                    query = query.Where(r => r.RequestItems.Any(ri => ri.Status == RequestItemStatus.PendingWithIssuer));
                }
                else // Approved
                {
                    query = query.Where(r => r.Status == RequestStatus.Approved);
                }
            }
            else // ADMIN
            {
                if (statusFilter.HasValue && statusFilter.Value != RequestStatus.PendingAdminApproval)
                    return BadRequest(new { message = "ADMIN can only query status=PendingAdminApproval." });

                query = query.Where(r => r.RequestItems.Any(ri => ri.Status == RequestItemStatus.PendingAdminApproval));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var pattern = $"%{q.Trim()}%";
                query = query.Where(r =>
                    (r.User != null && (
                        EF.Functions.ILike(r.User.Username ?? string.Empty, pattern) ||
                        EF.Functions.ILike(r.User.Email ?? string.Empty, pattern)
                    ))
                    || r.RequestItems.Any(ri => ri.Item != null && EF.Functions.ILike(ri.Item.Name ?? string.Empty, pattern))
                );
            }

            var total2 = await query.CountAsync();
            var totalPages2 = (int)Math.Ceiling(total2 / (double)pageSize);

            // DTO projection (no Includes): only bring the columns we need for the dashboard.
            var data2 = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    id = r.Id,
                    userId = r.UserId,
                    username = r.User != null ? (r.User.Username ?? string.Empty) : string.Empty,
                    email = r.User != null ? (r.User.Email ?? string.Empty) : string.Empty,
                    status = r.Status.ToString(),
                    createdAt = r.CreatedAt,
                    updatedAt = r.UpdatedAt,
                    items = r.RequestItems
                        .OrderBy(ri => ri.Id)
                        .Where(ri => isUserRole
                            || isIssuerRole && ri.Status == RequestItemStatus.PendingWithIssuer
                            || isAdminRole && ri.Status == RequestItemStatus.PendingAdminApproval)
                        .Select(ri => new
                        {
                            id = ri.Id,
                            itemId = ri.ItemId,
                            itemName = ri.Item != null ? (ri.Item.Name ?? string.Empty) : string.Empty,
                            quantityRequested = ri.QuantityRequested,
                            quantityIssued = ri.QuantityIssued,
                            status = ri.Status.ToString()
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(new { total = total2, totalPages = totalPages2, currentPage = pageNumber, data = data2 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.Get");
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// GET /api/requests/my (legacy-friendly alias for the user list)
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMy([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId();
        var data = await _requestService.GetUserRequestsAsync(userId, pageNumber, pageSize);
        return Ok(data);
    }

    /// <summary>
    /// GET /api/requests/{id}
    /// Request detail (USER/ISSUER/ADMIN); USER can only view own request.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            int? userId = null;
            string role = "USER";

            if (User.IsInRole("ADMIN")) role = "ADMIN";
            else if (User.IsInRole("ISSUER")) role = "ISSUER";
            else userId = User.GetUserId();

            var dto = await _requestService.GetRequestByIdAsync(id, userId, role);
            if (dto == null) return NotFound(new { message = "Request not found or access denied" });
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.GetById");
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// GET /api/requests/{id}/editable
    /// Returns whether the request can still be edited by the current user.
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpGet("{id:int}/editable")]
    public async Task<IActionResult> GetEditable(int id)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _requestService.IsRequestEditableAsync(id, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.GetEditable (RequestId={RequestId})", id);
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// PUT /api/requests/{id}
    /// User updates an existing PendingWithIssuer request before the Issuer starts processing.
    /// Validates ownership, status, and that no issuer processing has begun.
    /// Upserts / deletes RequestItems as needed. Keeps the same RequestId.
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();
            var result = await _requestService.UpdateRequestAsync(id, userId, dto);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "NOT_FOUND"    => NotFound(new { message = result.Message }),
                    "FORBIDDEN"    => StatusCode(403, new { message = result.Message }),
                    "SERVER_ERROR" => StatusCode(500, new { message = result.Message }),
                    _              => BadRequest(new { message = result.Message })    // BAD_REQUEST / default
                };
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.Update (RequestId={RequestId})", id);
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// GET /api/requests/can-request
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpGet("can-request")]
    public async Task<IActionResult> CanRequest()
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _requestService.CheckCanRequestAsync(userId);
            return Ok(new { canRequest = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.CanRequest");
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// GET /api/requests/{id}/receipt
    /// Generate professional order receipt for user viewing
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpGet("{id:int}/receipt")]
    public async Task<IActionResult> GetReceipt(int id)
    {
        try
        {
            var userId = User.GetUserId();
            var request = await _context.Requests
                .Include(r => r.User)
                .Include(r => r.RequestItems)
                .ThenInclude(ri => ri.Item)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (request == null)
                return NotFound(new { message = "Request not found or access denied" });

            var issuer = await _context.Users.FindAsync(request.IssuedBy);
            var admin = await _context.Users.FindAsync(request.ApprovedBy);

            var items = request.RequestItems.Select(ri => new OrderReceiptItemDto
            {
                ItemName = ri.Item?.Name ?? "Unknown",
                RequestedQty = ri.QuantityRequested,
                IssuerIssued = ri.IssuerIssuedQuantity,
                IssuerRejected = ri.IssuerRejectedQuantity,
                AdminApproved = ri.AdminApprovedQuantity,
                AdminRejected = ri.AdminRejectedQuantity,
                FinalReceiveQty = ri.AdminApprovedQuantity // Final received is what admin approved
            }).ToList();

            var summary = new OrderReceiptSummaryDto
            {
                TotalRequested = items.Sum(i => i.RequestedQty),
                TotalIssuerApproved = items.Sum(i => i.IssuerIssued),
                TotalIssuerRejected = items.Sum(i => i.IssuerRejected),
                TotalAdminApproved = items.Sum(i => i.AdminApproved),
                TotalAdminRejected = items.Sum(i => i.AdminRejected),
                TotalFinalReceived = items.Sum(i => i.FinalReceiveQty)
            };

            // Generate remarks based on the workflow
            var remarks = new List<string>();
            if (summary.TotalIssuerRejected > 0)
                remarks.Add("Some items were rejected due to insufficient stock.");
            if (summary.TotalAdminRejected > 0)
                remarks.Add("Some items were rejected during admin approval.");
            if (summary.TotalFinalReceived > 0)
                remarks.Add("Only admin-approved items are available for collection.");

            var receipt = new OrderReceiptDto
            {
                id = request.Id,
                OrderNumber = $"ORD-{request.Id:D6}",
                RequestDate = request.CreatedAt,
                IssuedDate = request.IssuedDate,
                ApprovedDate = request.ApprovedDate,
                CurrentStatus = request.Status.ToString(),
                IssuerName = issuer?.Username ?? "Not assigned",
                AdminName = admin?.Username ?? "Not assigned",
                UserName = request.User?.Username ?? "Unknown",
                Items = items,
                Summary = summary,
                Remarks = string.Join(" ", remarks),
                GeneratedOn = DateTime.UtcNow
            };

            return Ok(receipt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.GetReceipt");
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// PATCH /api/requests/{id}/receive
    /// User confirms they received items -> RECEIVED (only if APPROVED).
    /// Creates OrderSummary and updates status, received date, and received by without modifying inventory.
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpPatch("{id:int}/receive")]
    public async Task<IActionResult> Receive(int id)
    {
        try
        {
            var userId = User.GetUserId();
            
            // Use OrderSummaryService to handle the complete receive workflow
            var result = await _orderSummaryService.CreateOrderSummaryAsync(id, userId);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new 
            { 
                message = result.Message,
                requestId = result.RequestId,
                orderSummaryId = result.OrderSummaryId,
                receivedDate = result.ReceivedDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.Receive (RequestId={RequestId})", id);
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// PATCH /api/requests/{requestId}/items/{requestItemId}/receive
    /// User confirms they received a single approved item -> Received.
    /// If all items reach a terminal state, the whole request becomes Received.
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpPatch("{requestId:int}/items/{requestItemId:int}/receive")]
    public async Task<IActionResult> ReceiveItem(int requestId, int requestItemId)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult? earlyReturn = null;
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var userId = User.GetUserId();
                var request = await _context.Requests
                    .Include(r => r.RequestItems)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null || request.UserId != userId)
                { earlyReturn = NotFound(new { message = "Request not found" }); return; }

                var requestItem = request.RequestItems.FirstOrDefault(ri => ri.Id == requestItemId);
                if (requestItem == null)
                { earlyReturn = NotFound(new { message = "Request item not found" }); return; }

                if (requestItem.Status != RequestItemStatus.Approved)
                { earlyReturn = BadRequest(new { message = $"Only approved items can be marked as received. Current status: {requestItem.Status}" }); return; }

                requestItem.Status = RequestItemStatus.Received;
                RecalculateRequestStatus(request);
                request.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
            return earlyReturn ?? Ok(new { message = "Item marked as received" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.ReceiveItem (RequestId={RequestId}, RequestItemId={RequestItemId})", requestId, requestItemId);
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// DELETE /api/requests/{id}
    /// User can cancel/delete only PendingWithIssuer (and legacy PENDING) requests.
    /// </summary>


    [Authorize(Roles = "USER")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _requestService.DeleteRequestAsync(id, userId);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(new { message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.Delete");
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    // ==========================================
    // ISSUER: issue / reject
    // ==========================================

    /// <summary>
    /// PATCH /api/requests/{id}/issue
    /// Issuer issues a PendingWithIssuer request -> PendingAdminApproval and reduces stock.
    /// </summary>
    [Authorize(Roles = "ISSUER")]
    [HttpPatch("{id:int}/issue")]
    public async Task<IActionResult> Issue(int id)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult? earlyReturn = null;
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var issuerId = User.GetUserId();
                var request = await _context.Requests
                    .Include(r => r.RequestItems)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null) { earlyReturn = NotFound(new { message = "Request not found" }); return; }
                if (request.Status != RequestStatus.PendingWithIssuer)
                { earlyReturn = BadRequest(new { message = $"Only requests pending with issuer can be issued. Current status: {request.Status}" }); return; }

                var oldStatus = request.Status;
                foreach (var item in request.RequestItems)
                {
                    if (item.Status != RequestItemStatus.PendingWithIssuer)
                        continue;

                    var stock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.ItemId == item.ItemId);
                    if (stock == null)
                    { earlyReturn = BadRequest(new { message = $"Stock record not found for ItemId={item.ItemId}" }); return; }

                    var qty = item.QuantityRequested;
                    if (stock.AvailableQuantity < qty)
                    { earlyReturn = BadRequest(new { message = $"Insufficient stock for ItemId={item.ItemId}. Available: {stock.AvailableQuantity}, Required: {qty}" }); return; }

                    stock.AvailableQuantity -= qty;
                    item.QuantityIssued = qty;
                    item.QuantityApproved = qty;
                    item.Status = RequestItemStatus.PendingAdminApproval;
                }

                RecalculateRequestStatus(request);
                request.UpdatedAt = DateTime.UtcNow;
                _context.IssueLogs.Add(new IssueLog
                {
                    RequestId = request.Id,
                    IssuedBy = issuerId,
                    UserId = issuerId,
                    IssuedDate = DateTime.UtcNow
                });
                _context.ApprovalLogs.Add(new ApprovalLog
                {
                    RequestId = request.Id,
                    ApprovedBy = issuerId,
                    UserId = issuerId,
                    Status = $"{oldStatus} -> {request.Status}",
                    Remarks = "Issued by issuer; pending admin approval",
                    ActionDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
            return earlyReturn ?? Ok(new { message = "Items issued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.Issue (RequestId={Id})", id);
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// PATCH /api/requests/{requestId}/items/{requestItemId}/issue
    /// Issuer issues one request item -> PendingAdminApproval and reduces stock.
    /// </summary>
    [Authorize(Roles = "ISSUER,ADMIN")]
    [HttpPatch("{requestId:int}/items/{requestItemId:int}/issue")]
    public async Task<IActionResult> IssueItem(int requestId, int requestItemId)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult? earlyReturn = null;
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var issuerId = User.GetUserId();
                var request = await _context.Requests
                    .Include(r => r.RequestItems)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null) { earlyReturn = NotFound(new { message = "Request not found" }); return; }

                var requestItem = request.RequestItems.FirstOrDefault(ri => ri.Id == requestItemId);
                if (requestItem == null) { earlyReturn = NotFound(new { message = "Request item not found" }); return; }

                if (requestItem.Status != RequestItemStatus.PendingWithIssuer)
                { earlyReturn = BadRequest(new { message = $"Only items pending with issuer can be issued. Current status: {requestItem.Status}" }); return; }

                var stock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.ItemId == requestItem.ItemId);
                if (stock == null)
                { earlyReturn = BadRequest(new { message = $"Stock record not found for ItemId={requestItem.ItemId}" }); return; }

                if (stock.AvailableQuantity < requestItem.QuantityRequested)
                { earlyReturn = BadRequest(new { message = $"Insufficient stock for ItemId={requestItem.ItemId}. Available: {stock.AvailableQuantity}, Required: {requestItem.QuantityRequested}" }); return; }

                var oldItemStatus = requestItem.Status;
                stock.AvailableQuantity -= requestItem.QuantityRequested;
                requestItem.QuantityIssued = requestItem.QuantityRequested;
                requestItem.QuantityApproved = requestItem.QuantityRequested;
                requestItem.Status = RequestItemStatus.PendingAdminApproval;

                RecalculateRequestStatus(request);
                request.UpdatedAt = DateTime.UtcNow;
                _context.IssueLogs.Add(new IssueLog
                {
                    RequestId = request.Id,
                    IssuedBy = issuerId,
                    UserId = issuerId,
                    IssuedDate = DateTime.UtcNow
                });
                _context.ApprovalLogs.Add(new ApprovalLog
                {
                    RequestId = request.Id,
                    ApprovedBy = issuerId,
                    UserId = issuerId,
                    Status = $"Item {requestItem.Id}: {oldItemStatus} -> {requestItem.Status}",
                    Remarks = "Item issued by issuer; pending admin approval",
                    ActionDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
            return earlyReturn ?? Ok(new { message = "Issued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.IssueItem (RequestId={RequestId}, RequestItemId={RequestItemId})", requestId, requestItemId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// PATCH /api/requests/{requestId}/items/{requestItemId}/not-issue
    /// Issuer marks one request item as NotIssued.
    /// </summary>
    [Authorize(Roles = "ISSUER")]
    [HttpPatch("{requestId:int}/items/{requestItemId:int}/not-issue")]
    public async Task<IActionResult> NotIssueItem(int requestId, int requestItemId)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult? earlyReturn = null;
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var issuerId = User.GetUserId();
                var request = await _context.Requests
                    .Include(r => r.RequestItems)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null) { earlyReturn = NotFound(new { message = "Request not found" }); return; }

                var requestItem = request.RequestItems.FirstOrDefault(ri => ri.Id == requestItemId);
                if (requestItem == null) { earlyReturn = NotFound(new { message = "Request item not found" }); return; }

                if (requestItem.Status != RequestItemStatus.PendingWithIssuer)
                { earlyReturn = BadRequest(new { message = $"Only items pending with issuer can be marked not issued. Current status: {requestItem.Status}" }); return; }

                var oldItemStatus = requestItem.Status;
                requestItem.Status = RequestItemStatus.NotIssued;
                requestItem.QuantityIssued = 0;
                requestItem.QuantityApproved = 0;

                RecalculateRequestStatus(request);
                request.UpdatedAt = DateTime.UtcNow;
                _context.ApprovalLogs.Add(new ApprovalLog
                {
                    RequestId = request.Id,
                    ApprovedBy = issuerId,
                    UserId = issuerId,
                    Status = $"Item {requestItem.Id}: {oldItemStatus} -> {requestItem.Status}",
                    Remarks = "Item not issued by issuer",
                    ActionDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
            return earlyReturn ?? Ok(new { message = "Item marked as not issued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.NotIssueItem (RequestId={RequestId}, RequestItemId={RequestItemId})", requestId, requestItemId);
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    // ==========================================
    // ADMIN: approve / reject
    // ==========================================

    /// <summary>
    /// PATCH /api/requests/{id}/approve
    /// Admin final-approves a PendingAdminApproval request -> Approved.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPatch("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult? earlyReturn = null;
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var adminId = User.GetUserId();
                var request = await _context.Requests.FirstOrDefaultAsync(r => r.Id == id);
                if (request == null) { earlyReturn = NotFound(new { message = "Request not found" }); return; }

                if (request.Status == RequestStatus.Rejected || request.Status == RequestStatus.Received)
                { earlyReturn = BadRequest(new { message = $"Cannot approve a '{request.Status}' request" }); return; }

                if (request.Status == RequestStatus.Approved)
                { earlyReturn = Ok(new { message = "Request already approved" }); return; }

                if (request.Status != RequestStatus.PendingAdminApproval)
                { earlyReturn = BadRequest(new { message = $"Only requests pending admin approval can be approved by admin. Current status: {request.Status}" }); return; }

                var oldStatus = request.Status;
                var requestItems = await _context.RequestItems.Where(ri => ri.RequestId == request.Id).ToListAsync();
                foreach (var requestItem in requestItems.Where(ri => ri.Status == RequestItemStatus.PendingAdminApproval))
                {
                    requestItem.Status = RequestItemStatus.Approved;
                }

                RecalculateRequestStatus(request, requestItems);
                request.UpdatedAt = DateTime.UtcNow;
                _context.ApprovalLogs.Add(new ApprovalLog
                {
                    RequestId = request.Id,
                    ApprovedBy = adminId,
                    UserId = adminId,
                    Status = $"{oldStatus} -> {request.Status}",
                    Remarks = "Approved by admin",
                    ActionDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
            return earlyReturn ?? Ok(new { message = "Request approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.Approve (RequestId={Id})", id);
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// PATCH /api/requests/{requestId}/items/{requestItemId}/approve
    /// Admin approves one issued request item.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPatch("{requestId:int}/items/{requestItemId:int}/approve")]
    public async Task<IActionResult> ApproveItem(int requestId, int requestItemId)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult? earlyReturn = null;
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var adminId = User.GetUserId();
                var request = await _context.Requests
                    .Include(r => r.RequestItems)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null) { earlyReturn = NotFound(new { message = "Request not found" }); return; }

                var requestItem = request.RequestItems.FirstOrDefault(ri => ri.Id == requestItemId);
                if (requestItem == null) { earlyReturn = NotFound(new { message = "Request item not found" }); return; }

                if (requestItem.Status != RequestItemStatus.PendingAdminApproval)
                { earlyReturn = BadRequest(new { message = $"Only items pending admin approval can be approved. Current status: {requestItem.Status}" }); return; }

                var oldItemStatus = requestItem.Status;
                requestItem.Status = RequestItemStatus.Approved;
                RecalculateRequestStatus(request);
                request.UpdatedAt = DateTime.UtcNow;
                _context.ApprovalLogs.Add(new ApprovalLog
                {
                    RequestId = request.Id,
                    ApprovedBy = adminId,
                    UserId = adminId,
                    Status = $"Item {requestItem.Id}: {oldItemStatus} -> {requestItem.Status}",
                    Remarks = "Item approved by admin",
                    ActionDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
            return earlyReturn ?? Ok(new { message = "Item approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.ApproveItem (RequestId={RequestId}, RequestItemId={RequestItemId})", requestId, requestItemId);
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// PATCH /api/requests/{requestId}/items/{requestItemId}/reject
    /// Admin rejects one issued request item.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPatch("{requestId:int}/items/{requestItemId:int}/reject")]
    public async Task<IActionResult> RejectItem(int requestId, int requestItemId)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult? earlyReturn = null;
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var adminId = User.GetUserId();
                var request = await _context.Requests
                    .Include(r => r.RequestItems)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null) { earlyReturn = NotFound(new { message = "Request not found" }); return; }

                var requestItem = request.RequestItems.FirstOrDefault(ri => ri.Id == requestItemId);
                if (requestItem == null) { earlyReturn = NotFound(new { message = "Request item not found" }); return; }

                if (requestItem.Status != RequestItemStatus.PendingAdminApproval)
                { earlyReturn = BadRequest(new { message = $"Only items pending admin approval can be rejected. Current status: {requestItem.Status}" }); return; }

                var oldItemStatus = requestItem.Status;
                requestItem.Status = RequestItemStatus.Rejected;
                RecalculateRequestStatus(request);
                request.UpdatedAt = DateTime.UtcNow;
                _context.ApprovalLogs.Add(new ApprovalLog
                {
                    RequestId = request.Id,
                    ApprovedBy = adminId,
                    UserId = adminId,
                    Status = $"Item {requestItem.Id}: {oldItemStatus} -> {requestItem.Status}",
                    Remarks = "Item rejected by admin",
                    ActionDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
            return earlyReturn ?? Ok(new { message = "Item rejected" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.RejectItem (RequestId={RequestId}, RequestItemId={RequestItemId})", requestId, requestItemId);
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    /// <summary>
    /// PATCH /api/requests/{id}/reject
    /// - ISSUER can mark PendingWithIssuer requests as NotIssued
    /// - ADMIN  can reject PendingAdminApproval requests
    /// </summary>
    [Authorize(Roles = "ISSUER,ADMIN")]
    [HttpPatch("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult? earlyReturn = null;
            var isIssuer = User.IsInRole("ISSUER");
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var actionBy = User.GetUserId();
                var request = await _context.Requests.FirstOrDefaultAsync(r => r.Id == id);
                if (request == null) { earlyReturn = NotFound(new { message = "Request not found" }); return; }

                if (request.Status == RequestStatus.Rejected || request.Status == RequestStatus.NotIssued)
                { earlyReturn = Ok(new { message = "Request already closed" }); return; }

                var oldStatus = request.Status;
                if (isIssuer)
                {
                    var requestItems = await _context.RequestItems.Where(ri => ri.RequestId == request.Id).ToListAsync();
                    if (!requestItems.Any(ri => ri.Status == RequestItemStatus.PendingWithIssuer))
                    { earlyReturn = BadRequest(new { message = $"Only requests pending with issuer can be marked not issued. Current status: {request.Status}" }); return; }

                    foreach (var requestItem in requestItems.Where(ri => ri.Status == RequestItemStatus.PendingWithIssuer))
                    {
                        requestItem.Status = RequestItemStatus.NotIssued;
                        requestItem.QuantityIssued = 0;
                        requestItem.QuantityApproved = 0;
                    }

                    RecalculateRequestStatus(request, requestItems);
                }
                else // ADMIN
                {
                    var requestItems = await _context.RequestItems.Where(ri => ri.RequestId == request.Id).ToListAsync();
                    if (!requestItems.Any(ri => ri.Status == RequestItemStatus.PendingAdminApproval))
                    { earlyReturn = BadRequest(new { message = $"Only requests pending admin approval can be rejected by the admin. Current status: {request.Status}" }); return; }

                    foreach (var requestItem in requestItems.Where(ri => ri.Status == RequestItemStatus.PendingAdminApproval))
                    {
                        requestItem.Status = RequestItemStatus.Rejected;
                    }

                    RecalculateRequestStatus(request, requestItems);
                }

                request.UpdatedAt = DateTime.UtcNow;
                _context.ApprovalLogs.Add(new ApprovalLog
                {
                    RequestId = request.Id,
                    ApprovedBy = actionBy,
                    UserId = actionBy,
                    Status = $"{oldStatus} -> {request.Status}",
                    Remarks = isIssuer ? "Not issued by issuer" : "Rejected by admin",
                    ActionDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
            return earlyReturn ?? Ok(new { message = isIssuer ? "Request marked as not issued" : "Request rejected" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RequestsController.Reject (RequestId={Id})", id);
            return StatusCode(500, new { message = "An internal server error occurred." });
        }
    }

    private static void RecalculateRequestStatus(Request request)
        => RecalculateRequestStatus(request, request.RequestItems);

    private static void RecalculateRequestStatus(Request request, IEnumerable<RequestItem> requestItems)
    {
        var itemStatuses = requestItems.Select(ri => ri.Status).ToList();
        if (itemStatuses.Count == 0)
        {
            request.Status = RequestStatus.PendingWithIssuer;
            return;
        }

        if (itemStatuses.Any(status => status == RequestItemStatus.PendingWithIssuer))
        {
            request.Status = RequestStatus.PendingWithIssuer;
            return;
        }

        if (itemStatuses.Any(status => status == RequestItemStatus.PendingAdminApproval))
        {
            request.Status = RequestStatus.PendingAdminApproval;
            return;
        }

        if (itemStatuses.All(status => status == RequestItemStatus.NotIssued))
        {
            request.Status = RequestStatus.NotIssued;
            return;
        }

        if (itemStatuses.All(status => status == RequestItemStatus.Received || status == RequestItemStatus.NotIssued || status == RequestItemStatus.Rejected))
        {
            // All items are in a terminal state
            if (itemStatuses.All(status => status == RequestItemStatus.Received || status == RequestItemStatus.NotIssued))
            {
                request.Status = RequestStatus.Received;
                return;
            }
            request.Status = RequestStatus.Rejected;
            return;
        }

        if (itemStatuses.All(status => status == RequestItemStatus.Approved || status == RequestItemStatus.NotIssued || status == RequestItemStatus.Received))
        {
            request.Status = RequestStatus.Approved;
            return;
        }

        request.Status = RequestStatus.Rejected;
    }

    /// <summary>
    /// GET /api/requests/orders
    /// Get user's order history (paginated)
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrderHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.GetUserId();

            _logger.LogInformation("Fetching order history for UserId={UserId}, Page={Page}, PageSize={PageSize}",
                userId, pageNumber, pageSize);

            var result = await _orderSummaryService.GetUserOrdersAsync(userId, pageNumber, pageSize);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order history");
            return StatusCode(500, new { message = "Error fetching order history", error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/requests/orders/by-request/{requestId}
    /// Get order summary by original request ID.
    /// IMPORTANT: This route MUST be declared before orders/{id:int} so that
    /// the literal segment "by-request" is matched first and is not swallowed
    /// by the {id:int} integer-constraint route.
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpGet("orders/by-request/{requestId:int}")]
    public async Task<IActionResult> GetOrderSummaryByRequest(int requestId)
    {
        try
        {
            var userId = User.GetUserId();

            _logger.LogInformation("Fetching order summary by RequestId={RequestId}, UserId={UserId}", requestId, userId);

            var orderSummary = await _orderSummaryService.GetOrderSummaryByRequestAsync(requestId);

            if (orderSummary == null)
            {
                _logger.LogWarning("Order summary not found for RequestId={RequestId}", requestId);
                return NotFound(new { message = $"No order summary found for request {requestId}. The user may not have confirmed receipt yet." });
            }

            // Verify user owns this order
            if (orderSummary.UserId != userId)
            {
                _logger.LogWarning("Unauthorized access to order summary: RequestId={RequestId}, UserId={UserId}, ActualUserId={ActualUserId}",
                    requestId, userId, orderSummary.UserId);
                return Forbid();
            }

            return Ok(orderSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order summary by RequestId={RequestId}", requestId);
            return StatusCode(500, new { message = "Error fetching order summary", error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpOptions("orders/by-request/{requestId:int}")]
    public IActionResult OptionsOrderByRequest()
    {
        return Ok();
    }

    /// <summary>
    /// GET /api/requests/orders/{id}
    /// Get complete order summary details (receipt-style)
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> GetOrderSummary(int id)
    {
        try
        {
            var userId = User.GetUserId();

            _logger.LogInformation("Fetching order summary: Id={Id}, UserId={UserId}", id, userId);

            var orderSummary = await _orderSummaryService.GetOrderSummaryByIdAsync(id);

            if (orderSummary == null)
            {
                _logger.LogWarning("Order summary not found: Id={Id}", id);
                return NotFound(new { message = "Order summary not found" });
            }

            // Verify user owns this order
            if (orderSummary.UserId != userId)
            {
                _logger.LogWarning("Unauthorized access to order summary: Id={Id}, UserId={UserId}, ActualUserId={ActualUserId}",
                    id, userId, orderSummary.UserId);
                return Forbid();
            }

            return Ok(orderSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order summary: Id={Id}", id);
            return StatusCode(500, new { message = "Error fetching order summary", error = ex.Message });
        }
    }



    /// <summary>
    /// GET /api/requests/{id}/reorderable-items
    /// Get list of items that can be reordered due to issuer rejection
    /// </summary>
    [Authorize(Roles = "USER,ISSUER,ADMIN")]
    [HttpGet("{id:int}/reorderable-items")]
    public async Task<IActionResult> GetReorderableItems(int id)
    {
        try
        {
            var isFeatureEnabled = _configuration.GetValue<bool>("FeatureFlags:Reorder");
            if (!isFeatureEnabled)
            {
                return NotFound(new { message = "Reorder feature is currently disabled." });
            }

            var userId = User.GetUserId();
            var role = User.IsInRole("ADMIN") ? "ADMIN" : (User.IsInRole("ISSUER") ? "ISSUER" : "USER");

            _logger.LogInformation("Fetching reorderable items for RequestId={RequestId} by UserId={UserId}", id, userId);

            // Verify access
            var request = await _requestService.GetRequestByIdAsync(id, userId, role);
            if (request == null)
            {
                return NotFound(new { message = "Request not found or access denied." });
            }

            var suggestions = await _orderSummaryService.GetReorderableItemsAsync(id);
            if (suggestions == null || !suggestions.Any())
            {
                return Ok(new List<ReorderSuggestion>());
            }

            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reorderable items for RequestId={RequestId}", id);
            return StatusCode(500, new { message = "Error fetching reorderable items", error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpOptions("{id:int}/reorderable-items")]
    public IActionResult OptionsReorderableItems()
    {
        return Ok();
    }

    /// <summary>
    /// GET /api/requests/order-stats
    /// Get user's order statistics (for dashboard)
    /// </summary>
    [Authorize(Roles = "USER")]
    [HttpGet("order-stats")]
    public async Task<IActionResult> GetOrderStatistics()
    {
        try
        {
            var userId = User.GetUserId();

            _logger.LogInformation("Fetching order statistics for UserId={UserId}", userId);

            var stats = await _orderSummaryService.GetUserStatisticsAsync(userId);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order statistics");
            return StatusCode(500, new { message = "Error fetching order statistics", error = ex.Message });
        }
    }
}
