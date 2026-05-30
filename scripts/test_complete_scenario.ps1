$ProgressPreference = 'SilentlyContinue'
$BaseUrl = "http://localhost:5001"
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
        UseBasicParsing = $true
    }
    
    if ($BodyObject) {
        $params["Body"] = ($BodyObject | ConvertTo-Json -Compress -Depth 10)
    }
    
    try {
        $response = Invoke-WebRequest @params
        $json = $null
        if ($response.Content) {
            $json = $response.Content | ConvertFrom-Json
        }
        return [pscustomobject]@{
            Success = $true
            StatusCode = $response.StatusCode
            Data = $json
        }
    } catch {
        $status = $null
        $errText = $_.Exception.Message
        if ($_.Exception -and $_.Exception.Response) {
            try {
                $status = [int]$_.Exception.Response.StatusCode
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

$results = New-Object System.Collections.Generic.List[object]
function Add-Verification([string]$Step, [string]$Requirement, [bool]$Passed, [string]$Details = "") {
    $results.Add([pscustomobject]@{ Step = $Step; Requirement = $Requirement; Passed = $Passed; Details = $Details }) | Out-Null
    $tag = if ($Passed) { "PASS" } else { "FAIL" }
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host ("[{0}] Step {1}: {2} - {3}" -f $tag, $Step, $Requirement, $Details) -ForegroundColor $color
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "STARTING E2E ROLE-BASED WORKFLOW VALIDATION" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 0. Clean up previous test user from database to ensure fresh run
Write-Host "Cleaning up previous test users in the database..." -ForegroundColor Yellow
$cleanupCmd = 'docker exec -i inveeer-db-1 psql -U postgres -d InvMgmtDb -c "DELETE FROM \"Users\" WHERE \"Email\" = ''testuser1@example.com''; DELETE FROM \"RegistrationRequests\" WHERE \"Email\" = ''testuser1@example.com'';"'
Invoke-Expression $cleanupCmd | Out-Null
Write-Host "Cleanup complete." -ForegroundColor Green

# 1. Register a new user with valid details
$regPayload = @{
    Username = "Test User"
    Email = "testuser1@example.com"
    Password = "Test@123"
    RoleId = 1 # USER
    DepartmentId = 2 # IT
    Designation = "Engineer"
}
Write-Host "Registering Test User (testuser1@example.com)..." -ForegroundColor Yellow
$regResult = Invoke-Http -Method "POST" -Path "/api/auth/register" -BodyObject $regPayload
$regPassed = $regResult.Success -and $regResult.StatusCode -eq 200 -and $regResult.Data.message -match "pending"
Add-Verification -Step "1" -Requirement "Register new user with valid details" -Passed $regPassed -Details "StatusCode: $($regResult.StatusCode), Message: $($regResult.Data.message)"

# 2. Verify registration request created & user cannot login before approval
Write-Host "Testing login for unapproved user..." -ForegroundColor Yellow
$loginPayload = @{
    Email = "testuser1@example.com"
    Password = "Test@123"
}
$loginBeforeApprove = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject $loginPayload
$loginBlocked = ($loginBeforeApprove.Success -eq $false) -and ($loginBeforeApprove.StatusCode -eq 403) -and ($loginBeforeApprove.Error -match "pending")
Add-Verification -Step "2.1" -Requirement "User cannot login before approval" -Passed $loginBlocked -Details "Blocked correctly: $loginBlocked (StatusCode: $($loginBeforeApprove.StatusCode), Error: $($loginBeforeApprove.Error))"

# 3. Login as Admin and verify request appears in pending list
Write-Host "Logging in as Admin..." -ForegroundColor Yellow
$adminLogin = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{ Email = "admin@gmail.com"; Password = "admin@123" }
$adminToken = $adminLogin.Data.token
$adminLoggedIn = $adminLogin.Success -and ($null -ne $adminToken)
Add-Verification -Step "3.1" -Requirement "Login as Admin using seeded credentials" -Passed $adminLoggedIn -Details "Token present: $($null -ne $adminToken)"

Write-Host "Fetching pending registrations..." -ForegroundColor Yellow
$pendingUsers = Invoke-Http -Method "GET" -Path "/api/admin/pending-users" -Token $adminToken
$userRequest = $pendingUsers.Data.data | Where-Object { $_.Email -eq "testuser1@example.com" }
$requestInPendingList = ($null -ne $userRequest) -and ($userRequest.Status -eq "Pending")
Add-Verification -Step "2.2" -Requirement "Request appears in Admin pending registrations" -Passed $requestInPendingList -Details "Found request with ID: $($userRequest.Id) in pending list"

# 4. Approve Test User
Write-Host "Approving Test User (ID: $($userRequest.Id))..." -ForegroundColor Yellow
$approveResult = Invoke-Http -Method "PUT" -Path "/api/admin/approve/$($userRequest.Id)" -Token $adminToken
$approvePassed = $approveResult.Success -and $approveResult.StatusCode -eq 200
Add-Verification -Step "3.2" -Requirement "Admin approves Test User" -Passed $approvePassed -Details "Approve response message: $($approveResult.Data.message)"

# 5. Verify user status changes to approved, can login, and dashboard loads
Write-Host "Testing login for newly approved user..." -ForegroundColor Yellow
$userLogin = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject $loginPayload
$userToken = $userLogin.Data.token
$loginSuccess = $userLogin.Success -and ($null -ne $userToken)
Add-Verification -Step "4.1" -Requirement "User status approved & can login successfully" -Passed $loginSuccess -Details "Token obtained successfully"

Write-Host "Probing user dashboard (can-request & inventory)..." -ForegroundColor Yellow
$canReqCheck = Invoke-Http -Method "GET" -Path "/api/requests/can-request" -Token $userToken
$inventoryCheck = Invoke-Http -Method "GET" -Path "/api/inventory" -Token $userToken
$dashboardLoads = $canReqCheck.Success -and $canReqCheck.Data.canRequest -and $inventoryCheck.Success
Add-Verification -Step "4.2" -Requirement "User dashboard loads correctly" -Passed $dashboardLoads -Details "canRequest: $($canReqCheck.Data.canRequest), Inventory count: $($inventoryCheck.Data.Count)"

# 6. Login as approved user and request Laptop, Mouse, Keyboard, Monitor
# Let's map items from inventory response
$laptopItem = $inventoryCheck.Data | Where-Object { $_.name -eq "Laptop" }
$mouseItem = $inventoryCheck.Data | Where-Object { $_.name -eq "Mouse" }
$keyboardItem = $inventoryCheck.Data | Where-Object { $_.name -eq "Keyboard" }
$monitorItem = $inventoryCheck.Data | Where-Object { $_.name -eq "Monitor" }

$mappedAllItems = ($null -ne $laptopItem) -and ($null -ne $mouseItem) -and ($null -ne $keyboardItem) -and ($null -ne $monitorItem)
Add-Verification -Step "5.1" -Requirement "Find Laptop, Mouse, Keyboard, Monitor in Inventory" -Passed $mappedAllItems -Details "LaptopId=$($laptopItem.id), MouseId=$($mouseItem.id), KeyboardId=$($keyboardItem.id), MonitorId=$($monitorItem.id)"

Write-Host "Creating item request for the 4 items..." -ForegroundColor Yellow
$createRequestPayload = @{
    CategoryId = 2 # IT Related
    Items = @(
        @{ ItemId = $laptopItem.id; Quantity = 1 },
        @{ ItemId = $mouseItem.id; Quantity = 1 },
        @{ ItemId = $keyboardItem.id; Quantity = 1 },
        @{ ItemId = $monitorItem.id; Quantity = 1 }
    )
}
$requestSubmit = Invoke-Http -Method "POST" -Path "/api/requests" -BodyObject $createRequestPayload -Token $userToken
$requestId = $requestSubmit.Data.id
$requestSubmitted = $requestSubmit.Success -and ($null -ne $requestId)
Add-Verification -Step "5.2" -Requirement "Request submitted successfully" -Passed $requestSubmitted -Details "Request ID created: $requestId"

# 7. Verify request visible in Issuer dashboard, status = Pending (PendingWithIssuer)
Write-Host "Logging in as Issuer..." -ForegroundColor Yellow
$issuerLogin = Invoke-Http -Method "POST" -Path "/api/auth/login" -BodyObject @{ Email = "issuer1@gmail.com"; Password = "admin@123" }
$issuerToken = $issuerLogin.Data.token
$issuerLoggedIn = $issuerLogin.Success -and ($null -ne $issuerToken)
Add-Verification -Step "7.1" -Requirement "Login as Issuer using seeded credentials" -Passed $issuerLoggedIn -Details "Token present: $($null -ne $issuerToken)"

Write-Host "Fetching requests for Issuer..." -ForegroundColor Yellow
$issuerRequests = Invoke-Http -Method "GET" -Path "/api/requests?status=PendingWithIssuer" -Token $issuerToken
$foundRequestInIssuer = $issuerRequests.Data.data | Where-Object { $_.id -eq $requestId }
$issuerCheckPassed = ($null -ne $foundRequestInIssuer) -and ($foundRequestInIssuer.status -eq "PendingWithIssuer")
Add-Verification -Step "6" -Requirement "Request visible in Issuer dashboard with status PendingWithIssuer" -Passed $issuerCheckPassed -Details "Request status: $($foundRequestInIssuer.status)"

# 8. Issuer processes same request (Laptop=Issue, Mouse=Issue, Keyboard=Issue, Monitor=Reject)
# We need the individual RequestItemIds to process them. Let's fetch request details.
$reqDetails = Invoke-Http -Method "GET" -Path "/api/requests/$requestId" -Token $issuerToken
$items = $reqDetails.Data.items

$laptopReqItem = $items | Where-Object { $_.itemName -eq "Laptop" }
$mouseReqItem = $items | Where-Object { $_.itemName -eq "Mouse" }
$keyboardReqItem = $items | Where-Object { $_.itemName -eq "Keyboard" }
$monitorReqItem = $items | Where-Object { $_.itemName -eq "Monitor" }

Write-Host "Processing Laptop (Issue) - RequestItemId: $($laptopReqItem.id)" -ForegroundColor Yellow
$issueLaptop = Invoke-Http -Method "PATCH" -Path "/api/requests/$requestId/items/$($laptopReqItem.id)/issue" -Token $issuerToken

Write-Host "Processing Mouse (Issue) - RequestItemId: $($mouseReqItem.id)" -ForegroundColor Yellow
$issueMouse = Invoke-Http -Method "PATCH" -Path "/api/requests/$requestId/items/$($mouseReqItem.id)/issue" -Token $issuerToken

Write-Host "Processing Keyboard (Issue) - RequestItemId: $($keyboardReqItem.id)" -ForegroundColor Yellow
$issueKeyboard = Invoke-Http -Method "PATCH" -Path "/api/requests/$requestId/items/$($keyboardReqItem.id)/issue" -Token $issuerToken

Write-Host "Processing Monitor (Reject) - RequestItemId: $($monitorReqItem.id)" -ForegroundColor Yellow
$rejectMonitor = Invoke-Http -Method "PATCH" -Path "/api/requests/$requestId/items/$($monitorReqItem.id)/not-issue" -Token $issuerToken

$issuerProcessingPassed = $issueLaptop.Success -and $issueMouse.Success -and $issueKeyboard.Success -and $rejectMonitor.Success
Add-Verification -Step "7.2" -Requirement "Issuer processes same request (Laptop, Mouse, Keyboard = Issue; Monitor = Reject)" -Passed $issuerProcessingPassed -Details "Laptop status=$($issueLaptop.StatusCode), Mouse status=$($issueMouse.StatusCode), Keyboard status=$($issueKeyboard.StatusCode), Monitor status=$($rejectMonitor.StatusCode)"

# 9. Verify partial issue works correctly, issued/rejected items saved, request appears in Admin approval list
Write-Host "Fetching request details to verify partial issue statuses..." -ForegroundColor Yellow
$reqDetailsAfterIssuer = Invoke-Http -Method "GET" -Path "/api/requests/$requestId" -Token $issuerToken
$postIssuerItems = $reqDetailsAfterIssuer.Data.items

$laptopPostStatus = ($postIssuerItems | Where-Object { $_.itemName -eq "Laptop" }).status
$mousePostStatus = ($postIssuerItems | Where-Object { $_.itemName -eq "Mouse" }).status
$keyboardPostStatus = ($postIssuerItems | Where-Object { $_.itemName -eq "Keyboard" }).status
$monitorPostStatus = ($postIssuerItems | Where-Object { $_.itemName -eq "Monitor" }).status

$partialIssueSaved = ($laptopPostStatus -eq "PendingAdminApproval") -and ($mousePostStatus -eq "PendingAdminApproval") -and ($keyboardPostStatus -eq "PendingAdminApproval") -and ($monitorPostStatus -eq "NotIssued")
$reqStatusPostIssuer = $reqDetailsAfterIssuer.Data.status -eq "PendingAdminApproval"

Add-Verification -Step "8.1" -Requirement "Partial issue works and individual item statuses are saved" -Passed ($partialIssueSaved -and $reqStatusPostIssuer) -Details "Laptop: $laptopPostStatus, Mouse: $mousePostStatus, Keyboard: $keyboardPostStatus, Monitor: $monitorPostStatus. Request Status: $($reqDetailsAfterIssuer.Data.status)"

Write-Host "Verifying request appears in Admin approval list..." -ForegroundColor Yellow
$adminIssuedRequests = Invoke-Http -Method "GET" -Path "/api/admin/issued-requests" -Token $adminToken
$foundInAdminApproveList = $adminIssuedRequests.Data.data | Where-Object { $_.id -eq $requestId }
Add-Verification -Step "8.2" -Requirement "Request appears in Admin approval list" -Passed ($null -ne $foundInAdminApproveList) -Details "Found request in Admin issued list: $($null -ne $foundInAdminApproveList)"

# 10. Login as Admin and perform Final review: Approve Laptop, Mouse, Keyboard, Keep Monitor rejected
Write-Host "Admin final review processing..." -ForegroundColor Yellow
$approveLaptopAdmin = Invoke-Http -Method "PATCH" -Path "/api/requests/$requestId/items/$($laptopReqItem.id)/approve" -Token $adminToken
$approveMouseAdmin = Invoke-Http -Method "PATCH" -Path "/api/requests/$requestId/items/$($mouseReqItem.id)/approve" -Token $adminToken
$approveKeyboardAdmin = Invoke-Http -Method "PATCH" -Path "/api/requests/$requestId/items/$($keyboardReqItem.id)/approve" -Token $adminToken

$adminReviewPassed = $approveLaptopAdmin.Success -and $approveMouseAdmin.Success -and $approveKeyboardAdmin.Success
Add-Verification -Step "9" -Requirement "Admin final review: Approve Laptop, Mouse, Keyboard" -Passed $adminReviewPassed -Details "Laptop: $($approveLaptopAdmin.StatusCode), Mouse: $($approveMouseAdmin.StatusCode), Keyboard: $($approveKeyboardAdmin.StatusCode)"

# 11. Verify final approval saved successfully (overall request becomes Approved)
Write-Host "Verifying final overall request status is Approved..." -ForegroundColor Yellow
$reqDetailsPostAdmin = Invoke-Http -Method "GET" -Path "/api/requests/$requestId" -Token $adminToken
$finalRequestApproved = $reqDetailsPostAdmin.Data.status -eq "Approved"
Add-Verification -Step "10" -Requirement "Final approval saved successfully (RequestStatus = Approved)" -Passed $finalRequestApproved -Details "Request Status: $($reqDetailsPostAdmin.Data.status)"

# 12. Login again as User and open My Requests / Request History
Write-Host "User fetching request history..." -ForegroundColor Yellow
$userRequestsAfterAdmin = Invoke-Http -Method "GET" -Path "/api/requests" -Token $userToken
$userRequestInHistory = $userRequestsAfterAdmin.Data.data | Where-Object { $_.id -eq $requestId }
$userHistoryLoads = $userRequestsAfterAdmin.Success -and ($null -ne $userRequestInHistory)
Add-Verification -Step "11" -Requirement "User can view request in My Requests / Request History" -Passed $userHistoryLoads -Details "Request found in user history list: $($null -ne $userRequestInHistory)"

# 13. Verify final item statuses: Laptop = Approved, Mouse = Approved, Keyboard = Approved, Monitor = Rejected (NotIssued)
$finalItems = $userRequestInHistory.items
$finalLaptopStatus = ($finalItems | Where-Object { $_.itemName -eq "Laptop" }).status
$finalMouseStatus = ($finalItems | Where-Object { $_.itemName -eq "Mouse" }).status
$finalKeyboardStatus = ($finalItems | Where-Object { $_.itemName -eq "Keyboard" }).status
$finalMonitorStatus = ($finalItems | Where-Object { $_.itemName -eq "Monitor" }).status

$finalStatusesPassed = ($finalLaptopStatus -eq "Approved") -and ($finalMouseStatus -eq "Approved") -and ($finalKeyboardStatus -eq "Approved") -and ($finalMonitorStatus -eq "NotIssued")
Add-Verification -Step "12" -Requirement "Verify final item statuses (Laptop, Mouse, Keyboard = Approved; Monitor = Rejected/NotIssued)" -Passed $finalStatusesPassed -Details "Laptop: $finalLaptopStatus, Mouse: $finalMouseStatus, Keyboard: $finalKeyboardStatus, Monitor: $finalMonitorStatus"

# 14. Verify role-based dashboard visibility and access control
Write-Host "Testing role-based access restrictions..." -ForegroundColor Yellow
$userAccessingAdmin = Invoke-Http -Method "GET" -Path "/api/admin/pending-users" -Token $userToken
$issuerAccessingAdmin = Invoke-Http -Method "GET" -Path "/api/admin/pending-users" -Token $issuerToken
$adminAccessingUserCreate = Invoke-Http -Method "POST" -Path "/api/requests" -BodyObject $createRequestPayload -Token $adminToken

$accessControlPassed = ($userAccessingAdmin.StatusCode -eq 403) -and ($issuerAccessingAdmin.StatusCode -eq 403) -and ($adminAccessingUserCreate.StatusCode -eq 403)
Add-Verification -Step "13" -Requirement "Verify dashboards and role-based access control works" -Passed $accessControlPassed -Details "User access Admin: $($userAccessingAdmin.StatusCode), Issuer access Admin: $($issuerAccessingAdmin.StatusCode), Admin access User Create: $($adminAccessingUserCreate.StatusCode)"

# 15. Validate duplicate request creation logic
Write-Host "Verifying user canRequest status is false while a request is active (Approved, not yet received)..." -ForegroundColor Yellow
$userCanRequestBeforeReceive = Invoke-Http -Method "GET" -Path "/api/requests/can-request" -Token $userToken
$blockDuplicateRequestSubmit = Invoke-Http -Method "POST" -Path "/api/requests" -BodyObject $createRequestPayload -Token $userToken

$duplicateBlockPassed = ($userCanRequestBeforeReceive.Data.canRequest -eq $false) -and ($blockDuplicateRequestSubmit.StatusCode -eq 400)
Add-Verification -Step "14.1" -Requirement "Validate restriction: no duplicate requests when a request is active" -Passed $duplicateBlockPassed -Details "canRequest: $($userCanRequestBeforeReceive.Data.canRequest), duplicate submit status: $($blockDuplicateRequestSubmit.StatusCode)"

# 16. User confirms receipt of approved request and verify they can request again
Write-Host "User confirming receipt of request..." -ForegroundColor Yellow
$userReceiveRequest = Invoke-Http -Method "PATCH" -Path "/api/requests/$requestId/receive" -Token $userToken
$userCanRequestAfterReceive = Invoke-Http -Method "GET" -Path "/api/requests/can-request" -Token $userToken

$receiptPassed = $userReceiveRequest.Success -and $userReceiveRequest.StatusCode -eq 200 -and ($userCanRequestAfterReceive.Data.canRequest -eq $true)
Add-Verification -Step "14.2" -Requirement "User confirms receipt and restriction is cleared" -Passed $receiptPassed -Details "Receive request: $($userReceiveRequest.StatusCode), canRequest again: $($userCanRequestAfterReceive.Data.canRequest)"

# 17. Verify inventory quantities update correctly
Write-Host "Checking final inventory stock levels..." -ForegroundColor Yellow
$finalInventory = Invoke-Http -Method "GET" -Path "/api/inventory" -Token $userToken

$finalLaptop = $finalInventory.Data | Where-Object { $_.name -eq "Laptop" }
$finalMouse = $finalInventory.Data | Where-Object { $_.name -eq "Mouse" }
$finalKeyboard = $finalInventory.Data | Where-Object { $_.name -eq "Keyboard" }
$finalMonitor = $finalInventory.Data | Where-Object { $_.name -eq "Monitor" }

# Stocks originally: Laptop=50, Mouse=25, Keyboard=50, Monitor=50
# Expected final stocks: Laptop=49, Mouse=24, Keyboard=49, Monitor=50 (Monitor was rejected, so stock remains unchanged)
$laptopStockOk = $finalLaptop.availableQuantity -eq 49
$mouseStockOk = $finalMouse.availableQuantity -eq 24
$keyboardStockOk = $finalKeyboard.availableQuantity -eq 49
$monitorStockOk = $finalMonitor.availableQuantity -eq 50

$stockUpdatesOk = $laptopStockOk -and $mouseStockOk -and $keyboardStockOk -and $monitorStockOk
Add-Verification -Step "14.3" -Requirement "Validate item quantities update correctly in database" -Passed $stockUpdatesOk -Details "Laptop: 50 -> $($finalLaptop.availableQuantity) (Ok: $laptopStockOk), Mouse: 25 -> $($finalMouse.availableQuantity) (Ok: $mouseStockOk), Keyboard: 50 -> $($finalKeyboard.availableQuantity) (Ok: $keyboardStockOk), Monitor: 50 -> $($finalMonitor.availableQuantity) (Ok: $monitorStockOk)"

# 18. Verify logout works for all users (clearing JWT credentials prevents access)
Write-Host "Verifying logout / stateless authorization check..." -ForegroundColor Yellow
$unauthCheck = Invoke-Http -Method "GET" -Path "/api/inventory"
$unauthBlocked = $unauthCheck.StatusCode -eq 401
Add-Verification -Step "14.4" -Requirement "Verify logout / unauthorized request blocked" -Passed $unauthBlocked -Details "Blocked correctly (StatusCode: $($unauthCheck.StatusCode))"

# Print E2E Summary Report
Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "E2E WORKFLOW VALIDATION SUMMARY REPORT" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$passedCount = ($results | Where-Object { $_.Passed }).Count
$failedCount = ($results | Where-Object { -not $_.Passed }).Count
$totalCount = $results.Count

Write-Host "Total Verifications: $totalCount"
Write-Host "Passed Checks:       $passedCount" -ForegroundColor Green
Write-Host "Failed Checks:       $failedCount" -ForegroundColor $(if ($failedCount -gt 0) { "Red" } else { "Green" })
Write-Host ""

$results | Format-Table Step, Requirement, Passed, Details

if ($failedCount -gt 0) {
    Write-Host "❌ Some checks failed! Please review details above." -ForegroundColor Red
    exit 1
} else {
    Write-Host "🎉 ALL E2E WORKFLOW CHECKS PASSED SUCCESSFULLY WITHOUT ERRORS!" -ForegroundColor Green
    exit 0
}
