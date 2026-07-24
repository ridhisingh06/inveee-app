using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using invmgmt.web.DTOs;
using invmgmt.web.Services;

namespace invmgmt.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class BillsController : ControllerBase
    {
        private readonly IBillService _billService;
        private readonly ILogger<BillsController> _logger;

        public BillsController(IBillService billService, ILogger<BillsController> logger)
        {
            _billService = billService;
            _logger = logger;
        }

        /// <summary>
        /// Get initialization data (items and vendors) for the form
        /// GET /api/bills/init
        /// </summary>
        [HttpGet("init")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInitData()
        {
            try
            {
                _logger.LogInformation("Challan/Bill init data requested");
                var data = await _billService.GetInitDataAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting init data");
                return StatusCode(500, new { message = "Failed to fetch initialization data", error = ex.Message });
            }
        }

        /// <summary>
        /// Search items by query
        /// GET /api/bills/items/search?query=keyword
        /// </summary>
        [HttpGet("items/search")]
        public async Task<IActionResult> SearchItems([FromQuery] string query = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    _logger.LogInformation("Items search requested with empty query");
                }
                else
                {
                    _logger.LogInformation("Items search requested with query: {Query}", query);
                }

                var items = await _billService.SearchItemsAsync(query);
                return Ok(new { items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching items");
                return StatusCode(500, new { message = "Failed to search items", error = ex.Message });
            }
        }

        /// <summary>
        /// Get list of vendors/suppliers
        /// GET /api/bills/vendors
        /// </summary>
        [HttpGet("vendors")]
        public async Task<IActionResult> GetVendors()
        {
            try
            {
                _logger.LogInformation("Vendors list requested");
                var vendors = await _billService.GetVendorsAsync();
                return Ok(new { vendors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vendors");
                return StatusCode(500, new { message = "Failed to fetch vendors", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new bill with items
        /// POST /api/bills
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateBill([FromBody] CreateBillDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Invalid bill data" });
                }

                // Get current user ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim?.Value, out var userId))
                {
                    _logger.LogWarning("Could not extract user ID from claims");
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                _logger.LogInformation("Creating bill for user {UserId}: BillNo={BillNo}, Vendor={Vendor}, ItemCount={ItemCount}",
                    userId, dto.BillNo, dto.VendorName, dto.Items?.Count ?? 0);

                // Validate DTO
                if (string.IsNullOrWhiteSpace(dto.BillNo))
                    return BadRequest(new { message = "Bill number is required" });

                if (string.IsNullOrWhiteSpace(dto.VendorName))
                    return BadRequest(new { message = "Vendor name is required" });

                if (dto.Items == null || dto.Items.Count == 0)
                    return BadRequest(new { message = "At least one item must be added" });

                // Validate each item
                foreach (var item in dto.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.ItemCode))
                        return BadRequest(new { message = "Valid item Code is required" });

                    if (item.Quantity <= 0)
                        return BadRequest(new { message = $"Quantity must be greater than 0 for item {item.ItemCode}" });

                    if (item.UnitPrice < 0)
                        return BadRequest(new { message = $"Unit price cannot be negative for item {item.ItemCode}" });
                }

                var billDetail = await _billService.CreateBillAsync(dto, userId);
                
                _logger.LogInformation("Bill created successfully: BillId={BillId}", billDetail.Id);
                
                return CreatedAtAction(nameof(GetBillById), new { id = billDetail.Id },
                    new 
                    { 
                        message = "Bill created successfully",
                        id = billDetail.Id,
                        billNo = billDetail.BillNo,
                        grandTotal = billDetail.GrandTotal,
                        itemCount = billDetail.Items.Count
                    });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating bill");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation creating bill");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bill");
                return StatusCode(500, new { message = "Failed to create bill", error = ex.Message });
            }
        }

        /// <summary>
        /// Get bill details by ID
        /// GET /api/bills/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBillById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid bill ID" });

                _logger.LogInformation("Bill details requested: BillId={BillId}", id);

                var bill = await _billService.GetBillByIdAsync(id);
                
                if (bill == null)
                    return NotFound(new { message = "Bill not found" });

                return Ok(bill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bill details");
                return StatusCode(500, new { message = "Failed to fetch bill details", error = ex.Message });
            }
        }

        /// <summary>
        /// Get list of bills with pagination
        /// GET /api/bills?pageNumber=1&pageSize=20
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBills(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1)
                    pageNumber = 1;

                if (pageSize < 1 || pageSize > 100)
                    pageSize = 20;

                _logger.LogInformation("Bills list requested: Page={PageNumber}, Size={PageSize}", pageNumber, pageSize);

                var bills = await _billService.GetBillsAsync(pageNumber, pageSize);
                
                return Ok(new
                {
                    pageNumber,
                    pageSize,
                    count = bills.Count,
                    data = bills
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bills list");
                return StatusCode(500, new { message = "Failed to fetch bills list", error = ex.Message });
            }
        }
    }
}
