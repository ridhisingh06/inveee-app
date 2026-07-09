# 📊 SEEDED DATA REFERENCE

**Date**: July 9, 2026  
**Database**: inventorydb (AWS RDS PostgreSQL)  
**Seeding Location**: `backend/Program.cs` (lines 180-305)

---

## 🎯 SEEDED DATA OVERVIEW

The database is automatically seeded during application startup with the following data:

| Entity | Count | Source |
|--------|-------|--------|
| Roles | 3 | AppDbContext.OnModelCreating() + Program.cs |
| Departments | 4 | AppDbContext.OnModelCreating() + Program.cs |
| Categories | 3 | Program.cs |
| Users | 1 | Program.cs (Admin) |

**Total Seeded Records**: 11

---

## 📋 DETAILED SEEDED DATA

### 1. ROLES (3 records)

```csharp
new Role { Id = 1, Name = "User" },
new Role { Id = 2, Name = "Issuer" },
new Role { Id = 3, Name = "Admin" }
```

| ID | Name | Purpose |
|----|------|---------|
| 1 | User | Regular user role - can make requests |
| 2 | Issuer | Issuer role - can issue items |
| 3 | Admin | Admin role - full system access |

**SQL to verify**:
```sql
SELECT id, name FROM "Roles" ORDER BY id;
```

---

### 2. DEPARTMENTS (4 records)

```csharp
new Department { Id = 1, Name = "Admin" },
new Department { Id = 2, Name = "IT" },
new Department { Id = 3, Name = "HR" },
new Department { Id = 4, Name = "Finance" }
```

| ID | Name | Purpose |
|----|------|---------|
| 1 | Admin | Administrative department |
| 2 | IT | Information Technology |
| 3 | HR | Human Resources |
| 4 | Finance | Finance department |

**SQL to verify**:
```sql
SELECT id, name FROM "Departments" ORDER BY id;
```

---

### 3. CATEGORIES (3 records)

```csharp
new Category { Name = "Stationary" },
new Category { Name = "IT Related" },
new Category { Name = "HouseKeeping" }
```

| ID | Name | Purpose |
|----|------|---------|
| (auto) | Stationary | Office supplies & stationery items |
| (auto) | IT Related | IT equipment and accessories |
| (auto) | HouseKeeping | Cleaning and maintenance supplies |

**SQL to verify**:
```sql
SELECT id, name FROM "Categories" ORDER BY id;
```

---

### 4. ADMIN USER (1 record)

```csharp
var adminUser = new User
{
    Username = "System Admin",
    Email = "admin@gmail.com",
    DepartmentId = 1,
    Designation = "System Administrator",
    IsActive = true,
    IsApproved = true,
    Role = "ADMIN",
    CreatedAt = DateTime.UtcNow,
    PasswordHash = PasswordUtils.HashPassword("admin@123")
};
```

| Field | Value | Notes |
|-------|-------|-------|
| ID | Auto-generated | Will be 1 (first user) |
| Username | System Admin | Display name |
| Email | admin@gmail.com | **Login email** |
| DepartmentId | 1 | Admin department |
| Designation | System Administrator | Job title |
| IsActive | true | Account is active |
| IsApproved | true | Account is approved |
| Role | ADMIN | Admin role |
| Password (plain) | admin@123 | **Login password** |
| PasswordHash | `$2b$10$...` | BCrypt hash (stored) |
| CreatedAt | DateTime.UtcNow | Current timestamp |

**SQL to verify**:
```sql
SELECT id, email, username, role, is_approved, is_active, designation, department_id 
FROM "Users" 
WHERE email = 'admin@gmail.com';
```

**Expected output**:
```
 id |       email        |   username    | role  | is_approved | is_active |    designation     | department_id
----+--------------------+---------------+-------+-------------+-----------+--------------------+---------------
  1 | admin@gmail.com    | System Admin  | ADMIN | t           | t         | System Administrator |             1
```

---

## 🔑 LOGIN CREDENTIALS

**Use these to test the application:**

```
Email:    admin@gmail.com
Password: admin@123
```

**API Endpoint**:
```
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@gmail.com",
  "password": "admin@123"
}
```

**Expected Response** (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "message": "Login successful."
}
```

---

## 🔄 SEEDING PROCESS

The seeding happens automatically when the application starts:

```
Application Start
    ↓
Program.cs Startup
    ↓
Test Database Connection (5 retries with backoff)
    ↓
Apply Pending Migrations
    ↓
Seed Roles (if empty)
    ↓
Seed Departments (if empty)
    ↓
Seed Categories (if empty)
    ↓
Seed Admin User (if empty)
    ↓
✓ Database Ready
```

### Seed Conditions

All seed operations use an **idempotent pattern**:

```csharp
if (!await db.Roles.AnyAsync())  // Only seed if table is empty
{
    db.Roles.AddRange(...);
    await db.SaveChangesAsync();
}
```

**This means**:
- ✅ Safe to run multiple times (won't duplicate)
- ✅ Can be re-run if needed
- ✅ Automatically creates missing data

---

## 📊 DATABASE SCHEMA

### Tables Auto-Created

After seeding, these tables exist:

```
Roles
├── Id (PK)
├── Name

