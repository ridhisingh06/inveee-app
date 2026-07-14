using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.Repositories;
using invmgmt.web.Services;
using invmgmt.web.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace invmgmt.web.Tests.Services
{
    /// <summary>
    /// End-to-end tests for the Edit Request flow:
    ///   IsRequestEditableAsync  →  UpdateRequestAsync
    ///
    /// Each test uses an isolated in-memory database (via BaseTestFixture)
    /// so there is no shared state between cases.
    /// </summary>
    public class EditRequestFlowTests : IDisposable
    {
        private readonly BaseTestFixture _fixture;
        private readonly RequestRepository _repo;
        private readonly RequestService _service;

        // Seed IDs
        private const int UserId   = 1;
        private const int OtherUid = 2;
        private const int ItemId1  = 10;
        private const int ItemId2  = 20;
        private const int ItemId3  = 30;

        public EditRequestFlowTests()
        {
            _fixture = new BaseTestFixture();
            _repo    = new RequestRepository(
                _fixture.DbContext,
                NullLogger<RequestRepository>.Instance);
            _service = new RequestService(
                _repo,
                NullLogger<RequestService>.Instance);
        }

        public void Dispose() => _fixture.Dispose();

        // ──────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────

        private async Task SeedBaseDataAsync()
        {
            _fixture.DbContext.Users.AddRange(
                new User { Id = UserId,   Email = "user@test.com",  Username = "User",  Role = "USER",  IsActive = true, IsApproved = true },
                new User { Id = OtherUid, Email = "other@test.com", Username = "Other", Role = "USER",  IsActive = true, IsApproved = true }
            );
            _fixture.DbContext.Categories.Add(new Category { Id = 1, Name = "Stationery" });
            _fixture.DbContext.Items.AddRange(
                new Item { Id = ItemId1, Name = "Pen",     CategoryId = 1, IsActive = true },
                new Item { Id = ItemId2, Name = "Stapler", CategoryId = 1, IsActive = true },
                new Item { Id = ItemId3, Name = "Ruler",   CategoryId = 1, IsActive = true }
            );
            await _fixture.DbContext.SaveChangesAsync();
        }

        private async Task<int> SeedPendingRequestAsync(
            int userId = UserId,
            RequestItemStatus item1Status = RequestItemStatus.PendingWithIssuer,
            RequestItemStatus item2Status = RequestItemStatus.PendingWithIssuer,
            RequestStatus requestStatus  = RequestStatus.PendingWithIssuer)
        {
            var request = new Request
            {
                UserId    = userId,
                Status    = requestStatus,
                CreatedAt = DateTime.UtcNow,
                RequestItems = new List<RequestItem>
                {
                    new RequestItem { ItemId = ItemId1, QuantityRequested = 5, Status = item1Status },
                    new RequestItem { ItemId = ItemId2, QuantityRequested = 2, Status = item2Status }
                }
            };
            _fixture.DbContext.Requests.Add(request);
            await _fixture.DbContext.SaveChangesAsync();
            return request.Id;
        }

        // ══════════════════════════════════════════════════════════════════════
        // IsRequestEditableAsync
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task IsEditable_ReturnsFalse_WhenRequestNotFound()
        {
            var result = await _service.IsRequestEditableAsync(9999, UserId);
            Assert.False(result.Editable);
            Assert.Contains("not found", result.Reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task IsEditable_ReturnsFalse_WhenRequestBelongsToAnotherUser()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync(userId: OtherUid);

            var result = await _service.IsRequestEditableAsync(reqId, UserId);

            Assert.False(result.Editable);
        }

        [Fact]
        public async Task IsEditable_ReturnsFalse_WhenStatusIsPendingAdminApproval()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync(
                requestStatus: RequestStatus.PendingAdminApproval,
                item1Status: RequestItemStatus.PendingAdminApproval,
                item2Status: RequestItemStatus.PendingAdminApproval);

            var result = await _service.IsRequestEditableAsync(reqId, UserId);

            Assert.False(result.Editable);
            Assert.Contains("PendingAdminApproval", result.Reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task IsEditable_ReturnsFalse_WhenStatusIsApproved()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync(
                requestStatus: RequestStatus.Approved,
                item1Status: RequestItemStatus.Approved,
                item2Status: RequestItemStatus.Approved);

            var result = await _service.IsRequestEditableAsync(reqId, UserId);

            Assert.False(result.Editable);
        }

        [Fact]
        public async Task IsEditable_ReturnsFalse_WhenStatusIsReceived()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync(
                requestStatus: RequestStatus.Received,
                item1Status: RequestItemStatus.Received,
                item2Status: RequestItemStatus.Received);

            var result = await _service.IsRequestEditableAsync(reqId, UserId);

            Assert.False(result.Editable);
        }

        [Fact]
        public async Task IsEditable_ReturnsFalse_WhenOneItemTouchedByIssuer()
        {
            await SeedBaseDataAsync();
            // One item still pending, another already issued to admin — issuer has started
            var reqId = await SeedPendingRequestAsync(
                item1Status: RequestItemStatus.PendingWithIssuer,
                item2Status: RequestItemStatus.PendingAdminApproval);

            var result = await _service.IsRequestEditableAsync(reqId, UserId);

            Assert.False(result.Editable);
            Assert.Contains("issuer", result.Reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task IsEditable_ReturnsTrue_WhenAllItemsPendingWithIssuer()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            var result = await _service.IsRequestEditableAsync(reqId, UserId);

            Assert.True(result.Editable);
        }

        // ══════════════════════════════════════════════════════════════════════
        // UpdateRequestAsync — guard conditions
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_Fails_WhenRequestNotFound()
        {
            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto> { new() { ItemId = ItemId1, Quantity = 3 } }
            };
            var result = await _service.UpdateRequestAsync(9999, UserId, dto);

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Update_Fails_WhenRequestBelongsToAnotherUser()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync(userId: OtherUid);

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto> { new() { ItemId = ItemId1, Quantity = 3 } }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Update_Fails_WhenStatusIsNotPendingWithIssuer()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync(
                requestStatus: RequestStatus.PendingAdminApproval,
                item1Status: RequestItemStatus.PendingAdminApproval,
                item2Status: RequestItemStatus.PendingAdminApproval);

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto> { new() { ItemId = ItemId1, Quantity = 3 } }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.False(result.Success);
            Assert.Contains("PendingAdminApproval", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Update_Fails_WhenIssuerHasStartedProcessing()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync(
                item1Status: RequestItemStatus.PendingWithIssuer,
                item2Status: RequestItemStatus.PendingAdminApproval);

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto> { new() { ItemId = ItemId1, Quantity = 3 } }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.False(result.Success);
            Assert.Contains("issuer", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Update_Fails_WhenPayloadIsEmpty()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            var dto = new UpdateRequestDto { Items = new List<UpdateRequestLineDto>() };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.False(result.Success);
            Assert.Contains("at least one item", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Update_Fails_WhenQuantityIsZeroOrNegative()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto> { new() { ItemId = ItemId1, Quantity = 0 } }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.False(result.Success);
            Assert.Contains("greater than 0", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Update_Fails_WhenItemIdDoesNotExist()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto> { new() { ItemId = 9999, Quantity = 1 } }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.False(result.Success);
            Assert.Contains("do not exist", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════════════════════════════
        // UpdateRequestAsync — successful mutations
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_Succeeds_WhenRequestIsEditable()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 10 },
                    new() { ItemId = ItemId2, Quantity = 3  }
                }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.True(result.Success);
            Assert.Equal(reqId, result.RequestId);
            Assert.NotNull(result.UpdatedAt);
        }

        [Fact]
        public async Task Update_UpdatesExistingItemQuantity()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 99 }, // was 5
                    new() { ItemId = ItemId2, Quantity = 2  }
                }
            };
            await _service.UpdateRequestAsync(reqId, UserId, dto);

            var saved = await _repo.GetByIdWithItemsAsync(reqId);
            var pen   = saved!.RequestItems.First(ri => ri.ItemId == ItemId1);
            Assert.Equal(99, pen.QuantityRequested);
        }

        [Fact]
        public async Task Update_RemovesItemWhenNotInPayload()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            // Only send Pen — Stapler should be deleted
            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 5 }
                }
            };
            await _service.UpdateRequestAsync(reqId, UserId, dto);

            var saved = await _repo.GetByIdWithItemsAsync(reqId);
            Assert.Single(saved!.RequestItems);
            Assert.Equal(ItemId1, saved.RequestItems.First().ItemId);
        }

        [Fact]
        public async Task Update_AddsNewItemNotInOriginalRequest()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync(); // has Pen + Stapler

            // Add Ruler (ItemId3) which was not in the original request
            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 5 },
                    new() { ItemId = ItemId2, Quantity = 2 },
                    new() { ItemId = ItemId3, Quantity = 1 }
                }
            };
            await _service.UpdateRequestAsync(reqId, UserId, dto);

            var saved = await _repo.GetByIdWithItemsAsync(reqId);
            Assert.Equal(3, saved!.RequestItems.Count);
            Assert.Contains(saved.RequestItems, ri => ri.ItemId == ItemId3 && ri.QuantityRequested == 1);
        }

        [Fact]
        public async Task Update_MergesDuplicateItemIdsInPayload()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            // Send ItemId1 twice — should merge to total quantity 8
            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 5 },
                    new() { ItemId = ItemId1, Quantity = 3 }
                }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.True(result.Success);
            var saved = await _repo.GetByIdWithItemsAsync(reqId);
            var pen   = saved!.RequestItems.First(ri => ri.ItemId == ItemId1);
            Assert.Equal(8, pen.QuantityRequested);
        }

        [Fact]
        public async Task Update_KeepsRequestStatusAsPendingWithIssuer()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 7 }
                }
            };
            await _service.UpdateRequestAsync(reqId, UserId, dto);

            var saved = await _repo.GetByIdWithItemsAsync(reqId);
            Assert.Equal(RequestStatus.PendingWithIssuer, saved!.Status);
        }

        [Fact]
        public async Task Update_KeepsRequestIdUnchanged()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 3 }
                }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.Equal(reqId, result.RequestId);
        }

        [Fact]
        public async Task Update_SetsUpdatedAt()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            var before = DateTime.UtcNow.AddSeconds(-1);

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 3 }
                }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.NotNull(result.UpdatedAt);
            Assert.True(result.UpdatedAt.Value >= before);
        }

        [Fact]
        public async Task Update_NewItemsHaveCorrectDefaultFields()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 1 },
                    new() { ItemId = ItemId3, Quantity = 4 } // new item
                }
            };
            await _service.UpdateRequestAsync(reqId, UserId, dto);

            var saved  = await _repo.GetByIdWithItemsAsync(reqId);
            var ruler  = saved!.RequestItems.First(ri => ri.ItemId == ItemId3);

            Assert.Equal(4, ruler.QuantityRequested);
            Assert.Equal(0, ruler.QuantityApproved);
            Assert.Equal(0, ruler.QuantityIssued);
            Assert.Equal(RequestItemStatus.PendingWithIssuer, ruler.Status);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Combined editable-check + update flow (mirrors the Angular component)
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task FullFlow_EditableTrue_ThenUpdate_Succeeds()
        {
            await SeedBaseDataAsync();
            var reqId = await SeedPendingRequestAsync();

            // Step 1: check editable (mirrors Angular component loadAll)
            var editable = await _service.IsRequestEditableAsync(reqId, UserId);
            Assert.True(editable.Editable);

            // Step 2: update (mirrors Angular component saveChanges)
            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 8 },
                    new() { ItemId = ItemId3, Quantity = 1 }
                }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);

            Assert.True(result.Success);

            var saved = await _repo.GetByIdWithItemsAsync(reqId);
            Assert.Equal(2, saved!.RequestItems.Count);
            Assert.Equal(8, saved.RequestItems.First(ri => ri.ItemId == ItemId1).QuantityRequested);
            Assert.True(saved.RequestItems.Any(ri => ri.ItemId == ItemId3));
            Assert.False(saved.RequestItems.Any(ri => ri.ItemId == ItemId2)); // Stapler removed
        }

        [Fact]
        public async Task FullFlow_IssuerTouchesItem_ThenUpdate_Fails()
        {
            await SeedBaseDataAsync();
            // Simulate issuer having started — one item is now PendingAdminApproval
            var reqId = await SeedPendingRequestAsync(
                item1Status: RequestItemStatus.PendingAdminApproval,
                item2Status: RequestItemStatus.PendingWithIssuer);

            // Step 1: editable check should return false
            var editable = await _service.IsRequestEditableAsync(reqId, UserId);
            Assert.False(editable.Editable);

            // Step 2: even if the frontend bypasses the guard, update should also fail
            var dto = new UpdateRequestDto
            {
                Items = new List<UpdateRequestLineDto>
                {
                    new() { ItemId = ItemId1, Quantity = 3 }
                }
            };
            var result = await _service.UpdateRequestAsync(reqId, UserId, dto);
            Assert.False(result.Success);
        }
    }
}
