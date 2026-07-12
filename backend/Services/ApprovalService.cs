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
    /// Service for admin approval operations
    /// Implements partial approval workflow with inventory restoration for rejected items
    /// </summary>
    public class ApprovalService : IApprovalService
    {
        private readonly IRequestItemRepository _requestItemRepo;
        private readonly IInventoryRepository _inventoryRepo;
        private readonly AppDbContext _context;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(
            IRequestItemRepository requestItemRepo,
            IInventoryRepository inventoryRepo,
            AppDbContext context,
            ILogger<ApprovalService> logger)
        {
            _requestItemRepo = requestItemRepo;
            _inventoryRepo = inventoryRepo;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all pending items waiting for admin approval
        /// </summary>
        public async Task<AdminPendingListDto> GetPendingItemsAsync(int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation("Fetching pending admin approval items: Page={Page}, PageSize={PageSize}", pageNumber, pageSize);

            var items = await _requestItemRepo.GetPendingWithAdminAsync(pageNumber, pageSize);
            var totalCount = await _requestItemRepo.GetPendingWithAdminCountAsync();

            var dtoItems = items.Select(ri => MapToAdminPendingItemDto(ri)).ToList();

            var result = new AdminPendingListDto
            {
                Items = dtoItems,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _logger.LogInformation("Pending admin approval items fetched: Count={Count}, Total={Total}",
                dtoItems.Count, totalCount);

            return result;
        }

        /// <summary>
        /// Get pending items for a specific request
        /// </summary>
        public async Task<AdminPendingListDto> GetPendingItemsByRequestAsync(int requestId)
        {
            _logger.LogInformation("Fetching pending admin approval items for RequestId={RequestId}", requestId);

            var items = await _requestItemRepo.GetByRequestIdAsync(requestId);
            var pendingItems = items
                .Where(ri => ri.Status == RequestItemStatus.PendingAdminApproval)
                .ToList();

            var dtoItems = pendingItems.Select(ri => MapToAdminPendingItemDto(ri)).ToList();

            var result = new AdminPendingListDto
            {
                Items = dtoItems,
                TotalCount = dtoItems.Count,
                PageNumber = 1,
                PageSize = dtoItems.Count
            };

            _logger.LogInformation("Pending admin approval items for request fetched: Count={Count}", dtoItems.Count);

            return result;
        }

        /// <summary>
        /// Approve items partially with inventory restoration for rejected items
        /// 
        /// Workflow:
        /// 1. Validate all items exist and are pending admin approval
        /// 2. Validate quantities (approve + reject = issued, approve <= issued)
        /// 3. Lock inventory for rejected items
        /// 4. Restore rejected quantities back to inventory
        /// 5. Update request items with admin quantities
        /// 6. Mark request as approved if all items processed
        /// </summary>
        public async Task<ApprovePartiallyResponseDto> ApprovePartiallyAsync(ApprovePartiallyDto dto, int adminId)
        {
            _logger.LogInformation("Starting partial approval for RequestId={RequestId}, AdminId={AdminId}, ItemCount={ItemCount}",
                dto.RequestId, adminId, dto.Items.Count);

            var response = new ApprovePartiallyResponseDto();

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

                // STEP 3: Validate all items exist and are pending admin approval
                foreach (var dtoItem in dto.Items)
                {
                    if (!requestItemDict.TryGetValue(dtoItem.RequestItemId, out var requestItem))
                    {
                        _logger.LogWarning("RequestItem not found: Id={Id}", dtoItem.RequestItemId);
                        response.Success = false;
                        response.Message = $"RequestItem {dtoItem.RequestItemId} not found.";
                        return response;
                    }

                    if (requestItem.Status != RequestItemStatus.PendingAdminApproval)
                    {
                        _logger.LogWarning("RequestItem is not pending admin approval: Id={Id}, Status={Status}",
                            dtoItem.RequestItemId, requestItem.Status);
                        response.Success = false;
                        response.Message = $"Item {dtoItem.RequestItemId} is not pending admin approval.";
                        return response;
                    }
                }

                // STEP 4: Validate quantities and prepare restoration operations
                var restorationOps = new List<(int itemId, int restoreQuantity)>();

                foreach (var dtoItem in dto.Items)
                {
                    var requestItem = requestItemDict[dtoItem.RequestItemId];

                    // Validation: ApproveQuantity + RejectQuantity = IssuerIssuedQuantity
                    if (dtoItem.ApproveQuantity + dtoItem.RejectQuantity != requestItem.IssuerIssuedQuantity)
                    {
                        _logger.LogWarning(
                            "Quantity mismatch: RequestItemId={Id}, Approve={Approve}, Reject={Reject}, IssuerIssued={IssuerIssued}",
                            dtoItem.RequestItemId, dtoItem.ApproveQuantity, dtoItem.RejectQuantity, requestItem.IssuerIssuedQuantity);
                        response.Success = false;
                        response.Message = $"Item {dtoItem.RequestItemId}: Approve + Reject must equal Issuer Issued quantity.";
                        return response;
                    }

                    // Validation: Admin cannot approve more than issuer issued
                    if (dtoItem.ApproveQuantity > requestItem.IssuerIssuedQuantity)
                    {
                        _logger.LogWarning(
                            "Approve exceeds issued: RequestItemId={Id}, Approve={Approve}, IssuerIssued={IssuerIssued}",
                            dtoItem.RequestItemId, dtoItem.ApproveQuantity, requestItem.IssuerIssuedQuantity);
                        response.Success = false;
                        response.Message = $"Item {dtoItem.RequestItemId}: Cannot approve more than issuer issued.";
                        return response;
                    }

                    // Validation: No negative quantities
                    if (dtoItem.ApproveQuantity < 0 || dtoItem.RejectQuantity < 0)
                    {
                        response.Success = false;
                        response.Message = $"Item {dtoItem.RequestItemId}: Quantities cannot be negative.";
                        return response;
                    }

                    // Track items to restore
                    if (dtoItem.RejectQuantity > 0)
                    {
                        restorationOps.Add((requestItem.ItemId, dtoItem.RejectQuantity));
                    }
                }

                // STEP 5: Execute inside a retriable execution strategy so that
                // NpgsqlRetryingExecutionStrategy can replay the entire unit of work
                // on transient failures without throwing the "does not support
                // user-initiated transactions" exception.
                //
                // IMPORTANT: Do NOT set response fields and `return` inside the lambda —
                // `return` only exits the lambda, not this method. Any failure inside the
                // transaction must throw so the outer catch can handle it correctly.
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _logger.LogInformation("Restoring inventory for {Count} items", restorationOps.Count);

                        // Restore rejected quantities back to inventory
                        foreach (var (itemId, restoreQty) in restorationOps)
                        {
                            var success = await _inventoryRepo.RestoreAsync(itemId, restoreQty);
                            if (!success)
                            {
                                // Throw — not return — so the catch below rolls back and
                                // the outer catch sets response.Success = false correctly.
                                throw new InvalidOperationException(
                                    $"Failed to restore inventory for ItemId {itemId}. " +
                                    "Inventory record not found.");
                            }
                        }

                        _logger.LogInformation("Inventory restored successfully");

                        // STEP 6: Update request items with admin quantities
                        var approvedItems = new List<ApprovedItemDetailDto>();
                        var approvedDate = DateTime.UtcNow;

                        foreach (var dtoItem in dto.Items)
                        {
                            var requestItem = requestItemDict[dtoItem.RequestItemId];

                            await _requestItemRepo.UpdateAdminQuantitiesAsync(
                                dtoItem.RequestItemId,
                                dtoItem.ApproveQuantity,
                                dtoItem.RejectQuantity,
                                adminId,
                                approvedDate);

                            approvedItems.Add(new ApprovedItemDetailDto
                            {
                                RequestItemId = dtoItem.RequestItemId,
                                ItemId = requestItem.ItemId,
                                ItemName = requestItem.Item.Name,
                                IssuerIssuedQuantity = requestItem.IssuerIssuedQuantity,
                                ApprovedQuantity = dtoItem.ApproveQuantity,
                                RejectedQuantity = dtoItem.RejectQuantity
                            });
                        }

                        // STEP 7: Update request status if all items approved
                        if (await _requestItemRepo.AllItemsApprovedAsync(dto.RequestId))
                        {
                            request.Status = RequestStatus.Approved;
                            request.ApprovedDate = approvedDate;
                            request.ApprovedBy = adminId;
                            request.UpdatedAt = approvedDate;
                            _context.Requests.Update(request);

                            _logger.LogInformation("Request status updated to Approved: RequestId={RequestId}", dto.RequestId);
                        }

                        // STEP 8: Single SaveChanges — covers inventory restoration,
                        // request item updates, and request status in one round-trip.
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();

                        _logger.LogInformation("Partial approval committed successfully: RequestId={RequestId}, ItemsApproved={ItemsApproved}",
                            dto.RequestId, approvedItems.Count);

                        response.Success = true;
                        response.Message = $"Successfully approved {approvedItems.Count} item(s).";
                        response.RequestId = dto.RequestId;
                        response.ApprovedDate = approvedDate;
                        response.ApprovedItems = approvedItems;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during partial approval transaction: RequestId={RequestId}", dto.RequestId);
                        await transaction.RollbackAsync();
                        throw; // re-throw so the execution strategy can retry on transient faults
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during partial approval: RequestId={RequestId}", dto.RequestId);
                response.Success = false;
                response.Message = $"Error processing partial approval: {ex.Message}";
                return response;
            }
        }

        /// <summary>
        /// Get count of pending items waiting for admin
        /// </summary>
        public async Task<int> GetPendingCountAsync()
        {
            return await _requestItemRepo.GetPendingWithAdminCountAsync();
        }

        // ============================================================================
        // PRIVATE HELPER METHODS
        // ============================================================================

        private AdminPendingItemDto MapToAdminPendingItemDto(RequestItem requestItem)
        {
            return new AdminPendingItemDto
            {
                RequestItemId = requestItem.Id,
                ItemId = requestItem.ItemId,
                ItemName = requestItem.Item.Name,
                RequestedQuantity = requestItem.QuantityRequested,
                IssuerIssuedQuantity = requestItem.IssuerIssuedQuantity,
                IssuerRejectedQuantity = requestItem.IssuerRejectedQuantity,
                Status = requestItem.Status,
                RequestId = requestItem.RequestId,
                RequestedByUserName = requestItem.Request.User.Username,
                IssuedByUserName = requestItem.Request.User.Username, // Will be updated later with actual issuer
                IssuedDate = requestItem.IssuedDate ?? DateTime.MinValue
            };
        }
    }
}
