using invmgmt.web.Data;
using invmgmt.web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using invmgmt.web.DTOs;

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

    // 🔐 LOGIN API
    [HttpPost("login")]
    public IActionResult Login(LoginRequest req)
    {
        // 1. User find by email
        var user = _context.Users
            .FirstOrDefault(x => x.Email == req.Email);

        if (user == null)
            return Unauthorized("Invalid credentials");

        // 2. BCrypt password verify
        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Invalid password");

        // 3. Approval check
        if (!user.IsActive)
            return BadRequest("User not approved yet");

        // 4. Generate JWT
        var token = GenerateToken(user);

        return Ok(new
        {
            token = token,
            message = "Login successful"
        });
    }

    // 🔐 TOKEN GENERATION
    private string GenerateToken(User user)
    {
        // Get roles from DB
        var roles = _context.UserRoles
            .Where(x => x.UserId == user.Id)
            .Select(x => x.Role.Name)
            .ToList();

        // Claims
        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        // Add roles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Key
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"])
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Token
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}