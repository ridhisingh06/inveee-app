using System.ComponentModel.DataAnnotations;

namespace invmgmt.web.DTOs
{
    public class AddItemDto
    {
        [Required(ErrorMessage = "Item ID is required.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Item ID must be 1‑50 characters.")]
        public string ItemId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Item name is required.")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Item name must be 1‑255 characters.")]
        public string Name { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "CategoryId must be non‑negative.")]
        public int CategoryId { get; set; }

        public string Description { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "TotalQuantity must be non‑negative.")]
        public int TotalQuantity { get; set; }
    }
}
