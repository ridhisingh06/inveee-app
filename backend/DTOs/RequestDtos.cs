using invmgmt.web.Models.Enums;

namespace invmgmt.web.DTOs;

public sealed class CreateRequestFromCartDto
{
    public int? CategoryId { get; set; }
    public List<CreateRequestLineDto> Items { get; set; } = new();
}

public sealed class CreateRequestLineDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public sealed class RequestSummaryDto
{
    public int Id { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class RequestItemDetailDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int QuantityRequested { get; set; }
    public int QuantityApproved { get; set; }
    public int QuantityIssued { get; set; }
    public RequestItemStatus Status { get; set; }
}

public sealed class RequestDetailDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<RequestItemDetailDto> Items { get; set; } = new();
}
public sealed class UpdateRequestDto
{
    public List<UpdateRequestLineDto> Items { get; set; } = new();
}

public sealed class UpdateRequestLineDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}
