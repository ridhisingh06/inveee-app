using invmgmt.web.Models.Enums;
using System;
using System.Collections.Generic;

namespace invmgmt.web.Models
{
    /// <summary>
    /// Immutable Order Summary - Created when user receives items after admin approval.
    /// This serves as a permanent receipt/record of the complete transaction lifecycle.
    /// </summary>
    public class OrderSummary
    {
        public int Id { get; set; }

        // Link to original request
        public int RequestId { get; set; }
        public Request Request { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        // Issuer who issued the items
        public int? IssuedByUserId { get; set; }
        public User IssuedByUser { get; set; }

        // Admin who approved the items
        public int? ApprovedByUserId { get; set; }
        public User ApprovedByUser { get; set; }

        // Key Dates
        public DateTime RequestedDate { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime ApprovedDate { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.Now;

        // Summary Quantities (totals across all items in request)
        public int TotalRequestedQuantity { get; set; }
        public int TotalIssuedQuantity { get; set; }
        public int TotalApprovedQuantity { get; set; }
        public int TotalRejectedQuantity { get; set; }
        public int TotalReceivedQuantity { get; set; }

        // Status
        public RequestStatus Status { get; set; }

        // Child items in this order
        public ICollection<OrderSummaryItem> Items { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
    }
}
