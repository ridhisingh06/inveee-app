using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class BillItem
{
    public int Id { get; set; }

    public int BillId { get; set; }

    public int ItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Bill Bill { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;
}
