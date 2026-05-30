$ProgressPreference = 'SilentlyContinue'
$BaseUrl = "http://localhost:5181"
$ErrorActionPreference = "Stop"

# Helper for HTTP JSON Requests
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

Write-Host "Starting E2E Request Workflow Validation..." -ForegroundColor Cyan

# 1. Login as Admin
Write-Host "Logging in as Admin..." -ForegroundColor Yellow
$adminLogin = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{
    email = "admin@gmail.com"
    password = "admin@123"
}
if (-not $adminLogin.Success) {
    Write-Host "Admin login failed: $($adminLogin.Error)" -ForegroundColor Red
    exit 1
}
$adminToken = $adminLogin.Data.token
Write-Host "Admin logged in successfully!" -ForegroundColor Green

# 2. Register Test User
$userEmail = "user_e2e_$(Get-Random)@example.com"
Write-Host "Registering test user: $userEmail" -ForegroundColor Yellow
$userReg = Invoke-Http -Method "POST" -Path "/api/auth/register" -BodyObject @{
    username = "e2e_user"
    email = $userEmail
    password = "UserPassword123!"
    designation = "Staff"
    departmentId = 2
    roleId = 1 # USER
}
if (-not $userReg.Success) {
    Write-Host "User registration failed: $($userReg.Error)" -ForegroundColor Red
    exit 1
}

# 3. Register Test Issuer
$issuerEmail = "issuer_e2e_$(Get-Random)@example.com"
Write-Host "Registering test issuer: $issuerEmail" -ForegroundColor Yellow
$issuerReg = Invoke-Http -Method "POST" -Path "/api/auth/register" -BodyObject @{
    username = "e2e_issuer"
    email = $issuerEmail
    password = "IssuerPassword123!"
    designation = "Issuer Agent"
    departmentId = 2
    roleId = 2 # ISSUER
}
if (-not $issuerReg.Success) {
    Write-Host "Issuer registration failed: $($issuerReg.Error)" -ForegroundColor Red
    exit 1
}

# 4. Approve Test User and Test Issuer
Write-Host "Fetching pending registrations..." -ForegroundColor Yellow
$pending = Invoke-Http -Method "GET" -Path "/api/admin/pending-users" -Token $adminToken
$userReq = $pending.Data.data | Where-Object { $_.email -eq $userEmail }
$issuerReq = $pending.Data.data | Where-Object { $_.email -eq $issuerEmail }

Write-Host "Approving user registration ID: $($userReq.id)" -ForegroundColor Yellow
$userApprove = Invoke-Http -Method "PUT" -Path "/api/admin/approve/$($userReq.id)" -Token $adminToken -BodyObject @{
    roleId = 1
    departmentId = 2
}
if (-not $userApprove.Success) {
    Write-Host "User approval failed: $($userApprove.Error)" -ForegroundColor Red
    exit 1
}

Write-Host "Approving issuer registration ID: $($issuerReq.id)" -ForegroundColor Yellow
$issuerApprove = Invoke-Http -Method "PUT" -Path "/api/admin/approve/$($issuerReq.id)" -Token $adminToken -BodyObject @{
    roleId = 2
    departmentId = 2
}
if (-not $issuerApprove.Success) {
    Write-Host "Issuer approval failed: $($issuerApprove.Error)" -ForegroundColor Red
    exit 1
}

# 5. Login as Test User
Write-Host "Logging in as Test User..." -ForegroundColor Yellow
$userLogin = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{
    email = $userEmail
    password = "UserPassword123!"
}
if (-not $userLogin.Success) {
    Write-Host "User login failed: $($userLogin.Error)" -ForegroundColor Red
    exit 1
}
$userToken = $userLogin.Data.token
Write-Host "User token role claim: $($userLogin.Data.role)" -ForegroundColor Green

# 6. Check user can request
Write-Host "Checking if user can create request..." -ForegroundColor Yellow
$canReq1 = Invoke-Http -Method "GET" -Path "/api/request/can-request" -Token $userToken
Write-Host "Can Request status: $($canReq1.Data.canRequest) - $($canReq1.Data.message)" -ForegroundColor Green
if ($canReq1.Data.canRequest -ne $true) {
    Write-Host "Expected canRequest to be true" -ForegroundColor Red
    exit 1
}

