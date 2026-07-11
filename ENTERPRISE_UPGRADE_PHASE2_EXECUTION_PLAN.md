# PHASE 2: DETAILED STEP-BY-STEP EXECUTION PLAN

## Document Overview

This document provides the exact code changes, file modifications, and implementation sequence for the Enterprise Inventory System upgrade. Each step is explained with **why** it's necessary before implementation.

**Important:** This is Phase 2A (Detailed Planning). Actual code implementation follows in Phase 2B.

---

## STEP 1: DATABASE MODEL MODIFICATIONS

### Why This Step?
The current models don't support partial quantities tracking at different workflow stages. We need to extend the models to track:
- Issuer's partial issue quantities
- Admin's partial approval quantities
- Audit information (who, when)
- Permanent order records

### 1.1 Modify RequestItem.cs

**What Changes:**
- Add fields for partial issuing by issuer
- Add fields for partial approval by admin
- Add audit fields (dates, user IDs)
- Keep existing fields for backward compatibility

**File:** `backend/Models/RequestItem.cs`

**Changes Required:**

```
ADD AFTER existing QuantityIssued field:

    // ============================================================
    // ISSUER STAGE - Partial Issuing (NEW)
    // ============================================================
    /// <summary>
    /// Quantity actually issued by the Issuer (can be partial)
    /// If IssuerIssuedQuantity = 0, item was completely rejected
    /// Validation: IssuerIssuedQuantity + IssuerRejectedQuantity = QuantityRequested
    /// </summary>
    public int IssuerIssuedQuantity { get; set; } = 0;

    /// <summary>
    /// Quantity rejected by the Issuer (can be partial)
    /// Validation: IssuerIssuedQuantity + IssuerRejectedQuantity = QuantityRequested
    /// </summary>
    public int IssuerRejectedQuantity { get; set; } = 0;

    /// <summary>
    /// When the Issuer processed this item
    /// </summary>
    public DateTime? IssuedDate { get; set; }

    /// <summary>
    /// Which Issuer processed this item (Foreign key to User)
    /// </summary>
    public int? IssuedBy { get; set; }

    // ============================================================
    // ADMIN STAGE - Partial Approval (NEW)
    // ============================================================
    /// <summary>
    /// Quantity approved by Admin (can only approve what issuer issued)
    /// Validation: AdminApprovedQuantity + AdminRejectedQuantity = IssuerIssuedQuantity
    /// </summary>
    public int AdminApprovedQuantity { get; set; } = 0;

    /// <summary>
    /// Quantity rejected by Admin (can only reject what issuer issued)
    /// Validation: AdminApprovedQuantity + AdminRejectedQuantity = IssuerIssuedQuantity
    /// </summary>
    public int AdminRejectedQuantity { get; set; } = 0;

    /// <summary>
    /// When the Admin processed this item
    /// </summary>
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// Which Admin processed this item (Foreign key to User)
    /// </summary>
    public int? ApprovedBy { get; set; }

    // ============================================================
    // USER RECEIVES - Final Quantity
    // ============================================================
    /// <summary>
    /// Quantity finally received by the User
    /// This = AdminApprovedQuantity (only approved items)
    /// </summary>
    public int ReceivedQuantity { get; set; } = 0;

    /// <summary>
    /// When the User received this item
    /// </summary>
    public DateTime? ReceivedDate { get; set; }

    // ============================================================
    // BACKWARD COMPATIBILITY - Maps to new fields
    // ============================================================
    // NOTE: Keep QuantityApproved and QuantityIssued for legacy code
    // These now map to AdminApprovedQuantity and IssuerIssuedQuantity
```

**Why Each Field:**
- `IssuerIssuedQuantity` - Track what issuer actually issued (can be less than requested)
- `IssuerRejectedQuantity` - Track what issuer rejected (must sum with issued to equal requested)
- `IssuedDate` - Audit trail
- `IssuedBy` - Know which issuer made decision
- `AdminApprovedQuantity` - Track admin approval (can only approve issued qty)
- `AdminRejectedQuantity` - Track admin rejection (triggers inventory restore)
- `ApprovedDate` - Audit trail
- `ApprovedBy` - Know which admin made decision
- `ReceivedQuantity` - What user actually received (= AdminApprovedQuantity)
- `ReceivedDate` - When transaction completed

