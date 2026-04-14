using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.DTOs;

namespace invmgmt.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RequestController(AppDbContext context)
        {
            _context = context;
        }

        // CREATE REQUEST
        [Authorize(Roles = "User")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto dto)
        {
            //  Re-request block logic
            var activeRequest = await _context.Requests
                .Where(r => r.UserId == dto.UserId &&
                       (r.Status == RequestStatus.Pending ||
                        r.Status == RequestStatus.Approved ||
                        r.Status == RequestStatus.Issued))
                .FirstOrDefaultAsync();

            if (activeRequest != null)
                return BadRequest("You already have an active request");

            //  Create Request
            var request = new Request
            {
                UserId = dto.UserId,
                CategoryId = dto.CategoryId,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            //  Add Items
            foreach (var item in dto.Items)
            {
                _context.RequestItems.Add(new RequestItem
                {
                    RequestId = request.Id,
                    ItemId = item.ItemId,
                    QuantityRequested = item.QuantityRequested,
                    QuantityApproved = 0,
                    QuantityIssued = 0
                });
            }

            await _context.SaveChangesAsync();

            return Ok("Request Created Successfully");
        }

        //  GET PENDING (ADMIN)
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var data = await _context.Requests
                .Where(r => r.Status == RequestStatus.Pending)
                .Include(r => r.User)
                .Include(r => r.RequestItems)
                    .ThenInclude(ri => ri.Item)
                .ToListAsync();

            return Ok(data);
        }

        //  APPROVE REQUEST (ADMIN)
        [Authorize(Roles = "Admin")]
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.Requests
                .Include(r => r.RequestItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("Request not found");

            foreach (var item in request.RequestItems)
            {
                item.QuantityApproved = item.QuantityRequested;
            }

            request.Status = RequestStatus.Approved;
            request.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok("Request Approved");
        }

        //  REJECT REQUEST (ADMIN)
        [Authorize(Roles = "Admin")]
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.Requests.FindAsync(id);

            if (request == null)
                return NotFound("Request not found");

            request.Status = RequestStatus.Rejected;
            request.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok("Request Rejected");
        }


        //  RECEIVE ITEMS
        [Authorize(Roles = "User")]
        [HttpPost("receive/{id}")]
        public async Task<IActionResult> Receive(int id)
        {
            var request = await _context.Requests.FindAsync(id);

            if (request == null)
                return NotFound();

            if (request.Status != RequestStatus.Issued)
                return BadRequest("Items not issued yet");

            request.Status = RequestStatus.Received;
            request.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok("Items Received Successfully");
        }
    }
}