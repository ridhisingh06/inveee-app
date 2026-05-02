using invmgmt.web.Models;
namespace invmgmt.web.Models
{
    public class RoleItemLimit
    {
        public int Id { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; }

        public int MaxLimit { get; set; }
    }
}