### 1.2 Modify Request.cs

**What Changes:**
- Add audit fields at request level
- Track workflow timestamps

**File:** `backend/Models/Request.cs`

**Changes Required:**

```
ADD at end of class before ICollection properties:

    // ============================================================
    // WORKFLOW AUDIT FIELDS (NEW)
    // ============================================================
    /// <summary>
    /// When the request was first issued by the Issuer
    /// </summary>
    public DateTime? IssuedDate { get; set; }

    /// <summary>
    /// Which user ID is the Issuer who processed this request
    /// </summary>
    public int? IssuedBy { get; set; }

    /// <summary>
    /// When the request was approved by Admin
    /// </summary>
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// Which user ID is the Admin who processed this request
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// When the user received the approved items
    /// </summary>
    public DateTime? ReceivedDate { get; set; }
```

**Why Each Field:**
- Track complete workflow timeline
- Generate audit reports
- Display timeline in UI
- Compliance documentation

### 1.3 Create OrderSummary.cs (NEW FILE)

**Why New Table?**
Once user receives items, we need a permanent, immutable record. The OrderSummary is generated once and never modified. This is different from the Request which might have updates during workflow.

**File:** `backend/Models/OrderSummary.cs` (CREATE NEW)

```csharp
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace invmgmt.web.Models
{
    [Index(nameof(UserId), nameof(ReceivedDate))]
    [Index(nameof(RequestId), IsUnique = true)]
    public class OrderSummary
    {
        // ============================================================
        // IDENTIFICATION
        // ============================================================
        public int Id { get; set; }

        /// <summary>
        /// Link to original Request
        /// </summary>
        public int RequestId { get; set; }
        public Request? Request { get; set; }

        // ============================================================
        // WORKFLOW PARTICIPANTS
        // ============================================================
        /// <summary>
        /// User who requested the items
        /// </summary>
        public int UserId { get; set; }
        public User? User { get; set; }

        /// <summary>
        /// User ID of the Issuer
        /// </summary>
        public int? IssuedBy { get; set; }

        /// <summary>
        /// User ID of the Admin
        /// </summary>
        public int? ApprovedBy { get; set; }

        // ============================================================
        // TIMESTAMPS - Immutable once created
        // ============================================================
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime IssuedDate { get; set; }
        public DateTime ApprovedDate { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

        // ============================================================
        // SUMMARY TOTALS (Denormalized for performance)
        // ============================================================
        public int TotalRequested { get; set; }
        public int TotalIssued { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }

        public string Status { get; set; } = "Received"; // Always "Received"

        // ============================================================
        // RELATIONSHIPS
        // ============================================================
        public ICollection<OrderSummaryItem> Items { get; set; } = 
            new List<OrderSummaryItem>();
    }
}
```

**Why Denormalized?**
- Never changes after creation
- Fast queries for UI
- Reduces joins
- Archive-friendly

### 1.4 Create OrderSummaryItem.cs (NEW FILE)

**Why New Table?**
Permanent line-item records matching items in order. Once created, these never change.

**File:** `backend/Models/OrderSummaryItem.cs` (CREATE NEW)

```csharp
namespace invmgmt.web.Models
{
    public class OrderSummaryItem
    {
        public int Id { get; set; }

        // ============================================================
        // REFERENCE TO PARENT
        // ============================================================
        public int OrderSummaryId { get; set; }
        public OrderSummary? OrderSummary { get; set; }

        // ============================================================
        // ITEM INFORMATION
        // ============================================================
        public int ItemId { get; set; }
        public Item? Item { get; set; }

        /// <summary>
        /// Item name (denormalized for report)
        /// </summary>
        public string ItemName { get; set; } = string.Empty;

        // ============================================================
        // QUANTITIES AT EACH STAGE
        // ============================================================
        /// <summary>
        /// What the user requested
        /// </summary>
        public int QuantityRequested { get; set; }

        /// <summary>
        /// What the Issuer actually issued
        /// </summary>
        public int IssuerIssuedQuantity { get; set; }

        /// <summary>
        /// What the Issuer rejected
        /// </summary>
        public int IssuerRejectedQuantity { get; set; }

        /// <summary>
        /// What the Admin approved (of what issuer issued)
        /// </summary>
        public int AdminApprovedQuantity { get; set; }

        /// <summary>
        /// What the Admin rejected (of what issuer issued)
        /// </summary>
        public int AdminRejectedQuantity { get; set; }

        /// <summary>
        /// What the user finally received
        /// (Same as AdminApprovedQuantity)
        /// </summary>
        public int ReceivedQuantity { get; set; }
    }
}
```

