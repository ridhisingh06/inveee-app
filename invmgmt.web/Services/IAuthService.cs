using invmgmt.web.DTOs;

namespace invmgmt.web.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Token, string Message)> LoginAsync(LoginRequest dto);
    }
}
