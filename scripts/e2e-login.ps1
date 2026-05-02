param(
  [string]$BaseUrl = "http://localhost:5181",
  [string]$AdminEmail = $env:E2E_ADMIN_EMAIL,
  [string]$AdminPassword = $env:E2E_ADMIN_PASSWORD
)

$ErrorActionPreference = "Stop"

function Get-RandomEmail {
  return ("e2e_" + ([Guid]::NewGuid().ToString("N").Substring(0, 10)) + "@example.com")
}

function Convert-FromBase64Url([string]$value) {
  $s = $value.Replace('-', '+').Replace('_', '/')
  switch ($s.Length % 4) {
    2 { $s += "==" }
    3 { $s += "=" }
    0 { }
    default { throw "Invalid base64url length" }
  }

  $bytes = [Convert]::FromBase64String($s)
  return [Text.Encoding]::UTF8.GetString($bytes)
}

Add-Type -AssemblyName System.Net.Http | Out-Null

$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(10)

function Invoke-HttpJson {
  param(
    [Parameter(Mandatory=$true)][string]$Method,
    [Parameter(Mandatory=$true)][string]$Url,
    [object]$BodyObject,
    [hashtable]$Headers
  )

  try {
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $Url)

    if ($Headers) {
      foreach ($k in $Headers.Keys) {
        $req.Headers.TryAddWithoutValidation($k, [string]$Headers[$k]) | Out-Null
      }
    }

    if ($BodyObject -ne $null) {
      $json = $BodyObject | ConvertTo-Json -Compress -Depth 10
      $req.Content = [System.Net.Http.StringContent]::new($json, [Text.Encoding]::UTF8, "application/json")
    }

    $resp = $client.SendAsync($req).GetAwaiter().GetResult()
    $bodyText = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    return [pscustomobject]@{
      Ok = $true
      StatusCode = [int]$resp.StatusCode
      Body = $bodyText
    }
  } catch {
    return [pscustomobject]@{
      Ok = $false
      StatusCode = $null
      Body = $null
      Error = $_.Exception.Message
    }
  }
}

$results = New-Object System.Collections.Generic.List[object]
function Add-Result([string]$Name, [bool]$Passed, [string]$Details) {
  $results.Add([pscustomobject]@{ Name = $Name; Passed = $Passed; Details = $Details }) | Out-Null
  $tag = if ($Passed) { "PASS" } else { "FAIL" }
  Write-Host ("[{0}] {1} - {2}" -f $tag, $Name, $Details)
}

Write-Host "BaseUrl: $BaseUrl"

# 1) Input validation
$r = Invoke-HttpJson -Method "POST" -Url "$BaseUrl/api/auth/login" -BodyObject @{}
Add-Result "Login empty body -> 400" ($r.Ok -and $r.StatusCode -eq 400) ("status={0} body={1}" -f $r.StatusCode, ($r.Body | Select-Object -First 200))

$r = Invoke-HttpJson -Method "POST" -Url "$BaseUrl/api/auth/login" -BodyObject @{ email = ""; password = "" }
Add-Result "Login empty strings -> 400" ($r.Ok -and $r.StatusCode -eq 400) ("status={0} body={1}" -f $r.StatusCode, ($r.Body | Select-Object -First 200))

# 2) Invalid credentials
$randomEmail = Get-RandomEmail
$r = Invoke-HttpJson -Method "POST" -Url "$BaseUrl/api/auth/login" -BodyObject @{ email = $randomEmail; password = "wrong" }
Add-Result "Login invalid creds -> 401" ($r.Ok -and $r.StatusCode -eq 401) ("status={0} body={1}" -f $r.StatusCode, ($r.Body | Select-Object -First 200))

# 3) Unapproved account flow
$pendingEmail = Get-RandomEmail
$regBody = @{
  username = "e2e"
  email = $pendingEmail
  password = "12345"
  designation = ""
  departmentId = 1
  roleId = 1
}
$r = Invoke-HttpJson -Method "POST" -Url "$BaseUrl/api/auth/register" -BodyObject $regBody
Add-Result "Register pending -> 200" ($r.Ok -and $r.StatusCode -eq 200) ("status={0} body={1}" -f $r.StatusCode, ($r.Body | Select-Object -First 200))

