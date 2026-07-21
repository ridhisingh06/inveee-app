using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<RegistrationRequest> RegistrationRequests { get; set; } = new List<RegistrationRequest>();

    public virtual ICollection<RoleItemLimit> RoleItemLimits { get; set; } = new List<RoleItemLimit>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
