using invmgmt.web.DTOs;
using invmgmt.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace invmgmt.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly ILogger<RegistrationController> _logger;

        public RegistrationController(IRegistrationService registrationService, ILogger<RegistrationController> logger)
        {
            _registrationService = registrationService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationRequestDto req)
        {
            try
            {
                if (req == null) 
                    return BadRequest(new { message = "Invalid request." });

                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

                var result = await _registrationService.RegisterAsync(req);
                if (!result.Success) return BadRequest(new { message = result.Message });

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", req?.Email);
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            try
            {
                var data = await _registrationService.GetPendingRequestsAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching pending requests");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var result = await _registrationService.ApproveAsync(id);
                if (!result.Success) return BadRequest(new { message = result.Message });

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error approving registration {Id}", id);
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var result = await _registrationService.RejectAsync(id);
                if (!result.Success) return BadRequest(new { message = result.Message });

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error rejecting registration {Id}", id);
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }
    }
}
