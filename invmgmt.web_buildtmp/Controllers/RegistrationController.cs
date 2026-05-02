using invmgmt.web.Data;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Utils;

namespace invmgmt.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RegistrationController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // REGISTER
        // =========================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationRequestDto req)
        {
            if (req == null ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password) ||
                string.IsNullOrWhiteSpace(req.Username))
            {
                return BadRequest("Username, Email and Password are required.");
            }

            var exists = await _context.RegistrationRequests
                .AnyAsync(u => u.Email == req.Email);

            if (exists)
                return BadRequest("Email already registered");

            var request = new RegistrationRequest
            {
                Username = req.Username,
                Email = req.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                DepartmentId = req.DepartmentId,
                RoleId = req.RoleId,
                Designation = req.Designation,
                Status = RegistrationStatus.Pending,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.RegistrationRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok("Registration submitted");
        }

        // =========================
        // PENDING (ADMIN)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var data = await _context.RegistrationRequests
                .Where(x => x.Status == RegistrationStatus.Pending)
                .Include(x => x.Role)
                .Include(x => x.Department)
                .Select(x => new PendingRegistrationDto
                {
                    Id = x.Id,
                    Username = x.Username,
                    Email = x.Email,
                    Designation = x.Designation,
                    Department = x.Department != null ? x.Department.Name : string.Empty,
                    Role = x.Role != null ? x.Role.Name : string.Empty,
                    Status = x.Status.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return Ok(data);
        }

        // =========================
        // APPROVE
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            var req = await _context.RegistrationRequests.FindAsync(id);

            if (req == null)
                return NotFound("Request not found");

            var exists = await _context.Users
                .AnyAsync(x => x.Email == req.Email);

            if (exists)
                return BadRequest("User already exists");

            var user = new User
            {
                Username = req.Username,
                Email = req.Email,
                PasswordHash = PasswordUtils.LooksLikeBcryptHash(req.PasswordHash)
                    ? req.PasswordHash
                    : BCrypt.Net.BCrypt.HashPassword(req.PasswordHash ?? string.Empty),
                DepartmentId = req.DepartmentId,
                Designation = req.Designation,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = req.RoleId
            });

            req.Status = RegistrationStatus.Approved;
            req.IsActive = true;
            req.ApprovedAt = DateTime.UtcNow;
            req.PasswordHash = user.PasswordHash;

            await _context.SaveChangesAsync();

            return Ok("User approved");
        }

        // =========================
        // REJECT
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> Reject(int id)
        {
            var req = await _context.RegistrationRequests.FindAsync(id);

            if (req == null)
                return NotFound("Request not found");

            req.Status = RegistrationStatus.Rejected;
            req.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok("User rejected");
        }
    }
}
