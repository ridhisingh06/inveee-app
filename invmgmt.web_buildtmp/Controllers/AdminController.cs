using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Utils;
using invmgmt.web.Models.Enums;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("pending-users")]
    public async Task<IActionResult> GetPendingUsers()
    {
        var pendingUsers = await _context.RegistrationRequests
            .Where(x => x.Status == RegistrationStatus.Pending)
            .Include(x => x.Role)
            .Select(x => new PendingRegistrationDto
            {
                Id = x.Id,
                Username = x.Username,
                Email = x.Email,
                Role = x.Role != null ? x.Role.Name : string.Empty,
                Designation = x.Designation,
                Department = x.Department != null ? x.Department.Name : string.Empty,
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(pendingUsers);
    }

    [HttpPut("approve/{id}")]
    public async Task<IActionResult> Approve(int id)
    {
        var request = await _context.RegistrationRequests.FirstOrDefaultAsync(x => x.Id == id);

        if (request == null)
        {
            return NotFound(new { message = "Pending request not found" });     
        }

        // Make approve idempotent so the UI can safely retry.
        if (request.Status == RegistrationStatus.Approved)
        {
            return Ok(new { message = "User already approved" });
        }

        if (request.Status == RegistrationStatus.Rejected)
        {
            return BadRequest(new { message = "This request was rejected and cannot be approved" });
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);

        if (existingUser == null)
        {
            existingUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = PasswordUtils.LooksLikeBcryptHash(request.PasswordHash)
                    ? request.PasswordHash
                    : BCrypt.Net.BCrypt.HashPassword(request.PasswordHash ?? string.Empty),
                DepartmentId = request.DepartmentId,
                Designation = request.Designation,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();
        }
        else
        {
            existingUser.IsActive = true;
        }

        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(x => x.UserId == existingUser.Id);

        if (userRole == null)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = existingUser.Id,
                RoleId = request.RoleId
            });
        }
        else
        {
            userRole.RoleId = request.RoleId;
        }

        var roleExists = await _context.Roles.AnyAsync(x => x.Id == request.RoleId);
        if (!roleExists)
        {
            return BadRequest(new { message = "Invalid RoleId provided." });
        }

        var departmentExists = await _context.Departments.AnyAsync(x => x.Id == request.DepartmentId);
        if (!departmentExists)
        {
            return BadRequest(new { message = "Invalid DepartmentId provided." });
        }

        request.Status = RegistrationStatus.Approved;
        request.IsActive = true;
        request.ApprovedAt = DateTime.UtcNow;
        request.PasswordHash = existingUser.PasswordHash;

        await _context.SaveChangesAsync();

        return Ok(new { message = "User approved successfully" });
    }

    [HttpPut("reject/{id}")]
    public async Task<IActionResult> Reject(int id)
    {
        var request = await _context.RegistrationRequests.FirstOrDefaultAsync(x => x.Id == id);

        if (request == null)
        {
            return NotFound(new { message = "Pending request not found" });
        }

        request.Status = RegistrationStatus.Rejected;
        request.IsActive = false;
        request.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "User rejected successfully" });
    }

    // =========================
    // REQUEST WORKFLOW (ADMIN)
    // =========================

    // GET /api/admin/requests
    [HttpGet("requests")]
    public async Task<IActionResult> GetAllRequests()
    {
        var data = await _context.Requests
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminRequestListItemDto
            {
                Id = r.Id,
                UserId = r.UserId,
                Username = r.User.Username ?? string.Empty,
                Email = r.User.Email ?? string.Empty,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(data);
    }

    // PUT /api/admin/request/{id}/approve
    [HttpPut("request/{id}/approve")]
    public async Task<IActionResult> ApproveRequest(int id)
    {
        var request = await _context.Requests
            .Include(r => r.RequestItems)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        if (request.Status == RequestStatus.Rejected || request.Status == RequestStatus.Received)
        {
            return BadRequest(new { message = $"Cannot approve a {request.Status} request" });
        }

        // Idempotent: approving an already approved request is OK.
        if (request.Status == RequestStatus.Approved)
        {
            return Ok(new { message = "Request already approved" });
        }

        if (request.Status != RequestStatus.Pending)
        {
            return BadRequest(new { message = "Only pending requests can be approved" });
        }

        foreach (var item in request.RequestItems)
        {
            item.QuantityApproved = item.QuantityRequested;
        }

        request.Status = RequestStatus.Approved;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Request approved" });
    }

    // PUT /api/admin/request/{id}/reject
    [HttpPut("request/{id}/reject")]
    public async Task<IActionResult> RejectRequest(int id)
    {
        var request = await _context.Requests.FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        if (request.Status == RequestStatus.Rejected)
        {
            return Ok(new { message = "Request already rejected" });
        }

        if (request.Status != RequestStatus.Pending)
        {
            return BadRequest(new { message = "Only pending requests can be rejected" });
        }

        request.Status = RequestStatus.Rejected;
        request.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Request rejected" });
    }
}
