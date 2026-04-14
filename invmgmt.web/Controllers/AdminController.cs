using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using invmgmt.web.Data;
using invmgmt.web.Models;
[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    // approve request
    [Authorize(Roles = "Admin")]
    [HttpPost("approve/{id}")]
    public IActionResult Approve(int id)
    {
        return Ok($"Request {id} approved");
    }

    // reject request
    [Authorize(Roles = "Admin")]
    [HttpPost("reject/{id}")]
    public IActionResult Reject(int id)
    {
        return Ok($"Request {id} rejected");
    }

    // pending requests
    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public IActionResult GetPending()
    {
        return Ok("Pending requests list");
    }
}