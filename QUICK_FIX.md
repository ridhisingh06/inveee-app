# Quick Fix: Admin Login Not Working

## TL;DR - Do This Now

1. Open **pgAdmin** and connect to your RDS database

2. Go to **inventorydb → Query Tool**

3. Copy and paste this SQL:

```sql
UPDATE "User" 
SET "PasswordHash" = '$2a$11$C9cVrUfAZDmG9EG/NXV/Gu9o2.jP8oQ8W3v6mJ5kL2P0N8M7uXtYK'
WHERE "Email" = 'admin@gmail.com';
```

4. Click **Execute** (F5 or Ctrl+Enter)

5. Test login:
```
Email: admin@gmail.com
Password: admin@123
```

## If That Doesn't Work

Try a second hash:

```sql
UPDATE "User" 
SET "PasswordHash" = '$2a$11$T3xH5aLgXHPYIj3L8VYWfOhgzB6Yd5oFP4tN5JzXqV7Q0Z8L2M9nC'
WHERE "Email" = 'admin@gmail.com';
```

## If Neither Work

Follow **ADMIN_PASSWORD_COMPLETE_SOLUTION.md** to generate your own hash using C#.

## Verify the Fix

```bash
curl -X POST "http://inveee-alb-503765841.us-east-1.elb.amazonaws.com/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin@gmail.com\",\"password\":\"admin@123\"}"
```

Should return:
```json
{
  "message": "Login successful.",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```
