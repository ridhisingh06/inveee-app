# 🔧 LOGIN 401 Error - Quick Troubleshooting Guide

**Commit**: 584b831  
**Status**: Fix deployed, waiting for GitHub Actions

---

## ⚡ WHAT WAS WRONG

Your backend was returning `401 Unauthorized` on login because:

**Most Likely Cause**: Admin user was never seeded in the production database

This happens when:
1. First deployment: DB connection failed during startup → seed was skipped
2. Subsequent deployments: Program checked `if (admin == null)` → user was never created
3. Every login returned "User not found" → 401 response

---

## ✅ WHAT WAS FIXED

Updated `backend/Program.cs` with:

1. **Forced connection test first**
   - Explicitly tests database with `SELECT 1` before proceeding
   - Retries 5 times with exponential backoff
   - **Fails fast** if connection impossible (instead of silently skipping seed)

2. **Detailed logging for every step**
   - Logs connection status, migration status, user creation status
   - Makes it obvious WHERE initialization failed

3. **Fail-fast behavior**
   - If DB connection fails → app startup fails immediately
   - App won't run in broken state
   - (Before: app would start but all logins would fail)

---

## 🚀 DEPLOYMENT STATUS

**GitHub Actions**: Automatically triggered when you pushed

Watch progress at: https://github.com/ridhisingh06/inveee-app/actions

**Steps happening now**:
1. Build backend Docker image
2. Push to ECR
3. Update ECS service with new task definition
4. ECS pulls new image and starts task
5. Container runs Program.cs → database initialization happens
6. App boots successfully (or fails loudly with logs)

**Expected time**: 7-11 minutes

---

## 🔍 HOW TO CHECK IF FIX WORKED

### Step 1: Check CloudWatch Logs (After deployment completes)

```powershell
# Get recent logs from ECS
aws logs get-log-events --log-group-name '/ecs/inveee-app' `
  --log-stream-name $(aws logs describe-log-streams --log-group-name '/ecs/inveee-app' --query 'logStreams[0].logStreamName' --output text) `
  --query 'events[*].message' --output text | tail -50
```

**Look for these SUCCESS messages**:
```
[DB Init] ✓ Database connection successful!
[DB Init] ✓ Migrations applied successfully.
[DB Init] ✓ ADMIN USER CREATED: ID=1, Email=admin@gmail.com
[DB Init] ✓ Database initialization COMPLETE
```

**If you see ERROR messages**:
```
[DB Init] ✗ FAILED to connect after 5 attempts!
[DB Init] ✗ ===== CRITICAL DATABASE INITIALIZATION ERROR =====
```

Then check:
- RDS is running: `aws rds describe-db-instances --query 'DBInstances[0].[DBInstanceIdentifier,DBInstanceStatus]'`
- Security group allows port 5432 from ECS tasks
- Connection string is correct in task-definition.json

### Step 2: Test Login Endpoint

```powershell
# Wait for ECS task to be HEALTHY (check every 10 seconds)
for ($i = 0; $i -lt 60; $i++) {
    $task = aws ecs describe-services --cluster inveee-cluster --services inveee-service `
        --query 'services[0].runningCount' --output text
    
    if ($task -eq "1") {
        Write-Host "✓ Task is running"
        break
    }
    
    Write-Host "Waiting for task to start... (attempt $($i+1)/60)"
    Start-Sleep -Seconds 10
}

# Test login
$response = Invoke-WebRequest -Uri "http://54.89.134.48:5000/api/auth/login" `
    -Method POST `
    -Headers @{"Content-Type" = "application/json"} `
    -Body '{"email":"admin@gmail.com","password":"admin@123"}' `
    -UseBasicParsing

$response.StatusCode
# Should be: 200 (success) or at least 401 (authentication error, not 404)

