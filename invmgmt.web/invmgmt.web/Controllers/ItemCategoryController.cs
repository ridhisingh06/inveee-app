using Microsoft.AspNetCore.Mvc;
[Route("api/[controller]")]
[ApiController]
public class ItemCategoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public ItemCategoryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetCategories()
    {
        var categories = _context.Categories.ToList();
        return Ok(categories);
    }
}