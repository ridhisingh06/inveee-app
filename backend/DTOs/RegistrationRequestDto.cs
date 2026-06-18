using System.ComponentModel.DataAnnotations;

namespace invmgmt.web.DTOs
{
	public class RegistrationRequestDto
	{
		[Required(ErrorMessage = "Username is required.")]
		public string Username { get; set; } = string.Empty;

		[Required(ErrorMessage = "Email is required.")]
		[EmailAddress(ErrorMessage = "Invalid email format.")]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "Password is required.")]
		public string Password { get; set; } = string.Empty;

		[Required(ErrorMessage = "Department is required.")]
		public int? DepartmentId { get; set; }

		[Required(ErrorMessage = "Role is required.")]
		public int? RoleId { get; set; }

		[Required(ErrorMessage = "Designation is required.")]
		public string Designation { get; set; } = string.Empty;
	}
}