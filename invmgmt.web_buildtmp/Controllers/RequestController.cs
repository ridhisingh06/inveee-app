using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.DTOs;
using invmgmt.web.Utils;

namespace invmgmt.web.Controllers
{
    [Route("api/request")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RequestController(AppDbContext context)
        {
            _context = context;
        }

        // CREATE REQUEST (from cart)
        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestFromCartDto dto)
        {
            if (dto == null || dto.Items == null || dto.Items.Count == 0)
            {
                return BadRequest(new { message = "Cart is empty" });
            }

            if (dto.Items.Any(i => i.ItemId <= 0 || i.Quantity <= 0))
            {
                return BadRequest(new { message = "Invalid item or quantity" });
            }

            var userId = User.GetUserId();
            var itemIds = dto.Items.Select(i => i.ItemId).Distinct().ToList();

            // Rule: user cannot request same item again while an active request exists for that item
            // in Pending/Approved/Issued.
            var activeStatuses = new[] { RequestStatus.Pending, RequestStatus.Approved, RequestStatus.Issued };
            var inProcess = await _context.RequestItems
                .Where(ri =>
                    itemIds.Contains(ri.ItemId) &&
                    activeStatuses.Contains(ri.Request.Status) &&
                    ri.Request.UserId == userId)
                .Include(ri => ri.Item)
                .ToListAsync();

            if (inProcess.Count > 0)
            {
                return BadRequest(new
                {
                    message = "Item already requested and in process",
                    items = inProcess.Select(x => new { x.ItemId, name = x.Item.Name }).Distinct()
                });
            }

            // Validate item ids exist
            var existingItems = await _context.Items
                .Where(i => itemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.CategoryId })
                .ToListAsync();

            var missing = itemIds.Except(existingItems.Select(i => i.Id)).ToList();
            if (missing.Count > 0)
            {
                return BadRequest(new { message = "One or more items not found", itemIds = missing });
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            var request = new Request
            {
                UserId = userId,
                CategoryId = dto.CategoryId,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            foreach (var line in dto.Items)
            {
                _context.RequestItems.Add(new RequestItem
                {
                    RequestId = request.Id,
                    ItemId = line.ItemId,
                    QuantityRequested = line.Quantity,
                    QuantityApproved = 0,
                    QuantityIssued = 0
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return CreatedAtAction(nameof(GetById), new { id = request.Id }, new { id = request.Id });
        }

        // GET MY REQUESTS
        [Authorize(Roles = "User")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var userId = User.GetUserId();

            var data = await _context.Requests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RequestSummaryDto
                {
                    Id = r.Id,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();

            return Ok(data);
        }

        // GET REQUEST DETAILS (User/Admin/Issuer)
        [Authorize(Roles = "User,Admin,Issuer")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _context.Requests
                .Include(r => r.RequestItems)
                    .ThenInclude(ri => ri.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound(new { message = "Request not found" });
            }

            if (User.IsInRole("User"))
            {
                var userId = User.GetUserId();
                if (request.UserId != userId)
                {
                    return Forbid();
                }
            }

            var dto = new RequestDetailDto
            {
                Id = request.Id,
                UserId = request.UserId,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                Items = request.RequestItems.Select(ri => new RequestItemDetailDto
                {
                    Id = ri.Id,
                    ItemId = ri.ItemId,
                    ItemName = ri.Item.Name,
                    QuantityRequested = ri.QuantityRequested,
                    QuantityApproved = ri.QuantityApproved,
                    QuantityIssued = ri.QuantityIssued
                }).ToList()
            };

            return Ok(dto);
        }

        // CONFIRM RECEIVED
        [Authorize(Roles = "User")]
        [HttpPost("{id:int}/confirm-received")]
        public async Task<IActionResult> ConfirmReceived(int id)
        {
            var userId = User.GetUserId();
            var request = await _context.Requests.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (request == null)
            {
                return NotFound(new { message = "Request not found" });
            }

            if (request.Status != RequestStatus.Issued)
            {
                return BadRequest(new { message = "Only issued requests can be marked as received" });
            }

            request.Status = RequestStatus.Received;
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Request marked as received" });
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
