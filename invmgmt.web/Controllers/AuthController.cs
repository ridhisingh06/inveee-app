using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // =========================
    // LOGIN API
    // =========================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            Console.WriteLine("\n=== LOGIN ATTEMPT ===");
            Console.WriteLine($"Email: {req?.Email}");

            if (req == null || string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
            {
                return BadRequest("Email and password are required");
            }

            // 1. Find user
            Console.WriteLine($"🔍 Searching for user: {req.Email}");
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == req.Email);

            if (user == null)
            {
                Console.WriteLine("❌ User not found");
                return Unauthorized(new { message = "Invalid credentials" });
            }

            Console.WriteLine($"✅ User found: {user.Username}");

            // 2. Compare plaintext password
            Console.WriteLine("🔐 Verifying password (plaintext comparison)...");
            if (req.Password != user.PasswordHash)  // ← PLAINTEXT COMPARISON
            {
                Console.WriteLine("❌ Password mismatch");
                return Unauthorized(new { message = "Invalid credentials" });
            }

            Console.WriteLine("✅ Password verified");

            // 3. Active check
            if (!user.IsActive)
            {
                Console.WriteLine("❌ User is not active");
                return BadRequest(new { message = "User not approved yet" });
            }

            // 4. Get roles
            var roles = await _context.UserRoles
                .Include(x => x.Role)
                .Where(x => x.UserId == user.Id)
                .Select(x => x.Role.Name)
                .ToListAsync();

            // 5. Create claims
            var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? "")
        };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
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

            Console.WriteLine("✅ Login successful");
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role = roles.FirstOrDefault(),
                message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            return StatusCode(500, new { message = ex.Message });
        }
    }