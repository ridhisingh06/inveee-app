using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class RoleItemLimit
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public int ItemId { get; set; }

    public int MaxLimit { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
