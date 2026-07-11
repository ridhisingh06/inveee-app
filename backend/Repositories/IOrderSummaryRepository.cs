using invmgmt.web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace invmgmt.web.Repositories
{
    /// <summary>
    /// Repository interface for OrderSummary entity
    /// Handles immutable order records created when users receive items
    /// </summary>
    public interface IOrderSummaryRepository
    {
        /// <summary>Get order summary by ID with all items</summary>
        Task<OrderSummary?> GetByIdWithItemsAsync(int id);

        /// <summary>Get order summary by request ID (one-to-one relationship)</summary>
        Task<OrderSummary?> GetByRequestIdAsync(int requestId);

        /// <summary>Get all order summaries for a user (paginated)</summary>
        Task<IEnumerable<OrderSummary>> GetByUserIdAsync(int userId, int pageNumber, int pageSize);

        /// <summary>Get count of order summaries for a user</summary>
        Task<int> GetCountByUserIdAsync(int userId);

        /// <summary>Get order summaries with filters (status, date range)</summary>
        Task<IEnumerable<OrderSummary>> GetFilteredAsync(
            int? userId = null,
            int? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 10);

        /// <summary>Get count of order summaries with filters</summary>
        Task<int> GetFilteredCountAsync(
            int? userId = null,
            int? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>Create a new order summary</summary>
        Task CreateAsync(OrderSummary orderSummary);

        /// <summary>Check if order summary already exists for a request</summary>
        Task<bool> ExistsByRequestIdAsync(int requestId);

        /// <summary>Get recent order summaries (for dashboard)</summary>
        Task<IEnumerable<OrderSummary>> GetRecentAsync(int count = 10);

        /// <summary>Get summary statistics for reporting</summary>
        Task<OrderSummaryStatisticsDto> GetStatisticsAsync(int? userId = null);

        /// <summary>Save all changes to database</summary>
        Task SaveChangesAsync();
    }

    /// <summary>DTO for order summary statistics</summary>
    public class OrderSummaryStatisticsDto
    {
        public int TotalOrders { get; set; }
        public int TotalQuantityReceived { get; set; }
        public int TotalQuantityRejected { get; set; }
        public DateTime? OldestOrder { get; set; }
        public DateTime? LatestOrder { get; set; }
        public decimal? AverageQuantityPerOrder { get; set; }
    }
}
