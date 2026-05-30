using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Services;
using invmgmt.web.Tests.Fixtures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace invmgmt.web.Tests.Services
{
    public class SectionWiseQueryServiceTests : IDisposable
    {
        private readonly BaseTestFixture _fixture;
        private readonly SectionWiseQueryService _service;
        private readonly IMemoryCache _cache;

        public SectionWiseQueryServiceTests()
        {
            _fixture = new BaseTestFixture();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new SectionWiseQueryService(_fixture.DbContext, NullLogger<SectionWiseQueryService>.Instance, _cache);
        }

        public void Dispose()
        {
            _fixture.Dispose();
            _cache.Dispose();
        }

        [Fact]
        public async Task GetOfficersAsync_ShouldReturnOfficers_FromPersonnel()
        {
            // Arrange
            _fixture.DbContext.Personnel.Add(new Personnel { Name = "Officer 1", Building = "B1", Email = "o1@a.com" });
            _fixture.DbContext.Personnel.Add(new Personnel { Name = "Officer 2", Building = "B2", Email = "o2@a.com" });
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetOfficersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetBhawansAsync_ShouldReturnDistinctBhawans()
        {
            // Arrange
            _fixture.DbContext.Personnel.Add(new Personnel { Name = "Officer 1", Building = "B1", Email = "o1@a.com" });
            _fixture.DbContext.Personnel.Add(new Personnel { Name = "Officer 2", Building = "B1", Email = "o2@a.com" });
            _fixture.DbContext.Personnel.Add(new Personnel { Name = "Officer 3", Building = "B2", Email = "o3@a.com" });
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetBhawansAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("B1", result);
            Assert.Contains("B2", result);
        }

        [Fact]
        public async Task SearchItemsAsync_ShouldReturnMatchingItems()
        {
            // Arrange
            _fixture.DbContext.Categories.Add(new Category { Id = 1, Name = "Stationery" });
            _fixture.DbContext.Items.Add(new Item { Id = 1, Name = "Blue Pen", CategoryId = 1, IsActive = true, Description = "pen" });
            _fixture.DbContext.Items.Add(new Item { Id = 2, Name = "Red Pen", CategoryId = 1, IsActive = true, Description = "pen" });
            _fixture.DbContext.Items.Add(new Item { Id = 3, Name = "Marker", CategoryId = 1, IsActive = true, Description = "marker" });
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var result = await _service.SearchItemsAsync("Pen");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // ILike on Name and Description
        }

        [Fact]
        public async Task GetSectionWiseQueryAsync_ShouldFilterByDate()
        {
            // Arrange
            var user = new User { Id = 1, Email = "u1@a.com", Username = "U1" };
            _fixture.DbContext.Users.Add(user);
            _fixture.DbContext.Categories.Add(new Category { Id = 1, Name = "Stationery" });
            _fixture.DbContext.Items.Add(new Item { Id = 1, Name = "Blue Pen", CategoryId = 1, IsActive = true, Description = "pen" });
            
            var req1 = new Request { Id = 1, UserId = 1, User = user, CreatedAt = new DateTime(2023, 1, 10) };
            var req2 = new Request { Id = 2, UserId = 1, User = user, CreatedAt = new DateTime(2023, 1, 20) };
            _fixture.DbContext.Requests.AddRange(req1, req2);

            _fixture.DbContext.RequestItems.Add(new RequestItem { RequestId = 1, Request = req1, ItemId = 1 });
            _fixture.DbContext.RequestItems.Add(new RequestItem { RequestId = 2, Request = req2, ItemId = 1 });

            await _fixture.DbContext.SaveChangesAsync();

            var filter = new SectionWiseQueryFilterDto
            {
                FromDate = new DateTime(2023, 1, 1),
                ToDate = new DateTime(2023, 1, 15)
            };

            // Act
            var result = await _service.GetSectionWiseQueryAsync(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Data);
            Assert.Equal(1, result.Data.First().RequestId);
        }
    }
}
