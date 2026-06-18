using invmgmt.web.Models.Enums;

namespace invmgmt.web.Models
{
    public class RequestItem
    {
        public int Id { get; set; }

        public int RequestId { get; set; }
        public Request Request { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; }

        public int QuantityRequested { get; set; }
        public int QuantityApproved { get; set; }
        public int QuantityIssued { get; set; }
        public RequestItemStatus Status { get; set; } = RequestItemStatus.PendingWithIssuer;
    }
}
