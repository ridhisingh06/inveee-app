$body = @{
    email = "admin@gmail.com"
    password = "admin@123"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5181/api/auth/login" -Method Post -Body $body -ContentType "application/json"
Write-Output $response.token
