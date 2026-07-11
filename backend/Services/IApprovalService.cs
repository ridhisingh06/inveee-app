using invmgmt.web.DTOs;
using System.Threading.Tasks;

namespace invmgmt.web.Services
{
    /// <summary>
    /// Service interface for admin approval operations
    /// Handles partial approval with inventory restoration for rejected items
    /// </summary>
    public interface IApprovalService
    {
        /// <summary>Get all pending items waiting for admin approval</summary>
        Task<AdminPendingListDto> GetPendingItemsAsync(int pageNumber = 1, int pageSize = 10);

        /// <summary>Get pending items for a specific request</summary>
        Task<AdminPendingListDto> GetPendingItemsByRequestAsync(int requestId);

        /// <summary>Approve items partially (Admin approves/rejects issued items)</summary>
        Task<ApprovePartiallyResponseDto> ApprovePartiallyAsync(ApprovePartiallyDto dto, int adminId);

        /// <summary>Get count of pending items waiting for admin</summary>
        Task<int> GetPendingCountAsync();
    }
}
