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
    /// Service for order summary operations
    /// Creates immutable order records and retrieves order history
    /// </summary>
    public class OrderSummaryService : IOrderSummaryService
    {
        private readonly IOrderSummaryRepository _orderSummaryRepo;
        private readonly IRequestItemRepository _requestItemRepo;
        private readonly AppDbContext _context;
        private readonly ILogger<OrderSummaryService> _logger;

        public OrderSummaryService(
            IOrderSummaryRepository orderSummaryRepo,
            IRequestItemRepository requestItemRepo,
            AppDbContext context,
            ILogger<OrderSummaryService> logger)
        {
            _orderSummaryRepo = orderSummaryRepo;
            _requestItemRepo = requestItemRepo;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Create an order summary when user receives approved items
        /// 
        /// This creates an immutable record of the complete transaction lifecycle:
        /// - What was requested
        /// - What was issued
        /// - What was approved
        /// - What was received
        /// - Who performed each action
        /// - When each action occurred
        /// </summary>
        public async Task<ReceiveItemsResponseDto> CreateOrderSummaryAsync(int requestId, int userId, string? notes = null)
        {
            _logger.LogInformation("Creating order summary for RequestId={RequestId}, UserId={UserId}", requestId, userId);

            var response = new ReceiveItemsResponseDto();

            try
            {
                // STEP 1: Get the request with all items
                var request = await _context.Requests
                    .Include(r => r.RequestItems)
                        .ThenInclude(ri => ri.Item)
                            .ThenInclude(i => i.Category)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null)
                {
                    _logger.LogWarning("Request not found for order summary: RequestId={RequestId}", requestId);
                    response.Success = false;
                    response.Message = $"Request {requestId} not found.";
                    return response;
                }

                // STEP 2: Validate request is ready to receive.
                // ✅ Accept both Approved (all items processed) and the edge case where
                // the request is still technically Approved while some items are NotIssued
                // — both represent a completed workflow that the user can now receive.
                if (request.Status != RequestStatus.Approved)
                {
                    _logger.LogWarning("Request is not approved: RequestId={RequestId}, Status={Status}", requestId, request.Status);
                    response.Success = false;
                    response.Message = $"Only approved requests can be received. Current status: {request.Status}";
                    return response;
                }

                // STEP 3: Check if order summary already exists
                if (await _orderSummaryRepo.ExistsByRequestIdAsync(requestId))
                {
                    _logger.LogWarning("Order summary already exists for RequestId={RequestId}", requestId);
                    response.Success = false;
                    response.Message = "Order summary already exists for this request.";
                    return response;
                }

                // STEP 4: Calculate summary quantities from request items.
                // FinalRejectedQuantity = IssuerRejectedQuantity + AdminRejectedQuantity (per item).
                var orderItems = new List<OrderSummaryItem>();
                int totalRequested = 0;
                int totalIssued    = 0;
                int totalApproved  = 0;
                int totalRejected  = 0;
                int totalReceived  = 0;

                foreach (var requestItem in request.RequestItems)
                {
                    // Items the issuer fully rejected (NotIssued) have 0 issued / 0 approved.
                    var issuedQty    = requestItem.IssuerIssuedQuantity;
                    var approvedQty  = requestItem.AdminApprovedQuantity;
                    var receivedQty  = approvedQty; // user receives what admin approved

                    // ✅ Final rejected = issuer rejected + admin rejected
                    var issuerRejected = requestItem.IssuerRejectedQuantity;
                    var adminRejected  = requestItem.AdminRejectedQuantity;

                    totalRequested += requestItem.QuantityRequested;
                    totalIssued    += issuedQty;
                    totalApproved  += approvedQty;
                    totalRejected  += issuerRejected + adminRejected;
                    totalReceived  += receivedQty;

                    orderItems.Add(new OrderSummaryItem
                    {
                        ItemId                 = requestItem.ItemId,
                        RequestedQuantity      = requestItem.QuantityRequested,
                        IssuedQuantity         = issuedQty,
                        IssuerRejectedQuantity = issuerRejected,
                        ApprovedQuantity       = approvedQty,
                        AdminRejectedQuantity  = adminRejected,
                        ReceivedQuantity       = receivedQty,
                        RequestItemId          = requestItem.Id,
                        CreatedAt              = DateTime.UtcNow
                    });
                }

                // Mark all Approved items as Received; NotIssued/Rejected items stay as-is.
                foreach (var ri in request.RequestItems)
                {
                    if (ri.Status == RequestItemStatus.Approved)
                    {
                        ri.Status           = RequestItemStatus.Received;
                        ri.ReceivedQuantity = ri.AdminApprovedQuantity;
                        ri.ReceivedDate     = DateTime.UtcNow;
                    }
                }

                // STEP 5: Create order summary
                var now = DateTime.UtcNow;
                var orderSummary = new OrderSummary
                {
                    RequestId = requestId,
                    UserId = userId,
                    IssuedByUserId = request.IssuedBy,
                    ApprovedByUserId = request.ApprovedBy,
                    RequestedDate = request.CreatedAt,
                    IssuedDate = request.IssuedDate ?? now,
                    ApprovedDate = request.ApprovedDate ?? now,
                    ReceivedDate = now,
                    TotalRequestedQuantity = totalRequested,
                    TotalIssuedQuantity = totalIssued,
                    TotalApprovedQuantity = totalApproved,
                    TotalRejectedQuantity = totalRejected,
                    TotalReceivedQuantity = totalReceived,
                    Status = RequestStatus.Received,
                    Items = orderItems,
                    Notes = notes,
                    CreatedAt = now
                };

                // STEP 6: Save order summary
                await _orderSummaryRepo.CreateAsync(orderSummary);
                await _orderSummaryRepo.SaveChangesAsync();

                // STEP 7: Update request status to Received
                request.Status = RequestStatus.Received;
                request.ReceivedDate = now;
                request.UpdatedAt = now;
                _context.Requests.Update(request);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Order summary created successfully: Id={OrderSummaryId}, RequestId={RequestId}, TotalReceived={TotalReceived}",
                    orderSummary.Id, requestId, totalReceived);

                // STEP 8: Build success response
                response.Success = true;
                response.Message = "Items received successfully and order summary created.";
                response.RequestId = requestId;
                response.OrderSummaryId = orderSummary.Id;
                response.ReceivedDate = now;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order summary for RequestId={RequestId}", requestId);
                response.Success = false;
                response.Message = $"Error creating order summary: {ex.Message}";
                return response;
            }
        }

        /// <summary>
        /// Get a complete order summary by ID with all details
        /// </summary>
        public async Task<OrderSummaryDto?> GetOrderSummaryByIdAsync(int id)
        {
            _logger.LogInformation("Fetching order summary by Id={Id}", id);

            var orderSummary = await _context.OrderSummaries
                .Include(os => os.Request)
                .Include(os => os.User)
                .Include(os => os.IssuedByUser)
                .Include(os => os.ApprovedByUser)
                .Include(os => os.Items)
                    .ThenInclude(osi => osi.Item)
                        .ThenInclude(i => i.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(os => os.Id == id);

            if (orderSummary == null)
            {
                _logger.LogWarning("Order summary not found: Id={Id}", id);
                return null;
            }

            return MapToOrderSummaryDto(orderSummary);
        }

        /// <summary>
        /// Get order summary for a specific request
        /// </summary>
        public async Task<OrderSummaryDto?> GetOrderSummaryByRequestAsync(int requestId)
        {
            _logger.LogInformation("Fetching order summary by RequestId={RequestId}", requestId);

            var orderSummary = await _orderSummaryRepo.GetByRequestIdAsync(requestId);
            if (orderSummary == null)
            {
                _logger.LogWarning("Order summary not found for RequestId={RequestId}", requestId);
                return null;
            }

            return MapToOrderSummaryDto(orderSummary);
        }

        /// <summary>
        /// Get all orders for a user (paginated)
        /// </summary>
        public async Task<OrderHistoryListDto> GetUserOrdersAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation("Fetching user order history: UserId={UserId}, Page={Page}, PageSize={PageSize}",
                userId, pageNumber, pageSize);

            var orders = await _orderSummaryRepo.GetByUserIdAsync(userId, pageNumber, pageSize);
            var totalCount = await _orderSummaryRepo.GetCountByUserIdAsync(userId);

            var dtoItems = orders.Select(os => MapToOrderHistoryItemDto(os)).ToList();

            var result = new OrderHistoryListDto
            {
                Orders = dtoItems,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _logger.LogInformation("User order history fetched: UserId={UserId}, Count={Count}, Total={Total}",
                userId, dtoItems.Count, totalCount);

            return result;
        }

        /// <summary>
        /// Get order history with filtering
        /// </summary>
        public async Task<OrderHistoryListDto> GetOrderHistoryAsync(
            int? userId = null,
            int? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            _logger.LogInformation(
                "Fetching filtered order history: UserId={UserId}, Status={Status}, FromDate={FromDate}, ToDate={ToDate}, Page={Page}",
                userId, status, fromDate, toDate, pageNumber);

            var orders = await _orderSummaryRepo.GetFilteredAsync(userId, status, fromDate, toDate, pageNumber, pageSize);
            var totalCount = await _orderSummaryRepo.GetFilteredCountAsync(userId, status, fromDate, toDate);

            var dtoItems = orders.Select(os => MapToOrderHistoryItemDto(os)).ToList();

            var result = new OrderHistoryListDto
            {
                Orders = dtoItems,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _logger.LogInformation("Filtered order history fetched: Count={Count}, Total={Total}", dtoItems.Count, totalCount);

            return result;
        }

        /// <summary>
        /// Get order summary statistics for a user
        /// </summary>
        public async Task<OrderSummaryStatisticsDto> GetUserStatisticsAsync(int userId)
        {
            _logger.LogInformation("Fetching order statistics for UserId={UserId}", userId);
            return await _orderSummaryRepo.GetStatisticsAsync(userId);
        }

        /// <summary>
        /// Get global order summary statistics
        /// </summary>
        public async Task<OrderSummaryStatisticsDto> GetGlobalStatisticsAsync()
        {
            _logger.LogInformation("Fetching global order statistics");
            return await _orderSummaryRepo.GetStatisticsAsync();
        }

        public async Task<List<ReorderSuggestion>> GetReorderableItemsAsync(int requestId)
        {
            _logger.LogInformation("Fetching reorderable items for RequestId={RequestId}", requestId);

            var items = await _context.RequestItems
                .Include(ri => ri.Item)
                .Where(ri => ri.RequestId == requestId && ri.IssuerRejectedQuantity > 0)
                .Select(ri => new ReorderSuggestion
                {
                    ItemId = ri.ItemId,
                    ItemName = ri.Item != null ? ri.Item.Name : "Unknown",
                    SuggestedQuantity = ri.IssuerRejectedQuantity
                })
                .ToListAsync();

            return items;
        }

        // ============================================================================
        // PRIVATE HELPER METHODS
        // ============================================================================

        private OrderSummaryDto MapToOrderSummaryDto(OrderSummary orderSummary)
        {
            return new OrderSummaryDto
            {
                Id = orderSummary.Id,
                RequestId = orderSummary.RequestId,
                UserId = orderSummary.UserId,
                UserName = orderSummary.User?.Username ?? "Unknown",
                UserEmail = orderSummary.User?.Email ?? "Unknown",
                IssuedByUserId = orderSummary.IssuedByUserId,
                IssuedByUserName = orderSummary.IssuedByUser?.Username,
                ApprovedByUserId = orderSummary.ApprovedByUserId,
                ApprovedByUserName = orderSummary.ApprovedByUser?.Username,
                RequestedDate = orderSummary.RequestedDate,
                IssuedDate = orderSummary.IssuedDate,
                ApprovedDate = orderSummary.ApprovedDate,
                ReceivedDate = orderSummary.ReceivedDate,
                TotalRequestedQuantity = orderSummary.TotalRequestedQuantity,
                TotalIssuedQuantity = orderSummary.TotalIssuedQuantity,
                TotalApprovedQuantity = orderSummary.TotalApprovedQuantity,
                TotalRejectedQuantity = orderSummary.TotalRejectedQuantity,
                TotalReceivedQuantity = orderSummary.TotalReceivedQuantity,
                Status = orderSummary.Status,
                Items = orderSummary.Items?.Select(osi => new OrderSummaryItemDto
                {
                    ItemId = osi.ItemId,
                    ItemName = osi.Item?.Name ?? "Unknown",
                    CategoryName = osi.Item?.Category?.Name ?? "Uncategorized",
                    RequestedQuantity = osi.RequestedQuantity,
                    IssuedQuantity = osi.IssuedQuantity,
                    IssuerRejectedQuantity = osi.IssuerRejectedQuantity,
                    ApprovedQuantity = osi.ApprovedQuantity,
                    AdminRejectedQuantity = osi.AdminRejectedQuantity,
                    ReceivedQuantity = osi.ReceivedQuantity
                }).ToList() ?? new List<OrderSummaryItemDto>(),
                Notes = orderSummary.Notes,
                CreatedAt = orderSummary.CreatedAt
            };
        }

        private OrderHistoryItemDto MapToOrderHistoryItemDto(OrderSummary orderSummary)
        {
            return new OrderHistoryItemDto
            {
                Id = orderSummary.Id,
                RequestId = orderSummary.RequestId,
                ReceivedDate = orderSummary.ReceivedDate,
                Status = orderSummary.Status,
                TotalRequestedQuantity = orderSummary.TotalRequestedQuantity,
                TotalApprovedQuantity = orderSummary.TotalApprovedQuantity,
                TotalRejectedQuantity = orderSummary.TotalRejectedQuantity,
                TotalReceivedQuantity = orderSummary.TotalReceivedQuantity,
                ItemCount = orderSummary.Items?.Count ?? 0
            };
        }
    }
}
