using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Services;
using invmgmt.web.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace invmgmt.web.Tests.Services
{
    public class BillServiceTests : IDisposable
    {
        private readonly BaseTestFixture _fixture;
        private readonly BillService _service;

        public BillServiceTests()
        {
            _fixture = new BaseTestFixture();
            _service = new BillService(_fixture.DbContext, NullLogger<BillService>.Instance);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task CreateBillAsync_ShouldFail_WhenBillNoIsMissing()
        {
            // Arrange
            var dto = new CreateBillDto { BillNo = "", VendorName = "Vendor" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateBillAsync(dto, 1));
            Assert.Contains("Bill number is required", ex.Message);
        }

        [Fact]
        public async Task CreateBillAsync_ShouldFail_WhenItemsAreEmpty()
        {
            // Arrange
            var dto = new CreateBillDto { BillNo = "B001", VendorName = "Vendor", Items = new List<CreateBillItemDto>() };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateBillAsync(dto, 1));
            Assert.Contains("At least one item must be added", ex.Message);
        }

        [Fact]
        public async Task CreateBillAsync_ShouldSucceed_AndCalculateTotal()
        {
            // Arrange
            _fixture.DbContext.Users.Add(new User { Id = 1, Email = "u@u.com", Username = "U" });
            _fixture.DbContext.Items.Add(new Item { Id = 1, Name = "Item 1", CategoryId = 1 });
            _fixture.DbContext.Categories.Add(new Category { Id = 1, Name = "Cat 1" });
            await _fixture.DbContext.SaveChangesAsync();

            var dto = new CreateBillDto
            {
                BillNo = "B001",
                VendorName = "Test Vendor",
                Items = new List<CreateBillItemDto>
                {
                    new CreateBillItemDto { ItemId = 1, Quantity = 5, UnitPrice = 10.5m }
                }
            };

            // Act
            var result = await _service.CreateBillAsync(dto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("B001", result.BillNo);
            Assert.Equal(52.5m, result.GrandTotal); // 5 * 10.5
            Assert.Single(result.Items);
            Assert.Equal(52.5m, result.Items[0].Amount);
        }

        [Fact]
        public async Task GetBillByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Act
            var result = await _service.GetBillByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBillByIdAsync_ShouldReturnBill()
        {
            // Arrange
            _fixture.DbContext.Users.Add(new User { Id = 1, Email = "u@u.com", Username = "U" });
            var bill = new Bill { Id = 1, BillNo = "B002", VendorName = "V1", CreatedByUserId = 1, GrandTotal = 100 };
            _fixture.DbContext.Bills.Add(bill);
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetBillByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("B002", result.BillNo);
        }
    }
}
