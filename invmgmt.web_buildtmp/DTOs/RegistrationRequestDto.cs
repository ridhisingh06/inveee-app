namespace invmgmt.web.DTOs
{
	public class RegistrationRequestDto
	{
		public string Username { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }

		public int DepartmentId { get; set; }
		public int RoleId { get; set; }
		public string Designation { get; set; }
	}
}