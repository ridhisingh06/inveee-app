using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace invmgmt.web.DTOs
{
    public sealed class SectionWiseQueryFilterDto
    {
        public int? OfficerId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public string? Bhawan { get; set; }

        public int? ItemId { get; set; }

        public string? ItemName { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }

    public sealed class SectionWiseQueryOfficerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Building { get; set; }
    }

    public sealed class SectionWiseQueryItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int AvailableQuantity { get; set; }
    }

    public sealed class SectionWiseQueryRowDto
    {
        public int RequestItemId { get; set; }
        public int RequestId { get; set; }
        public string OfficerName { get; set; } = string.Empty;
        public string? Bhawan { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int QuantityRequested { get; set; }
        public int QuantityApproved { get; set; }
        public int QuantityIssued { get; set; }
        public string RequestStatus { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
    }

    public sealed class SectionWiseQueryResultDto
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public IEnumerable<SectionWiseQueryRowDto> Data { get; set; } = Array.Empty<SectionWiseQueryRowDto>();
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
