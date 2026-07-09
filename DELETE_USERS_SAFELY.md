# 🗑️ SAFELY DELETE ALL USERS FROM DATABASE

**Database**: inventorydb (AWS RDS PostgreSQL)  
**Risk Level**: MEDIUM (requires careful execution)  
**Reversibility**: Can rollback with transaction

---

## ⚠️ BEFORE YOU DELETE

### What Will Happen

Deleting all users will:
- ✅ Remove all user accounts
- ✅ Remove user authentication data
- ❌ **May cascade delete related records** (Requests, Bills, etc.)

### Related Data That Will Be Affected

```
Users
├── UserRoles (cascade delete)
├── Requests (cascade delete)
│   └── RequestItems
├── Bills (cascade delete)
│   └── BillItems
├── ApprovalLogs (cascade delete)
├── AuditLogs (cascade delete)
└── ... other related records
```

---

## 🛡️ SAFE DELETE STRATEGY

### Option 1: Safe Delete (With Transaction - RECOMMENDED)

This approach allows you to rollback if something goes wrong:

```sql
-- START TRANSACTION (can rollback if needed)
BEGIN;

-- Step 1: Check how many users will be deleted
SELECT COUNT(*) as user_count FROM "Users";

-- Step 2: Check related data
SELECT COUNT(*) as request_count FROM "Requests" WHERE "UserId" IN (SELECT "Id" FROM "Users");
SELECT COUNT(*) as bill_count FROM "Bills" WHERE "CreatedByUserId" IN (SELECT "Id" FROM "Users");

-- Step 3: Delete all users (will cascade to related records)
DELETE FROM "Users";

-- Step 4: Verify deletion
SELECT COUNT(*) as remaining_users FROM "Users";

-- COMMIT the changes (or ROLLBACK if something went wrong)
COMMIT;
```

**How to use**:
```bash
psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com \
     -U postgres \
     -d inventorydb \
     -f delete_users.sql
```

---

### Option 2: Delete Specific Users Only

If you want to keep some users:

```sql
-- Delete all users EXCEPT admin
BEGIN;

DELETE FROM "Users" 
WHERE email != 'admin@gmail.com';

COMMIT;
```

Or delete by role:

```sql
-- Delete all non-admin users
BEGIN;

DELETE FROM "Users" 
WHERE "Role" != 'ADMIN';

COMMIT;
```

---

### Option 3: Disable Instead of Delete (Safest)

Instead of deleting, mark users as inactive:

```sql
-- Mark all users as inactive (instead of deleting)
BEGIN;

UPDATE "Users" 
SET "IsActive" = false, "IsApproved" = false
WHERE "Role" != 'ADMIN';

COMMIT;
```

---

## 📋 COMPLETE DELETE SCRIPT

**File**: `delete_all_users.sql`

```sql
-- ========================================
-- SAFELY DELETE ALL USERS
-- ========================================
-- This script deletes all users and related data
-- Use ROLLBACK if something goes wrong

BEGIN TRANSACTION;

-- Display what will be deleted
SELECT 'USERS TO DELETE:' as info;
SELECT "Id", "Email", "Username", "Role" FROM "Users";

SELECT 'RELATED REQUESTS:' as info;
SELECT COUNT(*) as count FROM "Requests" WHERE "UserId" IN (SELECT "Id" FROM "Users");

SELECT 'RELATED BILLS:' as info;
SELECT COUNT(*) as count FROM "Bills" WHERE "CreatedByUserId" IN (SELECT "Id" FROM "Users");

-- DELETE ALL USERS (cascades to UserRoles, Requests, Bills, etc.)
DELETE FROM "Users";

-- Verify deletion
SELECT 'REMAINING USERS:' as info;
SELECT COUNT(*) as remaining_users FROM "Users";

-- If everything looks good, COMMIT
-- Otherwise, ROLLBACK
COMMIT;
```

---

## 🚀 STEP-BY-STEP EXECUTION

### Step 1: Create the SQL file

```powershell
# Create delete_users.sql
@"
BEGIN;
DELETE FROM "Users";
SELECT COUNT(*) as remaining_users FROM "Users";
COMMIT;
"@ | Out-File delete_users.sql -Encoding UTF8
```

### Step 2: Execute the query

```bash
# Connect and execute
psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com \
     -U postgres \
     -d inventorydb \
     -c "DELETE FROM \"Users\";"
```

Or with transaction (safer):

