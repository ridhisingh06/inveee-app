# Personnel Management Module - Architecture & Extension Guide

## 📐 MODULE ARCHITECTURE

### Layered Architecture

```
┌─────────────────────────────────────────────────┐
│         PersonnelController (API)                │
│  - HTTP Request Handling                         │
│  - Authorization ([Authorize])                   │
│  - Request/Response Mapping                      │
└────────────┬────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────┐
│      IPersonnelService / PersonnelService       │
│  - Business Logic                               │
│  - Validation & Duplicate Email Check           │
│  - File Upload Handling                         │
│  - DTOs Mapping                                 │
└────────────┬────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────┐
│   IPersonnelRepository / PersonnelRepository     │
│  - Database Access                              │
│  - Entity Framework Operations                  │
│  - Query Building & Filtering                   │
└────────────┬────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────┐
│      AppDbContext / PostgreSQL Database         │
│  - Personnel Table                              │
│  - Data Persistence                             │
└─────────────────────────────────────────────────┘
```

---

## 📁 PROJECT STRUCTURE

```
invmgmt.web/
│
├── Models/
│   └── Personnel.cs
│       ├── Properties (Name, Email, Designation, etc.)
│       ├── Validations ([Required], [EmailAddress])
│       ├── Table Mapping ([Table("Personnel")])
│       └── Timestamps (CreatedAt, UpdatedAt)
│
├── DTOs/
│   └── PersonnelDtos.cs
│       ├── PersonnelCreateDto
│       │   └── Validation Attributes
│       ├── PersonnelResponseDto
│       │   └── Serialization Mapping
│       └── PersonnelPagedResultDto
│           └── Pagination Support
│
├── Controllers/
│   └── PersonnelController.cs
│       ├── [Authorize(Roles="ADMIN")]
│       ├── POST /api/personnel (Create)
│       ├── GET /api/personnel (List)
│       ├── GET /api/personnel/{id} (Get)
│       ├── PUT /api/personnel/{id} (Update)
│       └── DELETE /api/personnel/{id} (Delete)
│
├── Services/
│   ├── IPersonnelService.cs (Interface)
│   └── PersonnelService.cs
│       ├── CreateAsync()
│       ├── GetAllAsync()
│       ├── GetByIdAsync()
│       ├── UpdateAsync()
│       ├── DeleteAsync()
│       ├── SavePhotoAsync() [Private]
│       ├── DeletePhoto() [Private]
│       ├── MapToEntity() [Private]
│       └── MapToDto() [Private]
│
├── Repositories/
│   ├── IPersonnelRepository.cs (Interface)
│   └── PersonnelRepository.cs
│       ├── GetByIdAsync()
│       ├── GetAllAsync()
│       ├── EmailExistsAsync()
│       ├── AddAsync()
│       ├── UpdateAsync()
│       ├── DeleteAsync()
│       └── SaveChangesAsync()
│
├── Data/
│   └── AppDbContext.cs
│       ├── DbSet<Personnel> Personnel
│       └── OnModelCreating()
│           └── Email Unique Index
│
├── Migrations/
│   └── 20260521000000_AddPersonnelTable.cs
│       └── Creates Personnel Table
│
└── wwwroot/
    └── uploads/
        └── personnel/
            └── [Photo Files]
```

---

## 🔄 DATA FLOW

### Create Operation
```
1. POST Request → PersonnelController.Create()
2. ModelState Validation (DTO Attributes)
3. PersonnelService.CreateAsync()
   ├── Email Duplicate Check
   ├── File Validation (if provided)
   └── File Upload (SavePhotoAsync)
4. PersonnelRepository.AddAsync()
5. PersonnelRepository.SaveChangesAsync()
6. MapToDto() & Response (201 Created)
```

### Update Operation
```
1. PUT Request → PersonnelController.Update()
2. ModelState Validation
3. PersonnelService.UpdateAsync()
   ├── Get Existing Personnel
   ├── Email Duplicate Check (exclude self)
   ├── Update All Fields
   ├── Delete Old Photo (if new one provided)
   └── Upload New Photo (if provided)
4. PersonnelRepository.UpdateAsync()
5. PersonnelRepository.SaveChangesAsync()
6. MapToDto() & Response (200 OK)
```

