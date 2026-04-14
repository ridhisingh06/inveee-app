using invmgmt.web.Data;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Register(RegistrationRequestDto req)
        {
            if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
                return BadRequest("Email and Password are required.");

            var exists = await _context.RegistrationRequests
                .AnyAsync(u => u.Email == req.Email);

            if (exists)
                return BadRequest("Email already registered");

            var request = new RegistrationRequest
            {
                Username = req.Username,
                Email = req.Email,
                PasswordHash = req.Password, // plain for now
                DepartmentId = req.DepartmentId,
                RoleId = req.RoleId,
                Designation = req.Designation,
                Status = RegistrationStatus.Pending,
                IsActive = false,
                CreatedAt = DateTime.Now
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
                PasswordHash = req.PasswordHash,
                DepartmentId = req.DepartmentId,
                Designation = req.Designation,
                IsActive = true,
                CreatedAt = DateTime.Now
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
            req.ApprovedAt = DateTime.Now;

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