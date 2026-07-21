using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class Bill
{
    public int Id { get; set; }

    public string BillNo { get; set; } = null!;

    public DateTime BillDate { get; set; }

    public string VendorName { get; set; } = null!;

    public decimal GrandTotal { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();

    public virtual User CreatedByUser { get; set; } = null!;
}
