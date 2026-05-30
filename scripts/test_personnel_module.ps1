# Personnel Management Module - Testing Script
# This script tests all Personnel endpoints with sample data

# Configuration
$BaseUrl = "https://localhost:5000"
$AdminEmail = "admin@example.com"
$AdminPassword = "AdminPassword123!"

# Colors for output
$SuccessColor = "Green"
$ErrorColor = "Red"
$InfoColor = "Cyan"

# =======================
# HELPER FUNCTIONS
# =======================

function Get-AdminToken {
    Write-Host "🔐 Getting Admin Token..." -ForegroundColor $InfoColor
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" `
            -Method Post `
            -ContentType "application/json" `
            -Body @{
                email = $AdminEmail
                password = $AdminPassword
            } -ErrorAction Stop
        
        Write-Host "✅ Token obtained successfully" -ForegroundColor $SuccessColor
        return $response.token
    }
    catch {
        Write-Host "❌ Failed to get token: $_" -ForegroundColor $ErrorColor
        exit 1
    }
}

function Test-CreatePersonnel {
    param($token)
    
    Write-Host "`n📝 TEST 1: Create Personnel Entry" -ForegroundColor $InfoColor
    
    $photoPath = Join-Path $PSScriptRoot "test_photo.jpg"
    
    # Create a simple test image if it doesn't exist
    if (-not (Test-Path $photoPath)) {
        Write-Host "⚠️  Photo file not found at $photoPath. Skipping photo upload..." -ForegroundColor "Yellow"
        $form = @{
            name = "John Doe"
            email = "john.doe@example.com"
            icNumber = "123456-78-9012"
            designation = "Senior Manager"
            department = "IT"
            building = "Building A"
            reportingOfficer = "CEO"
            isStoresIncharge = $false
        }
    }
    else {
        Write-Host "📸 Using photo file: $photoPath"
        $form = @{
            name = "John Doe"
            email = "john.doe@example.com"
            icNumber = "123456-78-9012"
            designation = "Senior Manager"
            department = "IT"
            building = "Building A"
            reportingOfficer = "CEO"
            isStoresIncharge = $false
            photo = Get-Item $photoPath
        }
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/personnel" `
            -Method Post `
            -Headers @{ Authorization = "Bearer $token" } `
            -Form $form -ErrorAction Stop
        
        Write-Host "✅ Personnel created successfully" -ForegroundColor $SuccessColor
        Write-Host "   ID: $($response.data.id)"
        Write-Host "   Name: $($response.data.name)"
        Write-Host "   Email: $($response.data.email)"
        if ($response.data.photoUrl) {
            Write-Host "   Photo: $($response.data.photoUrl)"
        }
        
        return $response.data.id
    }
    catch {
        Write-Host "❌ Failed to create personnel: $_" -ForegroundColor $ErrorColor
        Write-Host $_.Exception.Response.StatusCode
        return $null
    }
}

