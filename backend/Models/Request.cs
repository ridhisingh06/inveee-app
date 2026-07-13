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
        /// </summary>
        public void RecalculateStatus()
        {
            if (RequestItems == null || RequestItems.Count == 0)
            {
                Status = RequestStatus.PendingWithIssuer;
                return;
            }

            var itemStatuses = RequestItems.Select(ri => ri.Status).ToList();

            if (itemStatuses.Any(status => status == RequestItemStatus.PendingWithIssuer))
            {
                Status = RequestStatus.PendingWithIssuer;
                return;
            }

            if (itemStatuses.Any(status => status == RequestItemStatus.PendingAdminApproval))
            {
                Status = RequestStatus.PendingAdminApproval;
                return;
            }

            if (itemStatuses.All(status => status == RequestItemStatus.NotIssued))
            {
                Status = RequestStatus.NotIssued;
                return;
            }

            if (itemStatuses.All(status => status == RequestItemStatus.Received || status == RequestItemStatus.NotIssued || status == RequestItemStatus.Rejected))
            {
                if (itemStatuses.All(status => status == RequestItemStatus.Received || status == RequestItemStatus.NotIssued))
                {
                    Status = RequestStatus.Received;
                    return;
                }
                Status = RequestStatus.Rejected;
                return;
            }

            if (itemStatuses.All(status => status == RequestItemStatus.Approved || status == RequestItemStatus.NotIssued || status == RequestItemStatus.Received))
            {
                Status = RequestStatus.Approved;
                return;
            }

            Status = RequestStatus.Rejected;
        }
    }
}
