using invmgmt.web.Models;
namespace invmgmt.web.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }= string.Empty;
        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<RoleItemLimit> RoleItemLimits { get; set; }
    }
}
