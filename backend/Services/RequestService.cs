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

            if (dto.Items.Any(i => i.ItemId <= 0 || i.Quantity <= 0))
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
                Id = r.Id,
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
                Id = request.Id,
                UserId = request.UserId,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                Items = request.RequestItems.Select(ri => new RequestItemDetailDto
                {
                    Id = ri.Id,
                    ItemId = ri.ItemId,
                    ItemName = ri.Item.Name,
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

            if (request.Status != RequestStatus.Approved)
                return (false, "Only approved requests can be marked as received");

            // Mark all Approved items as Received at item level
            foreach (var item in request.RequestItems.Where(ri => ri.Status == RequestItemStatus.Approved))
                item.Status = RequestItemStatus.Received;

            request.Status = RequestStatus.Received;
            request.UpdatedAt = DateTime.UtcNow;

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
    }
}
