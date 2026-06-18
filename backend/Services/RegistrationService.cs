using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.Repositories;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.Utils;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System;

namespace invmgmt.web.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IUserRepository _userRepo;
        private readonly AppDbContext _context;
        private readonly ILogger<RegistrationService> _logger;

        public RegistrationService(
            IUserRepository userRepo,
            AppDbContext context,
            ILogger<RegistrationService> logger)
        {
            _userRepo = userRepo;
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(RegistrationRequestDto dto)
        {
            var email = dto.Email.Trim();
            
            // 1. Check if user already exists
            var existingUser = await _userRepo.GetByEmailAsync(email);
            if (existingUser != null) return (false, "Email already in use.");

            // Check if pending registration request already exists
            var pendingReq = await _context.RegistrationRequests.FirstOrDefaultAsync(r => r.Email.ToLower() == email.ToLower() && r.Status == RegistrationStatus.Pending);
            if (pendingReq != null) return (false, "A registration request is already pending for this email.");

            // 3. Validate Dept and Role
            if (dto.RoleId != 1 && dto.RoleId != 2)
            {
                return (false, "Invalid Role. Only User and Issuer registration is allowed.");
            }

            var validDept = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId.Value);
            if (!validDept) return (false, "Invalid Department.");

            string roleString = dto.RoleId.Value switch
            {
                1 => "USER",
                2 => "ISSUER",
                _ => "USER"
            };

            // 4. Create User
            var user = new User
            {
                Username = dto.Username,
                Email = email,
                DepartmentId = dto.DepartmentId.Value,
                Designation = dto.Designation,
                IsActive = true,
                IsApproved = false,
                Role = roleString,
                CreatedAt = DateTime.UtcNow
            };
            
            user.PasswordHash = PasswordUtils.HashPassword(dto.Password);

            await _userRepo.AddUserAsync(user);

            // 5. Create RegistrationRequest
            var regRequest = new RegistrationRequest
            {
                Username = dto.Username,
                Email = email,
                PasswordHash = user.PasswordHash,
                DepartmentId = dto.DepartmentId.Value,
                RoleId = dto.RoleId.Value,
                Designation = dto.Designation,
                IsActive = false,
                Status = RegistrationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _context.RegistrationRequests.AddAsync(regRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered and waiting for approval {Email}", email);
            
            return (true, "Your registration is pending. Please wait for admin approval before signing in.");
        }

        public async Task<IEnumerable<PendingRegistrationDto>> GetPendingRequestsAsync()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => !u.IsApproved)
                .Include(u => u.Department)
                .ToListAsync();

            var pending = new List<PendingRegistrationDto>();
            foreach(var u in users)
            {
                pending.Add(new PendingRegistrationDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Designation = u.Designation,
                    Department = u.Department?.Name ?? string.Empty,
                    Role = u.Role, // Direct string role read
                    Status = "Pending",
                    CreatedAt = u.CreatedAt
                });
            }
            return pending;
        }

        public async Task<(bool Success, string Message)> ApproveAsync(int requestId)
        {
            var user = await _userRepo.GetByIdAsync(requestId);
            if (user == null || user.IsApproved)
                return (false, "Invalid or already processed request.");

            user.IsApproved = true;
            await _userRepo.UpdateUserAsync(user);

            _logger.LogInformation("Registration approved for {Email}. User ID {UserId}", user.Email, user.Id);
            return (true, "User approved and activated.");
        }

        public async Task<(bool Success, string Message)> RejectAsync(int requestId)
        {
            var user = await _userRepo.GetByIdAsync(requestId);
            if (user == null || user.IsApproved)
                return (false, "Invalid or already processed request.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Registration rejected for {Email}", user.Email);
            return (true, "User rejected.");
        }
    }
}
