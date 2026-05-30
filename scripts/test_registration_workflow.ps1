$ProgressPreference = 'SilentlyContinue'
$BaseUrl = "http://127.0.0.1:5181"
$ErrorActionPreference = "Stop"

function Invoke-Http {
    param(
        [string]$Method,
        [string]$Path,
        [object]$BodyObject = $null,
        [string]$Token = $null
    )
    $headers = @{}
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }
    
    $params = @{
        Uri = "$BaseUrl$Path"
        Method = $Method
        Headers = $headers
        ContentType = "application/json"
    }
    
    if ($BodyObject) {
        $params["Body"] = ($BodyObject | ConvertTo-Json -Compress -Depth 10)
    }
    
    try {
        $json = Invoke-RestMethod @params
        return [pscustomobject]@{
            Success = $true
            Data = $json
        }
    } catch {
        $status = $null
        $errText = $_.Exception.Message
        if ($_.Exception -and $_.Exception.Response) {
            try {
                $status = $_.Exception.Response.StatusCode.Value__
                $stream = $_.Exception.Response.GetResponseStream()
                if ($stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $errText = $reader.ReadToEnd()
                }
            } catch {}
        }
        return [pscustomobject]@{
            Success = $false
            StatusCode = $status
            Error = $errText
        }
    }
}

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "TESTING REGISTRATION & LOGIN WORKFLOW" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Login as Admin to manage registrations
Write-Host "[1] Logging in as Admin..." -ForegroundColor Yellow
$adminLogin = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{
    email = "admin@gmail.com"
    password = "admin123"
}
if (-not $adminLogin.Success) {
    Write-Host "Admin login failed: $($adminLogin.Error)" -ForegroundColor Red
    exit 1
}
$adminToken = $adminLogin.Data.token
Write-Host "Admin logged in successfully!" -ForegroundColor Green

# 2. Register a new user
$testEmail = "pending_test_$(Get-Random)@example.com"
Write-Host "[2] Registering test user: $testEmail" -ForegroundColor Yellow
$regRes = Invoke-Http -Method "POST" -Path "/api/auth/register" -BodyObject @{
    username = "pending_test_user"
    email = $testEmail
    password = "Password123!"
    designation = "Engineer"
    departmentId = 2
    roleId = 2 # ISSUER
}
if (-not $regRes.Success) {
    Write-Host "Registration failed: $($regRes.Error)" -ForegroundColor Red
    exit 1
}
Write-Host "Registration successful! Message: $($regRes.Data.message)" -ForegroundColor Green

# Verify message matches exactly the required pending message
$expectedRegMsg = "Your registration is pending. Please wait for admin approval before signing in."
if ($regRes.Data.message -ne $expectedRegMsg) {
    Write-Host "BUG: Registration success message does not match requirement." -ForegroundColor Red
    Write-Host "Expected: '$expectedRegMsg'" -ForegroundColor Red
    Write-Host "Got: '$($regRes.Data.message)'" -ForegroundColor Red
    exit 1
}

# 3. Try to log in with pending user (correct password)
Write-Host "[3] Testing login for PENDING user (Correct Password)..." -ForegroundColor Yellow
$loginPending = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{
    email = $testEmail
    password = "Password123!"
}
if ($loginPending.Success) {
    Write-Host "BUG: Allowed login for PENDING user!" -ForegroundColor Red
    exit 1
}
Write-Host "Correctly blocked login for PENDING user. Status code: $($loginPending.StatusCode)" -ForegroundColor Green
Write-Host "Error message: $($loginPending.Error)" -ForegroundColor Green

# Verify message contains "Your account is not approved yet"
if (-not ($loginPending.Error -like "*Your account is not approved yet*")) {
    Write-Host "BUG: Expected 'Your account is not approved yet' in response" -ForegroundColor Red
    exit 1
}

# 4. Try to log in with pending user (incorrect password)
Write-Host "[4] Testing login for PENDING user (Wrong Password)..." -ForegroundColor Yellow
$loginPendingWrong = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{
    email = $testEmail
    password = "WrongPassword!"
}
if ($loginPendingWrong.Success) {
    Write-Host "BUG: Allowed login with wrong password!" -ForegroundColor Red
    exit 1
}
Write-Host "Correctly blocked login with incorrect password. Status code: $($loginPendingWrong.StatusCode)" -ForegroundColor Green
Write-Host "Error message: $($loginPendingWrong.Error)" -ForegroundColor Green
if (-not ($loginPendingWrong.Error -like "*Invalid email or password*")) {
    Write-Host "BUG: Expected 'Invalid email or password' in response" -ForegroundColor Red
    exit 1
}

# 5. Get pending list and verify
Write-Host "[5] Getting pending user list..." -ForegroundColor Yellow
$pendingList = Invoke-Http -Method "GET" -Path "/api/admin/pending-users" -Token $adminToken
$userRequest = $pendingList.Data.data | Where-Object { $_.email -eq $testEmail }
if (-not $userRequest) {
    Write-Host "BUG: Registered user is not present in the pending list!" -ForegroundColor Red
    exit 1
}
Write-Host "Found user in pending requests. Request ID: $($userRequest.id), Status: $($userRequest.status), Role: $($userRequest.role)" -ForegroundColor Green