# Seed inventory item to ensure we have stock
Write-Host "Getting item inventory list..." -ForegroundColor Yellow
$inventory = Invoke-Http -Method "GET" -Path "/api/inventory" -Token $userToken
# Let's find an item that has stock
$testItem = $inventory.Data | Where-Object { $_.availableQuantity -gt 0 } | Select-Object -First 1
if (-not $testItem) {
    Write-Host "No active inventory items with stock found. Please seed items first." -ForegroundColor Red
    exit 1
}
Write-Host "Selected Item: $($testItem.name) (ID: $($testItem.id), Stock: $($testItem.availableQuantity))" -ForegroundColor Green

# 7. Create request
Write-Host "Creating request for item: $($testItem.name) Qty: 1" -ForegroundColor Yellow
$createReq = Invoke-Http -Method "POST" -Path "/api/request" -Token $userToken -BodyObject @{
    items = @(
        @{ itemId = $testItem.id; quantity = 1 }
    )
}
if (-not $createReq.Success) {
    Write-Host "Failed to create request: $($createReq.Error)" -ForegroundColor Red
    exit 1
}
$reqId = $createReq.Data.id
Write-Host "Request submitted successfully! Request ID: $reqId" -ForegroundColor Green

# 8. Check restriction: user cannot request when a request is active
Write-Host "Verifying user restriction (cannot request while one is active)..." -ForegroundColor Yellow
$canReq2 = Invoke-Http -Method "GET" -Path "/api/request/can-request" -Token $userToken
Write-Host "Can Request status (expected false): $($canReq2.Data.canRequest) - $($canReq2.Data.message)" -ForegroundColor Green
if ($canReq2.Data.canRequest -eq $true) {
    Write-Host "BUG: User allowed to check/create requests with an active request!" -ForegroundColor Red
    exit 1
}

# Try creating another request - should fail
$secondReq = Invoke-Http -Method "POST" -Path "/api/request" -Token $userToken -BodyObject @{
    items = @(
        @{ itemId = $testItem.id; quantity = 1 }
    )
}
if ($secondReq.Success) {
    Write-Host "BUG: User successfully created second request when one is active!" -ForegroundColor Red
    exit 1
} else {
    Write-Host "Correctly blocked second request: $($secondReq.Error)" -ForegroundColor Green
}

# 9. Login as Test Issuer
Write-Host "Logging in as Test Issuer..." -ForegroundColor Yellow
$issuerLogin = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{
    email = $issuerEmail
    password = "IssuerPassword123!"
}
if (-not $issuerLogin.Success) {
    Write-Host "Issuer login failed: $($issuerLogin.Error)" -ForegroundColor Red
    exit 1
}
$issuerToken = $issuerLogin.Data.token

# 10. Issuer issues the request
Write-Host "Issuer issuing request ID: $reqId" -ForegroundColor Yellow
$issueRes = Invoke-Http -Method "PUT" -Path "/api/issuer/request/$reqId/issue" -Token $issuerToken
if (-not $issueRes.Success) {
    Write-Host "Failed to issue request: $($issueRes.Error)" -ForegroundColor Red
    exit 1
}
Write-Host "Request issued successfully by issuer!" -ForegroundColor Green

# 11. Login as Admin & approve
Write-Host "Admin approving request ID: $reqId" -ForegroundColor Yellow
$adminApproveReq = Invoke-Http -Method "PUT" -Path "/api/admin/request/$reqId/approve" -Token $adminToken
if (-not $adminApproveReq.Success) {
    Write-Host "Failed to approve request: $($adminApproveReq.Error)" -ForegroundColor Red
    exit 1
}
Write-Host "Request approved successfully by Admin!" -ForegroundColor Green

# 12. User confirms receipt
Write-Host "User confirming receipt of request ID: $reqId" -ForegroundColor Yellow
$userConfirm = Invoke-Http -Method "POST" -Path "/api/request/$reqId/confirm-received" -Token $userToken
if (-not $userConfirm.Success) {
    Write-Host "Failed to confirm receipt: $($userConfirm.Error)" -ForegroundColor Red
    exit 1
}
Write-Host "Receipt confirmed successfully by User!" -ForegroundColor Green

# 13. Verify restriction is cleared and user can request again
Write-Host "Verifying user can request again after RECEIVED status..." -ForegroundColor Yellow
$canReq3 = Invoke-Http -Method "GET" -Path "/api/request/can-request" -Token $userToken
Write-Host "Can Request status (expected true): $($canReq3.Data.canRequest) - $($canReq3.Data.message)" -ForegroundColor Green
if ($canReq3.Data.canRequest -ne $true) {
    Write-Host "Expected canRequest to be true after request flow is completed" -ForegroundColor Red
    exit 1
}

Write-Host "🎉 ALL E2E WORKFLOW TESTS PASSED FLawlessly!" -ForegroundColor Green
