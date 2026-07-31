using System.Collections.Generic;

namespace invmgmt.web.DTOs
{
    public class PersonnelPagedResultDto
    {
        public IEnumerable<PersonnelResponseDto> Data { get; set; } = new List<PersonnelResponseDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
