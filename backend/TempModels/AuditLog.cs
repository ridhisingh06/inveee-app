using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class AuditLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public int EntityId { get; set; }

    public string OldValue { get; set; } = null!;

    public string NewValue { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
