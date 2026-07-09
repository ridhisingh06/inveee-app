-- ============================================================================
-- DELETE ALL USER DATA FROM INVENTORY DATABASE
-- ============================================================================
-- This script safely deletes all user data while respecting foreign key constraints
-- Database: inventorydb
-- 
-- IMPORTANT: This operation is IRREVERSIBLE. Make a backup before running.
--
-- Steps to execute:
-- 1. Connect to pgAdmin with credentials:
--    - Host: localhost (or RDS host)
--    - Port: 5432
--    - Database: inventorydb
--    - Username: postgres
--    - Password: Ridhisingh (local) or ridhisingh2003 (RDS)
--
-- 2. Run this entire script in a transaction (CTRL+A, CTRL+Enter)
-- ============================================================================

BEGIN TRANSACTION;

-- Show what we're about to delete
SELECT 'Total users to delete:' as Info, COUNT(*) as Count FROM "User";
SELECT 'Total requests to delete:' as Info, COUNT(*) as Count FROM "Request";
SELECT 'Total approval logs to delete:' as Info, COUNT(*) as Count FROM "ApprovalLog";
SELECT 'Total issue logs to delete:' as Info, COUNT(*) as Count FROM "IssueLog";
SELECT 'Total received logs to delete:' as Info, COUNT(*) as Count FROM "ReceivedLog";
SELECT 'Total request items to delete:' as Info, COUNT(*) as Count FROM "RequestItem";

-- Delete in correct order (respecting foreign key constraints)
-- 1. Delete approval logs (depends on Request)
DELETE FROM "ApprovalLog" WHERE "RequestId" IN (SELECT "Id" FROM "Request");

-- 2. Delete issue logs (depends on Request)
DELETE FROM "IssueLog" WHERE "RequestId" IN (SELECT "Id" FROM "Request");

-- 3. Delete received logs (depends on Request)
DELETE FROM "ReceivedLog" WHERE "RequestId" IN (SELECT "Id" FROM "Request");

-- 4. Delete request items (depends on Request)
DELETE FROM "RequestItem" WHERE "RequestId" IN (SELECT "Id" FROM "Request");

-- 5. Delete requests (depends on User)
DELETE FROM "Request" WHERE "UserId" IN (SELECT "Id" FROM "User");

-- 6. Finally delete all users
DELETE FROM "User";

-- Verify deletion
SELECT 'Users remaining:' as Info, COUNT(*) as Count FROM "User";
SELECT 'Requests remaining:' as Info, COUNT(*) as Count FROM "Request";
SELECT 'Approval logs remaining:' as Info, COUNT(*) as Count FROM "ApprovalLog";
SELECT 'Issue logs remaining:' as Info, COUNT(*) as Count FROM "IssueLog";
SELECT 'Received logs remaining:' as Info, COUNT(*) as Count FROM "ReceivedLog";
SELECT 'Request items remaining:' as Info, COUNT(*) as Count FROM "RequestItem";

-- COMMIT to finalize changes
-- ROLLBACK if you want to undo
COMMIT;

-- ============================================================================
-- ADMIN USER RECREATION (after deletion)
-- ============================================================================
-- If you want to recreate the admin user after deletion, use this:
--
-- INSERT INTO "User" (
--   "Username",
--   "Email",
--   "PasswordHash",
--   "DepartmentId",
--   "Designation",
--   "IsActive",
--   "IsApproved",
--   "Role",
--   "CreatedAt"
-- ) VALUES (
--   'admin',
--   'admin@gmail.com',
--   '$2a$11$redacted...',  -- BCrypt hash (will be auto-generated on app startup)
--   NULL,
--   'Administrator',
--   true,
--   true,
--   'ADMIN',
--   NOW()
-- );
--
-- The app will automatically recreate seeded data on next startup.
-- ============================================================================
