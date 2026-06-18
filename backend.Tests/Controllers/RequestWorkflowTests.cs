using System.Security.Claims;
using invmgmt.web.Controllers;
using invmgmt.web.Data;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.Repositories;
using invmgmt.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace invmgmt.web.Tests.Controllers;

public class RequestWorkflowTests
{
    [Fact]
    public async Task Scenario1_AdminRejects_IssuedRequest_UserSeesRejected()
    {
        await using var db = CreateDbContext();
        await SeedUsersAndInventory(db);

        var requestService = CreateRequestService(db);
        var createResult = await requestService.CreateRequestAsync(
            userId: 1,
            new CreateRequestFromCartDto
            {
                Items = new List<CreateRequestLineDto>
                {
                    new() { ItemId = 1, Quantity = 1 }
                }
            });

        Assert.True(createResult.Success);
        Assert.NotNull(createResult.RequestId);

        var requestId = createResult.RequestId.Value;
        var createdRequest = await db.Requests.FindAsync(requestId);
        Assert.Equal(RequestStatus.PendingWithIssuer, createdRequest!.Status);

        var issuerController = CreateRequestsController(db, requestService, userId: 2, role: "ISSUER");
        var issuerSearchResult = await issuerController.Get(status: "PendingWithIssuer");
        AssertRequestListCount(issuerSearchResult, expectedCount: 1);

        var issueResult = await issuerController.Issue(requestId);
        Assert.IsType<OkObjectResult>(issueResult);

        var afterIssue = await db.Requests.FindAsync(requestId);
        var monitorStock = await db.InventoryStocks.SingleAsync(x => x.ItemId == 1);
        Assert.Equal(RequestStatus.PendingAdminApproval, afterIssue!.Status);
        Assert.Equal(4, monitorStock.AvailableQuantity);
        Assert.Equal(1, await db.IssueLogs.CountAsync(x => x.RequestId == requestId));
        Assert.Contains(await db.ApprovalLogs.Where(x => x.RequestId == requestId).Select(x => x.Status).ToListAsync(),
            x => x == "PendingWithIssuer -> PendingAdminApproval");

        var adminController = CreateRequestsController(db, requestService, userId: 3, role: "ADMIN");
        var adminIssuedResult = await adminController.Get(status: "PendingAdminApproval");
        AssertRequestListCount(adminIssuedResult, expectedCount: 1);

        var rejectResult = await adminController.Reject(requestId);
        Assert.IsType<OkObjectResult>(rejectResult);

        var afterReject = await db.Requests.FindAsync(requestId);
        Assert.Equal(RequestStatus.Rejected, afterReject!.Status);
        Assert.Contains(await db.ApprovalLogs.Where(x => x.RequestId == requestId).Select(x => x.Status).ToListAsync(),
            x => x == "PendingAdminApproval -> Rejected");

        var userController = CreateRequestsController(db, requestService, userId: 1, role: "USER");
        var userListResult = await userController.Get();
        AssertRequestListContainsStatus(userListResult, RequestStatus.Rejected.ToString());
    }

    [Fact]
    public async Task RoleQueries_OnlyIssuerSeesPendingWithIssuer_OnlyAdminSeesPendingAdminApproval()
    {
        await using var db = CreateDbContext();
        await SeedUsersAndInventory(db);
        var requestService = CreateRequestService(db);

        db.Requests.AddRange(
            new Request { Id = 1, UserId = 1, Status = RequestStatus.PendingWithIssuer, CreatedAt = DateTime.UtcNow },
            new Request { Id = 2, UserId = 1, Status = RequestStatus.PendingAdminApproval, CreatedAt = DateTime.UtcNow });
        db.RequestItems.AddRange(
            new RequestItem { Id = 1, RequestId = 1, ItemId = 1, QuantityRequested = 1, Status = RequestItemStatus.PendingWithIssuer },
            new RequestItem { Id = 2, RequestId = 2, ItemId = 2, QuantityRequested = 1, Status = RequestItemStatus.PendingAdminApproval });
        await db.SaveChangesAsync();

        var issuerController = CreateRequestsController(db, requestService, userId: 2, role: "ISSUER");
        var adminController = CreateRequestsController(db, requestService, userId: 3, role: "ADMIN");

        AssertRequestListCount(await issuerController.Get(status: "PendingWithIssuer"), expectedCount: 1);
        AssertRequestListCount(await adminController.Get(status: "PendingAdminApproval"), expectedCount: 1);
        Assert.IsType<BadRequestObjectResult>(await issuerController.Get(status: "PendingAdminApproval"));
        Assert.IsType<BadRequestObjectResult>(await adminController.Get(status: "PendingWithIssuer"));
    }

