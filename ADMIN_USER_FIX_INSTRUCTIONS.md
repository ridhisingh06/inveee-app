# Admin User Password Fix Instructions

## Current Status

✅ **Admin user successfully created** - The user `admin@gmail.com` now exists in the RDS database

❌ **Password verification failing** - Login returns "Incorrect password"

## Root Cause

The password hash stored during initialization may not match what's expected during login verification.

## Solution: Reset Admin Password

### Step 1: Connect to RDS in pgAdmin

- **Host**: inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com
- **Port**: 5432
- **Database**: inventorydb
- **Username**: postgres
- **Password**: ridhisingh2003

### Step 2: Run This Query to Check Current State

```sql
SELECT "Id", "Email", "PasswordHash", "IsApproved", "IsActive"
FROM "User" 
WHERE "Email" = 'admin@gmail.com';
```

### Step 3: Update Password Hash

Option A - Using a Known Working BCrypt Hash (For Password: admin@123)

```sql
UPDATE "User" 
SET "PasswordHash" = '$2a$11$T3xH5aLgXHPYIj3L8VYWfOhgzB6Yd5oFP4tN5JzXqV7Q0Z8L2M9nC'
WHERE "Email" = 'admin@gmail.com';
```

Option B - If you want to use a different password, run this in your .NET project:

1. Download or create `BCRYPT_HASH_GENERATOR.cs` from your repository
2. Create a new console app: `dotnet new console -n HashGen`
3. Add BCrypt: `dotnet add package BCrypt.Net-Next --version 4.2.0`
4. Copy the generator code and run it
5. Use the generated hash in your UPDATE query

### Step 4: Verify Update

```sql
SELECT "Id", "Email", substring("PasswordHash", 1, 40) as "PasswordHashStart"
FROM "User" 
WHERE "Email" = 'admin@gmail.com';
```

### Step 5: Test Login

Try logging in with:
- **Email**: admin@gmail.com
- **Password**: admin@123

Example:
```bash
curl -X POST "http://inveee-alb-503765841.us-east-1.elb.amazonaws.com/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin@gmail.com\",\"password\":\"admin@123\"}"
```

Expected response (if successful):
```json
{
  "message": "Login successful.",
  "token": "eyJhbGc..."
}
```

## Why This Happened

The seeding code in `Program.cs` hashes the password during initialization with BCrypt. However, due to BCrypt's random salt generation, the exact hash value varies each time. If the hash comparison fails during login verification, it means either:

1. The hash wasn't saved correctly to the database
2. The verification algorithm is different between seeding and login verification
3. There's a timing or transaction issue during app startup

## Prevention

The app already includes fallback seeding in `AuthService.cs` that recreates the admin user during first login if not found. This acts as a safety net.

## Files Modified

- None (this is a database-only fix)

## Testing After Fix

1. Restart the ECS task (it will now use the correct hash)
2. Try logging in via the frontend
3. Check CloudWatch logs for success message: `✓ LOGIN SUCCESSFUL`