# If 200, check response body
$response.Content | ConvertFrom-Json
# Should contain: "token": "eyJhbGciOiJIUzI1NiIs..."
```

### Step 3: Test from Frontend

1. Open: https://inveee-app.vercel.app
2. Try login with:
   - Email: `admin@gmail.com`
   - Password: `admin@123`
3. Should see dashboard (no 401 error)

---

## 🔗 QUICK REFERENCE

| What | Where | Status |
|------|-------|--------|
| Backend API | http://54.89.134.48:5000 | Running |
| Frontend | https://inveee-app.vercel.app | Deployed |
| Health Check | http://54.89.134.48:5000/health | Should be `200 OK` |
| Login Endpoint | POST /api/auth/login | Should be `200 OK` with token |
| RDS Database | inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com | Running |
| ECS Task | inveee-cluster → inveee-service | Launching |
| CloudWatch Logs | /ecs/inveee-app | New logs should appear |

---

## ❌ IF LOGIN STILL RETURNS 401

### Scenario 1: Logs show connection failed
```
[DB Init] ✗ FAILED to connect after 5 attempts!
```

**Fix**:
```powershell
# Check RDS is running
aws rds describe-db-instances --db-instance-identifier inveee-postgres `
  --query 'DBInstances[0].[DBInstanceStatus,Endpoint.Address]'

# Check security group allows ECS to RDS
aws ec2 describe-security-groups --group-ids sg-xxxxx --query 'SecurityGroups[0].IpPermissions'

# Check connection string in task definition
aws ecs describe-task-definition --task-definition inveee-app-task `
  --query 'taskDefinition.containerDefinitions[0].environment[3]'
```

### Scenario 2: Logs show admin user created but still 401
```
[DB Init] ✓ ADMIN USER CREATED: ID=1, Email=admin@gmail.com
[DEBUG] Login attempt for email: admin@gmail.com
[DEBUG] Password match result: False
```

**Fix**: Password verification failing - likely hash format issue

```sql
-- Connect to RDS and check password hash
psql -h inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com \
     -U postgres -d inventorydb -c \
     "SELECT email, password_hash FROM \"Users\" WHERE email='admin@gmail.com';"

-- Hash should start with: $2b$10$
-- If it doesn't, update it
UPDATE "Users" 
SET password_hash = '$2b$10$abcd...' 
WHERE email = 'admin@gmail.com';
```

### Scenario 3: Still can't reach backend
```
curl: (7) Failed to connect to 54.89.134.48 port 5000: Connection refused
```

**Fix**:
```powershell
# Check ECS service status
aws ecs describe-services --cluster inveee-cluster --services inveee-service `
  --query 'services[0].[runningCount,desiredCount,deployments]'

# Check if task crashed
aws ecs list-tasks --cluster inveee-cluster --service-name inveee-service

# Describe the task
aws ecs describe-tasks --cluster inveee-cluster --tasks <task-arn> `
  --query 'tasks[0].[lastStatus,healthStatus,stoppedReason]'
```

---

## 📋 MANUAL VERIFICATION (If needed)

If you want to manually verify the admin user and password hash:

```powershell
# Generate correct BCrypt hash of "admin@123"
$password = "admin@123"
$hash = [System.Security.Cryptography.BCrypt+Sha512]::HashPassword($password)
Write-Host $hash

# Or use this .NET code
dotnet script -c "
using BCrypt.Net;
var hash = BCrypt.Net.BCrypt.HashPassword(\"admin@123\");
Console.WriteLine(hash);
var verified = BCrypt.Net.BCrypt.Verify(\"admin@123\", hash);
Console.WriteLine(\"Verified: \" + verified);
"
```

---

## ✅ SUCCESS CRITERIA

After deployment, you should see:

1. ✅ ECS task running and healthy
2. ✅ CloudWatch logs show database initialization succeeded
3. ✅ POST /api/auth/login returns 200 OK with JWT token
4. ✅ Frontend can login and see dashboard
5. ✅ No more 401 Unauthorized errors

---

**Deployment**: In progress  
**Expected completion**: ~10 minutes from push  
**Next step**: Wait for GitHub Actions to finish, then test login
