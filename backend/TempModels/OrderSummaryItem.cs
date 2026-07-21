using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class OrderSummaryItem
{
    public int Id { get; set; }

    public int OrderSummaryId { get; set; }

    public int ItemId { get; set; }

    public int RequestedQuantity { get; set; }

    public int IssuedQuantity { get; set; }

    public int IssuerRejectedQuantity { get; set; }

    public int ApprovedQuantity { get; set; }

    public int AdminRejectedQuantity { get; set; }

    public int ReceivedQuantity { get; set; }

    public int RequestItemId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual OrderSummary OrderSummary { get; set; } = null!;

    public virtual RequestItem RequestItem { get; set; } = null!;
}
