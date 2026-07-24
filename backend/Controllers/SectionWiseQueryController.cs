using invmgmt.web.DTOs;
using invmgmt.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace invmgmt.web.Controllers
{
    [Route("api/admin/section-wise-query")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class SectionWiseQueryController : ControllerBase
    {
        private readonly ISectionWiseQueryService _queryService;
        private readonly ILogger<SectionWiseQueryController> _logger;

        public SectionWiseQueryController(
            ISectionWiseQueryService queryService,
            ILogger<SectionWiseQueryController> logger)
        {
            _queryService = queryService;
            _logger = logger;
        }

        [HttpGet("officers")]
        public async Task<IActionResult> GetOfficers()
        {
            try
            {
                var officers = await _queryService.GetOfficersAsync();
                return Ok(new { officers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching section query officers");
                return StatusCode(500, new { message = "Failed to load officers", error = ex.Message });
            }
        }

        [HttpGet("bhawans")]
        public async Task<IActionResult> GetBhawans()
        {
            try
            {
                var bhawans = await _queryService.GetBhawansAsync();
                return Ok(new { bhawans });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bhawans");
                return StatusCode(500, new { message = "Failed to load bhawans", error = ex.Message });
            }
        }

        [HttpGet("items/search")]
        public async Task<IActionResult> SearchItems([FromQuery] string query = "")
        {
            try
            {
                var items = await _queryService.SearchItemsAsync(query);
                return Ok(new { items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching items");
                return StatusCode(500, new { message = "Failed to search items", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSectionWiseQuery([FromQuery] SectionWiseQueryFilterDto filter)
        {
            try
            {
                var result = await _queryService.GetSectionWiseQueryAsync(filter ?? new SectionWiseQueryFilterDto());
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid section query request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching section wise query");
                return StatusCode(500, new { message = "Failed to fetch section wise query results", error = ex.Message });
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportCsv([FromQuery] SectionWiseQueryFilterDto filter)
        {
            try
            {
                // Fetch full results (respect page size if provided; caller can set large pageSize)
                var result = await _queryService.GetSectionWiseQueryAsync(filter ?? new SectionWiseQueryFilterDto());

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("RequestItemId,RequestId,OfficerName,Bhawan,ItemId,ItemName,QuantityRequested,QuantityApproved,QuantityIssued,RequestStatus,RequestDate,RequestedBy");

                foreach (var r in result.Data)
                {
                    var line = string.Format("{0},{1},\"{2}\",\"{3}\",{4},\"{5}\",{6},{7},{8},\"{9}\",\"{10:yyyy-MM-dd HH:mm}\",\"{11}\"",
                        r.RequestItemId,
                        r.RequestId,
                        (r.OfficerName ?? string.Empty).Replace("\"", "\"\""),
                        (r.Bhawan ?? string.Empty).Replace("\"", "\"\""),
                        r.ItemCode,
                        (r.ItemName ?? string.Empty).Replace("\"", "\"\""),
                        r.QuantityRequested,
                        r.QuantityApproved,
                        r.QuantityIssued,
                        (r.RequestStatus ?? string.Empty),
                        r.RequestDate,
                        (r.RequestedBy ?? string.Empty)
                    );
                    sb.AppendLine(line);
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                var fileName = $"section-wise-query-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting CSV for section wise query");
                return StatusCode(500, new { message = "Failed to export CSV", error = ex.Message });
            }
        }
    }
}