**Why Each Field:**
- Permanent record of each decision
- Complete audit trail
- Generate reports
- Print receipt

---

## STEP 2: DATABASE MIGRATION

### Why This Step?
Entity Framework Core needs to migrate database schema to add new columns and tables.

### 2.1 Generate Migration

**Command:**
```bash
cd backend
dotnet ef migrations add AddPartialIssueApprovalAndOrderSummary -v
```

**What It Does:**
- EF analyzes RequestItem, Request, OrderSummary models
- Generates SQL for PostgreSQL
- Creates migration file with timestamp
- Generates Up() and Down() methods

**Generated Migration Will Include:**

```sql
-- RequestItem table changes
ALTER TABLE "RequestItem" ADD "IssuerIssuedQuantity" INT NOT NULL DEFAULT 0;
ALTER TABLE "RequestItem" ADD "IssuerRejectedQuantity" INT NOT NULL DEFAULT 0;
ALTER TABLE "RequestItem" ADD "IssuedDate" TIMESTAMP NULL;
ALTER TABLE "RequestItem" ADD "IssuedBy" INT NULL;
ALTER TABLE "RequestItem" ADD "AdminApprovedQuantity" INT NOT NULL DEFAULT 0;
ALTER TABLE "RequestItem" ADD "AdminRejectedQuantity" INT NOT NULL DEFAULT 0;
ALTER TABLE "RequestItem" ADD "ApprovedDate" TIMESTAMP NULL;
ALTER TABLE "RequestItem" ADD "ApprovedBy" INT NULL;
ALTER TABLE "RequestItem" ADD "ReceivedQuantity" INT NOT NULL DEFAULT 0;
ALTER TABLE "RequestItem" ADD "ReceivedDate" TIMESTAMP NULL;

-- Request table changes
ALTER TABLE "Request" ADD "IssuedDate" TIMESTAMP NULL;
ALTER TABLE "Request" ADD "IssuedBy" INT NULL;
ALTER TABLE "Request" ADD "ApprovedDate" TIMESTAMP NULL;
ALTER TABLE "Request" ADD "ApprovedBy" INT NULL;
ALTER TABLE "Request" ADD "ReceivedDate" TIMESTAMP NULL;

-- New OrderSummary table
CREATE TABLE "OrderSummary" (...)
CREATE TABLE "OrderSummaryItem" (...)

-- Indexes
CREATE INDEX ON "OrderSummary" ("UserId", "ReceivedDate");
CREATE INDEX ON "OrderSummary" ("RequestId");
```

### 2.2 Verify Migration

**Command:**
```bash
dotnet ef migrations list -v
```

**Expected Output:**
```
20260618_AddPartialIssueApprovalAndOrderSummary (Pending)
```

### 2.3 Apply Migration

**Command:**
```bash
dotnet ef database update -v
```

**What It Does:**
- Connects to PostgreSQL (from appsettings.json)
- Runs migration SQL
- Updates __EFMigrationsHistory table
- Adds new columns/tables to schema

**Verification:**
```sql
-- In PostgreSQL/pgAdmin, run:
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'RequestItem' 
ORDER BY ordinal_position;

-- Should show new columns:
-- IssuerIssuedQuantity, IssuerRejectedQuantity, AdminApprovedQuantity, etc.
```

---

## STEP 3: CREATE DTOs

### Why This Step?
DTOs provide:
- API contract between frontend and backend
- Input validation
- Hide internal model structure
- Version compatibility

### 3.1 Create IssuePartiallyDto

