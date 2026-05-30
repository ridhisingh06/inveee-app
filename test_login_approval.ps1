#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test script to verify the login approval fix
.DESCRIPTION
    This script performs end-to-end testing of the user approval and login flow
.PARAMETER ApiUrl
    Base URL of the API (default: http://localhost:5000)
.PARAMETER AdminEmail
    Admin email for approval operations
.PARAMETER AdminPassword
    Admin password for authentication
#>

param(
    [string]$ApiUrl = "http://localhost:5000",
    [string]$AdminEmail = "admin@example.com",
    [string]$AdminPassword = "AdminPassword123"
)

# Colors for output
$green = [System.ConsoleColor]::Green
$red = [System.ConsoleColor]::Red
$yellow = [System.ConsoleColor]::Yellow
$cyan = [System.ConsoleColor]::Cyan

function Write-Success { Write-Host $args -ForegroundColor $green -BackgroundColor Black }
function Write-Error-Custom { Write-Host $args -ForegroundColor $red -BackgroundColor Black }
function Write-Warning-Custom { Write-Host $args -ForegroundColor $yellow -BackgroundColor Black }
function Write-Info { Write-Host $args -ForegroundColor $cyan -BackgroundColor Black }

# Test counter
$testsPassed = 0
$testsFailed = 0

function Test-LoginFlow {
    Write-Info "`n================================"
    Write-Info "LOGIN APPROVAL FLOW TEST"
    Write-Info "================================`n"
    
    # Test 1: Register a new user
    Write-Info "[TEST 1] Registering new test user..."
    $testEmail = "testuser_$(Get-Random)@example.com"
    $testUsername = "testuser_$(Get-Random)"
    $testPassword = "TestPassword123!"
    
    $registerPayload = @{
        username = $testUsername
        email = $testEmail
        password = $testPassword
        designation = "Test Manager"
        departmentId = 1
        roleId = 2
    } | ConvertTo-Json
    
    try {
        $registerResponse = Invoke-RestMethod -Uri "$ApiUrl/api/auth/register" `
            -Method Post `
            -ContentType "application/json" `
            -Body $registerPayload `
            -SkipCertificateCheck
        
        Write-Success "✓ Registration successful"
        Write-Info "   Email: $testEmail"
        Write-Info "   Username: $testUsername"
        $testsPassed++
    } catch {
        Write-Error-Custom "✗ Registration failed: $($_.Exception.Message)"
        $testsFailed++
        return
    }
    
    # Test 2: Try logging in with pending account
    Write-Info "`n[TEST 2] Attempting login before approval (should fail)..."
    
    $loginPayload = @{
        email = $testEmail
        password = $testPassword
    } | ConvertTo-Json
    
    try {
        $loginResponse = Invoke-RestMethod -Uri "$ApiUrl/api/auth/login" `
            -Method Post `
            -ContentType "application/json" `
            -Body $loginPayload `
            -SkipCertificateCheck
        
        Write-Error-Custom "✗ Login should have failed for pending user"
        $testsFailed++
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        if ($statusCode -eq 403 -or $statusCode -eq 401) {
            $errorMessage = $_.ErrorDetails.Message
            if ($errorMessage -like "*pending*" -or $errorMessage -like "*approval*") {
                Write-Success "✓ Login correctly blocked with pending approval message"
                Write-Info "   Status: $statusCode"
                Write-Info "   Message: $errorMessage"
                $testsPassed++
            } else {
                Write-Error-Custom "✗ Wrong error message: $errorMessage"
                $testsFailed++
            }
        } else {
            Write-Error-Custom "✗ Unexpected status code: $statusCode"
            $testsFailed++
        }
    }
    
    # Test 3: Get pending users (as admin)
    Write-Info "`n[TEST 3] Getting pending users list..."
    
    try {
        # First login as admin
        $adminLoginPayload = @{
            email = $AdminEmail
            password = $AdminPassword
        } | ConvertTo-Json
        
        $adminLoginResponse = Invoke-RestMethod -Uri "$ApiUrl/api/auth/login" `
            -Method Post `
            -ContentType "application/json" `
            -Body $adminLoginPayload `
            -SkipCertificateCheck
        
        $adminToken = $adminLoginResponse.token
        $adminHeaders = @{
            Authorization = "Bearer $adminToken"
            "Content-Type" = "application/json"
        }
        
        # Get pending users
        $pendingResponse = Invoke-RestMethod -Uri "$ApiUrl/api/admin/pending-users" `
            -Method Get `
            -Headers $adminHeaders `
            -SkipCertificateCheck
        
        $pendingUser = $pendingResponse.data | Where-Object { $_.email -eq $testEmail }
        
        if ($pendingUser) {
            Write-Success "✓ Test user found in pending list"
            Write-Info "   ID: $($pendingUser.id)"
            Write-Info "   Email: $($pendingUser.email)"
            Write-Info "   Status: $($pendingUser.status)"
            $testsPassed++
            $pendingUserId = $pendingUser.id
            $pendingRoleId = $pendingUser.roleId
            $pendingDeptId = $pendingUser.departmentId
        } else {
            Write-Error-Custom "✗ Test user not found in pending list"
            $testsFailed++
            return
        }
    } catch {
        Write-Error-Custom "✗ Failed to get pending users: $($_.Exception.Message)"
        $testsFailed++
        return
    }
    
    # Test 4: Approve the user
    Write-Info "`n[TEST 4] Approving user..."
    
    $approvePayload = @{
        roleId = $pendingRoleId
        departmentId = $pendingDeptId
    } | ConvertTo-Json
    
    try {
        $approveResponse = Invoke-RestMethod -Uri "$ApiUrl/api/admin/approve/$pendingUserId" `
            -Method Put `
            -Headers $adminHeaders `
            -Body $approvePayload `
            -SkipCertificateCheck
        
        Write-Success "✓ User approved successfully"
        Write-Info "   Response: $($approveResponse.message)"
        if ($approveResponse.isApproved) {
            Write-Info "   isApproved: $($approveResponse.isApproved)"
        }
        if ($approveResponse.isActive) {
            Write-Info "   isActive: $($approveResponse.isActive)"
        }
        $testsPassed++
    } catch {
        Write-Error-Custom "✗ Approval failed: $($_.Exception.Message)"
        Write-Error-Custom "   Response: $($_.ErrorDetails.Message)"
        $testsFailed++
        return
    }
    
    # Test 5: Login with approved account
    Write-Info "`n[TEST 5] Attempting login after approval (should succeed)..."
    
    try {
        $loginResponse2 = Invoke-RestMethod -Uri "$ApiUrl/api/auth/login" `
            -Method Post `
            -ContentType "application/json" `
            -Body $loginPayload `
            -SkipCertificateCheck
        
        if ($loginResponse2.token) {
            Write-Success "✓ Login successful after approval"
            Write-Info "   Token length: $($loginResponse2.token.Length) characters"
            Write-Info "   Message: $($loginResponse2.message)"
            
            # Decode JWT to check role
            $parts = $loginResponse2.token.Split('.')
            if ($parts.Count -eq 3) {
                # Decode payload (add padding if needed)
                $payload = $parts[1]
                $padded = $payload + ('=' * (4 - $payload.Length % 4))
                $decodedBytes = [Convert]::FromBase64String($padded)
                $decodedJson = [System.Text.Encoding]::UTF8.GetString($decodedBytes)
                $jwtPayload = $decodedJson | ConvertFrom-Json
                Write-Info "   Role in token: $($jwtPayload.role)"
            }
            $testsPassed++
        } else {
            Write-Error-Custom "✗ Login failed - no token received"
            $testsFailed++
        }
    } catch {
        Write-Error-Custom "✗ Login after approval failed: $($_.Exception.Message)"
        $testsFailed++
        return
    }
    
    # Test 6: Try approving same user again (idempotency)
    Write-Info "`n[TEST 6] Testing idempotency (approving already approved user)..."
    
    try {
        $approveResponse2 = Invoke-RestMethod -Uri "$ApiUrl/api/admin/approve/$pendingUserId" `
            -Method Put `
            -Headers $adminHeaders `
            -Body $approvePayload `
            -SkipCertificateCheck
        
        Write-Success "✓ Re-approval is idempotent (returns success)"
        Write-Info "   Response: $($approveResponse2.message)"
        $testsPassed++
    } catch {
        Write-Error-Custom "✗ Idempotency test failed: $($_.Exception.Message)"
        $testsFailed++
    }
}

function Test-DirectDatabaseChecks {
    Write-Info "`n================================"
    Write-Info "DATABASE VERIFICATION QUERIES"
    Write-Info "================================`n"
    
    Write-Warning-Custom "Note: Run these SQL queries manually to verify database state:"
    Write-Info "`n[1] Check User table:"
    Write-Info "    SELECT Id, Username, Email, IsActive, CreatedAt FROM \"Users\" WHERE Email = '<test-email>';"
    
    Write-Info "`n[2] Check RegistrationRequests table:"
    Write-Info "    SELECT Id, Email, Status, IsActive, ApprovedAt, ApprovedBy FROM \"RegistrationRequests\" WHERE Email = '<test-email>';"
    
    Write-Info "`n[3] Check UserRoles table:"
    Write-Info "    SELECT ur.UserId, ur.RoleId, r.Name FROM \"UserRoles\" ur JOIN \"Roles\" r ON ur.RoleId = r.Id WHERE ur.UserId = (SELECT Id FROM \"Users\" WHERE Email = '<test-email>');"
}

function Show-Results {
    Write-Info "`n================================"
    Write-Info "TEST RESULTS SUMMARY"
    Write-Info "================================`n"
    
    Write-Success "✓ Tests Passed: $testsPassed"
    if ($testsFailed -gt 0) {
        Write-Error-Custom "✗ Tests Failed: $testsFailed"
    }
    
    $totalTests = $testsPassed + $testsFailed
    $passPercentage = if ($totalTests -gt 0) { [math]::Round(($testsPassed / $totalTests) * 100, 2) } else { 0 }
    
    Write-Info "`nTotal Tests: $totalTests"
    Write-Info "Pass Rate: $passPercentage%`n"
    
    if ($testsFailed -eq 0 -and $testsPassed -gt 0) {
        Write-Success "`n🎉 ALL TESTS PASSED! Login approval flow is working correctly."
    } else {
        Write-Warning-Custom "`n⚠️  Some tests failed. Check the output above for details."
    }
}

# Main execution
Write-Info "================================"
Write-Info "LOGIN APPROVAL FLOW TEST SUITE"
Write-Info "================================`n"

Write-Info "Configuration:"
Write-Info "  API URL: $ApiUrl"
Write-Info "  Admin Email: $AdminEmail`n"

try {
    Test-LoginFlow
    Test-DirectDatabaseChecks
    Show-Results
} catch {
    Write-Error-Custom "Fatal error: $($_.Exception.Message)"
    $testsFailed++
    Show-Results
}

# Exit with appropriate code
if ($testsFailed -eq 0) {
    exit 0
} else {
    exit 1
}
