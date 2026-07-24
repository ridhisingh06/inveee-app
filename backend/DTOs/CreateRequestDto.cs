public class CreateRequestDto
{
	public int UserId { get; set; }
	public int CategoryId { get; set; }
	public List<RequestItemDto> Items { get; set; }
}

public class RequestItemDto
{
	public string ItemCode { get; set; } = string.Empty;
	public int QuantityRequested { get; set; }
}