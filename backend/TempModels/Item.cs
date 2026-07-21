using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class Item
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int CategoryId { get; set; }

    public string Description { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();

    public virtual Category Category { get; set; } = null!;

    public virtual InventoryStock? InventoryStock { get; set; }

    public virtual ICollection<OrderSummaryItem> OrderSummaryItems { get; set; } = new List<OrderSummaryItem>();

    public virtual ICollection<RequestItem> RequestItems { get; set; } = new List<RequestItem>();

    public virtual ICollection<RoleItemLimit> RoleItemLimits { get; set; } = new List<RoleItemLimit>();
}
