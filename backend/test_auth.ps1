$body = @{
    email = "ridhi@gmail.com"
    password = "12345"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5181/api/auth/login" -Method Post -Body $body -ContentType "application/json"

$token = $response.token
if ([string]::IsNullOrEmpty($token)) {
    Write-Host "Failed to get token"
    exit 1
}

Write-Host "Got Token: $token"

$headers = @{
    Authorization = "Bearer $token"
}

try {
    $pending = Invoke-RestMethod -Uri "http://localhost:5181/api/admin/pending-users" -Method Get -Headers $headers
    Write-Host "Success! Pending users:"
    $pending | ConvertTo-Json
} catch {
    Write-Host "Error calling pending-users: $_"
    Write-Host $_.Exception.Response.StatusCode.value__
}