**File:** `backend/DTOs/IssuePartiallyDto.cs` (CREATE NEW)

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace invmgmt.web.DTOs
{
    /// <summary>
    /// DTO for Issuer to partially issue items
    /// POST /api/issuer/issue
    /// </summary>
    public class IssuePartiallyDto
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        [MinLength(1)]
        public List<IssueItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Individual item being issued
    /// </summary>
    public class IssueItemDto
    {
        [Required]
        public int RequestItemId { get; set; }

        [Range(0, int.MaxValue)]
        public int IssueQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int RejectQuantity { get; set; }

        // VALIDATION NOTE:
        // IssueQuantity + RejectQuantity must equal QuantityRequested
        // Validated in service layer
    }
}
```

**Why This Structure:**
- RequestId identifies which request
- Items array allows bulk operation
- Each item specifies issue/reject split
- Service validates totals

### 3.2 Create ApprovePartiallyDto

**File:** `backend/DTOs/ApprovePartiallyDto.cs` (CREATE NEW)

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace invmgmt.web.DTOs
{
    /// <summary>
    /// DTO for Admin to partially approve issued items
    /// PUT /api/admin/approve
    /// </summary>
    public class ApprovePartiallyDto
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        [MinLength(1)]
        public List<ApproveItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Individual item being approved/rejected
    /// Admin can only approve what issuer issued
    /// </summary>
    public class ApproveItemDto
    {
        [Required]
        public int RequestItemId { get; set; }

        /// <summary>
        /// Approve quantity (cannot exceed IssuerIssuedQuantity)
        /// </summary>
        [Range(0, int.MaxValue)]
        public int ApproveQuantity { get; set; }

        /// <summary>
        /// Reject quantity (for issued items only)
        /// Triggers inventory restore
        /// </summary>
        [Range(0, int.MaxValue)]
        public int RejectQuantity { get; set; }

        // VALIDATION NOTE:
        // ApproveQuantity + RejectQuantity = IssuerIssuedQuantity
        // Validated in service layer
    }
}
```

**Why This Structure:**
- Similar to IssuePartiallyDto but for admin
- Only works with issued quantities
- Triggers inventory restoration
- Complete audit trail

### 3.3 Create OrderSummaryDto

**File:** `backend/DTOs/OrderSummaryDto.cs` (CREATE NEW)

```csharp
using System;
using System.Collections.Generic;

namespace invmgmt.web.DTOs
{
    /// <summary>
    /// DTO for displaying completed order as a receipt
    /// </summary>
    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }

        // ============================================================
        // HEADER INFORMATION
        // ============================================================
        public DateTime RequestDate { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime ApprovedDate { get; set; }
        public DateTime ReceivedDate { get; set; }

        public string IssuerName { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public string Status { get; set; } = "Received";

        // ============================================================
        // LINE ITEMS
        // ============================================================
        public List<OrderSummaryItemDto> Items { get; set; } = new();

        // ============================================================
        // TOTALS
        // ============================================================
        public OrderSummaryTotalsDto Totals { get; set; } = new();
    }

    public class OrderSummaryItemDto
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;

        public int QuantityRequested { get; set; }
        public int IssuerIssued { get; set; }
        public int IssuerRejected { get; set; }
        public int AdminApproved { get; set; }
        public int AdminRejected { get; set; }
        public int ReceivedQuantity { get; set; }
    }

    public class OrderSummaryTotalsDto
    {
        public int TotalRequested { get; set; }
        public int TotalIssued { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
    }
}
```

**Why This Structure:**
- Mimic receipt layout
- All data in one DTO
- Immutable (read-only)
- Perfect for UI display/print

---

## STEP 4: CREATE REPOSITORIES (METHODS ONLY)

### Why This Step?
Repositories handle database access. New methods handle:
- Partial updates
- Stock locking
- Order summary queries

### 4.1 Extend RequestItemRepository

**File:** `backend/Repositories/RequestItemRepository.cs`

**New Methods to Add:**

