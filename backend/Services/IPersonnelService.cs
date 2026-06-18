using invmgmt.web.DTOs;

namespace invmgmt.web.Services
{
    public interface IPersonnelService
    {
        Task<PersonnelResponseDto> CreateAsync(PersonnelCreateDto dto, IFormFile? photo, string baseUrl);
        Task<PersonnelPagedResultDto> GetAllAsync(int page, int pageSize, string baseUrl);
        Task<PersonnelResponseDto?> GetByIdAsync(int id, string baseUrl);
        Task<PersonnelResponseDto> UpdateAsync(int id, PersonnelCreateDto dto, IFormFile? photo, string baseUrl);
        Task DeleteAsync(int id);
    }
}
