using System.ComponentModel.DataAnnotations;
using invmgmt.web.Models.Enums;

namespace invmgmt.web.DTOs;

// ============================================================================
// ISSUER PARTIAL ISSUING DTOs
// ============================================================================

/// <summary>
/// Request DTO for issuing items partially by the issuer.
/// Issuer specifies how many items to issue and how many to reject.
/// </summary>
public sealed class IssuePartiallyDto
{
    /// <summary>Unique request ID</summary>
    [Required(ErrorMessage = "RequestId is required")]
    public int RequestId { get; set; }

    /// <summary>List of items with partial quantities to issue</summary>
    [Required(ErrorMessage = "Items list cannot be empty")]
    [MinLength(1, ErrorMessage = "At least one item must be issued")]
    public List<IssueItemPartiallyDto> Items { get; set; } = new();
}

/// <summary>
/// Individual item line for partial issuing by issuer
/// </summary>
public sealed class IssueItemPartiallyDto
{
    /// <summary>RequestItem ID being issued</summary>
    [Required(ErrorMessage = "RequestItemId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "RequestItemId must be greater than 0")]
    public int RequestItemId { get; set; }

    /// <summary>Quantity the issuer is issuing (cannot exceed available inventory)</summary>
    [Required(ErrorMessage = "IssueQuantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "IssueQuantity cannot be negative")]
    public int IssueQuantity { get; set; }

    /// <summary>Quantity the issuer is rejecting</summary>
    [Required(ErrorMessage = "RejectQuantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "RejectQuantity cannot be negative")]
    public int RejectQuantity { get; set; }
}

/// <summary>
/// Response after issuer submits partial quantities
/// </summary>
public sealed class IssuePartiallyResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RequestId { get; set; }
    public DateTime IssuedDate { get; set; }
    public List<IssuedItemDetailDto> IssuedItems { get; set; } = new();
}

/// <summary>
/// Details of an issued item
/// </summary>
public sealed class IssuedItemDetailDto
{
    public int RequestItemId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int IssuedQuantity { get; set; }
    public int RejectedQuantity { get; set; }
}

// ============================================================================
// ISSUER PAGE DTO - FOR DISPLAYING AVAILABLE INVENTORY
// ============================================================================

/// <summary>
/// Response DTO showing items pending with issuer, along with available inventory
/// </summary>
public sealed class IssuerPendingItemDto
{
    /// <summary>Request Item ID</summary>
    public int RequestItemId { get; set; }

    /// <summary>Item ID</summary>
    public int ItemId { get; set; }

    /// <summary>Item name</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Quantity requested by the user</summary>
    public int RequestedQuantity { get; set; }

    /// <summary>Available quantity in inventory (real-time)</summary>
    public int AvailableQuantity { get; set; }

    /// <summary>Category of item</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Current status</summary>
    public RequestItemStatus Status { get; set; }

    /// <summary>Request ID</summary>
    public int RequestId { get; set; }

    /// <summary>User who requested the item</summary>
    public string RequestedByUserName { get; set; } = string.Empty;

    /// <summary>Date when request was created</summary>
    public DateTime RequestedDate { get; set; }
}

/// <summary>
/// List of all pending items for the issuer page
/// </summary>
public sealed class IssuerPendingListDto
{
    public List<IssuerPendingItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

// ============================================================================
// ADMIN PARTIAL APPROVAL DTOs
// ============================================================================

/// <summary>
/// Request DTO for admin to approve items partially
/// </summary>
public sealed class ApprovePartiallyDto
{
    /// <summary>Request ID being approved</summary>
    [Required(ErrorMessage = "RequestId is required")]
    public int RequestId { get; set; }

    /// <summary>Items to approve/reject</summary>
    [Required(ErrorMessage = "Items list cannot be empty")]
    [MinLength(1, ErrorMessage = "At least one item must be approved")]
    public List<ApproveItemPartiallyDto> Items { get; set; } = new();
}

/// <summary>
/// Individual item line for partial approval by admin
/// </summary>
public sealed class ApproveItemPartiallyDto
{
    /// <summary>RequestItem ID being approved</summary>
    [Required(ErrorMessage = "RequestItemId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "RequestItemId must be greater than 0")]
    public int RequestItemId { get; set; }

    /// <summary>Quantity admin approves (cannot exceed issuer issued quantity)</summary>
    [Required(ErrorMessage = "ApproveQuantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "ApproveQuantity cannot be negative")]
    public int ApproveQuantity { get; set; }

    /// <summary>Quantity admin rejects (will be restored to inventory)</summary>
    [Required(ErrorMessage = "RejectQuantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "RejectQuantity cannot be negative")]
    public int RejectQuantity { get; set; }
}

/// <summary>
/// Response after admin submits partial approval
/// </summary>
public sealed class ApprovePartiallyResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RequestId { get; set; }
    public DateTime ApprovedDate { get; set; }
    public List<ApprovedItemDetailDto> ApprovedItems { get; set; } = new();
}

/// <summary>
/// Details of an approved item
/// </summary>
public sealed class ApprovedItemDetailDto
{
    public int RequestItemId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int IssuerIssuedQuantity { get; set; }
    public int ApprovedQuantity { get; set; }
    public int RejectedQuantity { get; set; }
}

// ============================================================================
// ADMIN PAGE DTO - FOR DISPLAYING ISSUED ITEMS
// ============================================================================

/// <summary>
/// Response DTO showing items pending with admin (issued by issuer)
/// </summary>
public sealed class AdminPendingItemDto
{
    /// <summary>Request Item ID</summary>
    public int RequestItemId { get; set; }

    /// <summary>Item ID</summary>
    public int ItemId { get; set; }

    /// <summary>Item name</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Quantity requested by user</summary>
    public int RequestedQuantity { get; set; }

    /// <summary>Quantity issued by the issuer</summary>
    public int IssuerIssuedQuantity { get; set; }

    /// <summary>Quantity rejected by the issuer</summary>
    public int IssuerRejectedQuantity { get; set; }

    /// <summary>Current status</summary>
    public RequestItemStatus Status { get; set; }

    /// <summary>Request ID</summary>
    public int RequestId { get; set; }

    /// <summary>User who requested the item</summary>
    public string RequestedByUserName { get; set; } = string.Empty;

    /// <summary>Issuer name who issued the items</summary>
    public string IssuedByUserName { get; set; } = string.Empty;

    /// <summary>Date when issued</summary>
    public DateTime IssuedDate { get; set; }
}

/// <summary>
/// List of all pending items for the admin approval page
/// </summary>
public sealed class AdminPendingListDto
{
    public List<AdminPendingItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

// ============================================================================
// USER RECEIVE DTOs
// ============================================================================

/// <summary>
/// Request DTO for user to receive approved items
/// </summary>
public sealed class ReceiveItemsDto
{
    /// <summary>Request ID to receive</summary>
    [Required(ErrorMessage = "RequestId is required")]
    public int RequestId { get; set; }

    /// <summary>Optional notes when receiving</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Response after user receives items and order summary is generated
/// </summary>
public sealed class ReceiveItemsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RequestId { get; set; }
    public int OrderSummaryId { get; set; }
    public DateTime ReceivedDate { get; set; }
}

// ============================================================================
// ORDER SUMMARY DTOs
// ============================================================================

/// <summary>
/// Complete order summary with all transaction details (receipt-style)
/// </summary>
public sealed class OrderSummaryDto
{
    /// <summary>Unique order summary ID</summary>
    public int Id { get; set; }

    /// <summary>Original request ID</summary>
    public int RequestId { get; set; }

    /// <summary>User who requested</summary>
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>Issuer who issued the items</summary>
    public int? IssuedByUserId { get; set; }
    public string? IssuedByUserName { get; set; }

    /// <summary>Admin who approved the items</summary>
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }

    /// <summary>Key dates in the workflow</summary>
    public DateTime RequestedDate { get; set; }
    public DateTime IssuedDate { get; set; }
    public DateTime ApprovedDate { get; set; }
    public DateTime ReceivedDate { get; set; }

    /// <summary>Summary quantities</summary>
    public int TotalRequestedQuantity { get; set; }
    public int TotalIssuedQuantity { get; set; }
    public int TotalApprovedQuantity { get; set; }
    public int TotalRejectedQuantity { get; set; }
    public int TotalReceivedQuantity { get; set; }

    /// <summary>Final status</summary>
    public RequestStatus Status { get; set; }

    /// <summary>Line items in this order</summary>
    public List<OrderSummaryItemDto> Items { get; set; } = new();

    /// <summary>Optional notes</summary>
    public string? Notes { get; set; }

    /// <summary>When this order record was created</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Line item in an order summary
/// </summary>
public sealed class OrderSummaryItemDto
{
    /// <summary>Item ID</summary>
    public int ItemId { get; set; }

    /// <summary>Item name</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Category name</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Quantities at each stage</summary>
    public int RequestedQuantity { get; set; }
    public int IssuedQuantity { get; set; }
    public int IssuerRejectedQuantity { get; set; }
    public int ApprovedQuantity { get; set; }
    public int AdminRejectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
}

// ============================================================================
// ORDER HISTORY DTOs
// ============================================================================

/// <summary>
/// Compact order summary for order history listing
/// </summary>
public sealed class OrderHistoryItemDto
{
    /// <summary>Order summary ID</summary>
    public int Id { get; set; }

    /// <summary>Original request ID</summary>
    public int RequestId { get; set; }

    /// <summary>Order received date</summary>
    public DateTime ReceivedDate { get; set; }

    /// <summary>Status of the order</summary>
    public RequestStatus Status { get; set; }

    /// <summary>Total quantities</summary>
    public int TotalRequestedQuantity { get; set; }
    public int TotalApprovedQuantity { get; set; }
    public int TotalRejectedQuantity { get; set; }
    public int TotalReceivedQuantity { get; set; }

    /// <summary>Number of items in order</summary>
    public int ItemCount { get; set; }
}

/// <summary>
/// Paginated list of order history
/// </summary>
public sealed class OrderHistoryListDto
{
    public List<OrderHistoryItemDto> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

// ============================================================================
// REQUEST DETAIL WITH PARTIAL QUANTITIES (Enhanced)
// ============================================================================

/// <summary>
/// Enhanced request detail showing partial issuing/approval workflow
/// </summary>
public sealed class RequestDetailEnhancedDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Audit trail</summary>
    public DateTime? IssuedDate { get; set; }
    public int? IssuedByUserId { get; set; }
    public string? IssuedByUserName { get; set; }

    public DateTime? ApprovedDate { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }

    public DateTime? ReceivedDate { get; set; }

    /// <summary>Enhanced items with partial quantities</summary>
    public List<RequestItemDetailEnhancedDto> Items { get; set; } = new();
}

/// <summary>
/// Enhanced request item detail showing all quantities at each stage
/// </summary>
public sealed class RequestItemDetailEnhancedDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Original request</summary>
    public int QuantityRequested { get; set; }

    /// <summary>Issuer level</summary>
    public int IssuerIssuedQuantity { get; set; }
    public int IssuerRejectedQuantity { get; set; }
    public DateTime? IssuedDate { get; set; }
    public string? IssuedByUserName { get; set; }

    /// <summary>Admin level</summary>
    public int AdminApprovedQuantity { get; set; }
    public int AdminRejectedQuantity { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedByUserName { get; set; }

    /// <summary>User receives</summary>
    public int ReceivedQuantity { get; set; }
    public DateTime? ReceivedDate { get; set; }

    /// <summary>Status</summary>
    public RequestItemStatus Status { get; set; }
}

// ============================================================================
// VALIDATION AND UTILITY DTOs
// ============================================================================

/// <summary>
/// Response when validation fails for partial issuing/approval
/// </summary>
public sealed class ValidationErrorDto
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public List<FieldErrorDto> Errors { get; set; } = new();
}

/// <summary>
/// Individual field validation error
/// </summary>
public sealed class FieldErrorDto
{
    public string Field { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