```csharp
/// <summary>
/// Get RequestItem with related data, locked for update
/// </summary>
public async Task<RequestItem?> GetByIdForUpdateAsync(int id)
{
    return await _context.RequestItems
        .Include(ri => ri.Item)
        .Include(ri => ri.Request)
        .FromSqlInterpolated($@"
            SELECT * FROM ""RequestItem""
            WHERE ""Id"" = {id}
            FOR UPDATE
        ")
        .FirstOrDefaultAsync();
}

/// <summary>
/// Update partial issue quantities
/// </summary>
public async Task UpdateIssuerQuantitiesAsync(
    int requestItemId,
    int issueQuantity,
    int rejectQuantity,
    int issuerId)
{
    var item = await _context.RequestItems.FindAsync(requestItemId);
    if (item == null) return;

    item.IssuerIssuedQuantity = issueQuantity;
    item.IssuerRejectedQuantity = rejectQuantity;
    item.IssuedDate = DateTime.UtcNow;
    item.IssuedBy = issuerId;
    item.Status = RequestItemStatus.PendingAdminApproval;

    _context.RequestItems.Update(item);
}

/// <summary>
/// Update partial admin approval quantities
/// </summary>
public async Task UpdateAdminQuantitiesAsync(
    int requestItemId,
    int approveQuantity,
    int rejectQuantity,
    int adminId)
{
    var item = await _context.RequestItems.FindAsync(requestItemId);
    if (item == null) return;

    item.AdminApprovedQuantity = approveQuantity;
    item.AdminRejectedQuantity = rejectQuantity;
    item.ApprovedDate = DateTime.UtcNow;
    item.ApprovedBy = adminId;
    item.Status = approveQuantity > 0 
        ? RequestItemStatus.Approved 
        : RequestItemStatus.Rejected;

    _context.RequestItems.Update(item);
}

/// <summary>
/// Mark as received and set received quantity
/// </summary>
public async Task UpdateReceivedAsync(int requestItemId)
{
    var item = await _context.RequestItems.FindAsync(requestItemId);
    if (item == null) return;

    item.ReceivedQuantity = item.AdminApprovedQuantity;
    item.ReceivedDate = DateTime.UtcNow;
    item.Status = RequestItemStatus.Received;

    _context.RequestItems.Update(item);
}
```

### 4.2 Extend InventoryRepository

**File:** `backend/Repositories/InventoryRepository.cs`

**New Methods to Add:**

```csharp
/// <summary>
/// Lock inventory row and get current quantities
/// Must be within transaction with Serializable isolation level
/// </summary>
public async Task<InventoryStock?> LockAndGetAsync(int itemId)
{
    return await _context.InventoryStocks
        .FromSqlInterpolated($@"
            SELECT * FROM ""InventoryStocks""
            WHERE ""ItemId"" = {itemId}
            FOR UPDATE  -- PostgreSQL row lock
        ")
        .FirstOrDefaultAsync();
}

/// <summary>
/// Atomically deduct available quantity
/// Must be called within transaction after locking
/// </summary>
public async Task<bool> TryDeductAsync(int itemId, int quantity)
{
    var stock = await LockAndGetAsync(itemId);
    if (stock == null) return false;

    if (stock.AvailableQuantity < quantity)
        return false; // Insufficient stock

    stock.AvailableQuantity -= quantity;
    stock.UpdatedAt = DateTime.UtcNow;

    _context.InventoryStocks.Update(stock);
    return true;
}

/// <summary>
/// Restore quantity (when admin rejects)
/// </summary>
public async Task RestoreAsync(int itemId, int quantity)
{
    var stock = await _context.InventoryStocks
        .FirstOrDefaultAsync(s => s.ItemId == itemId);

    if (stock != null)
    {
        stock.AvailableQuantity += quantity;
        stock.UpdatedAt = DateTime.UtcNow;
        _context.InventoryStocks.Update(stock);
    }
}

/// <summary>
/// Get available quantity with current lock status
/// </summary>
public async Task<int> GetAvailableQuantityAsync(int itemId)
{
    var stock = await _context.InventoryStocks
        .AsNoTracking()
        .FirstOrDefaultAsync(s => s.ItemId == itemId);

    return stock?.AvailableQuantity ?? 0;
}
```

### 4.3 Create OrderSummaryRepository

**File:** `backend/Repositories/IOrderSummaryRepository.cs` (NEW - Interface)