$r = Invoke-HttpJson -Method "POST" -Url "$BaseUrl/api/auth/login" -BodyObject @{ email = $pendingEmail; password = "12345" }
Add-Result "Login pending user -> 400" ($r.Ok -and $r.StatusCode -eq 400) ("status={0} body={1}" -f $r.StatusCode, ($r.Body | Select-Object -First 200))

# 4) Unauthorized access without token
$r = Invoke-HttpJson -Method "GET" -Url "$BaseUrl/api/inventory"
Add-Result "Inventory without token -> 401" ($r.Ok -and $r.StatusCode -eq 401) ("status={0}" -f $r.StatusCode)

# 5) Valid login + token checks (optional)
$token = $null
if ([string]::IsNullOrWhiteSpace($AdminEmail) -or [string]::IsNullOrWhiteSpace($AdminPassword)) {
  Add-Result "Login valid admin -> 200" $true "SKIP (set E2E_ADMIN_EMAIL/E2E_ADMIN_PASSWORD)"
} else {
  $loginBody = @{ email = $AdminEmail; password = $AdminPassword }
  $r = Invoke-HttpJson -Method "POST" -Url "$BaseUrl/api/auth/login" -BodyObject $loginBody
  $ok = $r.Ok -and $r.StatusCode -eq 200
  if ($ok) {
    try {
      $json = $r.Body | ConvertFrom-Json
      $token = $json.token
    } catch { }
  }

  Add-Result "Login valid admin -> 200" ($ok -and -not [string]::IsNullOrWhiteSpace($token)) ("status={0} tokenPresent={1}" -f $r.StatusCode, (-not [string]::IsNullOrWhiteSpace($token)))

  if (-not [string]::IsNullOrWhiteSpace($token)) {
    try {
      $parts = $token.Split('.')
      $payloadJson = Convert-FromBase64Url $parts[1]
      $payload = $payloadJson | ConvertFrom-Json
      $hasUserId = -not [string]::IsNullOrWhiteSpace([string]$payload.UserId)
      $emailClaim =
        [string]$payload.email,
        [string]$payload.'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
        [string]$payload.'http://schemas.microsoft.com/ws/2008/06/identity/claims/emailaddress' |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1
      $hasEmail = -not [string]::IsNullOrWhiteSpace($emailClaim)
      Add-Result "Token contains claims" ($hasUserId -and $hasEmail) ("UserId={0} emailPresent={1}" -f $payload.UserId, $hasEmail)
    } catch {
      Add-Result "Token contains claims" $false "Failed to decode token payload"
    }

    $r = Invoke-HttpJson -Method "GET" -Url "$BaseUrl/api/inventory" -Headers @{ Authorization = "Bearer $token" }
    Add-Result "Inventory with token -> 200" ($r.Ok -and $r.StatusCode -eq 200) ("status={0}" -f $r.StatusCode)

    # 403 example: admin token should be forbidden for user-only endpoint
    $reqBody = @{
      userId = 1
      categoryId = 1
      items = @(@{ itemId = 1; quantityRequested = 1 })
    }
    $r = Invoke-HttpJson -Method "POST" -Url "$BaseUrl/api/request/create" -BodyObject $reqBody -Headers @{ Authorization = "Bearer $token" }
    Add-Result "User-only endpoint with admin -> 403" ($r.Ok -and $r.StatusCode -eq 403) ("status={0}" -f $r.StatusCode)
  } else {
    Add-Result "Token contains claims" $true "SKIP (no token)"
    Add-Result "Inventory with token -> 200" $true "SKIP (no token)"
    Add-Result "User-only endpoint with admin -> 403" $true "SKIP (no token)"
  }
}

# 6) Network failure simulation (use an unused port)
$r = Invoke-HttpJson -Method "POST" -Url "http://localhost:59999/api/auth/login" -BodyObject @{ email = "x"; password = "y" }
Add-Result "Network failure -> handled" (-not $r.Ok) ("error={0}" -f $r.Error)

$failed = $results | Where-Object { -not $_.Passed }
Write-Host ""
Write-Host ("Total: {0}, Passed: {1}, Failed: {2}" -f $results.Count, ($results | Where-Object Passed).Count, @($failed).Count)

if (@($failed).Count -gt 0) {
  exit 1
}

exit 0
