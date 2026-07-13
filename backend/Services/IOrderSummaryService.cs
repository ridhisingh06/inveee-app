using invmgmt.web.DTOs;
using System.Threading.Tasks;

namespace invmgmt.web.Services
{
    /// <summary>
    /// Service interface for order summary operations
    /// Handles creation and retrieval of immutable order records
    /// </summary>
    public interface IOrderSummaryService
    {
        /// <summary>Create an order summary when user receives approved items</summary>
        Task<ReceiveItemsResponseDto> CreateOrderSummaryAsync(int requestId, int userId, string? notes = null);

        /// <summary>Get a complete order summary by ID with all details</summary>
        Task<OrderSummaryDto?> GetOrderSummaryByIdAsync(int id);

        /// <summary>Get order summary for a specific request</summary>
        Task<OrderSummaryDto?> GetOrderSummaryByRequestAsync(int requestId);

        /// <summary>Get all orders for a user (paginated)</summary>
        Task<OrderHistoryListDto> GetUserOrdersAsync(int userId, int pageNumber = 1, int pageSize = 10);

        /// <summary>Get order history with filtering</summary>
        Task<OrderHistoryListDto> GetOrderHistoryAsync(
            int? userId = null,
            int? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 10);

        /// <summary>Get order summary statistics for a user</summary>
        Task<OrderSummaryStatisticsDto> GetUserStatisticsAsync(int userId);

        /// <summary>Get global order summary statistics</summary>
        Task<OrderSummaryStatisticsDto> GetGlobalStatisticsAsync();

        /// <summary>Get list of reorderable items based on issuer rejected quantity</summary>
        Task<List<ReorderSuggestion>> GetReorderableItemsAsync(int requestId);
    }
}
