using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace invmgmt.web.Models
{
    [Table("Items", Schema = "public")]
    [Index(nameof(Name), IsUnique = true)] // ✅ Unique constraint on Name column (case-insensitive)
    public class Item
    {
        public int Id { get; set; }

        /// <summary>
        /// Item ID - manually entered, must be unique
        /// </summary>
        [Required(ErrorMessage = "Item ID is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Item ID must be between 1 and 50 characters")]
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        /// Item name - must be unique (case-insensitive)
        /// </summary>
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Item name must be between 1 and 255 characters")]
        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public InventoryStock? InventoryStock { get; set; }

        // Navigation collections for related entities using ItemId as principal key
        public ICollection<BillItem>? BillItems { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RequestItem>? RequestItems { get; set; }

        
    }
}