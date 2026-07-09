-- ============================================================================
-- FIX ADMIN PASSWORD HASH
-- ============================================================================
-- This script updates the admin user's password to a working BCrypt hash
-- for the password "admin@123"
-- 
-- Generated hash: $2a$11$T3xH5aLgXHPYIj3L8VYWfOhgzB6Yd5oFP4tN5JzXqV7Q0Z8L2M9nC
-- Password: admin@123
--
-- Database: inventorydb
-- Host: inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com
-- ============================================================================

BEGIN TRANSACTION;

-- Check current admin user
SELECT 'Current admin user before update:' as Info;
SELECT "Id", "Email", "Username", "IsActive", "IsApproved", "Role", 
       substring("PasswordHash", 1, 30) as "PasswordHashStart"
FROM "User" 
WHERE "Email" = 'admin@gmail.com';

-- Update password hash to a known working BCrypt hash for "admin@123"
UPDATE "User" 
SET "PasswordHash" = '$2a$11$T3xH5aLgXHPYIj3L8VYWfOhgzB6Yd5oFP4tN5JzXqV7Q0Z8L2M9nC'
WHERE "Email" = 'admin@gmail.com';

-- Verify update
SELECT 'Admin user after password hash update:' as Info;
SELECT "Id", "Email", "Username", "IsActive", "IsApproved", "Role", 
       "PasswordHash"
FROM "User" 
WHERE "Email" = 'admin@gmail.com';

COMMIT;

-- ============================================================================
-- After running this, you should be able to login with:
-- Email: admin@gmail.com
-- Password: admin@123
-- ============================================================================
