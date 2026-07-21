using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class IssueLog
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public int IssuedBy { get; set; }

    public int UserId { get; set; }

    public DateTime IssuedDate { get; set; }

    public virtual Request Request { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
