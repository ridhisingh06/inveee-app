using invmgmt.web.Models;
namespace invmgmt.web.Models
{
    public class IssueLog
    {
        public int Id { get; set; }

        public int RequestId { get; set; }
        public Request Request { get; set; }

        public int IssuedBy { get; set; }
        public User User { get; set; }

        public DateTime IssuedDate { get; set; } = DateTime.Now;
    }
}
