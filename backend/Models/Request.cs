using invmgmt.web.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace invmgmt.web.Models
{
    [Index(nameof(UserId), nameof(Status))]
    [Index(nameof(Status), nameof(CreatedAt))]
    public class Request
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        // Category is optional; requests are primarily tracked at the item level.
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public RequestStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<RequestItem> RequestItems { get; set; }
        public ICollection<ApprovalLog> ApprovalLogs { get; set; }
        public ICollection<IssueLog> IssueLogs { get; set; }
        public ICollection<ReceivedLog> ReceivedLogs { get; set; }
    }
}
