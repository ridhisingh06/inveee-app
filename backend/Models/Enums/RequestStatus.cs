namespace invmgmt.web.Models.Enums
{
    public enum RequestStatus
    {
        PendingWithIssuer = 0,
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        PendingAdminApproval = 4,
        Received = 5,
        NotIssued = 6,

        // Legacy aliases kept so older migrations, records, and routes remain compatible.
        Requested = PendingWithIssuer,
        Issued = PendingAdminApproval
    }
}
