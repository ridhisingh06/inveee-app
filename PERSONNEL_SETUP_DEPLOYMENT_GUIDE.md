# Personnel Module - Setup & Deployment Guide

## ⚡ QUICK START

### Prerequisites
- .NET 7+ SDK installed
- PostgreSQL 12+ running
- Visual Studio Code or Visual Studio
- Git for version control

### 1. Apply Database Migration

```bash
# Navigate to the project directory
cd invmgmt.web

# Apply the Personnel table migration
dotnet ef database update
```

This will create the `Personnel` table in your PostgreSQL database with:
- All required columns
- Primary key (Id)
- Unique email index
- Proper data types

### 2. Verify Configuration

Check `appsettings.json` for:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=invmgmt;User Id=postgres;Password=yourpassword"
  },
  "Jwt": {
    "Issuer": "your-issuer",
    "Audience": "your-audience",
    "Key": "your-secret-key-at-least-32-chars"
  }
}
```

### 3. Ensure Upload Directory

The application will automatically create `wwwroot/uploads/personnel/` on first file upload.

**Verify wwwroot exists:**
```bash
# From project root
ls -la invmgmt.web/wwwroot/
# Should show: css/, favicon.ico, js/, lib/
```

### 4. Build & Run Application

```bash
# From invmgmt.web directory
dotnet build
dotnet run

# Or in watch mode for development
dotnet watch run
```

The API will be available at `https://localhost:5000/api/personnel`

---

## 🧪 TESTING

### Option 1: Using PowerShell Script

```bash
# From workspace root
.\scripts\test_personnel_module.ps1
```

This will:
1. Authenticate with admin credentials
2. Create a test personnel record
3. List all personnel
4. Get a single record
5. Update the record
6. Test validations (duplicate email, invalid format, etc.)
7. Delete the record

### Option 2: Using Postman

1. Create a new collection
2. Set base URL: `https://localhost:5000/api/personnel`
3. Add authentication header: `Authorization: Bearer {token}`
4. Test each endpoint (see `PERSONNEL_API_QUICK_REFERENCE.md`)

### Option 3: Using cURL

```bash
# Get admin token
TOKEN=$(curl -X POST https://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"AdminPassword123!"}' \
  -s | jq -r '.token')

# Create personnel
curl -X POST https://localhost:5000/api/personnel \
  -H "Authorization: Bearer $TOKEN" \
  -F "name=John Doe" \
  -F "email=john@example.com" \
  -F "designation=Manager"

# List personnel
curl -X GET "https://localhost:5000/api/personnel?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 📋 VERIFICATION CHECKLIST

### Database Level
- [ ] Personnel table exists in PostgreSQL
  ```sql
  SELECT * FROM "Personnel" LIMIT 1;
  ```
- [ ] Email column has unique index
  ```sql
  SELECT * FROM pg_indexes WHERE tablename = 'Personnel' AND indexname LIKE '%Email%';
  ```
- [ ] Table has all required columns
  ```sql
  SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'Personnel';
  ```

### Application Level
- [ ] Application compiles without errors
  ```bash
  dotnet build
  ```
- [ ] Migration applied successfully
  ```bash
  dotnet ef migrations list
  ```
- [ ] Services registered in DI container
  - PersonnelService
  - PersonnelRepository
- [ ] Controller accessible at `/api/personnel`

### API Level
- [ ] Authentication works (can get JWT token)
- [ ] GET `/api/personnel` returns 200 OK (with empty array if no data)
- [ ] POST `/api/personnel` creates records successfully
- [ ] Duplicate email validation works (409 Conflict)
- [ ] Required field validation works (400 Bad Request)
- [ ] Authorization works (401 without token, 403 without ADMIN role)

---

## 🚀 PRODUCTION DEPLOYMENT

### Step 1: Prepare Environment

```bash
# Set environment to Production
export ASPNETCORE_ENVIRONMENT=Production

