using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace invmgmt.web.Repositories
{
    /// <summary>
    /// Repository for OrderSummary entity
    /// Manages immutable order records and provides reporting queries
    /// </summary>
    public class OrderSummaryRepository : IOrderSummaryRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrderSummaryRepository> _logger;

        public OrderSummaryRepository(AppDbContext context, ILogger<OrderSummaryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OrderSummary?> GetByIdWithItemsAsync(int id)
        {
            return await _context.OrderSummaries
                .Include(os => os.Request)
                .Include(os => os.User)
                .Include(os => os.IssuedByUser)
                .Include(os => os.ApprovedByUser)
                .Include(os => os.Items)
                    .ThenInclude(osi => osi.Item)
                        .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(os => os.Id == id);
        }

        public async Task<OrderSummary?> GetByRequestIdAsync(int requestId)
        {
            return await _context.OrderSummaries
                .Include(os => os.Request)
                .Include(os => os.User)
                .Include(os => os.IssuedByUser)
                .Include(os => os.ApprovedByUser)
                .Include(os => os.Items)
                    .ThenInclude(osi => osi.Item)
                        .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(os => os.RequestId == requestId);
        }

        public async Task<IEnumerable<OrderSummary>> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            return await _context.OrderSummaries
                .Where(os => os.UserId == userId)
                .Include(os => os.Request)
                .Include(os => os.IssuedByUser)
                .Include(os => os.ApprovedByUser)
                .Include(os => os.Items)
                    .ThenInclude(osi => osi.Item)
                .OrderByDescending(os => os.ReceivedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetCountByUserIdAsync(int userId)
        {
            return await _context.OrderSummaries
                .Where(os => os.UserId == userId)
                .CountAsync();
        }

        public async Task<IEnumerable<OrderSummary>> GetFilteredAsync(
            int? userId = null,
            int? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var query = _context.OrderSummaries.AsQueryable();

            if (userId.HasValue)
                query = query.Where(os => os.UserId == userId.Value);

            if (status.HasValue)
                query = query.Where(os => (int)os.Status == status.Value);

            if (fromDate.HasValue)
                query = query.Where(os => os.ReceivedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(os => os.ReceivedDate <= toDate.Value);

            return await query
                .Include(os => os.Request)
                .Include(os => os.IssuedByUser)
                .Include(os => os.ApprovedByUser)
                .Include(os => os.Items)
                    .ThenInclude(osi => osi.Item)
                .OrderByDescending(os => os.ReceivedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetFilteredCountAsync(
            int? userId = null,
            int? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.OrderSummaries.AsQueryable();

            if (userId.HasValue)
                query = query.Where(os => os.UserId == userId.Value);

            if (status.HasValue)
                query = query.Where(os => (int)os.Status == status.Value);

            if (fromDate.HasValue)
                query = query.Where(os => os.ReceivedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(os => os.ReceivedDate <= toDate.Value);

            return await query.CountAsync();
        }

        public async Task CreateAsync(OrderSummary orderSummary)
        {
            // Validate that no order summary already exists for this request
            if (await ExistsByRequestIdAsync(orderSummary.RequestId))
            {
                _logger.LogWarning("Order summary already exists for RequestId={RequestId}", orderSummary.RequestId);
                throw new InvalidOperationException($"Order summary already exists for RequestId {orderSummary.RequestId}.");
            }

            await _context.OrderSummaries.AddAsync(orderSummary);

            _logger.LogInformation(
                "OrderSummary created: Id={Id}, RequestId={RequestId}, UserId={UserId}, TotalReceived={TotalReceived}",
                orderSummary.Id, orderSummary.RequestId, orderSummary.UserId, orderSummary.TotalReceivedQuantity);
        }

        public async Task<bool> ExistsByRequestIdAsync(int requestId)
        {
            return await _context.OrderSummaries
                .AnyAsync(os => os.RequestId == requestId);
        }

        public async Task<IEnumerable<OrderSummary>> GetRecentAsync(int count = 10)
        {
            return await _context.OrderSummaries
                .Include(os => os.User)
                .Include(os => os.IssuedByUser)
                .Include(os => os.ApprovedByUser)
                .OrderByDescending(os => os.ReceivedDate)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<OrderSummaryStatisticsDto> GetStatisticsAsync(int? userId = null)
        {
            var query = _context.OrderSummaries.AsQueryable();

            if (userId.HasValue)
                query = query.Where(os => os.UserId == userId.Value);

            var statistics = new OrderSummaryStatisticsDto
            {
                TotalOrders = await query.CountAsync(),
                TotalQuantityReceived = await query.SumAsync(os => os.TotalReceivedQuantity),
                TotalQuantityRejected = await query.SumAsync(os => os.TotalRejectedQuantity),
                OldestOrder = await query.MinAsync(os => (DateTime?)os.ReceivedDate),
                LatestOrder = await query.MaxAsync(os => (DateTime?)os.ReceivedDate)
            };

            // Calculate average
            if (statistics.TotalOrders > 0)
            {
                statistics.AverageQuantityPerOrder = (decimal)statistics.TotalQuantityReceived / statistics.TotalOrders;
            }

            _logger.LogInformation(
                "OrderSummary statistics retrieved: TotalOrders={TotalOrders}, TotalReceived={TotalReceived}, TotalRejected={TotalRejected}",
                statistics.TotalOrders, statistics.TotalQuantityReceived, statistics.TotalQuantityRejected);

            return statistics;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
