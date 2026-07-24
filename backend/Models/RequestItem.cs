using invmgmt.web.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace invmgmt.web.Models
{
    public class RequestItem
    {
        public int Id { get; set; }

        public int RequestId { get; set; }
        public Request Request { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; } = null!

        // Original fields (backward compatibility)
        public int QuantityRequested { get; set; }
        public int QuantityApproved { get; set; }
        public int QuantityIssued { get; set; }
        public RequestItemStatus Status { get; set; } = RequestItemStatus.PendingWithIssuer;

        // NEW FIELDS FOR PARTIAL ISSUING/APPROVAL WORKFLOW
        
        // Issuer-level partial issuing
        public int IssuerIssuedQuantity { get; set; } = 0;
        public int IssuerRejectedQuantity { get; set; } = 0;
        
        // Admin-level partial approval
        public int AdminApprovedQuantity { get; set; } = 0;
        public int AdminRejectedQuantity { get; set; } = 0;

        // User-level receiving
        public int ReceivedQuantity { get; set; } = 0;

        // Audit Trail
        public DateTime? IssuedDate { get; set; }
        public int? IssuedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }
        public int? ApprovedBy { get; set; }

        public DateTime? ReceivedDate { get; set; }

        // Version/Concurrency control for optimistic locking (optional, using rowversion is better for PostgreSQL)
        public int ConcurrencyToken { get; set; } = 0;
    }
}