# Or in PowerShell
$env:ASPNETCORE_ENVIRONMENT = "Production"
```

### Step 2: Configure Production Settings

Create or update `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-db-server;Database=invmgmt_prod;User Id=dbuser;Password=secure-password"
  },
  "Jwt": {
    "Issuer": "your-prod-issuer",
    "Audience": "your-prod-audience",
    "Key": "your-secure-prod-key-minimum-32-chars"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Step 3: Build for Production

```bash
dotnet publish -c Release -o ./publish
```

### Step 4: Apply Migrations (Production)

```bash
cd ./publish
dotnet invmgmt.web.dll --migrate-db
```

Or manually:
```bash
dotnet ef database update --project invmgmt.web --configuration Release
```

### Step 5: Configure Web Server

#### Using Kestrel (Simple)
```bash
cd ./publish
dotnet invmgmt.web.dll --urls https://0.0.0.0:5000
```

#### Using IIS (Windows)

1. Create application pool (.NET 7)
2. Create website pointing to publish directory
3. Configure HTTPS certificate
4. Set environment variables in web.config:
```xml
<system.webServer>
  <aspNetCore processPath="dotnet" arguments="invmgmt.web.dll">
    <environmentVariables>
      <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      <environmentVariable name="ASPNETCORE_URLS" value="https://+:443" />
    </environmentVariables>
  </aspNetCore>
</system.webServer>
```

#### Using Nginx (Linux)

```nginx
upstream dotnet_backend {
    server localhost:5000;
}

server {
    listen 443 ssl http2;
    server_name api.yourdomain.com;

    ssl_certificate /etc/ssl/certs/your-cert.crt;
    ssl_certificate_key /etc/ssl/private/your-key.key;

    location / {
        proxy_pass https://dotnet_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Static files (photos)
    location /uploads/personnel/ {
        alias /var/www/invmgmt/wwwroot/uploads/personnel/;
        expires 30d;
    }
}
```

### Step 6: Configure File Uploads

For production, consider storing photos in cloud storage (Azure Blob, AWS S3):

1. Update `PersonnelService.SavePhotoAsync()` to use cloud storage
2. Store blob URL in PhotoPath instead of relative path
3. Benefits: Scalability, CDN integration, backup

### Step 7: Set Up Monitoring

```bash
# Enable application insights
# In appsettings.Production.json
"ApplicationInsights": {
  "InstrumentationKey": "your-key"
}
```

### Step 8: Set Up Logging

```bash
# Configure centralized logging (ELK, Serilog, etc.)
# Configure log retention policies
# Set up alerts for errors
```

---

## 🔧 TROUBLESHOOTING

### Issue: Migration Failed

**Symptom:** "Unable to create Personnel table"

**Solution:**
```bash
# Check existing migrations
dotnet ef migrations list

# Remove failed migration (if not applied)
dotnet ef migrations remove

# Recreate migration
dotnet ef migrations add AddPersonnelTable

# Apply again
dotnet ef database update
```

---

### Issue: File Upload Fails

**Symptom:** "Failed to save photo" or files not appearing in wwwroot

**Solution:**
1. Verify wwwroot directory permissions:
```bash
# Linux/Mac
ls -la invmgmt.web/wwwroot/
chmod 755 invmgmt.web/wwwroot

# Windows - check in Explorer: Properties > Security > Edit > Full Control
```

2. Verify IWebHostEnvironment is properly injected:
```csharp
// PersonnelService constructor
public PersonnelService(IPersonnelRepository repo, 
                       IWebHostEnvironment env,  // Should be injected
                       ILogger<PersonnelService> logger)
```

3. Check that directory creation works:
```csharp
var dir = Path.Combine(_env.WebRootPath, "uploads/personnel");
Directory.CreateDirectory(dir);  // Should not throw
```

---

### Issue: Authorization Fails (401/403)

**Symptom:** "Unauthorized" or "Forbidden" even with valid token

**Solution:**
1. Verify JWT configuration matches in Program.cs and appsettings:
```csharp
// Program.cs
var jwtKey = config["Jwt:Key"] ?? "THIS_IS_MY_SECRET_KEY_12345";

// Ensure this matches your token generation
```

2. Verify ADMIN role exists in database:
```sql
SELECT * FROM "Roles" WHERE "Name" = 'Admin';
```

3. Verify user has ADMIN role:
```sql
SELECT u.*, r."Name" 
FROM "Users" u
JOIN "UserRoles" ur ON u."Id" = ur."UserId"
JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'admin@example.com';
```

4. Check token contains correct role claim:
- Decode JWT at jwt.io
- Verify "role": "ADMIN" in payload

---

### Issue: Duplicate Email Validation Not Working

**Symptom:** Can create two records with same email

**Solution:**
1. Verify unique index exists:
```sql
SELECT * FROM pg_indexes 
WHERE tablename = 'Personnel' 
AND indexname LIKE '%Email%';
```

2. If missing, create migration:
```csharp
// In migration Up()
migrationBuilder.CreateIndex(
    name: "IX_Personnel_Email",
    table: "Personnel",
    column: "Email",
    unique: true);
```

3. Verify service checks before save:
```csharp
// PersonnelService.CreateAsync()
if (await _repo.EmailExistsAsync(dto.Email))
    throw new InvalidOperationException(...);
```

---

### Issue: Database Connection Fails

**Symptom:** "Failed to connect to database" or "Connection timeout"

**Solution:**
1. Test PostgreSQL connection:
```bash
psql -h localhost -U postgres -d invmgmt -c "SELECT 1"
```

2. Verify connection string format:
```
Host=localhost;Port=5432;Database=invmgmt;User Id=postgres;Password=yourpassword
```

3. Check PostgreSQL is running:
```bash
# Windows
Get-Service PostgreSQL*

# Linux
sudo systemctl status postgresql
```

4. Verify firewall rules allow connection (if remote):
```bash
# Test connectivity
telnet db-server 5432
```

---

## 📚 ADDITIONAL RESOURCES

- [Entity Framework Core Docs](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Project Personnel Module Verification](./PERSONNEL_MODULE_VERIFICATION.md)
- [API Quick Reference](./PERSONNEL_API_QUICK_REFERENCE.md)
- [Architecture & Extension Guide](./PERSONNEL_ARCHITECTURE_GUIDE.md)

---

## ✅ SUCCESS CRITERIA

Your Personnel module is successfully deployed when:

1. ✅ Database migration applied (`dotnet ef database update` succeeds)
2. ✅ Application starts without errors (`dotnet run`)
3. ✅ Can authenticate and get JWT token
4. ✅ POST endpoint creates personnel records
5. ✅ GET endpoints retrieve data correctly
6. ✅ Email uniqueness prevents duplicates
7. ✅ Photo upload works (for JPG files)
8. ✅ Authorization works (ADMIN role required)
9. ✅ All test scripts pass
10. ✅ No errors in application logs

---

## 🎉 YOU'RE DONE!

The Personnel Management module is now ready for use. Start with:
1. Apply migrations
2. Run the application
3. Run test scripts
4. Monitor for any issues
5. Deploy to production when ready

For questions or issues, refer to the troubleshooting section above.

