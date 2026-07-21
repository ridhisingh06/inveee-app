using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class Request
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? CategoryId { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public int? IssuedBy { get; set; }

    public DateTime? IssuedDate { get; set; }

    public DateTime? ReceivedDate { get; set; }

    public virtual ICollection<ApprovalLog> ApprovalLogs { get; set; } = new List<ApprovalLog>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<IssueLog> IssueLogs { get; set; } = new List<IssueLog>();

    public virtual OrderSummary? OrderSummary { get; set; }

    public virtual ICollection<ReceivedLog> ReceivedLogs { get; set; } = new List<ReceivedLog>();

    public virtual ICollection<RequestItem> RequestItems { get; set; } = new List<RequestItem>();

    public virtual User User { get; set; } = null!;
}