    [Fact]
    public void WorkflowEndpoints_RequireExpectedRoles()
    {
        AssertEndpointRoles(nameof(RequestsController.Issue), "ISSUER");
        AssertEndpointRoles(nameof(RequestsController.IssueItem), "ISSUER");
        AssertEndpointRoles(nameof(RequestsController.NotIssueItem), "ISSUER");
        AssertEndpointRoles(nameof(RequestsController.Approve), "ADMIN");
        AssertEndpointRoles(nameof(RequestsController.ApproveItem), "ADMIN");
        AssertEndpointRoles(nameof(RequestsController.RejectItem), "ADMIN");
        AssertEndpointRoles(nameof(RequestsController.Reject), "ISSUER,ADMIN");
    }

    [Fact]
    public async Task Scenario2_MultipleItems_CanFinishWithIndependentStatuses()
    {
        await using var db = CreateDbContext();
        await SeedUsersAndInventory(db);

        var requestService = CreateRequestService(db);
        var createResult = await requestService.CreateRequestAsync(
            userId: 1,
            new CreateRequestFromCartDto
            {
                Items = new List<CreateRequestLineDto>
                {
                    new() { ItemId = 2, Quantity = 2 },
                    new() { ItemId = 3, Quantity = 1 },
                    new() { ItemId = 4, Quantity = 10 }
                }
            });

        Assert.True(createResult.Success);
        var requestId = createResult.RequestId!.Value;
        var requestItems = await db.RequestItems.OrderBy(ri => ri.Id).ToListAsync();
        var keyboard = requestItems.Single(ri => ri.ItemId == 2);
        var chair = requestItems.Single(ri => ri.ItemId == 3);
        var pen = requestItems.Single(ri => ri.ItemId == 4);

        var issuerController = CreateRequestsController(db, requestService, userId: 2, role: "ISSUER");
        Assert.IsType<OkObjectResult>(await issuerController.IssueItem(requestId, keyboard.Id));
        Assert.IsType<OkObjectResult>(await issuerController.NotIssueItem(requestId, chair.Id));
        Assert.IsType<OkObjectResult>(await issuerController.IssueItem(requestId, pen.Id));

        var keyboardStock = await db.InventoryStocks.SingleAsync(x => x.ItemId == 2);
        var chairStock = await db.InventoryStocks.SingleAsync(x => x.ItemId == 3);
        var penStock = await db.InventoryStocks.SingleAsync(x => x.ItemId == 4);
        Assert.Equal(8, keyboardStock.AvailableQuantity);
        Assert.Equal(5, chairStock.AvailableQuantity);
        Assert.Equal(90, penStock.AvailableQuantity);

        var adminController = CreateRequestsController(db, requestService, userId: 3, role: "ADMIN");
        Assert.IsType<OkObjectResult>(await adminController.ApproveItem(requestId, keyboard.Id));
        Assert.IsType<OkObjectResult>(await adminController.RejectItem(requestId, pen.Id));

        var finalItems = await db.RequestItems.OrderBy(ri => ri.Id).ToListAsync();
        Assert.Equal(RequestItemStatus.Approved, finalItems.Single(ri => ri.ItemId == 2).Status);
        Assert.Equal(RequestItemStatus.NotIssued, finalItems.Single(ri => ri.ItemId == 3).Status);
        Assert.Equal(RequestItemStatus.Rejected, finalItems.Single(ri => ri.ItemId == 4).Status);

        var userController = CreateRequestsController(db, requestService, userId: 1, role: "USER");
        var userListResult = await userController.Get();
        var ok = Assert.IsType<OkObjectResult>(userListResult);
        var rows = GetResponseProperty<IEnumerable<object>>(ok.Value!, "data");
        var row = Assert.Single(rows);
        var items = GetResponseProperty<IEnumerable<object>>(row, "items").ToList();
        Assert.Contains(items, item => GetResponseProperty<string>(item, "itemName") == "Keyboard" && GetResponseProperty<string>(item, "status") == RequestItemStatus.Approved.ToString());
        Assert.Contains(items, item => GetResponseProperty<string>(item, "itemName") == "Chair" && GetResponseProperty<string>(item, "status") == RequestItemStatus.NotIssued.ToString());
        Assert.Contains(items, item => GetResponseProperty<string>(item, "itemName") == "Pen" && GetResponseProperty<string>(item, "status") == RequestItemStatus.Rejected.ToString());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static RequestService CreateRequestService(AppDbContext db)
    {
        return new RequestService(new RequestRepository(db), NullLogger<RequestService>.Instance);
    }

    private static RequestsController CreateRequestsController(AppDbContext db, IRequestService requestService, int userId, string role)
    {
        var controller = new RequestsController(db, requestService, NullLogger<RequestsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreatePrincipal(userId, role)
            }
        };
        return controller;
    }

