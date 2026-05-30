using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.Repositories;
using invmgmt.web.Services;
using invmgmt.web.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace invmgmt.web.Tests.Services
{
    public class RequestServiceTests : IDisposable
    {
        private readonly BaseTestFixture _fixture;
        private readonly RequestService _service;
        private readonly RequestRepository _requestRepo;

        public RequestServiceTests()
        {
            _fixture = new BaseTestFixture();
            _requestRepo = new RequestRepository(_fixture.DbContext);
            _service = new RequestService(_requestRepo, NullLogger<RequestService>.Instance);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task CreateRequestAsync_ShouldFail_WhenCartIsEmpty()
        {
            // Act
            var result = await _service.CreateRequestAsync(1, new CreateRequestFromCartDto { Items = new List<CreateRequestLineDto>() });

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Cart is empty", result.Message);
        }

        [Fact]
        public async Task CreateRequestAsync_ShouldFail_WhenHasActiveRequest()
        {
            // Arrange
            _fixture.DbContext.Requests.Add(new Request { UserId = 1, Status = RequestStatus.Requested, CreatedAt = DateTime.UtcNow });
            await _fixture.DbContext.SaveChangesAsync();

            var dto = new CreateRequestFromCartDto
            {
                CategoryId = 1,
                Items = new List<CreateRequestLineDto> { new CreateRequestLineDto { ItemId = 1, Quantity = 5 } }
            };

            // Act
            var result = await _service.CreateRequestAsync(1, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("active request", result.Message);
        }

        [Fact]
        public async Task CreateRequestAsync_ShouldSucceed_AndSaveToDb()
        {
            // Arrange
            _fixture.DbContext.Users.Add(new User { Id = 1, Email = "u@u.com", Username = "U" });
            _fixture.DbContext.Categories.Add(new Category { Id = 1, Name = "Cat 1" });
            _fixture.DbContext.Items.Add(new Item { Id = 1, Name = "Pen", CategoryId = 1 });
            await _fixture.DbContext.SaveChangesAsync();

            var dto = new CreateRequestFromCartDto
            {
                CategoryId = 1,
                Items = new List<CreateRequestLineDto> { new CreateRequestLineDto { ItemId = 1, Quantity = 5 } }
            };

            // Act
            var result = await _service.CreateRequestAsync(1, dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.RequestId);

            var savedReq = await _requestRepo.GetByIdWithItemsAsync(result.RequestId.Value);
            Assert.NotNull(savedReq);
            Assert.Equal(RequestStatus.Requested, savedReq.Status);
            Assert.Single(savedReq.RequestItems);
            Assert.Equal(5, savedReq.RequestItems.First().QuantityRequested);
        }

        [Fact]
        public async Task ApproveRequestAsync_ShouldFail_IfStatusNotIssued()
        {
            // Arrange
            _fixture.DbContext.Users.Add(new User { Id = 1, Email = "u@u.com", Username = "U" });
            _fixture.DbContext.Categories.Add(new Category { Id = 1, Name = "Cat" });
            var req = new Request { Id = 1, UserId = 1, CategoryId = 1, Status = RequestStatus.Requested };
            _fixture.DbContext.Requests.Add(req);
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var result = await _service.ApproveRequestAsync(1);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Only requests pending admin approval can be approved", result.Message);
        }

        [Fact]
        public async Task ApproveRequestAsync_ShouldSucceed_IfStatusIsIssued()
        {
            // Arrange
            _fixture.DbContext.Users.Add(new User { Id = 1, Email = "u@u.com", Username = "U" });
            _fixture.DbContext.Categories.Add(new Category { Id = 1, Name = "Cat" });
            var req = new Request { Id = 1, UserId = 1, CategoryId = 1, Status = RequestStatus.Issued };
            _fixture.DbContext.Requests.Add(req);
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var result = await _service.ApproveRequestAsync(1);

            // Assert
            Assert.True(result.Success);
            
            var savedReq = await _requestRepo.GetByIdAsync(1);
            Assert.Equal(RequestStatus.Approved, savedReq!.Status);
        }
    }
}
