using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<RegistrationRequest> RegistrationRequests { get; set; } = new List<RegistrationRequest>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
