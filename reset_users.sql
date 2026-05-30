TRUNCATE TABLE "Users", "RegistrationRequests", "UserRoles" RESTART IDENTITY CASCADE;
SELECT 'Users table cleared.' AS status;
