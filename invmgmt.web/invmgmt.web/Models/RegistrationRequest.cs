using System;
using invmgmt.web.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace invmgmt.web.Models.Enums;

public class RegistrationRequest
{
    public int Id { get; set; }
    public string Username { get; set; }= string.Empty;
    public string Email { get; set; }=string.Empty;
    public string PasswordHash { get; set; } = string.Empty;


    public int DepartmentId { get; set; } = string.Empty;
    public Department Department { get; set; }
    public int RoleId { get; set; }
    public Role Role{ get; set; }

    public string Designation { get; set; }

    
    public bool IsActive { get; set; } = false;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

    
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    
    public int? ApprovedBy { get; set; }
    public User? ApprovedUser { get; set; }

    public DateTime? ApprovedAt { get; set; }

}
