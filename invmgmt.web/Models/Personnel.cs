using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace invmgmt.web.Models
{
    [Table("Personnel")]
    public class Personnel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // ── Personal Details ─────────────────────────────
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? ICNumber { get; set; }

        public DateOnly? BirthDate { get; set; }

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? ResidentialAddress { get; set; }

        [MaxLength(20)]
        public string? ResidentialPhone { get; set; }

        [MaxLength(20)]
        public string? OfficePhone { get; set; }

        // ── Employment Details ────────────────────────────
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

        // ── ID Card Details ───────────────────────────────
        [MaxLength(30)]
        public string? IdCardNumber { get; set; }

        public DateOnly? IdCardExpiryDate { get; set; }

        [MaxLength(500)]
        public string? PhotoPath { get; set; }

        // ── Audit ─────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
