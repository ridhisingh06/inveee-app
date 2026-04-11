using invmgmt.web.Models;
namespace invmgmt.web.Models
{
    public class Item
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? CategoryId { get; set; }
        public Category Category { get; set; }

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;


        public Inventorystock InventoryStock { get; set; }
        public ICollection<RequestItem> RequestItems { get; set; }
        public ICollection<RoleItemLimit> RoleItemLimits { get; set; }
    }
}