```csharp
using invmgmt.web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace invmgmt.web.Repositories
{
    public interface IOrderSummaryRepository
    {
        Task<OrderSummary?> GetByIdAsync(int id);
        Task<OrderSummary?> GetByRequestIdAsync(int requestId);
        Task<List<OrderSummary>> GetUserOrdersAsync(int userId, int pageNumber, int pageSize);
        Task<int> GetUserOrderCountAsync(int userId);
        Task AddAsync(OrderSummary orderSummary);
        Task SaveChangesAsync();
    }
}
```

**File:** `backend/Repositories/OrderSummaryRepository.cs` (NEW - Implementation)

```csharp
using invmgmt.web.Data;
using invmgmt.web.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace invmgmt.web.Repositories
{
    public class OrderSummaryRepository : IOrderSummaryRepository
    {
        private readonly AppDbContext _context;

        public OrderSummaryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderSummary?> GetByIdAsync(int id)
        {
            return await _context.OrderSummaries
                .Include(os => os.Items)
                .Include(os => os.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(os => os.Id == id);
        }

        public async Task<OrderSummary?> GetByRequestIdAsync(int requestId)
        {
            return await _context.OrderSummaries
                .Include(os => os.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(os => os.RequestId == requestId);
        }

        public async Task<List<OrderSummary>> GetUserOrdersAsync(int userId, int pageNumber, int pageSize)
        {
            return await _context.OrderSummaries
                .Where(os => os.UserId == userId)
                .OrderByDescending(os => os.ReceivedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetUserOrderCountAsync(int userId)
        {
            return await _context.OrderSummaries
                .CountAsync(os => os.UserId == userId);
        }

        public async Task AddAsync(OrderSummary orderSummary)
        {
            await _context.OrderSummaries.AddAsync(orderSummary);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
```

---

## SUMMARY OF STEP 1-4

These steps establish the foundation:

| Step | What | Why | Files |
|------|------|-----|-------|
| 1 | Extend models | Store partial quantities | RequestItem, Request, OrderSummary (new) |
| 2 | Run migration | Update database schema | Migration file (auto-generated) |
| 3 | Create DTOs | API contracts | IssuePartiallyDto, ApprovePartiallyDto, OrderSummaryDto |
| 4 | Extend repositories | Database access methods | Repository interfaces & implementations |

**Status After Step 4:** Database ready, DTOs ready, repository methods ready

**Next Steps (in Phase 2B):** Create service layer business logic

---

## STEP 5-16 WILL INCLUDE:

5. Create IssuerService (Partial issuing logic)
6. Create ApprovalService (Partial approval logic)
7. Create OrderSummaryService (Order summary logic)
8. Modify IssuerController (New endpoint)
9. Modify AdminController (New endpoint)
10. Modify RequestController (New endpoints)
11. Create Angular models
12. Create/Modify Angular services
13. Create Order History component
14. Create Order Summary component
15. Modify Issuer component (UI for partial issue)
16. Modify Admin component (UI for partial approval)

---

## Files Summary - Step 1-4

**Modified Files (5):**
- backend/Models/RequestItem.cs
- backend/Models/Request.cs
- backend/Repositories/RequestItemRepository.cs (add methods)
- backend/Repositories/InventoryRepository.cs (add methods)

**Created Files (6):**
- backend/Models/OrderSummary.cs
- backend/Models/OrderSummaryItem.cs
- backend/DTOs/IssuePartiallyDto.cs
- backend/DTOs/ApprovePartiallyDto.cs
- backend/DTOs/OrderSummaryDto.cs
- backend/Repositories/OrderSummaryRepository.cs + Interface

**Auto-Generated Files (1):**
- backend/Migrations/[TIMESTAMP]_AddPartialIssueApprovalAndOrderSummary.cs

**Total Changes: 12 files**

---

## Verification Checklist - After Step 4

- [ ] RequestItem model has all new fields
- [ ] Request model has audit fields
- [ ] OrderSummary and OrderSummaryItem models created
- [ ] Migration generated without errors
- [ ] Migration applied to database successfully
- [ ] New columns visible in PostgreSQL
- [ ] DTOs created with proper validation
- [ ] Repository methods added to interfaces
- [ ] Repository methods implemented
- [ ] Compilation successful (no build errors)

---

**Status: Phase 2A - Detailed Planning Complete**

**Next: Phase 2B - Implementation with actual code**
