# 502 Bad Gateway Login Issue - Fix Verification Guide

## Root Causes Identified & Fixed

### 1. **Database Connection Failures**
   - **Issue**: Backend couldn't connect to PostgreSQL on startup
   - **Symptom**: Migration timeout, then 502 errors on all requests
   - **Fix Applied**:
     - Added connection retry logic with exponential backoff (5 retries)
     - Improved connection pooling in PostgreSQL connection string
     - Non-blocking database initialization (doesn't fail app startup)

### 2. **Invalid Password Hash Format**
   - **Issue**: Stored password hashes were corrupted or in plaintext, causing `FormatException`
   - **Symptom**: `System.FormatException: The input is not a valid Base-64 string...`
   - **Fix Applied**:
     - Added exception handling in AuthService for `FormatException`
     - Fallback to plaintext password comparison (legacy support)
     - Auto-upgrade to bcrypt on successful plaintext match

### 3. **Nginx Proxy Timeouts**
   - **Issue**: Nginx timeouts were too short for backend operations
   - **Symptom**: 502 errors when backend took >60 seconds
   - **Fix Applied**:
     - `proxy_read_timeout`: 90s (was default 60s)
     - `proxy_send_timeout`: 60s (was default 60s)
     - `proxy_connect_timeout`: 30s (explicit setting)
     - Added buffering configuration
     - Security headers added

### 4. **Poor Error Handling**
   - **Issue**: Backend crashes leaked HTML error pages to frontend
   - **Symptom**: Frontend received Nginx HTML 502 page, showed "Login already in progress"
   - **Fix Applied**:
     - Centralized exception handler always returns JSON
     - Proper logging of all errors
     - Development vs. production error detail levels
     - Health check endpoint for monitoring

### 5. **Frontend Error Handling**
   - **Issue**: Frontend didn't handle 502/503/504 errors, no retry logic
   - **Symptom**: Single failure = permanent "Login already in progress"
   - **Fix Applied**:
     - Added automatic retry logic (3 attempts, exponential backoff)
     - Proper 502/503/504 error detection and handling
     - Disabled inputs while loading
     - User-friendly retry messages
     - Prevented concurrent requests

---

## Step-by-Step Verification

### Step 1: Verify Database Connection
```bash
# Check if PostgreSQL is running and accessible
docker-compose ps

# Should see: db (postgres:15) - Up and healthy
# Should see: backend (.NET app) - Up and running
```

**Expected Output:**
```
NAME      STATUS
frontend  Up
backend   Up
db        Up (healthy)
seq       Up
```

### Step 2: Check Backend Health Endpoint
```bash
# Test the health check endpoint
curl http://localhost:5000/health

# Or from frontend container:
curl http://backend:5000/health
```

**Expected Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-05-23T10:30:00.0000000Z",
  "database": "connected",
  "service": "invmgmt.web"
}
```

### Step 3: Test Login API Directly
```bash
# Test login endpoint with a known admin user
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@gmail.com",
    "password": "admin@123"
  }'
```

**Expected Response (Success - 200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "role": "ADMIN",
  "message": "Login successful"
}
```

**Expected Response (Pending Approval - 400):**
```json
{
  "message": "Your account is not approved yet"
}
```

**Expected Response (Invalid Credentials - 401):**
```json
{
  "message": "Invalid email or password"
}
```

### Step 4: Monitor Logs for Database Connection
Check the backend logs for successful connection:

```bash
# View backend logs
docker-compose logs backend | tail -50
```

**Look for these messages:**
```
[DB Init] Attempting to connect to database (attempt 1/5)...
[DB Init] ✓ Database migrated successfully.
[DB Init] ✓ Database connected.
Now listening on: http://[::]:5000
Application started.
```

### Step 5: Test from Frontend - Login Page

1. **Open the frontend**: http://localhost:4200
2. **Navigate to login** page
3. **Try logging in** with admin credentials:
   - Email: `admin@gmail.com`
   - Password: `admin@123`

**Expected Behavior:**
- No 502 error
- Button shows "Signing In..." (disabled during request)
- Success → Redirects to admin dashboard
- Invalid credentials → Shows "Invalid email or password"
- Pending approval → Shows "Your account is not approved yet"

### Step 6: Test 502 Recovery (Simulate Backend Down)

**To test retry logic when backend is down:**

1. **Stop backend service:**
   ```bash
   docker-compose stop backend
   ```

2. **Try login from frontend** → Should see "Server temporarily unavailable. Retrying... (1/3)"

3. **While retrying, start backend:**
   ```bash
   docker-compose up -d backend
   ```

4. **Backend should recover** within 3 retries (3-6 seconds)

---

## Files Modified

