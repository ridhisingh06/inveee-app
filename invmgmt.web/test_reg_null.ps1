$payload = @{
    username = "TestUser3"
    email = "test.user3@example.com"
    password = "123"
    designation = "Employee"
    departmentId = $null
    roleId = 1
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "http://localhost:5181/api/auth/register" -Method Post -Body $payload -ContentType "application/json"
} catch {
    $reader = new-object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $content = $reader.ReadToEnd()
    Write-Host "Response content: '$content'"
}
