using invmgmt.web.Models.Enums;

namespace invmgmt.web.Models
{
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




        public ICollection<RequestItem> RequestItems { get; set; }
        public ICollection<ApprovalLog> ApprovalLogs { get; set; }
        public ICollection<IssueLog> IssueLogs { get; set; }
        public ICollection<ReceivedLog> ReceivedLogs { get; set; }
    
}
}
