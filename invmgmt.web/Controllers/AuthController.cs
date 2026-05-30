using invmgmt.web.DTOs;
using invmgmt.web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace invmgmt.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRegistrationService _registrationService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IRegistrationService registrationService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _registrationService = registrationService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                {
                    _logger.LogWarning("Login request body is null or missing fields.");
                    return BadRequest(new { message = "Email and Password are required." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Login Model State invalid for {Email}: {Errors}", req.Email, string.Join(", ", errors));
                    return BadRequest(new { message = "Validation failed", errors });
                }

                _logger.LogInformation("Login attempt for {Email}", req.Email);

                var result = await _authService.LoginAsync(req);
                if (!result.Success)
                {
                    _logger.LogWarning("Login failed for {Email}: {Message}", req.Email, result.Message);

                    // Return 403 specifically for pending/rejected approval so the frontend can detect it
                    if (result.Message.Contains("pending", StringComparison.OrdinalIgnoreCase) || 
                        result.Message.Contains("approval", StringComparison.OrdinalIgnoreCase) ||
                        result.Message.Contains("rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        return StatusCode(403, new { message = result.Message });
                    }

                    return Unauthorized(new { message = result.Message });
                }

                _logger.LogInformation("Login successful for {Email}", req.Email);
                return Ok(new { token = result.Token, message = result.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Login API Crash: {ex.ToString()}");
                _logger.LogError(ex, "An unexpected error occurred during login for {Email}", req?.Email);
                return StatusCode(500, new 
                { 
                    message = "An internal server error occurred.", 
                    developerMessage = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationRequestDto req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Username))
                {
                    _logger.LogWarning("Register request body is null or missing required fields.");
                    return BadRequest(new { message = "Email, Username, and Password are required." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Register Model State invalid: {Errors}", string.Join(", ", errors));
                    return BadRequest(new { message = "Validation failed", errors });
                }

                _logger.LogInformation("Register attempt for {Email}", req.Email);

                var result = await _registrationService.RegisterAsync(req);
                if (!result.Success)
                {
                    _logger.LogWarning("Register failed for {Email}: {Message}", req.Email, result.Message);
                    return BadRequest(new { message = result.Message });
                }

                _logger.LogInformation("Register successful for {Email}", req.Email);
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Register API Crash: {ex.ToString()}");
                _logger.LogError(ex, "An unexpected error occurred during registration for {Email}", req?.Email);
                return StatusCode(500, new 
                { 
                    message = "An internal server error occurred.", 
                    developerMessage = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }
    }
}
