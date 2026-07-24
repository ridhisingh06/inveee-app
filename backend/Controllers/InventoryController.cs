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

    // Helper to verify a category exists (0 = No category / uncategorized)
    private async Task<bool> CategoryExistsAsync(int categoryId)
    {
        if (categoryId == 0) return true;
        return await _context.Categories.AnyAsync(c => c.Id == categoryId);
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
                itemCode = i.ItemCode,
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
        
        _logger.LogInformation("Add item requested: {ItemCode}, {Name} (CategoryId={CategoryId}, Qty={Qty})", dto.ItemCode, dto.Name, dto.CategoryId, dto.TotalQuantity);
        
        // ✅ Validate ItemCode is provided
        if (string.IsNullOrWhiteSpace(dto.ItemCode))
        {
            return BadRequest(new { message = "Item Code is required." });
        }

        // ✅ Validate name and check for duplicate item name (case-insensitive)
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Item name is required." });
        }

        // ✅ Check for duplicate ItemCode
        var normalizedItemCode = dto.ItemCode.Trim();
        var existingByItemCode = await _context.Items
            .FirstOrDefaultAsync(i => i.ItemCode == normalizedItemCode);
        
        if (existingByItemCode != null)
        {
            _logger.LogWarning("Duplicate ItemCode attempted: {ItemCode}", dto.ItemCode);
            return BadRequest(new { 
                message = $"An item with the Item Code \"{dto.ItemCode}\" already exists. Please enter a unique Item Code." 
            });
        }

        var normalizedName = dto.Name.Trim().ToLower();
        var existingItem = await _context.Items
            .FirstOrDefaultAsync(i => i.Name != null && i.Name.ToLower() == normalizedName);
        
        if (existingItem != null)
        {
            _logger.LogWarning("Duplicate item name attempted: {Name}", dto.Name);
            return BadRequest(new { 
                message = $"An item with the name \"{dto.Name}\" already exists. Please use a different name." 
            });
        }
        
        // Verify CategoryId if provided
        if (!await CategoryExistsAsync(dto.CategoryId))
        {
            _logger.LogWarning("AddItem: CategoryId {CategoryId} does not exist.", dto.CategoryId);
            return BadRequest(new { message = $"Category with Id {dto.CategoryId} does not exist." });
        }

        var item = new Item
        {
            ItemCode = normalizedItemCode,
            Name = dto.Name,
            CategoryId = dto.CategoryId,
            Description = dto.Description,
            IsActive = true
        };

        try
        {
            _context.Items.Add(item);
            await _context.SaveChangesAsync(); // generate Id
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "AddItem: DB error while inserting ItemCode {ItemCode}", dto.ItemCode);
            return BadRequest(new { message = "Unable to save the item – check data constraints." });
        }

        var stock = new InventoryStock
        {
            ItemId = item.Id,
            TotalQuantity = dto.TotalQuantity,
            AvailableQuantity = dto.TotalQuantity,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            _context.InventoryStocks.Add(stock);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "AddItem: DB error while inserting stock for ItemCode {ItemCode}", item.ItemCode);
            // Roll back previously added item to keep DB consistent
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return BadRequest(new { message = "Unable to save the stock record – check data constraints." });
        }

        _logger.LogInformation("Item added: ItemCode={ItemCode}", item.ItemCode);
        return Ok(new { message = "Item Added Successfully" });
    }

    //  UPDATE ITEM + STOCK
    [Authorize(Roles = "ADMIN,ISSUER")]
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] AddItemDto dto)
    {
        if (dto == null) return BadRequest("Invalid item data.");
        
        _logger.LogInformation("Update item requested: Id={Id}", id);
        var item = await _context.Items.FindAsync(id);

        if (item == null)
            return NotFound("Item not found");

        // ✅ Validate name and check for duplicate item name (case-insensitive), excluding current item
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Item name is required." });
        }

        var normalizedName = dto.Name.Trim().ToLower();
        var existingItem = await _context.Items
            .FirstOrDefaultAsync(i => i.Id != id && i.Name != null && i.Name.ToLower() == normalizedName);
        
        if (existingItem != null)
        {
            _logger.LogWarning("Duplicate item name attempted during update: {Name}", dto.Name);
            return BadRequest(new { 
                message = $"An item with the name \"{dto.Name}\" already exists. Please use a different name." 
            });
        }

        // Verify CategoryId if provided
        if (!await CategoryExistsAsync(dto.CategoryId))
        {
            _logger.LogWarning("UpdateItem: CategoryId {CategoryId} does not exist.", dto.CategoryId);
            return BadRequest(new { message = $"Category with Id {dto.CategoryId} does not exist." });
        }

        item.Name = dto.Name;
        item.CategoryId = dto.CategoryId;
        item.Description = dto.Description;

        var stock = await _context.InventoryStocks
            .FirstOrDefaultAsync(s => s.ItemId == item.Id);

        if (stock != null)
        {
            stock.TotalQuantity = dto.TotalQuantity;        //  FIX
            stock.AvailableQuantity = dto.TotalQuantity;    //  FIX
            stock.UpdatedAt = DateTime.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "UpdateItem: DB error while updating ItemCode {ItemCode}", item.ItemCode);
            return BadRequest(new { message = "Unable to update the item – check data constraints." });
        }

        _logger.LogInformation("Item updated: Id={Id}", id);

        // Load category name for the response
        var categoryName = "Uncategorized";
        if (item.CategoryId > 0)
        {
            var cat = await _context.Categories.FindAsync(item.CategoryId);
            if (cat != null) categoryName = cat.Name ?? categoryName;
        }

        return Ok(new
        {
            id = item.Id,
            itemCode = item.ItemCode,
            name = item.Name,
            categoryId = item.CategoryId,
            category = categoryName,
            availableQuantity = stock?.AvailableQuantity ?? dto.TotalQuantity,
            totalQuantity = stock?.TotalQuantity ?? dto.TotalQuantity,
            description = item.Description,
            createdDate = item.CreatedAt
        });
    }

    //  DELETE ITEM
    [Authorize(Roles="ADMIN,ISSUER")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        _logger.LogInformation("Delete item requested: Id={Id}", id);
        var item = await _context.Items.FindAsync(id);

        if (item == null)
            return NotFound("Item not found");

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Item deleted: Id={Id}", id);
        return Ok(new { message = "Item Deleted" });
    }

    //  INCREASE STOCK
    [Authorize(Roles = "ADMIN,ISSUER")]
    [HttpPatch("{id}/increase-stock")]
    public async Task<IActionResult> IncreaseStock(int id, [FromBody] StockChangeDto dto)
    {
        if (dto == null || dto.Quantity <= 0)
            return BadRequest("Quantity must be greater than 0");

        _logger.LogInformation("Increase stock requested: Id={Id}, Quantity={Qty}", id, dto.Quantity);

        var item = await _context.Items.FindAsync(id);
        if (item == null)
            return NotFound("Item not found");

        var stock = await _context.InventoryStocks
            .Include(s => s.Item)
            .ThenInclude(i => i.Category)
            .FirstOrDefaultAsync(s => s.ItemId == item.Id);

        if (stock == null)
            return NotFound("Item stock not found");

        stock.AvailableQuantity += dto.Quantity;
        stock.TotalQuantity += dto.Quantity;
        stock.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock increased: ItemId={ItemId}, NewAvailable={Available}", item.Id, stock.AvailableQuantity);

        return Ok(new
        {
            message = "Stock increased successfully",
            id = stock.Item.Id,
            itemCode = stock.Item.ItemCode,
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

        _logger.LogInformation("Decrease stock requested: Id={Id}, Quantity={Qty}", id, dto.Quantity);

        var item = await _context.Items.FindAsync(id);
        if (item == null)
            return NotFound("Item not found");

        var stock = await _context.InventoryStocks
            .Include(s => s.Item)
            .ThenInclude(i => i.Category)
            .FirstOrDefaultAsync(s => s.ItemId == item.Id);

        if (stock == null)
            return NotFound("Item stock not found");

        if (stock.AvailableQuantity < dto.Quantity)
            return BadRequest($"Insufficient stock. Available: {stock.AvailableQuantity}, Requested: {dto.Quantity}");

        stock.AvailableQuantity -= dto.Quantity;
        stock.TotalQuantity -= dto.Quantity;
        stock.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock decreased: ItemId={ItemId}, NewAvailable={Available}", item.Id, stock.AvailableQuantity);

        return Ok(new
        {
            message = "Stock decreased successfully",
            id = stock.Item.Id,
            itemCode = stock.Item.ItemCode,
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
        _logger.LogInformation("Get item requested: Id={Id}", id);

        var item = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.InventoryStock)
            .Where(i => i.Id == id)
            .Select(i => new
            {
                id = i.Id,
                itemCode = i.ItemCode,
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

