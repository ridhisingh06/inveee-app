using invmgmt.web.Models;
using System;

namespace invmgmt.web.Models
{
    public class ReceivedLog
    {
        public int Id { get; set; }

        public int RequestId { get; set; }
        public Request Request { get; set; }

        public int ReceivedBy { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime ReceivedDate { get; set; } = DateTime.Now;
    }
}
