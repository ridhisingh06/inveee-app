using invmgmt.web.Controllers;
using invmgmt.web.Data;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace invmgmt.web.Tests.Controllers;

public class InventoryControllerTests
{
    [Fact]
    public async Task AddItem_CreatesItemAndStock()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        db.Categories.Add(new Category { Id = 1, Name = "Pens" });
        await db.SaveChangesAsync();

        var controller = new InventoryController(db, NullLogger<InventoryController>.Instance);

        var dto = new AddItemDto
        {
            Name = "Blue Pen",
            CategoryId = 1,
            Description = "Test pen",
            TotalQuantity = 10
        };

        var result = await controller.AddItem(dto);
        Assert.IsType<OkObjectResult>(result);

        var item = await db.Items.Include(i => i.InventoryStock).SingleAsync();
        Assert.Equal("Blue Pen", item.Name);
        Assert.NotNull(item.InventoryStock);
        Assert.Equal(10, item.InventoryStock!.TotalQuantity);
        Assert.Equal(10, item.InventoryStock!.AvailableQuantity);
    }
}

