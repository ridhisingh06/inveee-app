$payload = @{
    username = "TestUser2"
    email = "testuser2@example.com"
    password = "123"
    designation = "Employee"
    departmentId = "IT" # INCORRECT TYPE (simulate bad input)
    roleId = "1"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5181/api/auth/register" -Method Post -Body $payload -ContentType "application/json"
    $response | ConvertTo-Json
} catch {
    Write-Host "Error details:"
    $out = $_.ErrorDetails.Message
    if ($out) {
        Write-Host $out
    } else {
        $reader = new-object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.ReadToEnd()
    }
}
