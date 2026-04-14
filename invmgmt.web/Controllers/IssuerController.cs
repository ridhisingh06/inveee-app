using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.Models.Enums;

namespace invmgmt.web.Controllers
{
    [Route("api/issuer")]
    [ApiController]
    public class IssuerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IssuerController(AppDbContext context)
        {
            _context = context;
        }

        // 📦 ISSUE ITEMS
        [Authorize(Roles = "Issuer")]
        [HttpPost("issue/{id}")]
        public async Task<IActionResult> Issue(int id)
        {
            var request = await _context.Requests
                .Include(r => r.RequestItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("Request not found");

            if (request.Status != RequestStatus.Approved)
                return BadRequest("Only approved requests can be issued");

            foreach (var item in request.RequestItems)
            {
                var stock = await _context.InventoryStocks
                    .FirstOrDefaultAsync(s => s.ItemId == item.ItemId);

                if (stock == null)
                    return BadRequest("Stock not found");

                if (stock.AvailableQuantity < item.QuantityApproved)
                    return BadRequest("Not enough stock");

                // 🔥 STOCK REDUCE
                stock.AvailableQuantity -= item.QuantityApproved;

                item.QuantityIssued = item.QuantityApproved;
            }

            request.Status = RequestStatus.Issued;
            request.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok("Items Issued Successfully");
        }
    }
}