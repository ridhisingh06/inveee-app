using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class ApprovalLog
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public int ApprovedBy { get; set; }

    public int UserId { get; set; }

    public string Status { get; set; } = null!;

    public string Remarks { get; set; } = null!;

    public DateTime ActionDate { get; set; }

    public virtual Request Request { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
