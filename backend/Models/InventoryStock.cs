using invmgmt.web.Models;

using System;

namespace invmgmt.web.Models
{
    public class InventoryStock
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; }

       

        public int TotalQuantity { get; set; }
        public int AvailableQuantity { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
