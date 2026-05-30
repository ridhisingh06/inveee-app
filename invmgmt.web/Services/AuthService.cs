using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Repositories;
using invmgmt.web.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using invmgmt.web.Data;
using Microsoft.EntityFrameworkCore;

namespace invmgmt.web.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _config;
        private readonly AppDbContext _context; // Needed to fetch roles
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepo,
            IConfiguration config,
            AppDbContext context,
            ILogger<AuthService> logger)
        {
            _userRepo = userRepo;
            _config = config;
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string Token, string Message)> LoginAsync(LoginRequest dto)
        {
            try
            {
                // 1. Validate request fields match (email, password)
                if (string.IsNullOrWhiteSpace(dto?.Email) || string.IsNullOrWhiteSpace(dto?.Password))
                {
                    _logger.LogWarning("Login attempt with null or empty credentials");
                    return (false, "", "Email and Password are required.");
                }

                var email = dto.Email.Trim();
                _logger.LogInformation("[DEBUG] Login attempt for email: {Email}", email);

                // 2. Compare email case-insensitively
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                // 3. Ensure user exists in DB (Fallback admin seed if missing during login)
                if (user == null && email.Equals("admin@gmail.com", StringComparison.OrdinalIgnoreCase))
                {
                    user = new User
                    {
                        Username = "System Admin",
                        Email = "admin@gmail.com",
                        Role = "ADMIN",
                        IsActive = true,
                        IsApproved = true,
                        PasswordHash = PasswordUtils.HashPassword("admin@123"), 
                        CreatedAt = DateTime.UtcNow,
                        DepartmentId = 1
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("[DEBUG] Admin user seeded as a fallback during login.");
                }

                // Verify email exists
                if (user == null)
                {
                    // If user is not found in Users, check if they were rejected
                    var rejectedReq = await _context.RegistrationRequests
                        .FirstOrDefaultAsync(r => r.Email.ToLower() == email.ToLower() && r.Status == invmgmt.web.Models.RegistrationStatus.Rejected);
                    
                    if (rejectedReq != null)
                    {
                        _logger.LogWarning("[DEBUG] Login blocked: User was rejected for email: {Email}", email);
                        return (false, "", "Account rejected");
                    }

                    _logger.LogWarning("[DEBUG] Login failed: User not found for email: {Email}", email);
                    return (false, "", "User not found");
                }

                _logger.LogInformation("[DEBUG] User found: Email={Email}, Role={Role}, IsApproved={IsApproved}", user.Email, user.Role, user.IsApproved);

                // Compare password correctly
                bool isPasswordValid = PasswordUtils.VerifyPassword(dto.Password, user.PasswordHash);
                
                // Detailed logging for password match result
                _logger.LogInformation("[DEBUG] Password match result for Email={Email}: {Result}", user.Email, isPasswordValid);
                
                if (!isPasswordValid)
                {
                    _logger.LogWarning("[DEBUG] Login failed: Invalid password for user. UserId={UserId}, Email={Email}", user.Id, email);
                    return (false, "", "Incorrect password");
                }

                _logger.LogInformation("[DEBUG] Password verified successfully for Email={Email}", user.Email);

                // Allow only if isApproved = true (for user/issuer)
                var userRole = user.Role?.Trim().ToUpper();
                if (!user.IsApproved && userRole != "ADMIN")
                {
                    _logger.LogWarning("[DEBUG] Login blocked: User not approved yet. UserId={UserId}, Email={Email}, Role={Role}, DB_IsApproved={IsApproved}", user.Id, email, userRole, user.IsApproved);
                    return (false, "", "Account pending admin approval");
                }

                _logger.LogInformation("[DEBUG] User is approved or is Admin. Generating token for Email={Email}, Role={Role}", user.Email, userRole);

                // Generate and return Token
                var token = await GenerateJwtToken(user);
                
                // Detailed logging for JWT generation success
                _logger.LogInformation("[DEBUG] JWT generation success for Email={Email}. Token length: {TokenLength}", user.Email, token?.Length ?? 0);
                
                _logger.LogInformation("✓ LOGIN SUCCESSFUL: UserId={UserId}, Email={Email}, Role={Role}", user.Id, user.Email, userRole);
                return (true, token, "Login successful.");
            }
            catch (Exception ex)
            {
                var reqEmail = dto?.Email?.Trim();
                _logger.LogError(ex, "✗ Unexpected error during login attempt: {Email}, Exception={Exception}", reqEmail, ex.Message);
                return (false, "", "An unexpected error occurred during login.");
            }
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "THIS_IS_MY_SECRET_KEY_12345";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.Role, user.Role?.Trim().ToUpper() ?? "USER") // Consistent uppercase role without spaces
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Masks email for safe logging (prevents exposing full PII in logs)
        /// </summary>
        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "<empty>";

            var parts = email.Split('@');
            if (parts.Length != 2)
                return "***";

            var local = parts[0];
            var domain = parts[1];

            // Show first char and domain, mask the rest
            if (local.Length <= 1)
                return "*@" + domain;

            return local[0] + "***@" + domain;
        }
    }
}
