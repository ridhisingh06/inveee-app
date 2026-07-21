using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace invmgmt.web.Services
{
    public class RequestService : IRequestService
    {
        private readonly IRequestRepository _requestRepo;
        private readonly ILogger<RequestService> _logger;

        public RequestService(IRequestRepository requestRepo, ILogger<RequestService> logger)
        {
            _requestRepo = requestRepo;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, int? RequestId)> CreateRequestAsync(int userId, CreateRequestFromCartDto dto)
        {
            if (dto == null || dto.Items == null || dto.Items.Count == 0)
                return (false, "Cart is empty", null);

            if (dto.Items.Any(i => string.IsNullOrWhiteSpace(i.ItemId) || i.Quantity <= 0))
                return (false, "Invalid item or quantity", null);

            // Block if user already has an active request
            var hasActive = await _requestRepo.HasActiveRequestAsync(userId);
            if (hasActive)
                return (false, "You already have an active request. Please wait until it is processed.", null);

            var itemIds = dto.Items.Select(i => i.ItemId).Distinct().ToList();
            if (!await _requestRepo.ItemsExistAsync(itemIds))
                return (false, "One or more items not found", null);

            var request = new Request
            {
                UserId = userId,
                CategoryId = dto.CategoryId,
                Status = RequestStatus.PendingWithIssuer,
                CreatedAt = DateTime.UtcNow
            };

            await _requestRepo.AddRequestAsync(request);
            await _requestRepo.SaveChangesAsync();

            var requestItems = dto.Items.Select(line => new RequestItem
            {
                RequestId = request.Id,
                ItemId = line.ItemId,
                QuantityRequested = line.Quantity,
                QuantityApproved = 0,
                QuantityIssued = 0,
                Status = RequestItemStatus.PendingWithIssuer
            });

            await _requestRepo.AddRequestItemsAsync(requestItems);
            await _requestRepo.SaveChangesAsync();

            return (true, "Request created successfully", request.Id);
        }

        public async Task<IEnumerable<RequestSummaryDto>> GetUserRequestsAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            var requests = await _requestRepo.GetUserRequestsAsync(userId, pageNumber, pageSize);
            return requests.Select(r => new RequestSummaryDto
            {
                id = r.Id,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });
        }

        public async Task<RequestDetailDto?> GetRequestByIdAsync(int id, int? userId = null, string? role = null)
        {
            var request = await _requestRepo.GetByIdWithItemsAsync(id);
            if (request == null) return null;

            if (!string.IsNullOrWhiteSpace(role)
                && role.Equals("USER", StringComparison.OrdinalIgnoreCase)
                && userId.HasValue
                && request.UserId != userId.Value)
                return null;

            return new RequestDetailDto
            {
                id = request.Id,
                UserId = request.UserId,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                Items = request.RequestItems.Select(ri => new RequestItemDetailDto
                {
                    id = ri.Id,
                    ItemId = ri.ItemId,
                    ItemName = ri.Item?.Name ?? string.Empty,
                    QuantityRequested = ri.QuantityRequested,
                    QuantityApproved = ri.QuantityApproved,
                    QuantityIssued = ri.QuantityIssued,
                    Status = ri.Status
                }).ToList()
            };
        }

        public async Task<(bool Success, string Message)> ConfirmReceivedAsync(int requestId, int userId)
        {
            var request = await _requestRepo.GetByIdWithItemsAsync(requestId);
            if (request == null || request.UserId != userId)
                return (false, "Request not found");

            if (request.Status == RequestStatus.Received)
                return (false, "Request already received");

            if (request.Status != RequestStatus.Approved)
                return (false, "Only approved requests can be marked as received");

            // Mark all Approved items as Received at item level
            foreach (var item in request.RequestItems.Where(ri => ri.Status == RequestItemStatus.Approved))
                item.Status = RequestItemStatus.Received;

            request.Status = RequestStatus.Received;
            request.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Request marked as received: RequestId={RequestId}, UserId={UserId}", requestId, userId);

            await _requestRepo.UpdateRequestAsync(request);
            await _requestRepo.SaveChangesAsync();

            return (true, "Request marked as received");
        }

        public async Task<IEnumerable<object>> GetPendingRequestsAsync(int pageNumber = 1, int pageSize = 10)
        {
            var requests = await _requestRepo.GetPendingRequestsAsync(pageNumber, pageSize);
            return requests.Select(r => new
            {
                id = r.Id,
                userId = r.UserId,
                username = r.User?.Username ?? string.Empty,
                email = r.User?.Email ?? string.Empty,
                status = r.Status.ToString(),
                createdAt = r.CreatedAt,
                updatedAt = r.UpdatedAt,
                items = r.RequestItems.Select(ri => new
                {
                    id = ri.Id,
                    itemId = ri.ItemId,
                    itemName = ri.Item?.Name ?? string.Empty,
                    quantityRequested = ri.QuantityRequested,
                    quantityApproved = ri.QuantityApproved,
                    quantityIssued = ri.QuantityIssued,
                    status = ri.Status.ToString()
                }).ToList()
            });
        }

        public async Task<(bool Success, string Message)> ApproveRequestAsync(int id)
        {
            var request = await _requestRepo.GetByIdWithItemsAsync(id);
            if (request == null) return (false, "Request not found");

            // Final approval happens only after issuer has issued items.
            if (request.Status == RequestStatus.Approved)
                return (true, "Request already approved");

            if (request.Status == RequestStatus.Rejected || request.Status == RequestStatus.Received)
                return (false, $"Cannot approve a '{request.Status}' request");

            if (request.Status != RequestStatus.PendingAdminApproval)
                return (false, $"Only requests pending admin approval can be approved. Current status: {request.Status}");

            request.Status = RequestStatus.Approved;
            request.UpdatedAt = DateTime.UtcNow;
            
            await _requestRepo.UpdateRequestAsync(request);
            await _requestRepo.SaveChangesAsync();

            return (true, "Request approved successfully");
        }

        public async Task<(bool Success, string Message)> RejectRequestAsync(int id)
        {
            var request = await _requestRepo.GetByIdAsync(id);
            if (request == null) return (false, "Request not found");

            if (request.Status == RequestStatus.Rejected)
                return (true, "Request already rejected");

            // This service method is used by legacy admin endpoints, so enforce the admin stage here.
            if (request.Status != RequestStatus.PendingAdminApproval && request.Status != RequestStatus.Pending)
                return (false, $"Only requests pending admin approval can be rejected here. Current status: {request.Status}");

            request.Status = RequestStatus.Rejected;
            request.UpdatedAt = DateTime.UtcNow;

            await _requestRepo.UpdateRequestAsync(request);
            await _requestRepo.SaveChangesAsync();

            return (true, "Request rejected");
        }
        
        public async Task<(bool Success, string Message)> CheckCanRequestAsync(int userId)
        {
            var hasActive = await _requestRepo.HasActiveRequestAsync(userId);
            if (hasActive)
                return (false, "You have an active request.");
            return (true, "You can make a request.");
        }

        public async Task<(bool Success, string Message)> DeleteRequestAsync(int id, int userId)
        {
            var request = await _requestRepo.GetByIdWithItemsAsync(id);
            if (request == null || request.UserId != userId)
                return (false, "Request not found");

            if (request.Status != RequestStatus.PendingWithIssuer && request.Status != RequestStatus.Pending)
                return (false, "Only requests pending with issuer can be cancelled");

            _requestRepo.DeleteRequest(request);
            await _requestRepo.SaveChangesAsync();

            return (true, "Request cancelled successfully");
        }

        /// <summary>
        /// Returns whether the given request can still be edited by the user.
        /// Editable = Status is PendingWithIssuer AND every RequestItem is still PendingWithIssuer
        /// (i.e. the issuer has not touched any item yet).
        /// </summary>
        public async Task<RequestEditableDto> IsRequestEditableAsync(int requestId, int userId)
        {
            var request = await _requestRepo.GetByIdWithItemsAsync(requestId);
            if (request == null || request.UserId != userId)
                return new RequestEditableDto { Editable = false, Reason = "Request not found." };

            if (request.Status != RequestStatus.PendingWithIssuer)
                return new RequestEditableDto { Editable = false, Reason = $"Request is in '{request.Status}' status and can no longer be edited." };

            var anyTouched = request.RequestItems.Any(ri => ri.Status != RequestItemStatus.PendingWithIssuer);
            if (anyTouched)
                return new RequestEditableDto { Editable = false, Reason = "The issuer has started processing this request. Editing is no longer allowed." };

            return new RequestEditableDto { Editable = true, Reason = "Request is editable." };
        }

        /// <summary>
        /// Updates an existing PendingWithIssuer request's items:
        ///   - Merges duplicate ItemIds in payload.
        ///   - Updates quantities for existing items.
        ///   - Deletes items removed from the payload.
        ///   - Inserts new items from the payload.
        /// Only allowed while no issuer processing has occurred.
        /// Returns result.HttpStatusCode to allow the controller to return the
        /// correct HTTP status (200, 400, 403, 404).
        /// </summary>
        public async Task<UpdateRequestResultDto> UpdateRequestAsync(int requestId, int userId, UpdateRequestDto dto)
        {
            var result = new UpdateRequestResultDto { RequestId = requestId };

            try
            {
                // ── 1. Load request ──────────────────────────────────────────────────────
                var request = await _requestRepo.GetByIdWithItemsAsync(requestId);
                if (request == null)
                {
                    result.Success = false;
                    result.Message = "Request not found.";
                    result.ErrorCode = "NOT_FOUND";
                    return result;
                }

                // ── 2. Ownership check ───────────────────────────────────────────────────
                if (request.UserId != userId)
                {
                    result.Success = false;
                    result.Message = "You are not authorized to edit this request.";
                    result.ErrorCode = "FORBIDDEN";
                    return result;
                }

                // ── 3. Check status ──────────────────────────────────────────────────────
                if (request.Status != RequestStatus.PendingWithIssuer)
                {
                    result.Success = false;
                    result.Message = $"Request is in '{request.Status}' status and can no longer be edited.";
                    result.ErrorCode = "FORBIDDEN";
                    return result;
                }

                // ── 4. Check issuer has NOT touched any item ─────────────────────────────
                if (request.RequestItems.Any(ri => ri.Status != RequestItemStatus.PendingWithIssuer))
                {
                    result.Success = false;
                    result.Message = "The issuer has started processing this request. Editing is no longer allowed.";
                    result.ErrorCode = "FORBIDDEN";
                    return result;
                }

                // ── 5. Validate payload ──────────────────────────────────────────────────
                if (dto.Items == null || dto.Items.Count == 0)
                {
                    result.Success = false;
                    result.Message = "Request must contain at least one item.";
                    result.ErrorCode = "BAD_REQUEST";
                    return result;
                }

                if (dto.Items.Any(i => i.Quantity <= 0))
                {
                    result.Success = false;
                    result.Message = "All item quantities must be greater than 0.";
                    result.ErrorCode = "BAD_REQUEST";
                    return result;
                }

                // ── 6. Merge duplicate ItemIds in payload ────────────────────────────────
                var merged = dto.Items
                    .GroupBy(i => i.ItemId)
                    .Select(g => new UpdateRequestLineDto { ItemId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                    .ToList();

                // ── 7. Validate all ItemIds exist ────────────────────────────────────────
                var incomingItemIds = merged.Select(i => i.ItemId).Distinct().ToList();
                if (!await _requestRepo.ItemsExistAsync(incomingItemIds))
                {
                    result.Success = false;
                    result.Message = "One or more items in the request do not exist.";
                    result.ErrorCode = "BAD_REQUEST";
                    return result;
                }

                // ── 8. Apply item-level changes ──────────────────────────────────────────
                var existingItems = request.RequestItems.ToList();
                var existingItemIds = existingItems.Select(ri => ri.ItemId).ToHashSet();
                var incomingSet = merged.ToDictionary(i => i.ItemId, i => i.Quantity);

                // 8a. Update or explicitly delete existing items.
                //     IMPORTANT: We call _requestRepo.RemoveRequestItem() BEFORE
                //     _requestRepo.UpdateRequestAsync() so that EF Core's deletion
                //     tracking is not overwritten by the subsequent Update() call.
                var itemsToRemove = new List<RequestItem>();
                foreach (var existing in existingItems)
                {
                    if (incomingSet.TryGetValue(existing.ItemId, out var newQty))
                    {
                        existing.QuantityRequested = newQty;
                        _logger.LogInformation("UpdateRequest: Updated item {ItemId} qty -> {Qty} (ReqId={RequestId})", existing.ItemId, newQty, requestId);
                    }
                    else
                    {
                        itemsToRemove.Add(existing);
                        _logger.LogInformation("UpdateRequest: Removed item {ItemId} from ReqId={RequestId}", existing.ItemId, requestId);
                    }
                }

                // Remove from DbSet explicitly so EF emits DELETE statements even
                // after the subsequent Requests.Update() call.
                foreach (var toRemove in itemsToRemove)
                {
                    request.RequestItems.Remove(toRemove);
                    _requestRepo.RemoveRequestItem(toRemove);
                }

                // 8b. Insert new items not already in the request
                foreach (var line in merged.Where(m => !existingItemIds.Contains(m.ItemId)))
                {
                    var newItem = new RequestItem
                    {
                        RequestId = requestId,
                        ItemId = line.ItemId,
                        QuantityRequested = line.Quantity,
                        QuantityApproved = 0,
                        QuantityIssued = 0,
                        Status = RequestItemStatus.PendingWithIssuer
                    };
                    request.RequestItems.Add(newItem);
                    _logger.LogInformation("UpdateRequest: Added new item {ItemId} qty={Qty} to ReqId={RequestId}", line.ItemId, line.Quantity, requestId);
                }

                // ── 9. Update request audit fields ───────────────────────────────────────
                request.UpdatedAt = DateTime.UtcNow;
                await _requestRepo.UpdateRequestAsync(request);
                await _requestRepo.SaveChangesAsync();

                _logger.LogInformation("UpdateRequest: Successfully updated ReqId={RequestId} by UserId={UserId}", requestId, userId);

                result.Success = true;
                result.Message = "Request updated successfully.";
                result.UpdatedAt = request.UpdatedAt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateRequest: Unexpected error for ReqId={RequestId}", requestId);
                result.Success = false;
                result.Message = "An unexpected error occurred while updating the request.";
                result.ErrorCode = "SERVER_ERROR";
            }

            return result;
        }
    }
}
