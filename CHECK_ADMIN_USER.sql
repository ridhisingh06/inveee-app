-- ============================================================================
-- CHECK CURRENT ADMIN USER STATE
-- ============================================================================
-- Database: inventorydb
-- This query shows the current state of the admin user account
-- ============================================================================

SELECT 
    "Id",
    "Email",
    "Username",
    "IsActive",
    "IsApproved",
    "Role",
    "PasswordHash",
    "CreatedAt"
FROM "User" 
WHERE "Email" = 'admin@gmail.com'
OR "Role" = 'ADMIN'
ORDER BY "CreatedAt" DESC;

-- Also check if there are any other users
SELECT 'Total users in database:' as Info, COUNT(*) as Count FROM "User";
