namespace invmgmt.web.Models.Enums
{
    public enum RequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Issued,
        Received
    }
}

//ITems ke liye request status define kiya hai, jisme pending, approved, rejected, issued aur received status hote hain. Ye status request ke lifecycle ko track karne me madad karta hai.