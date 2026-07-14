using invmgmt.web.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace invmgmt.web.Models
{
    [Index(nameof(UserId), nameof(Status))]
    [Index(nameof(Status), nameof(CreatedAt))]
    public class Request
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        // Category is optional; requests are primarily tracked at the item level.
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public RequestStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // NEW AUDIT FIELDS FOR ENTERPRISE WORKFLOW
        public DateTime? IssuedDate { get; set; }
        public int? IssuedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }
        public int? ApprovedBy { get; set; }

        public DateTime? ReceivedDate { get; set; }

        public ICollection<RequestItem> RequestItems { get; set; }
        public ICollection<ApprovalLog> ApprovalLogs { get; set; }
        public ICollection<IssueLog> IssueLogs { get; set; }
        public ICollection<ReceivedLog> ReceivedLogs { get; set; }

        /// <summary>
        /// Recalculates and updates the overall Request status based on its RequestItems.
        ///
        /// Priority order (highest wins):
        ///  1. Any item still PendingWithIssuer  → PendingWithIssuer
        ///  2. Any item still PendingAdminApproval → PendingAdminApproval
        ///  3. All items NotIssued               → NotIssued  (issuer rejected everything)
        ///  4. All items in {Approved, NotIssued, Received}
        ///     and at least one Approved          → Approved   (ReadyToReceive)
        ///  5. All items in terminal states
        ///     and at least one Received          → Received
        ///  6. All items terminal, none Received  → Rejected
        /// </summary>
        public void RecalculateStatus()
        {
            if (RequestItems == null || RequestItems.Count == 0)
            {
                Status = RequestStatus.PendingWithIssuer;
                return;
            }

            var itemStatuses = RequestItems.Select(ri => ri.Status).ToList();

            // 1. Still waiting for issuer
            if (itemStatuses.Any(s => s == RequestItemStatus.PendingWithIssuer))
            {
                Status = RequestStatus.PendingWithIssuer;
                return;
            }

            // 2. Still waiting for admin
            if (itemStatuses.Any(s => s == RequestItemStatus.PendingAdminApproval))
            {
                Status = RequestStatus.PendingAdminApproval;
                return;
            }

            // 3. Issuer rejected everything
            if (itemStatuses.All(s => s == RequestItemStatus.NotIssued))
            {
                Status = RequestStatus.NotIssued;
                return;
            }

            // 4. ✅ Approved (ReadyToReceive): all remaining items are Approved,
            //    NotIssued (issuer-rejected), or already Received — and at least
            //    one is Approved.  This handles partial-issue scenarios where some
            //    items were rejected by the issuer and others were approved by admin.
            var approvedGroup = new[]
            {
                RequestItemStatus.Approved,
                RequestItemStatus.NotIssued,
                RequestItemStatus.Received
            };
            if (itemStatuses.All(s => approvedGroup.Contains(s))
                && itemStatuses.Any(s => s == RequestItemStatus.Approved))
            {
                Status = RequestStatus.Approved;
                return;
            }

            // 5 & 6. All terminal
            var terminalGroup = new[]
            {
                RequestItemStatus.Received,
                RequestItemStatus.NotIssued,
                RequestItemStatus.Rejected
            };
            if (itemStatuses.All(s => terminalGroup.Contains(s)))
            {
                Status = itemStatuses.Any(s => s == RequestItemStatus.Received)
                    ? RequestStatus.Received
                    : RequestStatus.Rejected;
                return;
            }

            Status = RequestStatus.Rejected;
        }
    }
}
