using invmgmt.web.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace invmgmt.web.DTOs;

public sealed class CreateRequestFromCartDto
{
    public int? CategoryId { get; set; }
    public List<CreateRequestLineDto> Items { get; set; } = new();
}

public sealed class CreateRequestLineDto
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class RequestSummaryDto
{
    public int id { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class RequestItemDetailDto
{
    public int id { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int QuantityRequested { get; set; }
    public int QuantityApproved { get; set; }
    public int QuantityIssued { get; set; }
    public RequestItemStatus Status { get; set; }
}

public sealed class RequestDetailDto
{
    public int id { get; set; }
    public int UserId { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<RequestItemDetailDto> Items { get; set; } = new();
}

public sealed class UpdateRequestDto
{
    [Required]
    public List<UpdateRequestLineDto> Items { get; set; } = new();
}

public sealed class UpdateRequestLineDto
{
    public string ItemId { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}

public sealed class UpdateRequestResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RequestId { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Internal error classification used by the controller to choose the right
    /// HTTP status code.  NOT_FOUND → 404, FORBIDDEN → 403, BAD_REQUEST → 400,
    /// SERVER_ERROR → 500.  Null means success (200).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ErrorCode { get; set; }
}

public sealed class RequestEditableDto
{
    public bool Editable { get; set; }
    public string Reason { get; set; } = string.Empty;
}
