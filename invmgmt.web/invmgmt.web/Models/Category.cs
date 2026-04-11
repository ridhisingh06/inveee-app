using invmgmt.web.Models;
namespace invmgmt.web.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<Item> Items { get; set; }
        public ICollection<Request> Requests { get; set; }
    }
}
