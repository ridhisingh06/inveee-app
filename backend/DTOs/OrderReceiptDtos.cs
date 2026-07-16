using System.ComponentModel.DataAnnotations;

namespace invmgmt.web.DTOs;

public sealed class OrderReceiptDto
{
    public int id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public string IssuerName { get; set; } = string.Empty;
    public string AdminName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<OrderReceiptItemDto> Items { get; set; } = new();
    public OrderReceiptSummaryDto Summary { get; set; } = new();
    public string Remarks { get; set; } = string.Empty;
    public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;
}

public sealed class OrderReceiptItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public int RequestedQty { get; set; }
    public int IssuerIssued { get; set; }
    public int IssuerRejected { get; set; }
    public int AdminApproved { get; set; }
    public int AdminRejected { get; set; }
    public int FinalReceiveQty { get; set; }
}

public sealed class OrderReceiptSummaryDto
{
    public int TotalRequested { get; set; }
    public int TotalIssuerApproved { get; set; }
    public int TotalIssuerRejected { get; set; }
    public int TotalAdminApproved { get; set; }
    public int TotalAdminRejected { get; set; }
    public int TotalFinalReceived { get; set; }
}

public sealed class MarkAsReceivedDto
{
    [Required]
    public int RequestId { get; set; }
}
