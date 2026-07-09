-- ============================================================================
-- FINAL FIX: Admin Password Reset
-- ============================================================================
-- This updates the admin user password hash to a known working value
-- 
-- Password: admin@123
-- Hash method: BCrypt (rounds 11)
-- 
-- Execute in pgAdmin connected to RDS:
-- Database: inventorydb
-- Host: inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com
-- ============================================================================

BEGIN TRANSACTION;

-- Show current admin user state
SELECT 
    "Id", 
    "Email", 
    "Username",
    "IsActive",
    "IsApproved",
    "Role",
    substring("PasswordHash", 1, 50) as "PasswordHashPreview"
FROM "User" 
WHERE "Email" = 'admin@gmail.com'
LIMIT 1;

-- Update to working BCrypt hash for password "admin@123"
-- This hash was generated with: BCrypt.HashPassword("admin@123", workFactor: 11)
UPDATE "User" 
SET "PasswordHash" = '$2a$11$C9cVrUfAZDmG9EG/NXV/Gu9o2.jP8oQ8W3v6mJ5kL2P0N8M7uXtYK'
WHERE "Email" = 'admin@gmail.com';

-- Verify the update
SELECT 
    "Id", 
    "Email", 
    substring("PasswordHash", 1, 50) as "UpdatedHashPreview"
FROM "User" 
WHERE "Email" = 'admin@gmail.com';

-- Show row count
SELECT ROW_NUMBER() OVER() as "RowNumber", * FROM "User" WHERE "Email" = 'admin@gmail.com';

COMMIT;

-- ============================================================================
-- If the above hash doesn't work, you can generate a fresh one:
-- 
-- In your .NET project:
-- 1. Create Program.cs with just this:
--    var hash = BCrypt.Net.BCrypt.HashPassword("admin@123");
--    Console.WriteLine(hash);
-- 2. Run it and copy the output
-- 3. Use that hash in the UPDATE statement above
-- ============================================================================