### Delete Operation
```
1. DELETE Request → PersonnelController.Delete()
2. PersonnelService.DeleteAsync()
   ├── Get Personnel
   ├── Delete Photo File
   └── Mark for Deletion
3. PersonnelRepository.DeleteAsync()
4. PersonnelRepository.SaveChangesAsync()
5. Response (200 OK)
```

### List/Pagination Operation
```
1. GET Request → PersonnelController.GetAll()
2. Query Parameters (page, pageSize)
3. PersonnelService.GetAllAsync()
4. PersonnelRepository.GetAllAsync()
   ├── AsNoTracking() (Read-only)
   ├── OrderByDescending(CreatedAt)
   ├── CountAsync() (Total)
   ├── Skip & Take (Pagination)
5. MapToDto() for each record
6. Return PersonnelPagedResultDto
```

---

## 🧩 EXTENSIBILITY POINTS

### 1. Add New Fields to Personnel

**Step 1: Update Model**
```csharp
// Models/Personnel.cs
public class Personnel
{
    // Existing fields...
    
    [MaxLength(100)]
    public string? MiddleName { get; set; }  // NEW FIELD
    
    public DateTime? DateOfJoining { get; set; }  // NEW FIELD
}
```

**Step 2: Update DTO**
```csharp
// DTOs/PersonnelDtos.cs
public class PersonnelCreateDto
{
    // Existing fields...
    
    [MaxLength(100)]
    public string? MiddleName { get; set; }
    
    public DateOnly? DateOfJoining { get; set; }
}

public class PersonnelResponseDto
{
    // Existing fields...
    
    public string? MiddleName { get; set; }
    public DateOnly? DateOfJoining { get; set; }
}
```

**Step 3: Update Service Mappings**
```csharp
// Services/PersonnelService.cs
private static Personnel MapToEntity(PersonnelCreateDto dto) => new()
{
    // Existing fields...
    MiddleName = dto.MiddleName,
    DateOfJoining = dto.DateOfJoining
};

private static PersonnelResponseDto MapToDto(Personnel p, string baseUrl) => new()
{
    // Existing fields...
    MiddleName = p.MiddleName,
    DateOfJoining = p.DateOfJoining
};
```

**Step 4: Create Migration**
```bash
cd invmgmt.web
dotnet ef migrations add AddMiddleNameAndDateOfJoining
dotnet ef database update
```

---

### 2. Add Advanced Search/Filtering

**Step 1: Add Repository Method**
```csharp
// Repositories/IPersonnelRepository.cs
Task<(IEnumerable<Personnel> Items, int TotalCount)> SearchAsync(
    string? searchTerm, 
    string? department, 
    int page, 
    int pageSize);

// Repositories/PersonnelRepository.cs
public async Task<(IEnumerable<Personnel> Items, int TotalCount)> SearchAsync(
    string? searchTerm, 
    string? department, 
    int page, 
    int pageSize)
{
    var query = _context.Personnel.AsNoTracking();
    
    if (!string.IsNullOrWhiteSpace(searchTerm))
        query = query.Where(p => p.Name.Contains(searchTerm) || p.Email.Contains(searchTerm));
    
    if (!string.IsNullOrWhiteSpace(department))
        query = query.Where(p => p.Department == department);
    
    var total = await query.CountAsync();
    var items = await query
        .OrderByDescending(p => p.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return (items, total);
}
```

**Step 2: Add Service Method**
```csharp
// Services/IPersonnelService.cs
Task<PersonnelPagedResultDto> SearchAsync(
    string? searchTerm, 
    string? department, 
    int page, 
    int pageSize, 
    string baseUrl);

// Services/PersonnelService.cs
public async Task<PersonnelPagedResultDto> SearchAsync(
    string? searchTerm, 
    string? department, 
    int page, 
    int pageSize, 
    string baseUrl)
{
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);
    
    var (items, total) = await _repo.SearchAsync(searchTerm, department, page, pageSize);
    return new PersonnelPagedResultDto
    {
        Data = items.Select(p => MapToDto(p, baseUrl)),
        TotalCount = total,
        Page = page,
        PageSize = pageSize
    };
}
```

