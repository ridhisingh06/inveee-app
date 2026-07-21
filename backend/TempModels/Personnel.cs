using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class Personnel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Icnumber { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string Email { get; set; } = null!;

    public string? ResidentialAddress { get; set; }

    public string? ResidentialPhone { get; set; }

    public string? OfficePhone { get; set; }

    public string? Designation { get; set; }

    public string? JobDescription { get; set; }

    public string? Department { get; set; }

    public bool IsStoresIncharge { get; set; }

    public string? Building { get; set; }

    public string? ReportingOfficer { get; set; }

    public string? IdCardNumber { get; set; }

    public DateOnly? IdCardExpiryDate { get; set; }

    public string? PhotoPath { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
