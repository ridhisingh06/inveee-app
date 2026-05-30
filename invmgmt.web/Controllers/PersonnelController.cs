using invmgmt.web.DTOs;
using invmgmt.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace invmgmt.web.Controllers
{
    [ApiController]
    [Route("api/personnel")]
    [Authorize(Roles = "ADMIN")]
    public class PersonnelController : ControllerBase
    {
        private readonly IPersonnelService _service;
        private readonly ILogger<PersonnelController> _logger;

        public PersonnelController(IPersonnelService service, ILogger<PersonnelController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ── GET /api/personnel?page=1&pageSize=20 ────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var result = await _service.GetAllAsync(page, pageSize, baseUrl);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching personnel list");
                return StatusCode(500, new { message = "Failed to retrieve personnel records." });
            }
        }

        // ── GET /api/personnel/{id} ──────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var result = await _service.GetByIdAsync(id, baseUrl);
                if (result == null)
                    return NotFound(new { message = $"Personnel with id {id} not found." });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching personnel id={Id}", id);
                return StatusCode(500, new { message = "Failed to retrieve personnel record." });
            }
        }

        // ── POST /api/personnel  (multipart/form-data) ───────────────────────
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] PersonnelCreateDto dto, IFormFile? photo)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Validation failed.", errors = ModelState });

            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var result = await _service.CreateAsync(dto, photo, baseUrl);
                return CreatedAtAction(nameof(GetById), new { id = result.Id },
                    new { message = "Personnel record created successfully.", data = result });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating personnel");
                return StatusCode(500, new { message = "Failed to create personnel record." });
            }
        }

        // ── PUT /api/personnel/{id}  (multipart/form-data) ───────────────────
        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] PersonnelCreateDto dto, IFormFile? photo)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Validation failed.", errors = ModelState });

            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var result = await _service.UpdateAsync(id, dto, photo, baseUrl);
                return Ok(new { message = "Personnel record updated successfully.", data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating personnel id={Id}", id);
                return StatusCode(500, new { message = "Failed to update personnel record." });
            }
        }

        // ── DELETE /api/personnel/{id} ───────────────────────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Personnel record deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting personnel id={Id}", id);
                return StatusCode(500, new { message = "Failed to delete personnel record." });
            }
        }
    }
}
