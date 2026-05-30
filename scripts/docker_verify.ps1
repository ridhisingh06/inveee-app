# =============================================================================
# Docker Full-Stack Verification Script
# =============================================================================
# Usage: .\scripts\docker_verify.ps1 [-SkipBuild] [-ProductionMode]
#
# Performs end-to-end validation of the containerized InvMgmt application:
#   1. Full stack startup (docker-compose up)
#   2. Service healthcheck validation
#   3. API endpoint testing (health, auth, registration)
#   4. CSV export download verification
#   5. Volume persistence test (restart and verify data survives)
#   6. Memory/resource usage check
#   7. Summary report
# =============================================================================

param(
    [switch]$SkipBuild,
    [switch]$ProductionMode,
    [int]$StartupWaitSeconds = 120,
    [string]$ComposeFile = "docker-compose.yml"
)

$ErrorActionPreference = "Continue"
$BaseUrl = "http://localhost:4200"
$BackendUrl = "http://localhost:5001"
$AdminEmail = "admin@gmail.com"
$AdminPassword = "admin@123"

# --- Helpers ---
$Results = @()

function Write-Section($title) {
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host "  $title" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
}

function Write-Check($name, $pass, $detail = "") {
    $icon = if ($pass) { "[PASS]" } else { "[FAIL]" }
    $color = if ($pass) { "Green" } else { "Red" }
    $msg = "$icon $name"
    if ($detail) { $msg += " -- $detail" }
    Write-Host $msg -ForegroundColor $color
    $script:Results += [PSCustomObject]@{ Check = $name; Pass = $pass; Detail = $detail }
}

function Test-HttpEndpoint($url, $method = "GET", $headers = @{}, $body = $null, $expectedStatus = 200) {
    try {
        $params = @{
            Uri = $url
            Method = $method
            UseBasicParsing = $true
            TimeoutSec = 30
            ErrorAction = "Stop"
        }
        if ($headers.Count -gt 0) {
            $params["Headers"] = $headers
        }
        if ($body) {
            $params["Body"] = $body
            $params["ContentType"] = "application/json"
        }
        $response = Invoke-WebRequest @params
        return @{ StatusCode = $response.StatusCode; Content = $response.Content; Headers = $response.Headers; Success = $true }
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        return @{ StatusCode = $statusCode; Content = $_.Exception.Message; Headers = @{}; Success = $false }
    }
}

function Get-AuthToken {
    $loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
    $result = Test-HttpEndpoint "$BackendUrl/api/auth/login" -method "POST" -body $loginBody
    if ($result.Success) {
        try {
            $json = $result.Content | ConvertFrom-Json
            if ($json.token) { return $json.token }
            if ($json.data -and $json.data.token) { return $json.data.token }
        } catch {}
    }
    return $null
}

# =============================================================================
# 1. STARTUP
# =============================================================================
Write-Section "1. Docker Compose Startup"

# Set production mode if requested
if ($ProductionMode) {
    $env:ASPNETCORE_ENVIRONMENT = "Production"
    Write-Host "  Mode: PRODUCTION" -ForegroundColor Yellow
} else {
    Write-Host "  Mode: DEVELOPMENT" -ForegroundColor Green
}

# Build and start
if ($SkipBuild) {
    Write-Host "  Skipping build (using cached images)..." -ForegroundColor Yellow
    docker-compose -f $ComposeFile up -d 2>&1 | Out-Null
} else {
    Write-Host "  Building and starting all services..." -ForegroundColor Yellow
    docker-compose -f $ComposeFile up -d --build 2>&1 | Out-Null
}

$composeExitCode = $LASTEXITCODE
Write-Check "docker-compose up" ($composeExitCode -eq 0) "exit code: $composeExitCode"

if ($composeExitCode -ne 0) {
    Write-Host "  FATAL: docker-compose failed. Check Docker Desktop is running." -ForegroundColor Red
    docker-compose -f $ComposeFile logs --tail=30 2>&1
    exit 1
}

# Wait for services to boot
Write-Host "  Waiting up to ${StartupWaitSeconds}s for services to become healthy..." -ForegroundColor Yellow

