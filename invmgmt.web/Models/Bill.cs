using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace invmgmt.web.Models
{
    public class Bill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string BillNo { get; set; } = string.Empty;

        [Required]
        public DateTime BillDate { get; set; }

        [Required]
        [StringLength(200)]
        public string VendorName { get; set; } = string.Empty;

        [Required]
        public decimal GrandTotal { get; set; } = 0;

        [Required]
        public int CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public User CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<BillItem>? Items { get; set; }
    }
}
