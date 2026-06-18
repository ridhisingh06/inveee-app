using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using invmgmt.web.DTOs;
using invmgmt.web.Services;
using invmgmt.web.Utils;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace invmgmt.web.Controllers
{
    [Route("api/request")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly IRequestService _requestService;
        private readonly ILogger<RequestController> _logger;

        public RequestController(IRequestService requestService, ILogger<RequestController> logger)
        {
            _requestService = requestService;
            _logger = logger;
        }

        // CREATE REQUEST (from cart)
        [Authorize(Roles = "USER")]
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestFromCartDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _requestService.CreateRequestAsync(userId, dto);
                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return CreatedAtAction(nameof(GetById), new { id = result.RequestId }, new { id = result.RequestId, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateRequest");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        // GET MY REQUESTS
        [Authorize(Roles = "USER")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMy([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.GetUserId();
                var data = await _requestService.GetUserRequestsAsync(userId, pageNumber, pageSize);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetMy");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        // GET REQUEST DETAILS (User/Admin/Issuer)
        [Authorize(Roles = "USER,ADMIN,ISSUER")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var userId = User.GetUserId();
                var role = User.IsInRole("ADMIN") ? "ADMIN" : User.IsInRole("ISSUER") ? "ISSUER" : "USER";
                
                var dto = await _requestService.GetRequestByIdAsync(id, userId, role);

                if (dto == null)
                {
                    return NotFound(new { message = "Request not found or access denied" });
                }

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetById");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        // CONFIRM RECEIVED
        [Authorize(Roles = "USER")]
        [HttpPost("{id:int}/confirm-received")]
        public async Task<IActionResult> ConfirmReceived(int id)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _requestService.ConfirmReceivedAsync(id, userId);

                if (!result.Success)
                {
                    if (result.Message == "Request not found") return NotFound(new { message = result.Message });
                    return BadRequest(new { message = result.Message });
                }

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ConfirmReceived");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        //  GET PENDING (ADMIN)
        [Authorize(Roles = "ADMIN")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var data = await _requestService.GetPendingRequestsAsync(pageNumber, pageSize);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetPending");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        //  APPROVE REQUEST (ADMIN)
        [Authorize(Roles = "ADMIN")]
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var result = await _requestService.ApproveRequestAsync(id);
                if (!result.Success) return BadRequest(new { message = result.Message });
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Approve");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        //  REJECT REQUEST (ADMIN)
        [Authorize(Roles = "ADMIN")]
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var result = await _requestService.RejectRequestAsync(id);
                if (!result.Success) return BadRequest(new { message = result.Message });
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Reject");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        //  RECEIVE ITEMS
        [Authorize(Roles = "USER")]
        [HttpPost("receive/{id}")]
        public async Task<IActionResult> Receive(int id)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _requestService.ConfirmReceivedAsync(id, userId);

                if (!result.Success)
                {
                    if (result.Message == "Request not found") return NotFound(new { message = result.Message });
                    return BadRequest(new { message = result.Message });
                }

                return Ok(new { message = "Items Received Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Receive");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }
        
        [Authorize(Roles = "USER")]
        [HttpGet("can-request")]
        public async Task<IActionResult> CanRequest()
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _requestService.CheckCanRequestAsync(userId);
                return Ok(new { canRequest = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CanRequest");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }

        // CANCEL / DELETE REQUEST (USER - only Requested or Pending)
        [Authorize(Roles = "USER")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _requestService.DeleteRequestAsync(id, userId);
                if (!result.Success)
                    return BadRequest(new { message = result.Message });

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteRequest");
                return StatusCode(500, new { message = "An internal server error occurred.", error = ex.Message });
            }
        }
    }
}