    private static ClaimsPrincipal CreatePrincipal(int userId, string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuth"));
    }

    private static async Task SeedUsersAndInventory(AppDbContext db)
    {
        db.Users.AddRange(
            new User { Id = 1, Username = "Inventory User", Email = "user@test.local", Role = "USER", IsApproved = true },
            new User { Id = 2, Username = "Issuer", Email = "issuer@test.local", Role = "ISSUER", IsApproved = true },
            new User { Id = 3, Username = "Admin", Email = "admin@test.local", Role = "ADMIN", IsApproved = true });

        db.Categories.Add(new Category { Id = 1, Name = "Hardware" });
        db.Items.AddRange(
            new Item { Id = 1, Name = "Monitor", CategoryId = 1 },
            new Item { Id = 2, Name = "Keyboard", CategoryId = 1 },
            new Item { Id = 3, Name = "Chair", CategoryId = 1 },
            new Item { Id = 4, Name = "Pen", CategoryId = 1 });
        db.InventoryStocks.AddRange(
            new InventoryStock { Id = 1, ItemId = 1, TotalQuantity = 5, AvailableQuantity = 5 },
            new InventoryStock { Id = 2, ItemId = 2, TotalQuantity = 10, AvailableQuantity = 10 },
            new InventoryStock { Id = 3, ItemId = 3, TotalQuantity = 5, AvailableQuantity = 5 },
            new InventoryStock { Id = 4, ItemId = 4, TotalQuantity = 100, AvailableQuantity = 100 });

        await db.SaveChangesAsync();
    }

    private static void AssertRequestListCount(IActionResult result, int expectedCount)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var data = GetResponseProperty<IEnumerable<object>>(ok.Value!, "data");
        Assert.Equal(expectedCount, data.Count());
    }

    private static void AssertRequestListContainsStatus(IActionResult result, string expectedStatus)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var data = GetResponseProperty<IEnumerable<object>>(ok.Value!, "data");
        Assert.Contains(data, row => GetResponseProperty<string>(row, "status") == expectedStatus);
    }

    private static T GetResponseProperty<T>(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        var propertyValue = property.GetValue(value);
        Assert.NotNull(propertyValue);
        return Assert.IsAssignableFrom<T>(propertyValue);
    }

    private static void AssertEndpointRoles(string methodName, string expectedRoles)
    {
        var method = typeof(RequestsController).GetMethods().Single(x => x.Name == methodName);
        var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(expectedRoles, authorize.Roles);
    }
}