**Step 3: Add Controller Endpoint**
```csharp
// Controllers/PersonnelController.cs
[HttpGet("search")]
public async Task<IActionResult> Search(
    [FromQuery] string? searchTerm,
    [FromQuery] string? department,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    try
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _service.SearchAsync(searchTerm, department, page, pageSize, baseUrl);
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error searching personnel");
        return StatusCode(500, new { message = "Failed to search personnel records." });
    }
}
```

---

### 3. Add Export to Excel/CSV

**Step 1: Install NuGet Package**
```bash
dotnet add package ClosedXML
```

**Step 2: Add Service Method**
```csharp
// Services/IPersonnelService.cs
Task<byte[]> ExportToExcelAsync();

// Services/PersonnelService.cs
public async Task<byte[]> ExportToExcelAsync()
{
    var (items, _) = await _repo.GetAllAsync(1, int.MaxValue);
    
    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add("Personnel");
    
    // Add headers
    worksheet.Cell(1, 1).Value = "ID";
    worksheet.Cell(1, 2).Value = "Name";
    worksheet.Cell(1, 3).Value = "Email";
    worksheet.Cell(1, 4).Value = "Designation";
    // ... more columns
    
    // Add data
    int row = 2;
    foreach (var p in items)
    {
        worksheet.Cell(row, 1).Value = p.Id;
        worksheet.Cell(row, 2).Value = p.Name;
        worksheet.Cell(row, 3).Value = p.Email;
        worksheet.Cell(row, 4).Value = p.Designation;
        // ... more columns
        row++;
    }
    
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    return stream.ToArray();
}
```

**Step 3: Add Controller Endpoint**
```csharp
[HttpGet("export/excel")]
public async Task<IActionResult> ExportExcel()
{
    try
    {
        var bytes = await _service.ExportToExcelAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    $"Personnel_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error exporting personnel");
        return StatusCode(500, new { message = "Failed to export personnel records." });
    }
}
```

---

### 4. Add Bulk Operations

**Step 1: Add DTO**
```csharp
// DTOs/PersonnelDtos.cs
public class BulkCreateDto
{
    [Required]
    public IEnumerable<PersonnelCreateDto> Personnel { get; set; } = [];
}

public class BulkResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];
}
```

**Step 2: Add Service Method**
```csharp
// Services/IPersonnelService.cs
Task<BulkResultDto> CreateBulkAsync(BulkCreateDto dto, string baseUrl);

// Services/PersonnelService.cs
public async Task<BulkResultDto> CreateBulkAsync(BulkCreateDto dto, string baseUrl)
{
    var result = new BulkResultDto();
    var errors = new List<string>();
    
    foreach (var item in dto.Personnel)
    {
        try
        {
            await CreateAsync(item, null, baseUrl);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            result.FailureCount++;
            errors.Add($"{item.Email}: {ex.Message}");
        }
    }
    
    result.Errors = errors;
    return result;
}
```

**Step 3: Add Controller Endpoint**
```csharp
[HttpPost("bulk")]
public async Task<IActionResult> CreateBulk([FromBody] BulkCreateDto dto)
{
    try
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _service.CreateBulkAsync(dto, baseUrl);
        return Ok(new { message = "Bulk creation completed.", data = result });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in bulk create");
        return StatusCode(500, new { message = "Failed to create personnel in bulk." });
    }
}
```

---

### 5. Add Audit Trail / History

