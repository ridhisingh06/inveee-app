$body = @{
    email = "ridhi@gmail.com"
    password = "12345"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5181/api/auth/login" -Method Post -Body $body -ContentType "application/json"

$token = $response.token

$headers = @{
    Authorization = "Bearer $token"
}

$payload = @{
    name = "Pens"
    categoryId = 1
    totalQuantity = 100
    description = "A box of blue pens"
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "http://localhost:5181/api/inventory" -Method Post -Body $payload -ContentType "application/json" -Headers $headers
    Write-Host "Success! Server responded: $result"
} catch {
    Write-Host "Failure!"
    $reader = new-object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $content = $reader.ReadToEnd()
    Write-Host "Error Details: $content"
}
