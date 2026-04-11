using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

[Route("api/[controller]")]
[ApiController]
public class RegistrationController : ControllerBase
{
    private readonly AppDbContext _context;

    public RegistrationController(AppDbContext context)
    {
        _context = context;
    }

    //  1. USER REGISTER (PENDING)
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegistrationRequest req)
    {
        //  Duplicate email check
        var exists = await _context.RegistrationRequests
            .AnyAsync(u => u.Email == req.Email);

        if (exists)
            return BadRequest("Email already registered");

        req.Status = RegistrationStatus.Pending;
        req.IsActive = false;
        req.CreatedAt = DateTime.Now;

        //  Hash password before saving
        req.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.PasswordHash);

        _context.RegistrationRequests.Add(req);
        await _context.SaveChangesAsync();

        return Ok("Registration submitted for approval");
    }

    //  2. GET PENDING REQUESTS (ADMIN)
    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var data = await _context.RegistrationRequests
            .Where(r => r.Status == RegistrationStatus.Pending)
            .Include(r => r.Role)
            .Include(r => r.Department)
            .ToListAsync();

        return Ok(data);
    }

    // 3. APPROVE USER + ROLE ASSIGN
    [Authorize(Roles = "Admin")]
    [HttpPost("approve/{id}")]
    public async Task<IActionResult> Approve(int id)
    {
        var req = await _context.RegistrationRequests.FindAsync(id);

        if (req == null)
            return NotFound("Request not found");

        //  Check if already user exists
        var exists = await _context.Users
            .AnyAsync(u => u.Email == req.Email);

        if (exists)
            return BadRequest("User already exists");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            //  CREATE USER
            var user = new User
            {
                Username = req.Username,
                Email = req.Email,
                PasswordHash = req.PasswordHash, // already hashed
                DepartmentId = req.DepartmentId,
                Designation = req.Designation,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            //  ASSIGN ROLE
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = req.RoleId
            };

            _context.UserRoles.Add(userRole);

            //  UPDATE REQUEST
            req.Status = RegistrationStatus.Approved;
            req.IsActive = true;
            req.ApprovedAt = DateTime.Now;

            // Optional admin ID from JWT
            var adminId = User.FindFirst("id")?.Value;
            if (adminId != null)
                req.ApprovedBy = int.Parse(adminId);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok("User approved & role assigned");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Something went wrong");
        }
    }

    //  4 REJECT USER
    [Authorize(Roles = "Admin")]
    [HttpPost("reject/{id}")]
    public async Task<IActionResult> Reject(int id)
    {
        var req = await _context.RegistrationRequests.FindAsync(id);

        if (req == null)
            return NotFound("Request not found");

        req.Status = RegistrationStatus.Rejected;
        req.IsActive = false;
        req.ApprovedAt = DateTime.Now;

        // (Optional) Admin ID
        var adminId = User.FindFirst("id")?.Value;
        if (adminId != null)
            req.ApprovedBy = int.Parse(adminId);

        await _context.SaveChangesAsync();

        return Ok("User rejected");
    }
}