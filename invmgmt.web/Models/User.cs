using System;
using Microsoft.EntityFrameworkCore;

namespace invmgmt.web.Models
{
    [Index(nameof(IsApproved), nameof(CreatedAt))]
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public string Designation { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        
        public string Role { get; set; } = "USER";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}