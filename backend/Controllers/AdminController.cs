using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Utils;
using invmgmt.web.Models.Enums;
using invmgmt.web.Services;
using System.Linq;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IApprovalService _approvalService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext context, IApprovalService approvalService, ILogger<AdminController> logger)
    {
        _context = context;
        _approvalService = approvalService;
        _logger = logger;
    }

    [HttpGet("pending-users")]
    public async Task<IActionResult> GetPendingUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int limit = 50,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        try
        {
            int actualPage = pageNumber ?? page;
            int actualLimit = pageSize ?? limit;

            _logger.LogInformation("Pending users requested (Page: {Page}, Limit: {Limit})", actualPage, actualLimit);

            var query = _context.RegistrationRequests
                .Where(x => x.Status == RegistrationStatus.Pending);

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)actualLimit);

            var pendingUsers = await query
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Skip((actualPage - 1) * actualLimit)
                .Take(actualLimit)
                .Include(x => x.Role)
                .Select(x => new PendingRegistrationDto
                {
                    Id = x.Id,
                    Username = x.Username,
                    Email = x.Email,
                    Role = x.Role != null ? x.Role.Name : string.Empty,
                    RoleId = x.RoleId,
                    Designation = x.Designation,
                    Department = x.Department != null ? x.Department.Name : string.Empty,
                    DepartmentId = x.DepartmentId,
                    Status = x.Status.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return Ok(new 
            {
                totalRecords,
                totalPages,
                currentPage = page,
                data = pendingUsers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetPendingUsers");
            return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
        }
    }

    [HttpGet("pending-users-cursor")]
    public async Task<IActionResult> GetPendingUsersCursor(
        [FromQuery] int limit = 50,
        [FromQuery] int? afterId = null)
    {
        try
        {
            var query = _context.RegistrationRequests
                .Where(x => x.Status == RegistrationStatus.Pending);

            if (afterId.HasValue)
            {
                var lastItem = await _context.RegistrationRequests.FindAsync(afterId.Value);
                if (lastItem != null)
                {
                    query = query.Where(x => x.CreatedAt < lastItem.CreatedAt || (x.CreatedAt == lastItem.CreatedAt && x.Id < lastItem.Id));
                }
            }

            var totalRecords = await query.CountAsync();
            var pendingUsers = await query
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(limit)
                .Include(x => x.Role)
                .Select(x => new PendingRegistrationDto
                {
                    Id = x.Id,
                    Username = x.Username,
                    Email = x.Email,
                    Role = x.Role != null ? x.Role.Name : string.Empty,
                    RoleId = x.RoleId,
                    Designation = x.Designation,
                    Department = x.Department != null ? x.Department.Name : string.Empty,
                    DepartmentId = x.DepartmentId,
                    Status = x.Status.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return Ok(new { totalRecords, data = pendingUsers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetPendingUsersCursor");
            return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
        }
    }

    [HttpPut("approve/{id}")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            _logger.LogInformation("Admin approve requested: RegistrationRequestId={Id}", id);
            
            var request = await _context.RegistrationRequests
                .Include(r => r.Role)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (request == null)
            {
                _logger.LogWarning("Approval failed: RegistrationRequest not found for Id={Id}", id);
                return NotFound(new { message = "Pending request not found" });     
            }

            // Make approve idempotent so the UI can safely retry.
            if (request.Status == RegistrationStatus.Approved)
            {
                _logger.LogInformation("Approval already completed for RegistrationRequestId={Id}, Email={Email}", id, request.Email);
                return Ok(new { message = "User already approved", isAlreadyApproved = true });
            }

            if (request.Status == RegistrationStatus.Rejected)
            {
                _logger.LogWarning("Cannot approve rejected request: RegistrationRequestId={Id}, Email={Email}", id, request.Email);
                return BadRequest(new { message = "This request was rejected and cannot be approved" });
            }

            // Validate role and department exist BEFORE making any changes
            var roleExists = await _context.Roles.AnyAsync(x => x.Id == request.RoleId);
            if (!roleExists)
            {
                _logger.LogWarning("Approval failed: Invalid RoleId={RoleId} for RegistrationRequestId={Id}", request.RoleId, id);
                return BadRequest(new { message = "Invalid RoleId provided." });
            }

            var departmentExists = await _context.Departments.AnyAsync(x => x.Id == request.DepartmentId);
            if (!departmentExists)
            {
                _logger.LogWarning("Approval failed: Invalid DepartmentId={DepartmentId} for RegistrationRequestId={Id}", request.DepartmentId, id);
                return BadRequest(new { message = "Invalid DepartmentId provided." });
            }

            // Fetch or create the user (case-insensitive)
            var existingUser = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == request.Email.ToLower());

            string targetRoleStr = "USER";
            if (request.Role != null)
            {
                targetRoleStr = request.Role.Name.ToUpper();
            }
            else
            {
                targetRoleStr = request.RoleId switch
                {
                    1 => "USER",
                    2 => "ISSUER",
                    3 => "ADMIN",
                    _ => "USER"
                };
            }

            if (existingUser == null)
            {
                _logger.LogInformation("Creating new user during approval for Email={Email}, RegistrationRequestId={Id}", request.Email, id);
                
                existingUser = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = request.PasswordHash ?? string.Empty,
                    DepartmentId = request.DepartmentId,
                    Designation = request.Designation,
                    IsActive = true,
                    IsApproved = true,
                    Role = targetRoleStr,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(existingUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User created successfully: UserId={UserId}, Email={Email}, RegistrationRequestId={Id}", existingUser.Id, request.Email, id);
            }
            else
            {
                _logger.LogInformation("Activating existing user: UserId={UserId}, Email={Email}, RegistrationRequestId={Id}, CurrentIsActive={IsActive}", 
                    existingUser.Id, request.Email, id, existingUser.IsActive);
                
                // Explicitly mark as modified to ensure EF Core tracks the change
                existingUser.IsActive = true;
                existingUser.IsApproved = true;
                existingUser.Role = targetRoleStr;
                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User activated successfully: UserId={UserId}, Email={Email}, RegistrationRequestId={Id}", existingUser.Id, request.Email, id);
            }

            // Handle user roles
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(x => x.UserId == existingUser.Id);

            if (userRole == null)
            {
                _logger.LogInformation("Creating UserRole: UserId={UserId}, RoleId={RoleId}, RegistrationRequestId={Id}", 
                    existingUser.Id, request.RoleId, id);
                
                _context.UserRoles.Add(new UserRole
                {
                    UserId = existingUser.Id,
                    RoleId = request.RoleId
                });
            }
            else
            {
                _logger.LogInformation("Updating UserRole: UserId={UserId}, OldRoleId={OldRoleId}, NewRoleId={NewRoleId}, RegistrationRequestId={Id}", 
                    existingUser.Id, userRole.RoleId, request.RoleId, id);
                
                userRole.RoleId = request.RoleId;
            }

            // Update registration request status
            request.Status = RegistrationStatus.Approved;
            request.IsActive = true;
            request.ApprovedAt = DateTime.UtcNow;
            request.ApprovedBy = this.HttpContext.User?.FindFirst("UserId") != null 
                ? int.Parse(this.HttpContext.User.FindFirst("UserId")?.Value ?? "0") 
                : (int?)null;
            request.PasswordHash = existingUser.PasswordHash;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "✓ User approval completed successfully: RegistrationRequestId={Id}, UserId={UserId}, Email={Email}, Role={RoleName}, IsActive={IsActive}",
                id, existingUser.Id, request.Email, request.Role?.Name ?? "Unknown", existingUser.IsActive);

            return Ok(new 
            { 
                message = "User approved successfully",
                userId = existingUser.Id,
                email = request.Email,
                isApproved = true,
                isActive = existingUser.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Unexpected error during user approval: RegistrationRequestId={Id}, Exception={Exception}", id, ex.Message);
            return StatusCode(500, new { message = "An internal server error occurred during approval.", error = ex.Message });
        }
    }

    [HttpPut("reject/{id}")]
    public async Task<IActionResult> Reject(int id)
    {
        try
        {
            _logger.LogInformation("Admin reject requested: RegistrationRequestId={Id}", id);
            
            var request = await _context.RegistrationRequests.FirstOrDefaultAsync(x => x.Id == id);

            if (request == null)
            {
                _logger.LogWarning("Rejection failed: RegistrationRequest not found for Id={Id}", id);
                return NotFound(new { message = "Pending request not found" });
            }

            // Check if already rejected
            if (request.Status == RegistrationStatus.Rejected)
            {
                _logger.LogInformation("Request already rejected: RegistrationRequestId={Id}, Email={Email}", id, request.Email);
                return Ok(new { message = "User already rejected" });
            }

            request.Status = RegistrationStatus.Rejected;
            request.IsActive = false;
            request.ApprovedAt = DateTime.UtcNow;
            request.ApprovedBy = this.HttpContext.User?.FindFirst("UserId") != null 
                ? int.Parse(this.HttpContext.User.FindFirst("UserId")?.Value ?? "0") 
                : (int?)null;

            // Remove the corresponding User record if it exists (case-insensitive)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("✓ User rejected successfully: RegistrationRequestId={Id}, Email={Email}, Status={Status}", 
                id, request.Email, request.Status);
            
            return Ok(new { message = "User rejected successfully", isRejected = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Unexpected error during user rejection: RegistrationRequestId={Id}, Exception={Exception}", id, ex.Message);
            return StatusCode(500, new { message = "An internal server error occurred during rejection.", error = ex.Message });
        }
    }

    // =========================
    // ADMIN DASHBOARD SUMMARY
    // =========================

    /// <summary>GET /api/admin/summary — total items, categories, stock</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            var totalCategories = await _context.Categories.CountAsync();
            var totalItems      = await _context.Items.CountAsync(i => i.IsActive);
            var totalStock      = await _context.InventoryStocks.SumAsync(s => (long?)s.AvailableQuantity) ?? 0L;
            return Ok(new { totalCategories, totalItems, totalStock });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSummary");
            return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
        }
    }

    /// <summary>GET /api/admin/categories — category list with item count and available stock</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Select(c => new
                {
                    id        = c.Id,
                    name      = c.Name,
                    itemCount = c.Items.Count(i => i.IsActive),
                    totalStock = c.Items
                        .Where(i => i.IsActive)
                        .Select(i => i.InventoryStock != null ? i.InventoryStock.AvailableQuantity : 0)
                        .Sum()
                })
                .ToListAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetCategories");
            return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
        }
    }

    // =========================
    // REQUEST WORKFLOW (ADMIN)
    // =========================

    /// <summary>GET /api/admin/requests — all requests (any status)</summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetAllRequests([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Admin requests list requested");
            var query = _context.Requests.AsNoTracking().Include(r => r.User);
            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new AdminRequestListItemDto
                {
                    Id        = r.Id,
                    UserId    = r.UserId,
                    Username  = r.User.Username ?? string.Empty,
                    Email     = r.User.Email ?? string.Empty,
                    Status    = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();
            return Ok(new { total, currentPage = pageNumber, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetAllRequests");
            return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
        }
    }

    /// <summary>GET /api/admin/issued-requests — ISSUED requests waiting for admin final approval</summary>
    [HttpGet("issued-requests")]
    public async Task<IActionResult> GetIssuedRequests([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Admin fetching ISSUED requests");
            var query = _context.Requests
                .AsNoTracking()
                .Where(r => r.Status == RequestStatus.PendingAdminApproval);

            var total      = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var data = await query
                .OrderByDescending(r => r.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    id        = r.Id,
                    userId    = r.UserId,
                    username  = r.User != null ? r.User.Username : string.Empty,
                    email     = r.User != null ? r.User.Email    : string.Empty,
                    status    = r.Status.ToString(),
                    createdAt = r.CreatedAt,
                    updatedAt = r.UpdatedAt,
                    items     = r.RequestItems.Select(ri => new
                    {
                        id               = ri.Id,
                        itemId           = ri.ItemId,
                        itemName         = ri.Item != null ? ri.Item.Name : string.Empty,
                        quantityRequested = ri.QuantityRequested,
                        quantityIssued    = ri.QuantityIssued
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new { total, totalPages, currentPage = pageNumber, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetIssuedRequests");
            return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
        }
    }

    /// <summary>GET /api/admin/monthly-register — monthly request register report</summary>
    [HttpGet("monthly-register")]
    public async Task<IActionResult> GetMonthlyRegister(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        try
        {
            var now = DateTime.UtcNow;
            var targetMonth = month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : now.Month;
            var targetYear = year.HasValue && year.Value > 0 ? year.Value : now.Year;

            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            var query = _context.RequestItems
                .AsNoTracking()
                .Include(ri => ri.Request)
                    .ThenInclude(r => r.User)
                .Include(ri => ri.Item)
                .Where(ri => ri.Request.CreatedAt.Year == targetYear && ri.Request.CreatedAt.Month == targetMonth);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLower();
                query = query.Where(ri =>
                    (ri.Request.User.Username != null && ri.Request.User.Username.ToLower().Contains(normalizedSearch))
                    || (ri.Item.Name != null && ri.Item.Name.ToLower().Contains(normalizedSearch)));
            }

            var totalCount = await query.CountAsync();
            var totalQuantityRequested = await query.SumAsync(ri => ri.QuantityRequested);
            var totalQuantityApproved = await query.SumAsync(ri => ri.QuantityApproved);
            var totalQuantityIssued = await query.SumAsync(ri => ri.QuantityIssued);

            var data = await query
                .OrderByDescending(ri => ri.Request.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(ri => new MonthlyRegisterRowDto
                {
                    Id = ri.Id,
                    RequestId = ri.RequestId,
                    UserId = ri.Request.UserId,
                    UserName = ri.Request.User.Username ?? string.Empty,
                    ItemCode = ri.Item != null ? ri.Item.ItemCode : string.Empty,
                    ItemName = ri.Item != null ? ri.Item.Name : string.Empty,
                    Status = ri.Request.Status,
                    RequestDate = ri.Request.CreatedAt,
                    QuantityRequested = ri.QuantityRequested,
                    QuantityApproved = ri.QuantityApproved,
                    QuantityIssued = ri.QuantityIssued
                })
                .ToListAsync();

            var result = new MonthlyRegisterResultDto
            {
                Year = targetYear,
                Month = targetMonth,
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalQuantityRequested = totalQuantityRequested,
                TotalQuantityApproved = totalQuantityApproved,
                TotalQuantityIssued = totalQuantityIssued,
                Data = data
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetMonthlyRegister");
            return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/admin/request/{id}/approve
    /// Admin gives final approval to an ISSUED request → APPROVED
    /// Workflow: REQUESTED (User) → ISSUED (Issuer) → APPROVED (Admin)
    /// </summary>
    [HttpPut("request/{id}/approve")]
    public async Task<IActionResult> ApproveRequest(int id)
    {
        try
        {
            _logger.LogInformation("Admin approve request workflow: RequestId={Id}", id);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var adminId = User.GetUserId();
            var request = await _context.Requests
                .Include(r => r.RequestItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound(new { message = "Request not found" });

            // Guard: cannot re-approve rejected/received
            if (request.Status == RequestStatus.Rejected || request.Status == RequestStatus.Received)
                return BadRequest(new { message = $"Cannot approve a '{request.Status}' request" });

            // Idempotent
            if (request.Status == RequestStatus.Approved)
                return Ok(new { message = "Request already approved" });

            // Admin can only approve requests that are in ISSUED state
            if (request.Status != RequestStatus.PendingAdminApproval)
                return BadRequest(new { message = $"Only requests pending admin approval can be approved by admin. Current status: {request.Status}" });

            var oldStatus = request.Status;
            request.Status    = RequestStatus.Approved;
            request.UpdatedAt = DateTime.UtcNow;
            _context.ApprovalLogs.Add(new ApprovalLog
            {
                RequestId = request.Id,
                ApprovedBy = adminId,
                Status = $"{oldStatus} -> {request.Status}",
                Remarks = "Approved by admin",
                ActionDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Request approved by admin: RequestId={Id}", id);
            return Ok(new { message = "Request approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in ApproveRequest");
            return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
        }
    }

    // PUT /api/admin/request/{id}/reject
    [HttpPut("request/{id}/reject")]
    public async Task<IActionResult> RejectRequest(int id)
    {
        try
        {
            _logger.LogInformation("Admin reject request workflow: RequestId={Id}", id);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var adminId = User.GetUserId();
            var request = await _context.Requests.FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound(new { message = "Request not found" });
            }

            if (request.Status == RequestStatus.Rejected)
            {
                return Ok(new { message = "Request already rejected" });
            }

            if (request.Status != RequestStatus.PendingAdminApproval && request.Status != RequestStatus.Pending)
            {
                return BadRequest(new { message = "Only requests pending admin approval can be rejected" });
            }

            var oldStatus = request.Status;
            request.Status = RequestStatus.Rejected;
            request.UpdatedAt = DateTime.UtcNow;
            _context.ApprovalLogs.Add(new ApprovalLog
            {
                RequestId = request.Id,
                ApprovedBy = adminId,
                Status = $"{oldStatus} -> {request.Status}",
                Remarks = "Rejected by admin",
                ActionDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Request rejected: RequestId={Id}", id);
            return Ok(new { message = "Request rejected" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RejectRequest");
            return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
        }
    }

    // ====================================================================
    // ENTERPRISE WORKFLOW - PARTIAL APPROVAL ENDPOINTS (NEW)
    // ====================================================================

    /// <summary>
    /// GET /api/admin/pending
    /// Get all pending items waiting for admin to approve
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingItems(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Fetching pending admin approval items: Page={Page}, PageSize={PageSize}", pageNumber, pageSize);

            var result = await _approvalService.GetPendingItemsAsync(pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending admin approval items");
            return StatusCode(500, new { message = "Error fetching pending items", error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/admin/pending/count
    /// Get count of pending items waiting for admin
    /// </summary>
    [HttpGet("pending/count")]
    public async Task<IActionResult> GetPendingCount()
    {
        try
        {
            var count = await _approvalService.GetPendingCountAsync();
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending count");
            return StatusCode(500, new { message = "Error getting pending count", error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/admin/approve-partially
    /// Admin approves items partially with inventory restoration
    /// Admin specifies: ApproveQuantity + RejectQuantity = IssuerIssuedQuantity
    /// </summary>
    [HttpPut("approve-partially")]
    public async Task<IActionResult> ApprovePartially([FromBody] ApprovePartiallyDto dto)
    {
        try
        {
            // Validate input
            if (dto == null)
                return BadRequest(new { message = "Request body cannot be empty" });

            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { message = "At least one item must be approved" });

            _logger.LogInformation("Admin approving partially: RequestId={RequestId}, ItemCount={ItemCount}",
                dto.RequestId, dto.Items.Count);

            var adminId = User.GetUserId();
            var response = await _approvalService.ApprovePartiallyAsync(dto, adminId);

            if (!response.Success)
                return BadRequest(response);

            _logger.LogInformation("Partial approval successful: RequestId={RequestId}", dto.RequestId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during partial approval: RequestId={RequestId}", dto?.RequestId);
            return StatusCode(500, new { message = "Error processing partial approval", error = ex.Message });
        }
    }
}
