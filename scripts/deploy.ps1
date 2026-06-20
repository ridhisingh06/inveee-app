Write-Host "=== Deploying Angular build ==="

# Define the build output directory (relative to repository root)
$distPath = Join-Path -Path "frontend" -ChildPath "dist/frontend"

# Verify the directory exists
if (-Not (Test-Path $distPath)) {
    Write-Error "Build directory not found: $distPath"
    exit 1
}

# List contents for verification
Write-Host "Listing $distPath contents:"
Get-ChildItem -Path $distPath -Recurse | Format-Table Name, Length, LastWriteTime

# Sync all files except index.html to the S3 bucket with long‑term caching
aws s3 sync $distPath "s3://invmgmt-frontend" `
    --delete `
    --cache-control "public, max-age=31536000" `
    --exclude "index.html"

# Upload index.html separately with no‑cache headers
$indexFile = Join-Path $distPath "index.html"
if (Test-Path $indexFile) {
    aws s3 cp $indexFile "s3://invmgmt-frontend/index.html" `
        --cache-control "no-cache, no-store, must-revalidate"
} else {
    Write-Warning "index.html not found at $indexFile"
}

Write-Host "Deployment completed."
