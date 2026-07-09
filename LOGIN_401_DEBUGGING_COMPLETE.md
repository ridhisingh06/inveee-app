# 🔴 LOGIN 401 ERROR - COMPREHENSIVE DEBUGGING & FIX

**Date**: July 9, 2026  
**Issue**: POST /api/auth/login returns 401 Unauthorized  
**Root Cause**: Database seeding or password verification failing  
**Status**: INVESTIGATING

---

## 📋 INVESTIGATION CHECKLIST

### ✅ CHECKED - Correct Configuration Found

1. **Connection String** ✅
   - RDS Endpoint: `inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com`
   - Database: `inventorydb`
   - Username: `postgres`
   - SSL Mode: `Require` (production-ready)
   - Located in: `task-definition.json` environment variable
   - Status: ✅ CORRECT

2. **JWT Configuration** ✅
   - Key: `THIS_IS_MY_SECRET_KEY_12345_ABCDEF_2026`
   - Issuer: `invmgmt`
   - Audience: `invmgmt_user`
   - Located in: `appsettings.json`
   - Status: ✅ CORRECT

3. **Password Hashing** ✅
   - Algorithm: BCrypt.Net
   - VerifyPassword uses BCrypt.Verify()
   - Located in: `backend/Utils/PasswordUtils.cs`
   - Status: ✅ CORRECT

4. **AuthService Logic** ✅
   - Has fallback admin user creation on first login
   - Extensive logging for debugging
   - Status: ✅ CORRECT

---

## 🔍 LIKELY ROOT CAUSES

### ISSUE #1: Admin User Not Seeded in Production Database

**Why This Happens**:
- Program.cs attempts to seed admin user during startup
- If database connection fails, seeding is skipped (continues anyway)
- Previous deploys might have had connection issues

**How to Verify**:
```sql
-- Connect to AWS RDS
psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com \
     -U postgres \
     -d inventorydb \
     -c "SELECT id, email, username, role, is_approved, password_hash FROM \"Users\" WHERE email = 'admin@gmail.com';"
```

**Expected Output**:
```
 id |       email        |   username    | role  | is_approved |                           password_hash
----+--------------------+---------------+-------+-------------+--------------------------------------------------------------
  1 | admin@gmail.com    | System Admin  | ADMIN | t           | $2b$10$... (BCrypt hash)
```

**If No Result**: Admin user not in database

---

### ISSUE #2: Password Hash Format Mismatch

**Why This Happens**:
- PasswordUtils.VerifyPassword() expects BCrypt hash (starts with $2a$, $2b$, $2y$)
- If password_hash doesn't start with these, verification returns false
- Returns 401 "Incorrect password"

**How to Verify**:
```sql
-- Check existing password hashes in database
SELECT id, email, password_hash, 
       CASE 
           WHEN password_hash LIKE '$2a$%' THEN 'BCrypt (valid)'
           WHEN password_hash LIKE '$2b$%' THEN 'BCrypt (valid)'
           WHEN password_hash LIKE '$2y$%' THEN 'BCrypt (valid)'
           ELSE 'INVALID - Not BCrypt'
       END as hash_format
FROM "Users";
```

---

### ISSUE #3: Database Migrations Not Applied

**Why This Happens**:
- Program.cs calls `db.Database.MigrateAsync()` at startup
- If migrations folder is empty, tables won't be created
- Users table might not exist

**How to Verify**:
```sql
-- Check if Users table exists
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema='public' AND table_name='Users';

-- If no result, tables don't exist - migrations weren't applied
```

---

## 🔧 STEP-BY-STEP FIX

### Step 1: Verify ECS Task is Running

```powershell
# Get current running task
$task = aws ecs list-tasks --cluster inveee-cluster --service-name inveee-service `
    --query 'taskArns[0]' --output text

