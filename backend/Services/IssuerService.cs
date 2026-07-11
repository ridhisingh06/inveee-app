using invmgmt.web.Data;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace invmgmt.web.Services
{
    /// <summary>
    /// Service for issuer operations
    /// Implements partial issuing workflow with inventory locking and validation
    /// </summary>
    public class IssuerService : IIssuerService
    {
        private readonly IRequestItemRepository _requestItemRepo;
        private readonly IInventoryRepository _inventoryRepo;
        private readonly AppDbContext _context;
        private readonly ILogger<IssuerService> _logger;

        public IssuerService(
            IRequestItemRepository requestItemRepo,
            IInventoryRepository inventoryRepo,
            AppDbContext context,
            ILogger<IssuerService> logger)
        {
            _requestItemRepo = requestItemRepo;
            _inventoryRepo = inventoryRepo;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all pending items waiting for issuer to issue
        /// </summary>
        public async Task<IssuerPendingListDto> GetPendingItemsAsync(int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation("Fetching pending issuer items: Page={Page}, PageSize={PageSize}", pageNumber, pageSize);

            var items = await _requestItemRepo.GetPendingWithIssuerAsync(pageNumber, pageSize);
            var totalCount = await _requestItemRepo.GetPendingWithIssuerCountAsync();

            var dtoItems = items.Select(ri => MapToIssuerPendingItemDto(ri)).ToList();

            var result = new IssuerPendingListDto
            {
                Items = dtoItems,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _logger.LogInformation("Pending issuer items fetched: Count={Count}, Total={Total}", 
                dtoItems.Count, totalCount);

            return result;
        }

        /// <summary>
        /// Get pending items for a specific request
        /// </summary>
        public async Task<IssuerPendingListDto> GetPendingItemsByRequestAsync(int requestId)
        {
            _logger.LogInformation("Fetching pending issuer items for RequestId={RequestId}", requestId);

            var items = await _requestItemRepo.GetByRequestIdAsync(requestId);
            var pendingItems = items
                .Where(ri => ri.Status == RequestItemStatus.PendingWithIssuer)
                .ToList();

            var dtoItems = pendingItems.Select(ri => MapToIssuerPendingItemDto(ri)).ToList();

            var result = new IssuerPendingListDto
            {
                Items = dtoItems,
                TotalCount = dtoItems.Count,
                PageNumber = 1,
                PageSize = dtoItems.Count
            };

            _logger.LogInformation("Pending issuer items for request fetched: Count={Count}", dtoItems.Count);

            return result;
        }

        /// <summary>
        /// Issue items partially with inventory locking and deduction
        /// 
        /// Workflow:
        /// 1. Validate all items exist and are pending
        /// 2. Lock inventory rows to prevent race conditions
        /// 3. Validate quantities (issue + reject = requested, issue <= available)
        /// 4. Deduct issued quantities from inventory
        /// 5. Update request items with issuer quantities
        /// 6. Mark request as issued if all items processed
        /// </summary>
        public async Task<IssuePartiallyResponseDto> IssuePartiallyAsync(IssuePartiallyDto dto, int issuerId)
        {
            _logger.LogInformation("Starting partial issue for RequestId={RequestId}, IssuerId={IssuerId}, ItemCount={ItemCount}",
                dto.RequestId, issuerId, dto.Items.Count);

            var response = new IssuePartiallyResponseDto();

            try
            {
                // STEP 1: Validate request exists
                var request = await _context.Requests.FindAsync(dto.RequestId);
                if (request == null)
                {
                    _logger.LogWarning("Request not found: RequestId={RequestId}", dto.RequestId);
                    response.Success = false;
                    response.Message = $"Request {dto.RequestId} not found.";
                    return response;
                }

                // STEP 2: Get all request items
                var requestItems = await _requestItemRepo.GetByRequestIdAsync(dto.RequestId);
                var requestItemDict = requestItems.ToDictionary(ri => ri.Id);

                // STEP 3: Validate all items exist and are pending
                foreach (var dtoItem in dto.Items)
                {
                    if (!requestItemDict.TryGetValue(dtoItem.RequestItemId, out var requestItem))
                    {
                        _logger.LogWarning("RequestItem not found: Id={Id}", dtoItem.RequestItemId);
                        response.Success = false;
                        response.Message = $"RequestItem {dtoItem.RequestItemId} not found.";
                        return response;
                    }

                    if (requestItem.Status != RequestItemStatus.PendingWithIssuer)
                    {
                        _logger.LogWarning("RequestItem is not pending with issuer: Id={Id}, Status={Status}", 
                            dtoItem.RequestItemId, requestItem.Status);
                        response.Success = false;
                        response.Message = $"Item {dtoItem.RequestItemId} is not pending issuer approval.";
                        return response;
                    }
                }

                // STEP 4: Validate quantities and prepare inventory operations
                var inventoryOps = new List<(int itemId, int deductQuantity, int itemName)>();

                foreach (var dtoItem in dto.Items)
                {
                    var requestItem = requestItemDict[dtoItem.RequestItemId];

                    // Validation: IssueQuantity + RejectQuantity = RequestedQuantity
                    if (dtoItem.IssueQuantity + dtoItem.RejectQuantity != requestItem.QuantityRequested)
                    {
                        _logger.LogWarning(
                            "Quantity mismatch: RequestItemId={Id}, Issue={Issue}, Reject={Reject}, Requested={Requested}",
                            dtoItem.RequestItemId, dtoItem.IssueQuantity, dtoItem.RejectQuantity, requestItem.QuantityRequested);
                        response.Success = false;
                        response.Message = $"Item {dtoItem.RequestItemId}: Issue + Reject must equal Requested quantity.";
                        return response;
                    }

                    // Validation: No negative quantities
                    if (dtoItem.IssueQuantity < 0 || dtoItem.RejectQuantity < 0)
                    {
                        response.Success = false;
                        response.Message = $"Item {dtoItem.RequestItemId}: Quantities cannot be negative.";
                        return response;
                    }

                    if (dtoItem.IssueQuantity > 0)
                    {
                        inventoryOps.Add((requestItem.ItemId, dtoItem.IssueQuantity, requestItem.Item.Id));
                    }
                }

                // STEP 5: Execute inside a retriable execution strategy so that
                // NpgsqlRetryingExecutionStrategy can replay the entire unit of work
                // on transient failures without throwing the "does not support
                // user-initiated transactions" exception.
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _logger.LogInformation("Acquiring inventory locks for {Count} items", inventoryOps.Count);

                        // Lock and validate inventory availability
                        foreach (var (itemId, deductQty, _) in inventoryOps)
                        {
                            var inventory = await _inventoryRepo.LockAndGetAsync(itemId);
                            if (inventory == null)
                            {
                                _logger.LogWarning("Inventory not found for ItemId={ItemId}", itemId);
                                response.Success = false;
                                response.Message = $"Inventory not found for ItemId {itemId}.";
                                await transaction.RollbackAsync();
                                return;
                            }

                            if (inventory.AvailableQuantity < deductQty)
                            {
                                _logger.LogWarning(
                                    "Insufficient inventory: ItemId={ItemId}, Available={Available}, Requested={Requested}",
                                    itemId, inventory.AvailableQuantity, deductQty);
                                response.Success = false;
                                response.Message = $"Insufficient inventory for ItemId {itemId}. Available: {inventory.AvailableQuantity}, Requested: {deductQty}";
                                await transaction.RollbackAsync();
                                return;
                            }
                        }

                        _logger.LogInformation("Inventory locks acquired successfully");

                        // STEP 6: Deduct quantities from inventory
                        foreach (var (itemId, deductQty, _) in inventoryOps)
                        {
                            var success = await _inventoryRepo.TryDeductAsync(itemId, deductQty);
                            if (!success)
                            {
                                _logger.LogError("Failed to deduct inventory for ItemId={ItemId}", itemId);
                                response.Success = false;
                                response.Message = $"Failed to deduct inventory for ItemId {itemId}.";
                                await transaction.RollbackAsync();
                                return;
                            }
                        }

                        _logger.LogInformation("Inventory deducted successfully");

                        // STEP 7: Update request items with issuer quantities
                        var issuedItems = new List<IssuedItemDetailDto>();
                        var issuedDate = DateTime.UtcNow;

                        foreach (var dtoItem in dto.Items)
                        {
                            var requestItem = requestItemDict[dtoItem.RequestItemId];

                            await _requestItemRepo.UpdateIssuerQuantitiesAsync(
                                dtoItem.RequestItemId,
                                dtoItem.IssueQuantity,
                                dtoItem.RejectQuantity,
                                issuerId,
                                issuedDate);

                            issuedItems.Add(new IssuedItemDetailDto
                            {
                                RequestItemId = dtoItem.RequestItemId,
                                ItemId = requestItem.ItemId,
                                ItemName = requestItem.Item.Name,
                                RequestedQuantity = requestItem.QuantityRequested,
                                IssuedQuantity = dtoItem.IssueQuantity,
                                RejectedQuantity = dtoItem.RejectQuantity
                            });
                        }

                        // STEP 8: Update request status if all items issued
                        if (await _requestItemRepo.AllItemsIssuedAsync(dto.RequestId))
                        {
                            request.Status = RequestStatus.PendingAdminApproval;
                            request.IssuedDate = issuedDate;
                            request.IssuedBy = issuerId;
                            request.UpdatedAt = issuedDate;
                            _context.Requests.Update(request);

                            _logger.LogInformation("Request status updated to PendingAdminApproval: RequestId={RequestId}", dto.RequestId);
                        }

                        // STEP 9: Single SaveChanges — flushes inventory deductions,
                        // request item updates, and request status in one round-trip.
                        // Do NOT call SaveChangesAsync on individual repositories here;
                        // they share the same DbContext instance, so one call covers all.
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();

                        _logger.LogInformation("Partial issue committed successfully: RequestId={RequestId}, ItemsIssued={ItemsIssued}",
                            dto.RequestId, issuedItems.Count);

                        // Populate response inside the closure so it is available after ExecuteAsync returns.
                        response.Success = true;
                        response.Message = $"Successfully issued {issuedItems.Count} item(s).";
                        response.RequestId = dto.RequestId;
                        response.IssuedDate = issuedDate;
                        response.IssuedItems = issuedItems;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during partial issue transaction: RequestId={RequestId}", dto.RequestId);
                        await transaction.RollbackAsync();
                        throw; // re-throw so the execution strategy can retry on transient faults
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during partial issue: RequestId={RequestId}", dto.RequestId);
                response.Success = false;
                response.Message = $"Error processing partial issue: {ex.Message}";
                return response;
            }
        }

        /// <summary>
        /// Get count of pending items waiting for issuer
        /// </summary>
        public async Task<int> GetPendingCountAsync()
        {
            return await _requestItemRepo.GetPendingWithIssuerCountAsync();
        }

        // ============================================================================
        // PRIVATE HELPER METHODS
        // ============================================================================

        private IssuerPendingItemDto MapToIssuerPendingItemDto(RequestItem requestItem)
        {
            return new IssuerPendingItemDto
            {
                RequestItemId = requestItem.Id,
                ItemId = requestItem.ItemId,
                ItemName = requestItem.Item.Name,
                RequestedQuantity = requestItem.QuantityRequested,
                AvailableQuantity = requestItem.Item.InventoryStock?.AvailableQuantity ?? 0,
                CategoryName = requestItem.Item.Category?.Name ?? "Uncategorized",
                Status = requestItem.Status,
                RequestId = requestItem.RequestId,
                RequestedByUserName = requestItem.Request.User.Username,
                RequestedDate = requestItem.Request.CreatedAt
            };
        }
    }
}
