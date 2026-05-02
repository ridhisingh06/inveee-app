using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using invmgmt.web.Utils;


[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext context, IConfiguration config, ILogger<AuthController> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegistrationRequestDto req)
    {
        if (req == null ||
            string.IsNullOrWhiteSpace(req.Username) ||
            string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Password))
        {
            return BadRequest(new { message = "Name, email and password are required" });
        }

        // Validate foreign keys early so we return a clean 400 instead of a DbUpdateException.
        var roleExists = await _context.Roles.AnyAsync(x => x.Id == req.RoleId);
        if (!roleExists)
        {
            return BadRequest(new { message = "Invalid RoleId provided" });
        }

        var departmentExists = await _context.Departments.AnyAsync(x => x.Id == req.DepartmentId);
        if (!departmentExists)
        {
            return BadRequest(new { message = "Invalid DepartmentId provided" });
        }

        var existingUser = await _context.Users.AnyAsync(x => x.Email == req.Email);
        if (existingUser)
        {
            // Make register idempotent: if the account already exists, treat it as a no-op.
            return Ok(new { message = "Account already exists. Please login." });
        }

        var existingRequest = await _context.RegistrationRequests
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.Email == req.Email);

        if (existingRequest != null)
        {
            if (existingRequest.Status == RegistrationStatus.Pending)
            {
                // Idempotent: user may click register multiple times.
                return Ok(new { message = "Your registration request is already pending" });
            }

            if (existingRequest.Status == RegistrationStatus.Rejected)
            {
                // Allow resubmission by reusing the same row so admins don't see duplicates.
                existingRequest.Username = req.Username;
                existingRequest.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
                existingRequest.DepartmentId = req.DepartmentId;
                existingRequest.RoleId = req.RoleId;
                existingRequest.Designation = req.Designation;
                existingRequest.Status = RegistrationStatus.Pending;
                existingRequest.IsActive = false;
                existingRequest.CreatedAt = DateTime.UtcNow;
                existingRequest.ApprovedAt = null;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Your request is pending. Please wait for admin approval." });
            }
        }

        var registrationRequest = new RegistrationRequest
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

        _context.RegistrationRequests.Add(registrationRequest);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Your request is pending. Please wait for admin approval." });
    }

    // =========================
    // LOGIN API
    // =========================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "<empty>";
            }

            var at = email.IndexOf('@');
            if (at <= 1)
            {
                return "***";
            }

            // Keep a tiny prefix and the domain for debugging without logging full PII.
            return email.Substring(0, 2) + "***" + email.Substring(at);
        }

        try
        {
            _logger.LogInformation("Login attempt for {Email}", MaskEmail(req?.Email));

            if (req == null || string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            // 1. Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == req.Email);

            if (user == null)
            {
                var registrationRequest = await _context.RegistrationRequests
                    .FirstOrDefaultAsync(x => x.Email == req.Email);

                if (registrationRequest != null)
                {
                    if (registrationRequest.Status == RegistrationStatus.Pending ||
                        registrationRequest.Status == RegistrationStatus.Rejected)
                    {
                        _logger.LogInformation("Login blocked (not approved) for {Email}", MaskEmail(req.Email));
                        return BadRequest(new { message = "Your account is not approved yet" });
                    }
                }

                return Unauthorized(new { message = "Invalid credentials" });
            }

            // 2. Compare hashed password
            var storedPassword = user.PasswordHash;
            if (string.IsNullOrWhiteSpace(storedPassword))
            {
                _logger.LogWarning("User {UserId} has no password hash", user.Id);
                return Unauthorized(new { message = "Invalid credentials" });
            }

            if (PasswordUtils.LooksLikeBcryptHash(storedPassword))
            {
                if (!BCrypt.Net.BCrypt.Verify(req.Password, storedPassword))
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }
            }
            else
            {
                // Legacy data: some users were stored with plaintext passwords. Allow a one-time
                // login, then upgrade to bcrypt so future logins use the secure verifier.
                if (!PasswordUtils.FixedTimeEquals(req.Password, storedPassword))
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
                await _context.SaveChangesAsync();
                _logger.LogWarning("Upgraded legacy plaintext password to bcrypt for user {UserId}", user.Id);
            }

            // 3. Active check
            if (!user.IsActive)
            {
                _logger.LogInformation("Login blocked (inactive) for user {UserId}", user.Id);
                return BadRequest(new { message = "Your account is not approved yet" });
            }

            // 4. Get roles
            var roles = await (
                from userRole in _context.UserRoles
                join role in _context.Roles on userRole.RoleId equals role.Id
                where userRole.UserId == user.Id
                select role.Name
            ).ToListAsync();

            // 5. Create claims
            var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? "")
        };

            foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }

            // 6. JWT token
            var key = _config["Jwt:Key"] ?? "THIS_IS_MY_SECRET_KEY_12345";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            _logger.LogInformation("Login success for user {UserId}", user.Id);
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role = roles.FirstOrDefault(),
                message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during login");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
