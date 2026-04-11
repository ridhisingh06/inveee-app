using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace invmgmt.web.Controllers
{
    [Route("api/request[controller]")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        //  sirf user Request create karega
        [Authorize(Roles = "User")]
        [HttpPost("create")]
        public IActionResult CreateRequest()
        {
            return Ok("Request created");
        }
    }

    [Route("api/approval[controller]")]
    [ApiController]
    public class ApprovalController : ControllerBase
    {
        //  APPROVER → approve karega
        [Authorize(Roles = "Approver")]
        [HttpPost("approve/{id}")]
        public IActionResult Approve(int id)
        {
            return Ok($"Request {id} approved");
        }

        // ADMIN → pending requests dekhega
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public IActionResult GetPending()
        {
            return Ok("List of pending requests");
        }
    }
}