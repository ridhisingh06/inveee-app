$body = @{
    email = "admin@gmail.com"
    password = "admin@123"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5181/api/auth/login" -Method Post -Body $body -ContentType "application/json"
$token = $response.token

$headers = @{
    Authorization = "Bearer $token"
}

$inventory = Invoke-RestMethod -Uri "http://localhost:5181/api/inventory" -Method Get -Headers $headers
$inventory | ConvertTo-Json -Depth 10
