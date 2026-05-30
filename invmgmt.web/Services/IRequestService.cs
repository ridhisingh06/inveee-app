using invmgmt.web.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace invmgmt.web.Services
{
    public interface IRequestService
    {
        Task<(bool Success, string Message, int? RequestId)> CreateRequestAsync(int userId, CreateRequestFromCartDto dto);
        Task<IEnumerable<RequestSummaryDto>> GetUserRequestsAsync(int userId, int pageNumber = 1, int pageSize = 10);
        Task<RequestDetailDto?> GetRequestByIdAsync(int id, int? userId = null, string? role = null);
        Task<(bool Success, string Message)> ConfirmReceivedAsync(int requestId, int userId);
        Task<IEnumerable<object>> GetPendingRequestsAsync(int pageNumber = 1, int pageSize = 10);
        Task<(bool Success, string Message)> ApproveRequestAsync(int id);
        Task<(bool Success, string Message)> RejectRequestAsync(int id);
        Task<(bool Success, string Message)> CheckCanRequestAsync(int userId);
        Task<(bool Success, string Message)> DeleteRequestAsync(int id, int userId);
    }
}
