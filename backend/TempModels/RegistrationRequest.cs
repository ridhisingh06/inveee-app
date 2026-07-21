using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class RegistrationRequest
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int DepartmentId { get; set; }

    public int RoleId { get; set; }

    public string Designation { get; set; } = null!;

    public bool IsActive { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ApprovedBy { get; set; }

    public int? ApprovedUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public virtual User? ApprovedUser { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
