using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class InventoryStock
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    public int TotalQuantity { get; set; }

    public int AvailableQuantity { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Item Item { get; set; } = null!;
}
