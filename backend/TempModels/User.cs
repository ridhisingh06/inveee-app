using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int? DepartmentId { get; set; }

    public string Designation { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsApproved { get; set; }

    public string Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ApprovalLog> ApprovalLogs { get; set; } = new List<ApprovalLog>();

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<IssueLog> IssueLogs { get; set; } = new List<IssueLog>();

    public virtual ICollection<OrderSummary> OrderSummaryApprovedByUsers { get; set; } = new List<OrderSummary>();

    public virtual ICollection<OrderSummary> OrderSummaryIssuedByUsers { get; set; } = new List<OrderSummary>();

    public virtual ICollection<OrderSummary> OrderSummaryUsers { get; set; } = new List<OrderSummary>();

    public virtual ICollection<ReceivedLog> ReceivedLogs { get; set; } = new List<ReceivedLog>();

    public virtual ICollection<RegistrationRequest> RegistrationRequests { get; set; } = new List<RegistrationRequest>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