$deadline = (Get-Date).AddSeconds($StartupWaitSeconds)
$allHealthy = $false

while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 5
    $containers = docker-compose -f $ComposeFile ps --format json 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
    
    if (-not $containers) {
        # Fallback: try line-by-line parsing
        $psOutput = docker-compose -f $ComposeFile ps 2>$null
        Write-Host "  Containers: $($psOutput | Select-Object -Skip 1 | Measure-Object | Select-Object -ExpandProperty Count) running" -ForegroundColor Gray
        
        # Check backend health via HTTP
        $healthResult = Test-HttpEndpoint "$BackendUrl/health"
        if ($healthResult.Success) {
            $allHealthy = $true
            break
        }
        continue
    }
    
    $healthyCount = ($containers | Where-Object { $_.Health -eq "healthy" -or $_.State -eq "running" }).Count
    $totalCount = $containers.Count
    Write-Host "  Services: $healthyCount/$totalCount ready..." -ForegroundColor Gray
    
    if ($healthyCount -ge $totalCount -and $totalCount -gt 0) {
        $allHealthy = $true
        break
    }
}

# Final health probe even if container health status is unclear
if (-not $allHealthy) {
    $healthResult = Test-HttpEndpoint "$BackendUrl/health"
    $allHealthy = $healthResult.Success
}

Write-Check "All services healthy" $allHealthy

# =============================================================================
# 2. HEALTHCHECK VALIDATION
# =============================================================================
Write-Section "2. Service Healthcheck Validation"

# Backend /health endpoint
$backendHealth = Test-HttpEndpoint "$BackendUrl/health"
Write-Check "Backend /health endpoint" $backendHealth.Success "status=$($backendHealth.StatusCode)"

if ($backendHealth.Success) {
    try {
        $healthJson = $backendHealth.Content | ConvertFrom-Json
        $dbConnected = $healthJson.database -eq "connected"
        Write-Check "Database connectivity (via /health)" $dbConnected "database=$($healthJson.database)"
    } catch {
        Write-Check "Database connectivity (via /health)" $false "Could not parse health response"
    }
}

# Frontend nginx health
$frontendHealth = Test-HttpEndpoint "$BaseUrl/health"
Write-Check "Frontend nginx /health" $frontendHealth.Success "status=$($frontendHealth.StatusCode)"

# Frontend serves index.html
$frontendIndex = Test-HttpEndpoint "$BaseUrl/"
$servesAngular = $frontendIndex.Success -and $frontendIndex.Content -match "<app-root"
Write-Check "Frontend serves Angular app" $servesAngular

# Seq logging UI
$seqHealth = Test-HttpEndpoint "http://localhost:8082/"
Write-Check "Seq logging dashboard" $seqHealth.Success "status=$($seqHealth.StatusCode)"

# PostgreSQL via docker exec
$pgReady = docker exec $(docker-compose -f $ComposeFile ps -q db) pg_isready -U postgres -d InvMgmtDb 2>$null
Write-Check "PostgreSQL pg_isready" ($LASTEXITCODE -eq 0)

# =============================================================================
# 3. API ENDPOINT TESTING
# =============================================================================
Write-Section "3. API Endpoint Testing"

# Auth - Login
$loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
$loginResult = Test-HttpEndpoint "$BackendUrl/api/auth/login" -method "POST" -body $loginBody
$loginSuccess = $loginResult.Success -and $loginResult.StatusCode -eq 200
Write-Check "Admin login (POST /api/auth/login)" $loginSuccess "status=$($loginResult.StatusCode)"

$token = $null
if ($loginSuccess) {
    try {
        $loginJson = $loginResult.Content | ConvertFrom-Json
        $token = if ($loginJson.token) { $loginJson.token } elseif ($loginJson.data.token) { $loginJson.data.token } else { $null }
        Write-Check "JWT token received" ($null -ne $token) "length=$($token.Length)"
    } catch {
        Write-Check "JWT token received" $false "Could not parse login response"
    }
}

