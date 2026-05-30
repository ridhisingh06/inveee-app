using invmgmt.web.DTOs;
using invmgmt.web.Models;

namespace invmgmt.web.Services
{
    public interface IRegistrationService
    {
        Task<(bool Success, string Message)> RegisterAsync(RegistrationRequestDto dto);
        Task<IEnumerable<PendingRegistrationDto>> GetPendingRequestsAsync();
        Task<(bool Success, string Message)> ApproveAsync(int requestId);
        Task<(bool Success, string Message)> RejectAsync(int requestId);
    }
}
