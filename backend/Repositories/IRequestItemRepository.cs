using invmgmt.web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace invmgmt.web.Repositories
{
    /// <summary>
    /// Repository interface for RequestItem entity
    /// Handles CRUD and enterprise workflow operations for request items
    /// </summary>
    public interface IRequestItemRepository
    {
        /// <summary>Get a single request item by ID with all navigation properties</summary>
        Task<RequestItem?> GetByIdWithDetailsAsync(int id);

        /// <summary>Get multiple request items by request ID</summary>
        Task<IEnumerable<RequestItem>> GetByRequestIdAsync(int requestId);

        /// <summary>Get request item with inventory and related data</summary>
        Task<RequestItem?> GetByIdWithInventoryAsync(int id);

        /// <summary>Add a new request item</summary>
        Task AddAsync(RequestItem item);

        /// <summary>Add multiple request items</summary>
        Task AddRangeAsync(IEnumerable<RequestItem> items);

        /// <summary>Update issuer quantities (IssuerIssuedQuantity, IssuerRejectedQuantity)</summary>
        Task UpdateIssuerQuantitiesAsync(
            int requestItemId,
            int issuerIssuedQuantity,
            int issuerRejectedQuantity,
            int issuedByUserId,
            DateTime issuedDate);

        /// <summary>Update admin approval quantities (AdminApprovedQuantity, AdminRejectedQuantity)</summary>
        Task UpdateAdminQuantitiesAsync(
            int requestItemId,
            int adminApprovedQuantity,
            int adminRejectedQuantity,
            int approvedByUserId,
            DateTime approvedDate);

        /// <summary>Update received quantity when user receives items</summary>
        Task UpdateReceivedAsync(
            int requestItemId,
            int receivedQuantity,
            DateTime receivedDate);

        /// <summary>Get all pending items with issuer (not yet issued)</summary>
        Task<IEnumerable<RequestItem>> GetPendingWithIssuerAsync(int pageNumber, int pageSize);

        /// <summary>Get all pending items with admin (issued but not approved)</summary>
        Task<IEnumerable<RequestItem>> GetPendingWithAdminAsync(int pageNumber, int pageSize);

        /// <summary>Get items pending with issuer count</summary>
        Task<int> GetPendingWithIssuerCountAsync();

        /// <summary>Get items pending with admin count</summary>
        Task<int> GetPendingWithAdminCountAsync();

        /// <summary>Check if all items in a request have been issued</summary>
        Task<bool> AllItemsIssuedAsync(int requestId);

        /// <summary>Check if all items in a request have been approved</summary>
        Task<bool> AllItemsApprovedAsync(int requestId);

        /// <summary>Save all changes to database</summary>
        Task SaveChangesAsync();
    }
}
