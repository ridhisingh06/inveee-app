$payload = @{
    username = "TestUser"
    email = "test.user@example.com"
    password = "123"
    designation = "Employee"
    departmentId = 1
    roleId = 1
} | ConvertTo-Json

try {
    # First request
    Invoke-RestMethod -Uri "http://localhost:5181/api/auth/register" -Method Post -Body $payload -ContentType "application/json"
    Write-Host "First request succeeded."
    
    # Second request
    Invoke-RestMethod -Uri "http://localhost:5181/api/auth/register" -Method Post -Body $payload -ContentType "application/json"
} catch {
    Write-Host "Error details:"
    Write-Host $_.Exception.Response.StatusCode
    $reader = new-object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $content = $reader.ReadToEnd()
    Write-Host "Content: $content"
}
