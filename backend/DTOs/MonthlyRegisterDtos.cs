using invmgmt.web.Models.Enums;
using System;
using System.Collections.Generic;

namespace invmgmt.web.DTOs
{
    public class MonthlyRegisterRowDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public int QuantityRequested { get; set; }
        public int QuantityApproved { get; set; }
        public int QuantityIssued { get; set; }
    }

    public class MonthlyRegisterResultDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public IEnumerable<MonthlyRegisterRowDto> Data { get; set; } = Array.Empty<MonthlyRegisterRowDto>();
        public int TotalQuantityRequested { get; set; }
        public int TotalQuantityApproved { get; set; }
        public int TotalQuantityIssued { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
