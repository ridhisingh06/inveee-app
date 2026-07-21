using invmgmt.web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace invmgmt.web.Repositories
{
    /// <summary>
    /// Repository interface for InventoryStock entity
    /// Handles inventory management with concurrency control
    /// </summary>
    public interface IInventoryRepository
    {
        /// <summary>Get inventory stock for an item by item ID</summary>
        Task<InventoryStock?> GetByItemIdAsync(string itemId);

        /// <summary>Get inventory stock by ID</summary>
        Task<InventoryStock?> GetByIdAsync(int id);

        /// <summary>Lock and retrieve inventory for an item (pessimistic locking with FOR UPDATE)</summary>
        Task<InventoryStock?> LockAndGetAsync(string itemId);

        /// <summary>Deduct quantity from inventory (assumes already locked)</summary>
        Task<bool> TryDeductAsync(string itemId, int quantity);

        /// <summary>Restore quantity back to inventory (for rejected items)</summary>
        Task<bool> RestoreAsync(string itemId, int quantity);

        /// <summary>Get available quantity for an item</summary>
        Task<int> GetAvailableQuantityAsync(string itemId);

        /// <summary>Get total quantity for an item</summary>
        Task<int> GetTotalQuantityAsync(string itemId);

        /// <summary>Add new inventory stock</summary>
        Task AddAsync(InventoryStock stock);

        /// <summary>Update inventory stock</summary>
        Task UpdateAsync(InventoryStock stock);

        /// <summary>Check if inventory exists for item</summary>
        Task<bool> ExistsAsync(string itemId);

        /// <summary>Get all inventory stocks (for admin view)</summary>
        Task<IEnumerable<InventoryStock>> GetAllAsync();

        /// <summary>Save changes to database</summary>
        Task SaveChangesAsync();
    }
}