# 6. Reject user
Write-Host "[6] Declining/Rejecting request ID: $($userRequest.id)" -ForegroundColor Yellow
$rejectRes = Invoke-Http -Method "PUT" -Path "/api/admin/reject/$($userRequest.id)" -Token $adminToken
if (-not $rejectRes.Success) {
    Write-Host "Rejection failed: $($rejectRes.Error)" -ForegroundColor Red
    exit 1
}
Write-Host "Rejection successful! Message: $($rejectRes.Data.message)" -ForegroundColor Green

# 7. Try to log in with rejected user (correct password)
Write-Host "[7] Testing login for REJECTED user (Correct Password)..." -ForegroundColor Yellow
$loginRejected = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{
    email = $testEmail
    password = "Password123!"
}
if ($loginRejected.Success) {
    Write-Host "BUG: Allowed login for REJECTED user!" -ForegroundColor Red
    exit 1
}
Write-Host "Correctly blocked login for REJECTED user. Status code: $($loginRejected.StatusCode)" -ForegroundColor Green
Write-Host "Error message: $($loginRejected.Error)" -ForegroundColor Green
if (-not ($loginRejected.Error -like "*Your registration was rejected*")) {
    Write-Host "BUG: Expected 'Your registration was rejected' in response" -ForegroundColor Red
    exit 1
}

# 8. Try to log in with rejected user (incorrect password)
Write-Host "[8] Testing login for REJECTED user (Wrong Password)..." -ForegroundColor Yellow
$loginRejectedWrong = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{
    email = $testEmail
    password = "WrongPassword!"
}
if ($loginRejectedWrong.Success) {
    Write-Host "BUG: Allowed login with wrong password!" -ForegroundColor Red
    exit 1
}
Write-Host "Correctly blocked login with incorrect password. Status code: $($loginRejectedWrong.StatusCode)" -ForegroundColor Green
Write-Host "Error message: $($loginRejectedWrong.Error)" -ForegroundColor Green
if (-not ($loginRejectedWrong.Error -like "*Invalid email or password*")) {
    Write-Host "BUG: Expected 'Invalid email or password' in response" -ForegroundColor Red
    exit 1
}

# 9. Register a second user to test approval and login
$approveEmail = "approve_test_$(Get-Random)@example.com"
Write-Host "[9] Registering second test user to approve: $approveEmail" -ForegroundColor Yellow
$regRes2 = Invoke-Http -Method "POST" -Path "/api/auth/register" -BodyObject @{
    username = "approve_test_user"
    email = $approveEmail
    password = "ApprovePassword123!"
    designation = "Analyst"
    departmentId = 3
    roleId = 2 # ISSUER
}
if (-not $regRes2.Success) {
    Write-Host "Registration 2 failed: $($regRes2.Error)" -ForegroundColor Red
    exit 1
}

$pendingList2 = Invoke-Http -Method "GET" -Path "/api/admin/pending-users" -Token $adminToken
$userRequest2 = $pendingList2.Data.data | Where-Object { $_.email -eq $approveEmail }

Write-Host "Approving request ID: $($userRequest2.id)" -ForegroundColor Yellow
$approveRes = Invoke-Http -Method "PUT" -Path "/api/admin/approve/$($userRequest2.id)" -Token $adminToken -BodyObject @{
    roleId = 2
    departmentId = 3
}
if (-not $approveRes.Success) {
    Write-Host "Approval failed: $($approveRes.Error)" -ForegroundColor Red
    exit 1
}
Write-Host "Approval successful!" -ForegroundColor Green

# 10. Login with approved user
Write-Host "[10] Testing login for APPROVED user (Correct Password)..." -ForegroundColor Yellow
$loginApprove = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{
    email = $approveEmail
    password = "ApprovePassword123!"
}
if (-not $loginApprove.Success) {
    Write-Host "BUG: Failed login for APPROVED user! Error: $($loginApprove.Error)" -ForegroundColor Red
    exit 1
}
Write-Host "Successfully logged in approved user!" -ForegroundColor Green
$userToken = $loginApprove.Data.token

# Verify role in token is ISSUER (Role 2)
# Decode token (split by dot, second part is base64 payload)
$payloadBase64 = $userToken.Split(".")[1]
# Pad payload if necessary
while ($payloadBase64.Length % 4) { $payloadBase64 += "=" }
$decodedPayloadBytes = [System.Convert]::FromBase64String($payloadBase64)
$decodedPayloadString = [System.Text.Encoding]::UTF8.GetString($decodedPayloadBytes)
Write-Host "Decoded token claims: $decodedPayloadString" -ForegroundColor Cyan

if (-not ($decodedPayloadString -match 'role":"ISSUER"')) {
    Write-Host "BUG: Role in token is not ISSUER! Got payload: $decodedPayloadString" -ForegroundColor Red
    exit 1
}
Write-Host "Verified role is ISSUER successfully!" -ForegroundColor Green

Write-Host "==========================================" -ForegroundColor Green
Write-Host "🎉 ALL REGISTRATION E2E TESTS PASSED!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
