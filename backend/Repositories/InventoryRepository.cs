using invmgmt.web.Data;
using invmgmt.web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace invmgmt.web.Repositories
{
    /// <summary>
    /// Repository for InventoryStock entity
    /// Implements pessimistic row-level locking to prevent race conditions
    /// </summary>
    public class InventoryRepository : IInventoryRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InventoryRepository> _logger;

        public InventoryRepository(AppDbContext context, ILogger<InventoryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<InventoryStock?> GetByItemCodeAsync(string itemCode)
        {
            return await _context.InventoryStocks
                .Include(s => s.Item)
                .FirstOrDefaultAsync(s => s.Item.ItemCode == itemCode);
        }

        public async Task<InventoryStock?> GetByIdAsync(int id)
        {
            return await _context.InventoryStocks
                .Include(s => s.Item)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Lock and retrieve inventory using PostgreSQL FOR UPDATE (pessimistic locking)
        /// This prevents other transactions from modifying this row until current transaction completes
        /// </summary>
        public async Task<InventoryStock?> LockAndGetAsync(string itemCode)
        {
            var inventory = await _context.InventoryStocks
                .FromSqlInterpolated<InventoryStock>($@"
                    SELECT s.* FROM ""InventoryStocks"" s
                    INNER JOIN ""Items"" i ON s.""ItemId"" = i.""Id""
                    WHERE i.""ItemCode"" = {itemCode}
                    FOR UPDATE
                ")
                .Include(s => s.Item)
                .FirstOrDefaultAsync();

            if (inventory != null)
            {
                _logger.LogInformation(
                    "Inventory locked for ItemCode={ItemCode}, AvailableQuantity={Available}",
                    itemCode, inventory.AvailableQuantity);
            }

            return inventory;
        }

        /// <summary>
        /// Deduct quantity from available inventory
        /// Assumes inventory is already locked via LockAndGetAsync
        /// Returns false if available quantity is insufficient
        /// </summary>
        public async Task<bool> TryDeductAsync(string itemCode, int quantity)
        {
            var inventory = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.Item.ItemCode == itemCode);
            if (inventory == null)
            {
                _logger.LogWarning("Inventory not found for deduction: ItemCode={ItemCode}", itemCode);
                return false;
            }

            if (inventory.AvailableQuantity < quantity)
            {
                _logger.LogWarning(
                    "Insufficient inventory: ItemCode={ItemCode}, Available={Available}, Requested={Requested}",
                    itemCode, inventory.AvailableQuantity, quantity);
                return false;
            }

            // Deduct from available quantity
            inventory.AvailableQuantity -= quantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            _context.InventoryStocks.Update(inventory);

            _logger.LogInformation(
                "Inventory deducted: ItemCode={ItemCode}, Quantity={Quantity}, NewAvailable={NewAvailable}",
                itemCode, quantity, inventory.AvailableQuantity);

            return true;
        }

        /// <summary>
        /// Restore quantity back to inventory (when admin rejects issued items).
        ///
        /// The only valid failure is "inventory record not found". A previously
        /// issued quantity was already deducted from AvailableQuantity, so
        /// adding it back can never logically exceed TotalQuantity. The old
        /// guard (AvailableQuantity + quantity > TotalQuantity) was incorrect:
        /// it compared against the already-debited AvailableQuantity and
        /// always fired true for any non-zero rejection, blocking every restore
        /// and causing HTTP 400 on approve-partially.
        /// </summary>
        public async Task<bool> RestoreAsync(string itemCode, int quantity)
        {
            var inventory = await LockAndGetAsync(itemCode);
            if (inventory == null)
            {
                _logger.LogWarning("Inventory not found for restoration: ItemCode={ItemCode}", itemCode);
                return false;
            }

            // Restore the previously-deducted quantity.
            // Cap at TotalQuantity as a safety net against double-restore bugs,
            // but log a warning rather than silently rejecting the operation.
            var restored = Math.Min(quantity, inventory.TotalQuantity - inventory.AvailableQuantity);
            if (restored != quantity)
            {
                _logger.LogWarning(
                    "RestoreAsync capped: ItemCode={ItemCode}, Requested={Requested}, Actual={Actual}, " +
                    "Available={Available}, Total={Total}. Possible double-restore.",
                    itemCode, quantity, restored,
                    inventory.AvailableQuantity, inventory.TotalQuantity);
            }

            inventory.AvailableQuantity += restored;
            inventory.UpdatedAt = DateTime.UtcNow;

            _context.InventoryStocks.Update(inventory);

            _logger.LogInformation(
                "Inventory restored: ItemCode={ItemCode}, RestoredQuantity={Restored}, NewAvailable={NewAvailable}",
                itemCode, restored, inventory.AvailableQuantity);

            return true;
        }

        public async Task<int> GetAvailableQuantityAsync(string itemCode)
        {
            var inventory = await _context.InventoryStocks
                .Where(s => s.Item.ItemCode == itemCode)
                .Select(s => s.AvailableQuantity)
                .FirstOrDefaultAsync();

            return inventory;
        }

        public async Task<int> GetTotalQuantityAsync(string itemCode)
        {
            var inventory = await _context.InventoryStocks
                .Where(s => s.Item.ItemCode == itemCode)
                .Select(s => s.TotalQuantity)
                .FirstOrDefaultAsync();

            return inventory;
        }

        public async Task AddAsync(InventoryStock stock)
        {
            await _context.InventoryStocks.AddAsync(stock);
            _logger.LogInformation("InventoryStock added: ItemId={ItemId}, Quantity={Quantity}",
                stock.ItemId, stock.TotalQuantity);
        }

        public async Task UpdateAsync(InventoryStock stock)
        {
            stock.UpdatedAt = DateTime.UtcNow;
            _context.InventoryStocks.Update(stock);
            _logger.LogInformation("InventoryStock updated: ItemId={ItemId}, Available={Available}",
                stock.ItemId, stock.AvailableQuantity);
        }

        public async Task<bool> ExistsAsync(string itemCode)
        {
            return await _context.InventoryStocks.AnyAsync(s => s.Item.ItemCode == itemCode);
        }

        public async Task<IEnumerable<InventoryStock>> GetAllAsync()
        {
            return await _context.InventoryStocks
                .Include(s => s.Item)
                    .ThenInclude(i => i.Category)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