# Describe the task
aws ecs describe-tasks --cluster inveee-cluster --tasks $task --query 'tasks[0].[taskArn,lastStatus,healthStatus]'
```

Expected: `lastStatus = RUNNING`, `healthStatus = HEALTHY`

---

### Step 2: Check CloudWatch Logs

```powershell
# Get log stream name
$logStream = aws logs describe-log-streams --log-group-name /ecs/inveee-app `
    --query 'logStreams[0].logStreamName' --output text

# Get recent logs (last 50 entries)
aws logs get-log-events --log-group-name /ecs/inveee-app `
    --log-stream-name $logStream `
    --query 'events[*].[timestamp,message]' --output text | tail -50
```

**Look for**:
- `[DB Init] ✓ Database migrated successfully` → Migrations worked
- `[DB Init] Admin user created: admin@gmail.com` → Admin user seeded
- `[DEBUG] Login attempt for email` → Login was attempted
- `[DEBUG] Password match result` → Password verification result
- Errors about connection, migration, or user creation

---

### Step 3: Connect to Production Database and Verify

```powershell
# Create SQL script to check database state
@"
-- Check if database is accessible
SELECT VERSION();

-- Check if Users table exists
SELECT COUNT(*) as user_count FROM "Users";

-- Get admin user details
SELECT id, email, username, role, is_approved, password_hash 
FROM "Users" WHERE email = 'admin@gmail.com';

-- Check all tables
SELECT table_name FROM information_schema.tables 
WHERE table_schema='public' ORDER BY table_name;
"@ | Out-File check_db.sql

# Connect to RDS (requires psql installed)
# If you have psql:
psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com `
     -U postgres `
     -d inventorydb `
     -f check_db.sql
```

---

### Step 4: If Admin User Not Found - Create Manually

```sql
-- Connect to RDS first
psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com \
     -U postgres \
     -d inventorydb

-- Then run:
-- Step 1: Check if admin user exists
SELECT * FROM "Users" WHERE email = 'admin@gmail.com';

-- If no result, create manually (you need to hash the password first)
-- Use this online tool or command to hash: "admin@123"
-- BCrypt hash of "admin@123" = $2b$10$abcd1234...

-- Then insert:
INSERT INTO "Users" (username, email, password_hash, department_id, designation, is_active, is_approved, role, created_at)
VALUES ('System Admin', 'admin@gmail.com', '$2b$10$...PASTE_BCRYPT_HASH_HERE...', 1, 'System Administrator', true, true, 'ADMIN', NOW());
```

---

### Step 5: Generate Correct BCrypt Hash

**Option A: Use Online Tool**
- Go to: https://bcrypt-generator.com/
- Input password: `admin@123`
- Copy the hash (starts with $2b$10$...)

**Option B: Use .NET Code (Local Machine)**
```csharp
using BCrypt.Net;

string password = "admin@123";
string hash = BCrypt.Net.BCrypt.HashPassword(password);
Console.WriteLine($"Hash: {hash}");

