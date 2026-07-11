using invmgmt.web.DTOs;
using System.Threading.Tasks;

namespace invmgmt.web.Services
{
    /// <summary>
    /// Service interface for issuer operations
    /// Handles partial issuing of items with inventory locking and deduction
    /// </summary>
    public interface IIssuerService
    {
        /// <summary>Get all pending items waiting for issuer to issue</summary>
        Task<IssuerPendingListDto> GetPendingItemsAsync(int pageNumber = 1, int pageSize = 10);

        /// <summary>Get pending items for a specific request</summary>
        Task<IssuerPendingListDto> GetPendingItemsByRequestAsync(int requestId);

        /// <summary>Issue items partially (Issuer submits partial quantities)</summary>
        Task<IssuePartiallyResponseDto> IssuePartiallyAsync(IssuePartiallyDto dto, int issuerId);

        /// <summary>Get count of pending items waiting for issuer</summary>
        Task<int> GetPendingCountAsync();
    }
}
