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
                description = i.Description,
                createdDate = i.CreatedAt
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
        
        // ✅ Check for duplicate item name (case-insensitive)
        var normalizedName = dto.Name.Trim().ToLower();
        var existingItem = await _context.Items
            .FirstOrDefaultAsync(i => i.Name.ToLower() == normalizedName);
        
        if (existingItem != null)
        {
            _logger.LogWarning("Duplicate item attempted: {Name}", dto.Name);
            return BadRequest(new { 
                message = $"An item with the name \"{dto.Name}\" already exists. Please use a different name." 
            });
        }
        
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

        // ✅ Check for duplicate item name (case-insensitive), excluding current item
        var normalizedName = dto.Name.Trim().ToLower();
        var existingItem = await _context.Items
            .FirstOrDefaultAsync(i => i.Id != id && i.Name.ToLower() == normalizedName);
        
        if (existingItem != null)
        {
            _logger.LogWarning("Duplicate item name attempted during update: {Name}", dto.Name);
            return BadRequest(new { 
                message = $"An item with the name \"{dto.Name}\" already exists. Please use a different name." 
            });
        }

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

    //  INCREASE STOCK
    [Authorize(Roles = "ADMIN,ISSUER")]
    [HttpPatch("{id}/increase-stock")]
    public async Task<IActionResult> IncreaseStock(int id, [FromBody] StockChangeDto dto)
    {
        if (dto == null || dto.Quantity <= 0)
            return BadRequest("Quantity must be greater than 0");

        _logger.LogInformation("Increase stock requested: ItemId={ItemId}, Quantity={Qty}", id, dto.Quantity);

        var stock = await _context.InventoryStocks
            .Include(s => s.Item)
            .ThenInclude(i => i.Category)
            .FirstOrDefaultAsync(s => s.ItemId == id);

        if (stock == null)
            return NotFound("Item stock not found");

        stock.AvailableQuantity += dto.Quantity;
        stock.TotalQuantity += dto.Quantity;
        stock.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock increased: ItemId={ItemId}, NewAvailable={Available}", id, stock.AvailableQuantity);

        return Ok(new
        {
            message = "Stock increased successfully",
            id = stock.ItemId,
            name = stock.Item.Name,
            categoryId = stock.Item.CategoryId,
            category = stock.Item.Category != null ? stock.Item.Category.Name : "Uncategorized",
            availableQuantity = stock.AvailableQuantity,
            totalQuantity = stock.TotalQuantity,
            description = stock.Item.Description
        });
    }

    //  DECREASE STOCK
    [Authorize(Roles = "ADMIN,ISSUER")]
    [HttpPatch("{id}/decrease-stock")]
    public async Task<IActionResult> DecreaseStock(int id, [FromBody] StockChangeDto dto)
    {
        if (dto == null || dto.Quantity <= 0)
            return BadRequest("Quantity must be greater than 0");

        _logger.LogInformation("Decrease stock requested: ItemId={ItemId}, Quantity={Qty}", id, dto.Quantity);

        var stock = await _context.InventoryStocks
            .Include(s => s.Item)
            .ThenInclude(i => i.Category)
            .FirstOrDefaultAsync(s => s.ItemId == id);

        if (stock == null)
            return NotFound("Item stock not found");

        if (stock.AvailableQuantity < dto.Quantity)
            return BadRequest($"Insufficient stock. Available: {stock.AvailableQuantity}, Requested: {dto.Quantity}");

        stock.AvailableQuantity -= dto.Quantity;
        stock.TotalQuantity -= dto.Quantity;
        stock.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock decreased: ItemId={ItemId}, NewAvailable={Available}", id, stock.AvailableQuantity);

        return Ok(new
        {
            message = "Stock decreased successfully",
            id = stock.ItemId,
            name = stock.Item.Name,
            categoryId = stock.Item.CategoryId,
            category = stock.Item.Category != null ? stock.Item.Category.Name : "Uncategorized",
            availableQuantity = stock.AvailableQuantity,
            totalQuantity = stock.TotalQuantity,
            description = stock.Item.Description
        });
    }

    //  GET ITEM BY ID (for detailed view)
    [Authorize(Roles = "ADMIN,USER,ISSUER")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetItem(int id)
    {
        _logger.LogInformation("Get item requested: ItemId={ItemId}", id);

        var item = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.InventoryStock)
            .Where(i => i.Id == id)
            .Select(i => new
            {
                id = i.Id,
                name = i.Name,
                categoryId = i.CategoryId,
                category = i.Category != null ? i.Category.Name : "Uncategorized",
                availableQuantity = i.InventoryStock != null ? i.InventoryStock.AvailableQuantity : 0,
                totalQuantity = i.InventoryStock != null ? i.InventoryStock.TotalQuantity : 0,
                description = i.Description,
                createdAt = i.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound("Item not found");

        return Ok(item);
    }
}