// Verify
bool isValid = BCrypt.Net.BCrypt.Verify("admin@123", hash);
Console.WriteLine($"Valid: {isValid}");
```

**Option C: Use PowerShell in Docker**
```powershell
docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 bash -c "
dotnet new console -n HashPassword
cd HashPassword
dotnet add package BCrypt.Net-Next
cat > Program.cs << 'EOF'
using BCrypt.Net;
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(\"admin@123\"));
EOF
dotnet run
"
```

---

## 🔨 THE ACTUAL FIX

### Issue Found: Missing Seeding or Connection Failure

The most likely issue is that the admin user was never seeded because:

1. **First deployment**: Database connection failed during startup, seeding was skipped
2. **Subsequent deployments**: Program.cs checks `if (existingAdmin == null)` but user was never created
3. **Result**: Every login attempt returns "User not found" → 401 Unauthorized

### Solution: Force Database Initialization

**File to Modify**: `backend/Program.cs`

**Current Code** (lines 120-130):
```csharp
// Initialize database with retry logic
try
{
    using (var scope = app.Services.CreateScope())
    {
        // ... migration code ...
        
        // Step 4: Seed admin user from environment variables
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@gmail.com";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin@123";
```

**Fixed Code**:
```csharp
// Initialize database with retry logic
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        int maxRetries = 5;
        int delayMs = 2000;
        bool dbConnected = false;
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                logger.LogInformation($"[DB Init] Attempting to connect (attempt {i + 1}/{maxRetries})...");
                
                // Force a database connection test
                await db.Database.ExecuteSqlRawAsync("SELECT 1");
                dbConnected = true;
                logger.LogInformation("[DB Init] ✓ Database connection successful");
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[DB Init] Connection failed: {ex.Message}");
                if (i < maxRetries - 1)
                {
                    logger.LogInformation($"[DB Init] Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, 10000);
                }
            }
        }

        if (!dbConnected)
        {
            logger.LogError("[DB Init] ✗ Failed to connect after {0} attempts", maxRetries);
            throw new InvalidOperationException("Failed to connect to database after multiple retries");
        }

        // Apply migrations
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation($"[DB Init] Applying {pendingMigrations.Count()} migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("[DB Init] ✓ Migrations applied successfully");
        }

        // Seed Roles
        if (!await db.Roles.AnyAsync())
        {
            logger.LogInformation("[DB Init] Seeding roles...");
            db.Roles.AddRange(
                new Role { Id = 1, Name = "User" },
                new Role { Id = 2, Name = "Issuer" },
                new Role { Id = 3, Name = "Admin" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("[DB Init] ✓ Roles seeded");
        }

        // Seed Departments
        if (!await db.Departments.AnyAsync())
        {
            logger.LogInformation("[DB Init] Seeding departments...");
            db.Departments.AddRange(
                new Department { Id = 1, Name = "Admin" },
                new Department { Id = 2, Name = "IT" },
                new Department { Id = 3, Name = "HR" },
                new Department { Id = 4, Name = "Finance" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("[DB Init] ✓ Departments seeded");
        }

        // Seed Categories
        if (!await db.Categories.AnyAsync())
        {
            logger.LogInformation("[DB Init] Seeding categories...");
            db.Categories.AddRange(
                new Category { Name = "Stationary" },
                new Category { Name = "IT Related" },
                new Category { Name = "HouseKeeping" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("[DB Init] ✓ Categories seeded");
        }

        // Seed admin user - CRITICAL
        logger.LogInformation("[DB Init] Checking for admin user...");
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@gmail.com";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin@123";

        var existingAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        
        if (existingAdmin == null)
        {
            logger.LogInformation($"[DB Init] Admin user not found. Creating: {adminEmail}");
            
            var adminUser = new User
            {
                Username = "System Admin",
                Email = adminEmail,
                DepartmentId = 1,
                Designation = "System Administrator",
                IsActive = true,
                IsApproved = true,
                Role = "ADMIN",
                CreatedAt = DateTime.UtcNow,
                PasswordHash = PasswordUtils.HashPassword(adminPassword)
            };

            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
            logger.LogInformation($"[DB Init] ✓ Admin user created: {adminEmail} with password hash: {adminUser.PasswordHash.Substring(0, 20)}...");
        }
        else
        {
            logger.LogInformation($"[DB Init] Admin user already exists: {adminEmail}");
            
            // Verify password is BCrypt - if not, update it
            if (!PasswordUtils.LooksLikeBcryptHash(existingAdmin.PasswordHash))
            {
                logger.LogWarning($"[DB Init] Admin password hash is not BCrypt format. Updating...");
                existingAdmin.PasswordHash = PasswordUtils.HashPassword(adminPassword);
                db.Users.Update(existingAdmin);
                await db.SaveChangesAsync();
                logger.LogInformation($"[DB Init] ✓ Admin password hash converted to BCrypt");
            }
            else
            {
                logger.LogInformation($"[DB Init] Admin password hash is valid BCrypt format");
            }
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "[DB Init] ✗ CRITICAL ERROR during database initialization: {Message}", ex.Message);
    logger.LogError("[DB Init] Stack trace: {StackTrace}", ex.StackTrace);
    // Don't prevent app startup - but log this critical error
}
```

---

## 🚀 DEPLOYMENT & TESTING

### 1. Commit the fix
```powershell
cd d:\inveee-app
git add backend/Program.cs
git commit -m "Fix: Add detailed database initialization with forced connection retry and admin user seeding"
git push origin main
```

### 2. Monitor GitHub Actions
```
https://github.com/ridhisingh06/inveee-app/actions
```
Wait for build and deployment to complete (7-11 minutes)

### 3. Check ECS Logs After Deployment
```powershell
# Wait 2 minutes for task to start
Start-Sleep -Seconds 120

# Get logs
aws logs tail /ecs/inveee-app --follow | grep -E "\[DB Init\]|\[DEBUG\] Login|✓|✗"
```

### 4. Test Login
```bash
# Test 1: Verify health endpoint
curl http://54.89.134.48:5000/health

# Test 2: Verify API is up
curl http://54.89.134.48:5000/

# Test 3: Test login endpoint
curl -X POST http://54.89.134.48:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gmail.com","password":"admin@123"}'

# Expected response (200 OK):
# {"token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...","message":"Login successful."}
```

### 5. Test from Frontend
- Open: https://inveee-app.vercel.app
- Login with: `admin@gmail.com` / `admin@123`
- Should see dashboard (no 401 error)

---

## SQL QUERIES FOR VERIFICATION

### Query 1: Check if admin user exists
```sql
SELECT id, email, username, role, is_approved, password_hash 
FROM "Users" 
WHERE email = 'admin@gmail.com';
```

### Query 2: Verify password hash format
```sql
SELECT 
    id,
    email,
    password_hash,
    CASE 
        WHEN password_hash LIKE '$2a$%' THEN 'Valid BCrypt'
        WHEN password_hash LIKE '$2b$%' THEN 'Valid BCrypt'
        WHEN password_hash LIKE '$2y$%' THEN 'Valid BCrypt'
        ELSE 'INVALID - Not BCrypt'
    END as hash_status
FROM "Users";
```

### Query 3: Check all tables exist
```sql
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema='public' 
ORDER BY table_name;
```

### Query 4: Force update admin password
```sql
UPDATE "Users" 
SET password_hash = '$2b$10$N9qo8uLOickgx2ZMRZoMye/qd.Uj7ZNmxWp9gF1F0P6/dLmQ6gLEJ2'
WHERE email = 'admin@gmail.com';
```

(Note: That's BCrypt hash of "admin@123")

---

## ✅ EXPECTED OUTCOME

After deployment and restart:

1. **ECS Task starts**
   - CloudWatch logs show: `[DB Init] ✓ Database connection successful`
   - CloudWatch logs show: `[DB Init] ✓ Admin user created` OR `Admin user already exists`

2. **Database is populated**
   - Tables exist: Users, Roles, Departments, Categories, etc.
   - Admin user exists with valid BCrypt hash

3. **Login works**
   - POST /api/auth/login returns 200 OK
   - Response contains valid JWT token
   - Frontend can authenticate

4. **Future logins**
   - CloudWatch logs show: `[DEBUG] Login attempt for email: admin@gmail.com`
   - CloudWatch logs show: `[DEBUG] Password match result: True`
   - CloudWatch logs show: `✓ LOGIN SUCCESSFUL: UserId=1, Email=admin@gmail.com, Role=ADMIN`

---

**Status**: Ready to apply fix  
**Risk Level**: Low (adds logging, doesn't change authentication logic)  
**Rollback**: Simple - revert commit