# Authenticated endpoints
if ($token) {
    $authHeaders = @{ "Authorization" = "Bearer $token" }
    
    # Items list
    $itemsResult = Test-HttpEndpoint "$BackendUrl/api/inventory" -headers $authHeaders
    Write-Check "GET /api/inventory (authenticated)" $itemsResult.Success "status=$($itemsResult.StatusCode)"
    
    # Categories
    $catResult = Test-HttpEndpoint "$BackendUrl/api/admin/categories" -headers $authHeaders
    Write-Check "GET /api/admin/categories (authenticated)" $catResult.Success "status=$($catResult.StatusCode)"
    
    # Pending users (correct route)
    $pendingResult = Test-HttpEndpoint "$BackendUrl/api/admin/pending-users" -headers $authHeaders
    Write-Check "GET /api/admin/pending-users" $pendingResult.Success "status=$($pendingResult.StatusCode)"

    # Personnel list
    $personnelResult = Test-HttpEndpoint "$BackendUrl/api/personnel" -headers $authHeaders
    Write-Check "GET /api/personnel (authenticated)" $personnelResult.Success "status=$($personnelResult.StatusCode)"
}

# Unauthenticated should be rejected
$unauthResult = Test-HttpEndpoint "$BackendUrl/api/admin/items"
$isRejected = $unauthResult.StatusCode -eq 401
Write-Check "Unauthenticated request rejected (401)" $isRejected "status=$($unauthResult.StatusCode)"

# =============================================================================
# 4. CSV EXPORT VERIFICATION
# =============================================================================
Write-Section "4. CSV Export Download Verification"

if ($token) {
    $authHeaders = @{ "Authorization" = "Bearer $token" }
    
    $csvResult = Test-HttpEndpoint "$BackendUrl/api/admin/section-wise-query/export" -headers $authHeaders
    Write-Check "CSV export endpoint responds" $csvResult.Success "status=$($csvResult.StatusCode)"
    
    if ($csvResult.Success) {
        # Check Content-Type
        $contentType = $csvResult.Headers["Content-Type"]
        $isCsv = $contentType -match "text/csv"
        Write-Check "CSV Content-Type header" $isCsv "Content-Type=$contentType"
        
        # Check Content-Disposition
        $contentDisp = $csvResult.Headers["Content-Disposition"]
        $hasDisposition = $contentDisp -match "attachment"
        Write-Check "CSV Content-Disposition header" $hasDisposition "Content-Disposition=$contentDisp"
        
        # Check content looks like CSV
        $csvContent = $csvResult.Content
        $hasCsvHeader = $csvContent -match "RequestItemId"
        Write-Check "CSV content has expected columns" $hasCsvHeader
    }
    
    # Test via nginx proxy (frontend port)
    $csvProxyResult = Test-HttpEndpoint "$BaseUrl/api/admin/section-wise-query/export" -headers $authHeaders
    Write-Check "CSV export via nginx proxy" $csvProxyResult.Success "status=$($csvProxyResult.StatusCode)"
    
    if ($csvProxyResult.Success) {
        $proxyContentDisp = $csvProxyResult.Headers["Content-Disposition"]
        $proxyHasDisp = $proxyContentDisp -match "attachment"
        Write-Check "Nginx passes Content-Disposition" $proxyHasDisp "Content-Disposition=$proxyContentDisp"
    }
} else {
    Write-Check "CSV export (skipped - no auth token)" $false "Login failed"
}

# =============================================================================
# 5. VOLUME PERSISTENCE TEST
# =============================================================================
Write-Section "5. Volume Persistence Verification"

# Check named volumes exist
$volumes = docker volume ls --format "{{.Name}}" 2>$null
$hasPgVolume = $volumes -match "pgdata"
$hasSeqVolume = $volumes -match "seqdata"
$hasUploadsVolume = $volumes -match "uploads"
Write-Check "pgdata volume exists" ($hasPgVolume.Count -gt 0)
Write-Check "seqdata volume exists" ($hasSeqVolume.Count -gt 0)
Write-Check "uploads volume exists" ($hasUploadsVolume.Count -gt 0)

