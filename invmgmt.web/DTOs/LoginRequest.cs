namespace invmgmt.web.DTOs
{
    public class LoginRequest
    {
        public string Email { get; set; }= string.Empty;
        public string Password { get; set; }=string.Empty;

        public object GetPayload()
        {
            return new
            {
                email = this.Email,
                password = this.Password
            };
        }
    }
}