using Microsoft.AspNetCore.Mvc;
using invmgmt.web.Data;
[Route("api/[controller]")]
[ApiController]
public class ItemCategoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ItemCategoryController> _logger;

    public ItemCategoryController(AppDbContext context, ILogger<ItemCategoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetCategories()
    {
        _logger.LogInformation("Categories requested");
        var categories = _context.Categories.ToList();
        return Ok(categories);
    }
}
