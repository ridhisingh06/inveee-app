namespace invmgmt.web.DTOs
{
	public class AddItemDto
	{
		public string Name { get; set; }= string.Empty;
        public int CategoryId { get; set; }
		public string Description { get; set; } = string.Empty;
		public int TotalQuantity { get; set; }
	}

}