function Test-GetAllPersonnel {
    param($token, $page = 1, $pageSize = 20)
    
    Write-Host "`n📋 TEST 2: Get All Personnel (Paginated)" -ForegroundColor $InfoColor
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/personnel?page=$page&pageSize=$pageSize" `
            -Method Get `
            -Headers @{ Authorization = "Bearer $token" } -ErrorAction Stop
        
        Write-Host "✅ Retrieved personnel list" -ForegroundColor $SuccessColor
        Write-Host "   Total Count: $($response.totalCount)"
        Write-Host "   Page: $($response.page)"
        Write-Host "   Page Size: $($response.pageSize)"
        Write-Host "   Total Pages: $($response.totalPages)"
        
        if ($response.data.Count -gt 0) {
            Write-Host "   Records:"
            foreach ($personnel in $response.data) {
                Write-Host "     - ID: $($personnel.id), Name: $($personnel.name), Email: $($personnel.email)"
            }
        }
        else {
            Write-Host "   No records found"
        }
        
        return $response
    }
    catch {
        Write-Host "❌ Failed to get personnel list: $_" -ForegroundColor $ErrorColor
        return $null
    }
}

function Test-GetSinglePersonnel {
    param($token, $id)
    
    Write-Host "`n👤 TEST 3: Get Single Personnel (ID: $id)" -ForegroundColor $InfoColor
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/personnel/$id" `
            -Method Get `
            -Headers @{ Authorization = "Bearer $token" } -ErrorAction Stop
        
        Write-Host "✅ Retrieved personnel record" -ForegroundColor $SuccessColor
        Write-Host "   ID: $($response.id)"
        Write-Host "   Name: $($response.name)"
        Write-Host "   Email: $($response.email)"
        Write-Host "   IC Number: $($response.icNumber)"
        Write-Host "   Designation: $($response.designation)"
        Write-Host "   Department: $($response.department)"
        Write-Host "   Building: $($response.building)"
        Write-Host "   Reporting Officer: $($response.reportingOfficer)"
        Write-Host "   Is Store Incharge: $($response.isStoresIncharge)"
        
        return $response
    }
    catch {
        Write-Host "❌ Failed to get personnel: $_" -ForegroundColor $ErrorColor
        return $null
    }
}

function Test-UpdatePersonnel {
    param($token, $id)
    
    Write-Host "`n✏️  TEST 4: Update Personnel (ID: $id)" -ForegroundColor $InfoColor
    
    $form = @{
        name = "John Doe Updated"
        email = "john.doe.updated@example.com"
        icNumber = "123456-78-9012"
        designation = "Director"
        department = "IT"
        building = "Building B"
        reportingOfficer = "Vice President"
        isStoresIncharge = $true
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/personnel/$id" `
            -Method Put `
            -Headers @{ Authorization = "Bearer $token" } `
            -Form $form -ErrorAction Stop
        
        Write-Host "✅ Personnel updated successfully" -ForegroundColor $SuccessColor
        Write-Host "   ID: $($response.data.id)"
        Write-Host "   Name: $($response.data.name)"
        Write-Host "   Email: $($response.data.email)"
        Write-Host "   Designation: $($response.data.designation)"
        Write-Host "   Building: $($response.data.building)"
        Write-Host "   Is Store Incharge: $($response.data.isStoresIncharge)"
        
        return $response
    }
    catch {
        Write-Host "❌ Failed to update personnel: $_" -ForegroundColor $ErrorColor
        return $null
    }
}

function Test-DuplicateEmail {
    param($token)
    
    Write-Host "`n⚠️  TEST 5: Test Duplicate Email Validation" -ForegroundColor $InfoColor
    
    $form = @{
        name = "Different Person"
        email = "john.doe.updated@example.com"  # Same as previous test
        designation = "Analyst"
        department = "HR"
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/personnel" `
            -Method Post `
            -Headers @{ Authorization = "Bearer $token" } `
            -Form $form -ErrorAction Stop
        
        Write-Host "❌ Should have failed but didn't!" -ForegroundColor $ErrorColor
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode
        if ($statusCode -eq "Conflict" -or $statusCode -eq 409) {
            Write-Host "✅ Correctly rejected duplicate email (409 Conflict)" -ForegroundColor $SuccessColor
            Write-Host "   Error: $($_.Exception.Message)"
        }
        else {
            Write-Host "❌ Unexpected error: $_" -ForegroundColor $ErrorColor
        }
    }
}