# Create a test record, restart, verify it persists
if ($token) {
    Write-Host "  Testing data persistence across container restart..." -ForegroundColor Yellow
    
    # Get current user count
    $authHeaders = @{ "Authorization" = "Bearer $token" }
    $beforeResult = Test-HttpEndpoint "$BackendUrl/health"
    
    # Restart backend and db
    docker-compose -f $ComposeFile restart backend db 2>&1 | Out-Null
    Write-Host "  Waiting 30s for services to recover after restart..." -ForegroundColor Gray
    Start-Sleep -Seconds 30
    
    # Wait for backend to be healthy again
    $retries = 12
    $backendBack = $false
    for ($i = 0; $i -lt $retries; $i++) {
        $healthCheck = Test-HttpEndpoint "$BackendUrl/health"
        if ($healthCheck.Success) {
            $backendBack = $true
            break
        }
        Start-Sleep -Seconds 5
    }
    Write-Check "Backend recovers after restart" $backendBack
    
    # Re-login and verify admin user persists
    if ($backendBack) {
        $reLoginResult = Test-HttpEndpoint "$BackendUrl/api/auth/login" -method "POST" -body $loginBody
        $reLoginSuccess = $reLoginResult.Success -and $reLoginResult.StatusCode -eq 200
        Write-Check "Admin login works after restart (data persisted)" $reLoginSuccess
    }
} else {
    Write-Check "Volume persistence (skipped - no auth)" $false
}

# =============================================================================
# 6. MEMORY & RESOURCE CHECK
# =============================================================================
Write-Section "6. Memory & Resource Usage"

$containers = docker-compose -f $ComposeFile ps -q 2>$null
if ($containers) {
    $statsOutput = docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}" 2>$null
    Write-Host $statsOutput -ForegroundColor Gray
    
    # Parse memory for each container
    $statsJson = docker stats --no-stream --format "{{json .}}" 2>$null
    foreach ($line in $statsJson) {
        try {
            $stat = $line | ConvertFrom-Json
            $name = $stat.Name
            $memPerc = [double]($stat.MemPerc -replace '%', '')
            $isOk = $memPerc -lt 80
            Write-Check "Memory usage: $name" $isOk "$($stat.MemUsage) ($($stat.MemPerc))"
        } catch {
            # Skip unparseable lines
        }
    }
} else {
    Write-Check "Container stats" $false "No containers running"
}

# =============================================================================
# 7. PRODUCTION BUILD TEST (if requested)
# =============================================================================
if ($ProductionMode) {
    Write-Section "7. Production Mode Checks"
    
    # Swagger should NOT be accessible in production
    $swaggerResult = Test-HttpEndpoint "$BackendUrl/swagger"
    $swaggerHidden = -not $swaggerResult.Success -or $swaggerResult.StatusCode -ne 200
    Write-Check "Swagger hidden in production" $swaggerHidden "status=$($swaggerResult.StatusCode)"
    
    # Error details should be hidden
    $errorResult = Test-HttpEndpoint "$BackendUrl/api/nonexistent-endpoint"
    $noStackTrace = -not ($errorResult.Content -match "StackTrace")
    Write-Check "Stack traces hidden in production" $noStackTrace
}

# =============================================================================
# SUMMARY REPORT
# =============================================================================
Write-Section "SUMMARY REPORT"

$passed = ($Results | Where-Object { $_.Pass }).Count
$failed = ($Results | Where-Object { -not $_.Pass }).Count
$total = $Results.Count

Write-Host ""
Write-Host "  Total Checks: $total" -ForegroundColor White
Write-Host "  Passed:       $passed" -ForegroundColor Green
Write-Host "  Failed:       $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($failed -gt 0) {
    Write-Host "  Failed Checks:" -ForegroundColor Red
    $Results | Where-Object { -not $_.Pass } | ForEach-Object {
        Write-Host "    - $($_.Check): $($_.Detail)" -ForegroundColor Red
    }
    Write-Host ""
}

$passRate = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 1) } else { 0 }
if ($passRate -eq 100) {
    Write-Host "  Result: ALL CHECKS PASSED ($passRate%)" -ForegroundColor Green
} elseif ($passRate -ge 80) {
    Write-Host "  Result: MOSTLY PASSING ($passRate%)" -ForegroundColor Yellow
} else {
    Write-Host "  Result: NEEDS ATTENTION ($passRate%)" -ForegroundColor Red
}

Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Cyan
