$body = @{
    email = "ridhi@gmail.com"
    password = "12345"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5181/api/auth/login" -Method Post -Body $body -ContentType "application/json"
$token = $response.token
$headers = @{ Authorization = "Bearer $token" }

try {
    $result = Invoke-RestMethod -Uri "http://localhost:5181/api/inventory" -Method Get -Headers $headers
    $result | ConvertTo-Json -Depth 4
} catch {
    Write-Host "Error Details: "
    $reader = new-object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $content = $reader.ReadToEnd()
    Write-Host $content
}
