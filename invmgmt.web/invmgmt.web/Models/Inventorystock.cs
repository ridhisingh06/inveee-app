using invmgmt.web.Models;

using System;

namespace invmgmt.web.Models
{
    public class Inventorystock
    {
        public int Id { get; set; }

        
        public Item ItemName { get; set; }

        public int TotalQuantity { get; set; }
        public int AvailableQuantity { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
