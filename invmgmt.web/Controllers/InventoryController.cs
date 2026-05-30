using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.DTOs;

[Route("api/[controller]")]
[ApiController]
[Authorize]

public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<InventoryController> _logger;
    
    public InventoryController(AppDbContext context, ILogger<InventoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    //  GET ALL ITEMS
    [Authorize(Roles = "ADMIN,USER,ISSUER")]
    
    [HttpGet]
    public async Task<IActionResult> GetItems()
    {
        _logger.LogInformation("Inventory list requested");
        var items = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.InventoryStock)
            .Select(i => new
            {
                id = i.Id,
                name = i.Name,
                categoryId = i.CategoryId,
                category = i.Category != null ? i.Category.Name : "Uncategorized",
                availableQuantity = i.InventoryStock != null ? i.InventoryStock.AvailableQuantity : 0,
                totalQuantity = i.InventoryStock != null ? i.InventoryStock.TotalQuantity : 0,
                description = i.Description
            })
            .ToListAsync();

        return Ok(items);
    }

    //  ADD ITEM + STOCK
    [Authorize(Roles = "ADMIN,ISSUER")]
   
    [HttpPost]
    public async Task<IActionResult> AddItem([FromBody] AddItemDto dto)
    {
        if (dto == null) return BadRequest("Invalid item data.");
        
        _logger.LogInformation("Add item requested: {Name} (CategoryId={CategoryId}, Qty={Qty})", dto.Name, dto.CategoryId, dto.TotalQuantity);
        var item = new Item
        {
            Name = dto.Name,
            CategoryId = dto.CategoryId,
            Description = dto.Description,
            IsActive = true
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync(); //  item.Id generate

        var stock = new InventoryStock
        {
            ItemId = item.Id,
            TotalQuantity = dto.TotalQuantity,              //  FIX
            AvailableQuantity = dto.TotalQuantity,          //  FIX
            UpdatedAt = DateTime.UtcNow
        };

        _context.InventoryStocks.Add(stock);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Item added: ItemId={ItemId}", item.Id);
        return Ok(new { message = "Item Added Successfully" });
    }

    //  UPDATE ITEM + STOCK
    [Authorize(Roles = "ADMIN,ISSUER")]
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] AddItemDto dto)
    {
        if (dto == null) return BadRequest("Invalid item data.");
        
        _logger.LogInformation("Update item requested: ItemId={ItemId}", id);
        var item = await _context.Items.FindAsync(id);

        if (item == null)
            return NotFound("Item not found");

        item.Name = dto.Name;
        item.CategoryId = dto.CategoryId;
        item.Description = dto.Description;

        var stock = await _context.InventoryStocks
            .FirstOrDefaultAsync(s => s.ItemId == id);

        if (stock != null)
        {
            stock.TotalQuantity = dto.TotalQuantity;        //  FIX
            stock.AvailableQuantity = dto.TotalQuantity;    //  FIX
            stock.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Item updated: ItemId={ItemId}", id);
        return Ok(new { message = "Item Updated Successfully" });
    }

    //  DELETE ITEM
    [Authorize(Roles="ADMIN,ISSUER")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        _logger.LogInformation("Delete item requested: ItemId={ItemId}", id);
        var item = await _context.Items.FindAsync(id);

        if (item == null)
            return NotFound("Item not found");

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Item deleted: ItemId={ItemId}", id);
        return Ok(new { message = "Item Deleted" });
    }
}

