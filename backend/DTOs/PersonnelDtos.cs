using System.ComponentModel.DataAnnotations;

namespace invmgmt.web.DTOs
{
    // ── Create / Update DTO ───────────────────────────────────────────────────
    public class PersonnelCreateDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? ICNumber { get; set; }

        public DateOnly? BirthDate { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        public string? ResidentialAddress { get; set; }

        [MaxLength(20)]
        public string? ResidentialPhone { get; set; }

        [MaxLength(20)]
        public string? OfficePhone { get; set; }

        [MaxLength(100)]
        public string? Designation { get; set; }

        public string? JobDescription { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        public bool IsStoresIncharge { get; set; } = false;

        [MaxLength(100)]
        public string? Building { get; set; }

        [MaxLength(100)]
        public string? ReportingOfficer { get; set; }

        [MaxLength(30)]
        public string? IdCardNumber { get; set; }

        public DateOnly? IdCardExpiryDate { get; set; }

        // Photo is handled separately as IFormFile in the controller
    }

    // ── Response DTO ──────────────────────────────────────────────────────────
    public class PersonnelResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ICNumber { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string Email { get; set; } = string.Empty;
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
        public string? PhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ── Paginated list wrapper ─────────────────────────────────────────────────
    public class PersonnelPagedResultDto
    {
        public IEnumerable<PersonnelResponseDto> Data { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
