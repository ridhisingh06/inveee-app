namespace invmgmt.web.Models.Enums
{
    public enum RequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Issued = 3,
        Received = 4
    }
}

//ITems ke liye request status define kiya hai, jisme pending, approved, rejected, issued aur received status hote hain. Ye status request ke lifecycle ko track karne me madad karta hai.
