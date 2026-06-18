using invmgmt.web.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace invmgmt.web.Services
{
    public interface ISectionWiseQueryService
    {
        Task<List<SectionWiseQueryOfficerDto>> GetOfficersAsync();
        Task<List<string>> GetBhawansAsync();
        Task<List<SectionWiseQueryItemDto>> SearchItemsAsync(string query);
        Task<SectionWiseQueryResultDto> GetSectionWiseQueryAsync(SectionWiseQueryFilterDto filter);
    }
}