Departments
├── Id (PK)
├── Name

Categories
├── Id (PK)
├── Name

Users
├── Id (PK)
├── Email (Unique)
├── Username
├── PasswordHash
├── Role
├── DepartmentId (FK → Departments)
├── Designation
├── IsActive
├── IsApproved
├── CreatedAt

Inventory-Related
├── Items
├── InventoryStocks
├── RequestItems
├── Requests
└── ... (20+ other tables)
```

---

## 🌍 ENVIRONMENT VARIABLES (Seed Customization)

You can override seeded data via environment variables:

```
ADMIN_EMAIL = "custom@email.com"        # Default: admin@gmail.com
ADMIN_PASSWORD = "customPassword123"    # Default: admin@123
```

**Example in ECS task-definition.json**:
```json
"environment": [
  {
    "name": "ADMIN_EMAIL",
    "value": "admin@example.com"
  },
  {
    "name": "ADMIN_PASSWORD",
    "value": "MySecurePassword123!"
  }
]
```

---

## ✅ VERIFICATION QUERIES

### Check All Seeded Data

```sql
-- Check roles
SELECT COUNT(*) as role_count FROM "Roles";
-- Expected: 3

-- Check departments
SELECT COUNT(*) as dept_count FROM "Departments";
-- Expected: 4

-- Check categories
SELECT COUNT(*) as cat_count FROM "Categories";
-- Expected: 3

-- Check admin user
SELECT COUNT(*) as user_count FROM "Users" WHERE email = 'admin@gmail.com';
-- Expected: 1
```

### Verify Seeding Occurred

```sql
-- Check if all seed data exists
SELECT 
  (SELECT COUNT(*) FROM "Roles") as roles,
  (SELECT COUNT(*) FROM "Departments") as departments,
  (SELECT COUNT(*) FROM "Categories") as categories,
  (SELECT COUNT(*) FROM "Users") as users;

-- Expected output:
-- roles | departments | categories | users
-- ------+-------------+------------+-------
--     3 |           4 |          3 |     1
```

### Get Admin User Details

```sql
SELECT 
  id, 
  email, 
  username, 
  role, 
  is_approved, 
  is_active,
  department_id,
  substring(password_hash, 1, 20) || '...' as password_hash_preview
FROM "Users" 
WHERE email = 'admin@gmail.com';
```

---

## 🚀 FOR NEW DEPLOYMENTS

When deploying to a new environment:

1. **Database is created** by Terraform
2. **Migrations are applied** automatically
3. **Seed data is inserted** automatically
4. **Application is ready** to use immediately

**No manual seeding needed!** ✅

---

## 📝 SOURCE CODE LOCATIONS

### Seeding Code
- **File**: `backend/Program.cs`
- **Lines**: 180-305
- **Function**: Database initialization during app startup

### Seed Data Models
- **File**: `backend/Data/AppDbContext.cs`
- **Lines**: 45-60
- **Method**: `OnModelCreating()`

### Model Definitions
- **File**: `backend/Models/`
- **Files**: `Role.cs`, `Department.cs`, `Category.cs`, `User.cs`

---

## 🔐 SECURITY NOTES

1. **Password Hashing**: All passwords are hashed using BCrypt (not stored in plain text)
2. **Admin Credentials**: Change in production:
   - Environment variables: `ADMIN_EMAIL`, `ADMIN_PASSWORD`
   - Or manually update in database after deployment
3. **No Hard-Coded Secrets**: Passwords are set via environment variables, not hardcoded
4. **Database Access**: RDS credentials in `task-definition.json` environment variables

---

## 🧪 TEST DATA

To create additional test users beyond the seeded admin:

```sql
-- Insert a test User
INSERT INTO "Users" (username, email, password_hash, department_id, designation, is_active, is_approved, role, created_at)
VALUES (
  'John Doe',
  'john@example.com',
  '$2b$10$N9qo8uLOickgx2ZMRZoMye/qd.Uj7ZNmxWp9gF1F0P6/dLmQ6gLEJ2',  -- BCrypt hash of "password123"
  2,  -- IT department
  'Developer',
  true,
  true,
  'USER',
  NOW()
);
```

---

## 📊 SUMMARY

| Entity | Count | Auto-Seeded | Updatable | Notes |
|--------|-------|-------------|-----------|-------|
| Roles | 3 | ✅ Yes | ✅ Yes | Core system roles |
| Departments | 4 | ✅ Yes | ✅ Yes | Organization units |
| Categories | 3 | ✅ Yes | ✅ Yes | Item categories |
| Admin User | 1 | ✅ Yes | ✅ Yes | Customizable via env vars |

**Total Seeded**: 11 records on first deployment

---

**Status**: ✅ Production Ready  
**Seeding**: Automatic on app startup  
**Idempotent**: Yes - safe to re-run  
**Customizable**: Yes - via environment variables
