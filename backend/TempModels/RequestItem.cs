using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class RequestItem
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public int ItemId { get; set; }

    public int QuantityRequested { get; set; }

    public int QuantityApproved { get; set; }

    public int QuantityIssued { get; set; }

    public int Status { get; set; }

    public int AdminApprovedQuantity { get; set; }

    public int AdminRejectedQuantity { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public int ConcurrencyToken { get; set; }

    public int? IssuedBy { get; set; }

    public DateTime? IssuedDate { get; set; }

    public int IssuerIssuedQuantity { get; set; }

    public int IssuerRejectedQuantity { get; set; }

    public DateTime? ReceivedDate { get; set; }

    public int ReceivedQuantity { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual ICollection<OrderSummaryItem> OrderSummaryItems { get; set; } = new List<OrderSummaryItem>();

    public virtual Request Request { get; set; } = null!;
}
