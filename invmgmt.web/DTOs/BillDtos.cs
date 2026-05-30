namespace invmgmt.web.DTOs;

/// <summary>
/// DTO for creating/updating a bill with items
/// </summary>
public sealed class CreateBillDto
{
    public DateTime BillDate { get; set; }

    public string BillNo { get; set; } = string.Empty;

    public string VendorName { get; set; } = string.Empty;

    public List<CreateBillItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for individual bill items
/// </summary>
public sealed class CreateBillItemDto
{
    public int ItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}

/// <summary>
/// DTO for bill item details in response
/// </summary>
public sealed class BillItemDto
{
    public int Id { get; set; }

    public int BillId { get; set; }

    public int ItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for bill response/details
/// </summary>
public sealed class BillDetailDto
{
    public int Id { get; set; }

    public string BillNo { get; set; } = string.Empty;

    public DateTime BillDate { get; set; }

    public string VendorName { get; set; } = string.Empty;

    public decimal GrandTotal { get; set; }

    public int CreatedByUserId { get; set; }

    public string CreatedByUserName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<BillItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for bill summary (list view)
/// </summary>
public sealed class BillSummaryDto
{
    public int Id { get; set; }

    public string BillNo { get; set; } = string.Empty;

    public DateTime BillDate { get; set; }

    public string VendorName { get; set; } = string.Empty;

    public decimal GrandTotal { get; set; }

    public int ItemCount { get; set; }

    public string CreatedByUserName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for item search response
/// </summary>
public sealed class ItemSearchDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal? LastPrice { get; set; }

    public int AvailableQuantity { get; set; }
}

/// <summary>
/// DTO for vendor/supplier response
/// </summary>
public sealed class VendorDto
{
    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// DTO for API initialization (get items and vendors)
/// </summary>
public sealed class ChallanInitDto
{
    public List<ItemSearchDto> Items { get; set; } = new();

    public List<string> Vendors { get; set; } = new();
}
