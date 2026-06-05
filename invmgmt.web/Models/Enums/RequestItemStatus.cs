namespace invmgmt.web.Models.Enums
{
    public enum RequestItemStatus
    {
        PendingWithIssuer = 0,
        NotIssued = 1,
        PendingAdminApproval = 2,
        Approved = 3,
        Rejected = 4,
        Received = 5
    }
}
