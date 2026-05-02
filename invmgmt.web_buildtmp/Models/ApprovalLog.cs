using invmgmt.web.Models;
using System;
namespace invmgmt.web.Models
{
    public class ApprovalLog
    {
        public int Id { get; set; }

        public int RequestId { get; set; }
        public Request Request { get; set; }

        public int ApprovedBy { get; set; }
        public User User { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.Now;
    }
}
