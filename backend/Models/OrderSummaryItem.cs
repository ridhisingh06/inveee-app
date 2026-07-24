using System;

namespace invmgmt.web.Models
{
    /// <summary>
    /// Line item in an Order Summary. Immutable record of what was issued, approved, and received.
    /// </summary>
    public class OrderSummaryItem
    {
        public int Id { get; set; }

        public int OrderSummaryId { get; set; }
        public OrderSummary OrderSummary { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; }

        // Quantities at each stage
        public int RequestedQuantity { get; set; }
        public int IssuedQuantity { get; set; }
        public int IssuerRejectedQuantity { get; set; }
        public int ApprovedQuantity { get; set; }
        public int AdminRejectedQuantity { get; set; }
        public int ReceivedQuantity { get; set; }

        // Original RequestItem reference for traceability
        public int RequestItemId { get; set; }
        public RequestItem RequestItem { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