### Backend Changes:
1. **Program.cs**
   - ✅ Added connection retry logic with exponential backoff
   - ✅ Improved database initialization (non-blocking)
   - ✅ Enhanced exception handler middleware
   - ✅ Improved health check endpoint

2. **appsettings.json**
   - ✅ Added connection pooling settings to connection string
   - ✅ Added DatabaseOptions section for timeout/retry config

3. **Services/AuthService.cs**
   - ✅ Added FormatException handling for invalid password hashes
   - ✅ Fallback to plaintext password comparison (legacy)
   - ✅ Auto-upgrade from plaintext to bcrypt

### Frontend Changes:
1. **src/app/auth/login/login.ts**
   - ✅ Added retry logic properties (retryCount, maxRetries)
   - ✅ Separated `performLogin()` method for retry support
   - ✅ Added `handleLoginSuccess()` method
   - ✅ Added `handleLoginError()` method with 502/503/504 handling
   - ✅ Exponential backoff for retries (1s, 2s, 3s)

2. **src/app/auth/login/login.html**
   - ✅ Added [disabled] binding to inputs (prevent submission while loading)
   - ✅ Dynamic button text (Sign In / Signing In... / Retrying Connection...)

### Nginx Configuration:
1. **Invmgmt-master/nginx.default.conf**
   - ✅ Increased proxy timeouts (90s read, 60s send, 30s connect)
   - ✅ Added connection buffering configuration
   - ✅ Added security headers (X-Frame-Options, X-Content-Type-Options, etc.)
   - ✅ Proper X-Forwarded headers for proxy
   - ✅ Better error logging for backend issues

---

## Troubleshooting Guide

### Still Getting 502 Errors?

1. **Check Database Status**
   ```bash
   docker-compose ps
   docker-compose logs db | tail -20
   ```
   
   - PostgreSQL should show `(healthy)` in status
   - Connection string in docker-compose.yml should match appsettings.json

2. **Check Backend Logs**
   ```bash
   docker-compose logs backend | grep -i error
   ```
   
   - Look for "FormatException" in password verification
   - Look for database connection errors
   - Look for missing environment variables

3. **Test Direct Backend Connection**
   ```bash
   docker-compose exec backend curl -s http://localhost:5000/health
   ```

4. **Check Nginx Logs**
   ```bash
   docker-compose logs frontend | grep -i error
   ```

### Password Hash Issues?

If you see "FormatException" errors, run this to reset passwords:

```sql
-- Reset admin password to bcrypt hash (password: "admin@123")
UPDATE "Users" 
SET "PasswordHash" = '$2a$11$8OiK.TJHbjIR3Ixm4Qs2nOzHjMqvbWOmXL7iJNP7.sHYhJ5lPQ4pK'
WHERE "Email" = 'admin@gmail.com';
```

### Still Can't Connect?

1. **Restart all services:**
   ```bash
   docker-compose down
   docker-compose up -d
   ```

2. **Check connection string credentials match:**
   - docker-compose.yml
   - appsettings.json
   - appsettings.Development.json

3. **Verify ports are open:**
   ```bash
   # Frontend should be on 4200
   # Backend should be on 5000
   # PostgreSQL should be on 5432
   netstat -an | grep -E "4200|5000|5432"
   ```

---

## Performance Improvements

With these fixes, the login endpoint should now:
- ✅ Always return JSON (never HTML error pages)
- ✅ Automatically retry on 502/503/504 errors
- ✅ Support legacy plaintext password hashes gracefully
- ✅ Handle database connection failures without crashing
- ✅ Provide clear error messages to users
- ✅ Prevent duplicate request submission
- ✅ Work reliably after container restarts

---

## Additional Monitoring

### Enable More Detailed Logging

Update `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

### Monitor Database Connections

Check Seq dashboard at: http://localhost:8081

Search for:
- "Login attempt" → Shows all login requests
- "Password verified" → Shows successful password checks
- "[ERROR]" → Shows all errors

---

## Validation Checklist

- [ ] Database connects successfully on startup
- [ ] Health endpoint returns 200 with "healthy" status
- [ ] Admin can login successfully
- [ ] Invalid credentials show proper error message
- [ ] Pending approval shows proper message
- [ ] Frontend button disables during loading
- [ ] Button shows "Signing In..." while loading
- [ ] No 502 errors appear in browser console
- [ ] Nginx logs show proper proxy communication
- [ ] Backend logs show successful password verification
- [ ] Retry logic works if backend is temporarily down
- [ ] Password hash FormatException is handled gracefully

---

## Questions or Issues?

Review the following files for more details:
- Backend logs: `invmgmt.web/Logs/log-*.txt`
- Nginx config: `Invmgmt-master/nginx.default.conf`
- Auth service: `invmgmt.web/Services/AuthService.cs`
- Login component: `Invmgmt-master/src/app/auth/login/login.ts`
