using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.DTOs;
using invmgmt.web.Models;

namespace invmgmt.web.Services
{
    public interface IBillService
    {
        Task<BillDetailDto> CreateBillAsync(CreateBillDto dto, int userId);
        Task<BillDetailDto?> GetBillByIdAsync(int billId);
        Task<List<BillSummaryDto>> GetBillsAsync(int pageNumber = 1, int pageSize = 20);
        Task<List<ItemSearchDto>> SearchItemsAsync(string query);
        Task<List<string>> GetVendorsAsync();
        Task<ChallanInitDto> GetInitDataAsync();
    }

    public class BillService : IBillService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BillService> _logger;

        public BillService(AppDbContext context, ILogger<BillService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BillDetailDto> CreateBillAsync(CreateBillDto dto, int userId)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Validation
                    if (string.IsNullOrWhiteSpace(dto.BillNo))
                        throw new ArgumentException("Bill number is required");
                    
                    if (string.IsNullOrWhiteSpace(dto.VendorName))
                        throw new ArgumentException("Vendor name is required");

                    if (dto.Items == null || dto.Items.Count == 0)
                        throw new ArgumentException("At least one item must be added to the bill");

                    // Verify bill number is unique
                    var existingBill = await _context.Bills
                        .FirstOrDefaultAsync(b => b.BillNo == dto.BillNo);
                    
                    if (existingBill != null)
                        throw new InvalidOperationException($"Bill number '{dto.BillNo}' already exists");

                    // Validate all items exist
                    var itemIds = dto.Items.Select(i => i.ItemId).Distinct().ToList();
                    var items = await _context.Items
                        .Where(i => itemIds.Contains(i.ItemId))
                        .ToListAsync();

                    if (items.Count != itemIds.Count)
                        throw new ArgumentException("One or more items not found");

                    // Calculate grand total and validate quantities/prices
                    decimal grandTotal = 0;
                    var billItems = new List<BillItem>();

                    foreach (var itemDto in dto.Items)
                    {
                        if (itemDto.Quantity <= 0)
                            throw new ArgumentException($"Quantity must be greater than 0 for item {itemDto.ItemId}");

                        if (itemDto.UnitPrice < 0)
                            throw new ArgumentException($"Unit price cannot be negative for item {itemDto.ItemId}");

                        var amount = itemDto.Quantity * itemDto.UnitPrice;
                        grandTotal += amount;

                        billItems.Add(new BillItem
                        {
                            ItemId = itemDto.ItemId,
                            Quantity = itemDto.Quantity,
                            UnitPrice = itemDto.UnitPrice,
                            Amount = amount
                        });
                    }

                    // Create bill
                    var bill = new Bill
                    {
                        BillNo = dto.BillNo,
                        BillDate = dto.BillDate == default ? DateTime.UtcNow : dto.BillDate,
                        VendorName = dto.VendorName.Trim(),
                        GrandTotal = grandTotal,
                        CreatedByUserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Bills.Add(bill);
                    await _context.SaveChangesAsync();

                    // Add bill items
                    foreach (var billItem in billItems)
                    {
                        billItem.BillId = bill.Id;
                    }

                    _context.BillItems.AddRange(billItems);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Bill created successfully: BillId={BillId}, BillNo={BillNo}, GrandTotal={GrandTotal}", 
                        bill.Id, bill.BillNo, bill.GrandTotal);

                    return await GetBillByIdAsync(bill.Id) ?? new BillDetailDto();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating bill");
                    throw;
                }
            }
        }

        public async Task<BillDetailDto?> GetBillByIdAsync(int billId)
        {
            var bill = await _context.Bills
                .Include(b => b.CreatedByUser)
                .Include(b => b.Items)
                    .ThenInclude(bi => bi.Item)
                .FirstOrDefaultAsync(b => b.Id == billId);

            if (bill == null)
                return null;

            return new BillDetailDto
            {
                Id = bill.Id,
                BillNo = bill.BillNo,
                BillDate = bill.BillDate,
                VendorName = bill.VendorName,
                GrandTotal = bill.GrandTotal,
                CreatedByUserId = bill.CreatedByUserId,
                CreatedByUserName = bill.CreatedByUser?.Username ?? "Unknown",
                CreatedAt = bill.CreatedAt,
                UpdatedAt = bill.UpdatedAt,
                Items = bill.Items?.Select(bi => new BillItemDto
                {
                    Id = bi.Id,
                    BillId = bi.BillId,
                    ItemId = bi.ItemId,
                    ItemName = bi.Item?.Name ?? "Unknown",
                    Quantity = bi.Quantity,
                    UnitPrice = bi.UnitPrice,
                    Amount = bi.Amount,
                    CreatedAt = bi.CreatedAt
                }).ToList() ?? new List<BillItemDto>()
            };
        }

        public async Task<List<BillSummaryDto>> GetBillsAsync(int pageNumber = 1, int pageSize = 20)
        {
            var bills = await _context.Bills
                .Include(b => b.CreatedByUser)
                .Include(b => b.Items)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BillSummaryDto
                {
                    Id = b.Id,
                    BillNo = b.BillNo,
                    BillDate = b.BillDate,
                    VendorName = b.VendorName,
                    GrandTotal = b.GrandTotal,
                    ItemCount = b.Items != null ? b.Items.Count : 0,
                    CreatedByUserName = b.CreatedByUser != null ? b.CreatedByUser.Username : "Unknown",
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return bills;
        }

        public async Task<List<ItemSearchDto>> SearchItemsAsync(string query)
        {
            var q = query?.Trim().ToLower() ?? string.Empty;

            var items = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.InventoryStock)
                .Where(i => i.IsActive && 
                    (q == string.Empty || 
                     (i.Name != null && i.Name.ToLower().Contains(q)) || 
                     (i.Description != null && i.Description.ToLower().Contains(q))))
                .OrderBy(i => i.Name)
                .Take(50)
                .Select(i => new ItemSearchDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Category = i.Category != null ? i.Category.Name : "Uncategorized",
                    AvailableQuantity = i.InventoryStock != null ? i.InventoryStock.AvailableQuantity : 0,
                    LastPrice = null // Can be updated if you store price history
                })
                .ToListAsync();

            return items;
        }

        public async Task<List<string>> GetVendorsAsync()
        {
            var vendors = await _context.Bills
                .Select(b => b.VendorName)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            // Add some sample vendors if table is empty
            if (vendors.Count == 0)
            {
                vendors = new List<string>
                {
                    "ABC Stationery Supplies",
                    "XYZ Office Materials",
                    "Global Suppliers Ltd",
                    "Local Vendor Inc",
                    "Premium Brands Co."
                };
            }

            return vendors;
        }

        public async Task<ChallanInitDto> GetInitDataAsync()
        {
            var items = await SearchItemsAsync(string.Empty);
            var vendors = await GetVendorsAsync();

            return new ChallanInitDto
            {
                Items = items,
                Vendors = vendors
            };
        }
    }
}
