using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class OrderSummary
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public int UserId { get; set; }

    public int? IssuedByUserId { get; set; }

    public int? ApprovedByUserId { get; set; }

    public DateTime RequestedDate { get; set; }

    public DateTime IssuedDate { get; set; }

    public DateTime ApprovedDate { get; set; }

    public DateTime ReceivedDate { get; set; }

    public int TotalRequestedQuantity { get; set; }

    public int TotalIssuedQuantity { get; set; }

    public int TotalApprovedQuantity { get; set; }

    public int TotalRejectedQuantity { get; set; }

    public int TotalReceivedQuantity { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Notes { get; set; }

    public virtual User? ApprovedByUser { get; set; }

    public virtual User? IssuedByUser { get; set; }

    public virtual ICollection<OrderSummaryItem> OrderSummaryItems { get; set; } = new List<OrderSummaryItem>();

    public virtual Request Request { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