**Step 1: Create AuditLog Entity**
```csharp
// Models/PersonnelAuditLog.cs
public class PersonnelAuditLog
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public string Action { get; set; }  // Created, Updated, Deleted
    public string? OldValues { get; set; }  // JSON
    public string? NewValues { get; set; }  // JSON
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Step 2: Update DbContext**
```csharp
// Data/AppDbContext.cs
public DbSet<PersonnelAuditLog> PersonnelAuditLogs { get; set; }
```

**Step 3: Log Changes in Service**
```csharp
// Services/PersonnelService.cs
private async Task LogAuditAsync(int personnelId, string action, string? oldValues, string? newValues)
{
    var auditLog = new PersonnelAuditLog
    {
        PersonnelId = personnelId,
        Action = action,
        OldValues = oldValues,
        NewValues = newValues,
        CreatedAt = DateTime.UtcNow
    };
    
    await _repo.AddAuditLogAsync(auditLog);
    await _repo.SaveChangesAsync();
}
```

---

## 🧪 TESTING STRATEGIES

### Unit Tests
```csharp
// invmgmt.web.Tests/Services/PersonnelServiceTests.cs
[TestClass]
public class PersonnelServiceTests
{
    private Mock<IPersonnelRepository> _mockRepo;
    private Mock<IWebHostEnvironment> _mockEnv;
    private Mock<ILogger<PersonnelService>> _mockLogger;
    private PersonnelService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _mockRepo = new Mock<IPersonnelRepository>();
        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<PersonnelService>>();
        _service = new PersonnelService(_mockRepo.Object, _mockEnv.Object, _mockLogger.Object);
    }
    
    [TestMethod]
    public async Task CreateAsync_WithValidData_ReturnsPersonnelResponseDto()
    {
        // Arrange
        var dto = new PersonnelCreateDto { Name = "John", Email = "john@test.com" };
        
        // Act
        var result = await _service.CreateAsync(dto, null, "http://localhost");
        
        // Assert
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsException()
    {
        // Arrange
        var dto = new PersonnelCreateDto { Email = "duplicate@test.com" };
        _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        
        // Act
        await _service.CreateAsync(dto, null, "http://localhost");
        
        // Assert - Exception expected
    }
}
```

### Integration Tests
```csharp
// invmgmt.web.Tests/Controllers/PersonnelControllerTests.cs
[TestClass]
public class PersonnelControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    
    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }
    
    [TestMethod]
    public async Task CreatePersonnel_ReturnsCreatedAtAction()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("John Doe"), "name");
        content.Add(new StringContent("john@test.com"), "email");
        
        // Act
        var response = await _client.PostAsync("/api/personnel", content);
        
        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.Created, response.StatusCode);
    }
}
```

---

## 📝 CONFIGURATION CHECKLIST

### Before Deployment
- [ ] Database connection string configured in `appsettings.json`
- [ ] JWT settings configured (Issuer, Audience, Key)
- [ ] CORS policy configured for frontend domain
- [ ] File upload directory writable (`wwwroot/uploads/personnel/`)
- [ ] Maximum file size limits set in appsettings
- [ ] HTTPS enabled in production
- [ ] Logging configured for production environment
- [ ] Database migrations applied (`dotnet ef database update`)

### Monitoring
- [ ] Set up application logging
- [ ] Monitor file upload folder disk usage
- [ ] Set up alerts for API errors
- [ ] Monitor database performance (especially email uniqueness checks)
- [ ] Set up automated backups for uploaded photos

---

## 🔒 SECURITY CONSIDERATIONS

1. **Photo Upload**
   - Only JPG/JPEG allowed (not SVG or executable files)
   - 2 MB size limit enforced
   - Files stored outside web root is recommended (future improvement)
   - Filename randomized with GUID (no path traversal attacks)

2. **Authorization**
   - All endpoints require ADMIN role
   - JWT token validation enforced
   - Consider adding role-based endpoints (e.g., VIEW for non-admin)

3. **Data Validation**
   - Email format validated
   - Required fields enforced
   - String length limits applied
   - No SQL injection possible (EF Core + parameterized queries)

4. **Future Improvements**
   - Add rate limiting
   - Add request logging/audit trail
   - Encrypt sensitive fields (SSN, ID numbers)
   - Add two-factor authentication
   - Implement field-level access control
   - Add data export controls (DLP)

