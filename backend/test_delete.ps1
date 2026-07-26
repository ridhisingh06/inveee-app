$body = @{
    email = "admin@gmail.com"
    password = "admin@123"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5181/api/auth/login" -Method Post -Body $body -ContentType "application/json"
$token = $response.token

$headers = @{
    Authorization = "Bearer $token"
}

# Test 1: Verify ITEM1 (soft deleted) no longer appears in inventory list
Write-Host "Test 1: Checking if soft-deleted ITEM1 appears in inventory list..."
$inventory = Invoke-RestMethod -Uri "http://localhost:5181/api/inventory" -Method Get -Headers $headers
$item1 = $inventory | Where-Object { $_.itemCode -eq "ITEM1" }
if ($item1) {
    Write-Host "ERROR: Soft-deleted ITEM1 still appears in inventory list!"
} else {
    Write-Host "SUCCESS: Soft-deleted ITEM1 correctly hidden from inventory list"
}

# Test 2: Add a new item that has no references
Write-Host "`nTest 2: Adding new test item with no references..."
$newItem = @{
    itemCode = "TEST999"
    name = "Test Item for Deletion"
    categoryId = 1
    totalQuantity = 50
    description = "This item will be physically deleted"
} | ConvertTo-Json

$addItemResponse = Invoke-RestMethod -Uri "http://localhost:5181/api/inventory" -Method Post -Body $newItem -ContentType "application/json" -Headers $headers
Write-Host "Item added: $addItemResponse"

# Test 3: Delete the new item (should be physical delete since no references)
Write-Host "`nTest 3: Deleting new test item (should be physical delete)..."
try {
    $result = Invoke-RestMethod -Uri "http://localhost:5181/api/inventory/TEST999" -Method Delete -Headers $headers
    Write-Host "Success! Server responded: $result"
} catch {
    Write-Host "Failure!"
    $reader = new-object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $content = $reader.ReadToEnd()
    Write-Host "Error Details: $content"
}

# Test 4: Verify the physically deleted item is gone from the list
Write-Host "`nTest 4: Verifying physically deleted item is removed from inventory list..."
$inventory = Invoke-RestMethod -Uri "http://localhost:5181/api/inventory" -Method Get -Headers $headers
$testItem = $inventory | Where-Object { $_.itemCode -eq "TEST999" }
if ($testItem) {
    Write-Host "ERROR: Physically deleted item still exists in inventory list!"
} else {
    Write-Host "SUCCESS: Physically deleted item removed from inventory list"
}