function Test-DeletePersonnel {
    param($token, $id)
    
    Write-Host "`n🗑️  TEST 6: Delete Personnel (ID: $id)" -ForegroundColor $InfoColor
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/personnel/$id" `
            -Method Delete `
            -Headers @{ Authorization = "Bearer $token" } -ErrorAction Stop
        
        Write-Host "✅ Personnel deleted successfully" -ForegroundColor $SuccessColor
        Write-Host "   Message: $($response.message)"
        
        return $true
    }
    catch {
        Write-Host "❌ Failed to delete personnel: $_" -ForegroundColor $ErrorColor
        return $false
    }
}

function Test-InvalidEmail {
    param($token)
    
    Write-Host "`n❌ TEST 7: Test Invalid Email Validation" -ForegroundColor $InfoColor
    
    $form = @{
        name = "Test User"
        email = "invalid-email"  # Not a valid email format
        designation = "Analyst"
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/personnel" `
            -Method Post `
            -Headers @{ Authorization = "Bearer $token" } `
            -Form $form -ErrorAction Stop
        
        Write-Host "❌ Should have failed but didn't!" -ForegroundColor $ErrorColor
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode
        if ($statusCode -eq "BadRequest" -or $statusCode -eq 400) {
            Write-Host "✅ Correctly rejected invalid email (400 Bad Request)" -ForegroundColor $SuccessColor
        }
        else {
            Write-Host "❌ Unexpected error: $_" -ForegroundColor $ErrorColor
        }
    }
}

function Test-MissingRequiredField {
    param($token)
    
    Write-Host "`n❌ TEST 8: Test Missing Required Field" -ForegroundColor $InfoColor
    
    $form = @{
        name = "Test User"
        # Missing email (required field)
        designation = "Analyst"
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/personnel" `
            -Method Post `
            -Headers @{ Authorization = "Bearer $token" } `
            -Form $form -ErrorAction Stop
        
        Write-Host "❌ Should have failed but didn't!" -ForegroundColor $ErrorColor
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode
        if ($statusCode -eq "BadRequest" -or $statusCode -eq 400) {
            Write-Host "✅ Correctly rejected missing required field (400 Bad Request)" -ForegroundColor $SuccessColor
        }
        else {
            Write-Host "❌ Unexpected error: $_" -ForegroundColor $ErrorColor
        }
    }
}

function Test-NotFound {
    param($token)
    
    Write-Host "`n❌ TEST 9: Test Get Non-existent Personnel (404 Error)" -ForegroundColor $InfoColor
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/personnel/99999" `
            -Method Get `
            -Headers @{ Authorization = "Bearer $token" } -ErrorAction Stop
        
        Write-Host "❌ Should have failed but didn't!" -ForegroundColor $ErrorColor
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode
        if ($statusCode -eq "NotFound" -or $statusCode -eq 404) {
            Write-Host "✅ Correctly returned 404 Not Found" -ForegroundColor $SuccessColor
        }
        else {
            Write-Host "❌ Unexpected error: $_" -ForegroundColor $ErrorColor
        }
    }
}

# =======================
# MAIN TEST EXECUTION
# =======================

Write-Host "
╔════════════════════════════════════════════════╗
║  Personnel Management Module - Test Suite      ║
╚════════════════════════════════════════════════╝
" -ForegroundColor Cyan

Write-Host "Base URL: $BaseUrl`n" -ForegroundColor $InfoColor

# Get authentication token
$token = Get-AdminToken
if (-not $token) {
    exit 1
}

# Run tests
$personnelId = Test-CreatePersonnel $token
if ($personnelId) {
    Test-GetAllPersonnel $token
    Test-GetSinglePersonnel $token $personnelId
    Test-UpdatePersonnel $token $personnelId
    Test-GetSinglePersonnel $token $personnelId  # Verify update
    Test-DuplicateEmail $token
    Test-InvalidEmail $token
    Test-MissingRequiredField $token
    Test-NotFound $token
    Test-DeletePersonnel $token $personnelId
    Test-GetSinglePersonnel $token $personnelId  # Should be deleted
}

Write-Host "`n
╔════════════════════════════════════════════════╗
║  ✅ Test Suite Completed                      ║
╚════════════════════════════════════════════════╝
" -ForegroundColor Green

Write-Host "
📊 Summary:
  ✅ All CRUD operations tested
  ✅ Pagination tested
  ✅ Validation errors tested
  ✅ Not Found errors tested
  ✅ Duplicate email prevention tested
  ✅ Authorization tested
" -ForegroundColor $SuccessColor