```bash
psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com \
     -U postgres \
     -d inventorydb << 'EOF'
BEGIN;
DELETE FROM "Users";
SELECT COUNT(*) as remaining_users FROM "Users";
COMMIT;
EOF
```

### Step 3: Verify deletion

```bash
psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com \
     -U postgres \
     -d inventorydb \
     -c "SELECT COUNT(*) as user_count FROM \"Users\";"
```

Expected output:
```
 user_count
------------
          0
(1 row)
```

---

## 🔄 CASCADE DELETE DETAILS

When you delete users, these related tables will also be affected:

```sql
-- These will be auto-deleted due to foreign key constraints
DELETE FROM "UserRoles"           -- FK: UserId → Users.Id
DELETE FROM "ApprovalLogs"        -- FK: UserId → Users.Id (if exists)
DELETE FROM "AuditLogs"           -- FK: UserId → Users.Id (if exists)
DELETE FROM "RegistrationRequests" -- FK: UserId → Users.Id (if exists)

-- These might also cascade:
DELETE FROM "Bills"               -- FK: CreatedByUserId → Users.Id
DELETE FROM "BillItems"           -- FK: BillId → Bills.Id
DELETE FROM "Requests"            -- FK: UserId → Users.Id
DELETE FROM "RequestItems"        -- FK: RequestId → Requests.Id
DELETE FROM "IssueLog"            -- FK: UserId → Users.Id
DELETE FROM "ReceivedLog"         -- FK: UserId → Users.Id
```

---

## ✅ SAFE QUERIES (No Data Loss)

### Query 1: Delete Non-Admin Users Only

```sql
BEGIN;
DELETE FROM "Users" WHERE "Role" != 'ADMIN';
SELECT COUNT(*) as remaining_admin_users FROM "Users" WHERE "Role" = 'ADMIN';
COMMIT;
```

### Query 2: Deactivate Instead of Delete

```sql
BEGIN;
UPDATE "Users" SET "IsActive" = false WHERE "Role" != 'ADMIN';
SELECT COUNT(*) as inactive_users FROM "Users" WHERE "IsActive" = false;
COMMIT;
```

### Query 3: Delete Specific User

```sql
BEGIN;
DELETE FROM "Users" WHERE "Email" = 'user@example.com';
SELECT COUNT(*) as remaining_users FROM "Users";
COMMIT;
```

---

## 🎯 RECOMMENDED APPROACH

### For Testing/Development:

```sql
-- Safe to delete all users in dev environment
BEGIN;
DELETE FROM "Users";
COMMIT;
```

### For Production:

```sql
-- Don't delete - deactivate instead
BEGIN;
UPDATE "Users" 
SET "IsActive" = false, "IsApproved" = false
WHERE "Email" != 'admin@gmail.com';
COMMIT;
```

---

## 🔙 IF YOU MAKE A MISTAKE

### Rollback (if in transaction)

```sql
-- If you haven't committed yet:
ROLLBACK;
```

### Restore from Backup

```bash
# AWS RDS automatic backups are enabled
# Create a snapshot and restore
aws rds create-db-snapshot \
  --db-instance-identifier inveee-postgres \
  --db-snapshot-identifier pre-delete-backup
```

---

## 📊 FINAL CHECKLIST

- [ ] Have you backed up the database?
- [ ] Have you tested in a transaction first?
- [ ] Are you using the correct RDS endpoint?
- [ ] Have you verified the credentials?
- [ ] Do you understand what data will be deleted?
- [ ] Have you considered deactivating instead of deleting?

---

## ⚡ QUICK COMMAND

**Delete all users in one line** (with confirmation):

```bash
psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com \
     -U postgres \
     -d inventorydb \
     -c "BEGIN; DELETE FROM \"Users\"; COMMIT;"
```

**Verify deletion**:

```bash
psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com \
     -U postgres \
     -d inventorydb \
     -c "SELECT COUNT(*) FROM \"Users\";"
```

---

## 🛑 DO NOT USE THIS

❌ **Dangerous - bypasses all constraints**:
```sql
SET session_replication_role = 'replica';
DELETE FROM "Users";
SET session_replication_role = 'default';
```

❌ **Dangerous - deletes without constraints**:
```sql
TRUNCATE TABLE "Users";
```

✅ **Use this instead**:
```sql
DELETE FROM "Users";  -- Respects foreign key constraints
```

---

**Recommendation**: Use a transaction first, verify with SELECT, then COMMIT when confident.
