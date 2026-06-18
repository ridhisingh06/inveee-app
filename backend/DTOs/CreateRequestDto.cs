public class CreateRequestDto
{
	public int UserId { get; set; }
	public int CategoryId { get; set; }
	public List<RequestItemDto> Items { get; set; }
}

public class RequestItemDto
{
	public int ItemId { get; set; }
	public int QuantityRequested { get; set; }
}