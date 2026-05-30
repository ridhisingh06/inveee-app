using System.ComponentModel.DataAnnotations.Schema;

namespace invmgmt.web.Models
{
    public class Item
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public InventoryStock? InventoryStock { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RequestItem>? RequestItems { get; set; }
        public ICollection<RoleItemLimit>? RoleItemLimits { get; set; }
    }
}