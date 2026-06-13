#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy InvMgmt full-stack application to EC2 via SSH

.DESCRIPTION
    This script remotely deploys the application to EC2 using SSH.
    It requires the PEM key file and EC2 instance IP.

.PARAMETER InstanceIP
    EC2 instance public IP or DNS (default: 100.55.99.251)

.PARAMETER KeyPath
    Path to PEM private key file (default: $HOME/Downloads/inveeemgmt.pem)

.PARAMETER InstanceUser
    EC2 user (default: ubuntu)

.EXAMPLE
    .\deploy-remote.ps1 -InstanceIP 100.55.99.251 -KeyPath C:\Users\Singh\Downloads\inveeemgmt.pem
#>

param(
    [string]$InstanceIP = "100.55.99.251",
    [string]$KeyPath = "$HOME/Downloads/inveeemgmt.pem",
    [string]$InstanceUser = "ubuntu"
)

$ErrorActionPreference = "Stop"

# Colors
$Green = [System.ConsoleColor]::Green
$Red = [System.ConsoleColor]::Red
$Yellow = [System.ConsoleColor]::Yellow
$Blue = [System.ConsoleColor]::Cyan

function Write-Header {
    param([string]$Message)
    Write-Host "`n=================================" -ForegroundColor $Blue
    Write-Host $Message -ForegroundColor $Blue
    Write-Host "=================================" -ForegroundColor $Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor $Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor $Red
}

function Write-Warning-Custom {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor $Yellow
}

# Verify SSH key exists
Write-Header "InvMgmt Remote Deployment"

if (-not (Test-Path $KeyPath)) {
    Write-Error-Custom "SSH key not found: $KeyPath"
    exit 1
}

Write-Success "SSH key found: $KeyPath"

# Check SSH connectivity
Write-Host "`nTesting SSH connection to $InstanceIP..." -ForegroundColor $Yellow
try {
    $sshTest = & ssh -i $KeyPath -o ConnectTimeout=5 -o StrictHostKeyChecking=no "$InstanceUser@$InstanceIP" "echo 'SSH OK'" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "SSH connection successful"
    } else {
        throw "SSH connection failed"
    }
} catch {
    Write-Error-Custom "Cannot connect to $InstanceIP : $_"
    Write-Host "Make sure:"
    Write-Host "  1. EC2 instance is running"
    Write-Host "  2. Security group allows SSH (port 22)"
    Write-Host "  3. PEM key has correct permissions"
    exit 1
}

# Upload latest code
Write-Host "`nPreparing deployment..." -ForegroundColor $Yellow

# Create directory structure on EC2
Write-Host "Creating deployment directory..." -ForegroundColor $Yellow
ssh -i $KeyPath -o StrictHostKeyChecking=no "$InstanceUser@$InstanceIP" `
    "mkdir -p /home/$InstanceUser/inveee-app"

# Copy deployment script
Write-Host "Uploading deployment script..." -ForegroundColor $Yellow
scp -i $KeyPath -o StrictHostKeyChecking=no deploy.sh `
    "$InstanceUser@$InstanceIP:/home/$InstanceUser/deploy.sh"

# Make it executable and run
Write-Host "`nRunning deployment on EC2..." -ForegroundColor $Yellow
Write-Host "This may take 10-15 minutes..." -ForegroundColor $Yellow

ssh -i $KeyPath -o StrictHostKeyChecking=no "$InstanceUser@$InstanceIP" `
    "chmod +x /home/$InstanceUser/deploy.sh && /home/$InstanceUser/deploy.sh"

if ($LASTEXITCODE -eq 0) {
    Write-Header "✓ Deployment Successful!"
    Write-Success "Your application is now running on EC2"
    Write-Host "`nAccess your application:" -ForegroundColor $Yellow
    Write-Host "  Frontend: http://$InstanceIP" -ForegroundColor $Blue
    Write-Host "  Backend:  http://$InstanceIP`:5000" -ForegroundColor $Blue
    Write-Host "  API Docs: http://$InstanceIP`:5000/swagger" -ForegroundColor $Blue
    
    Write-Host "`nUseful SSH commands:" -ForegroundColor $Yellow
    Write-Host "  View logs:      ssh -i $KeyPath $InstanceUser@$InstanceIP 'docker-compose logs -f'" -ForegroundColor $Blue
    Write-Host "  Stop services:  ssh -i $KeyPath $InstanceUser@$InstanceIP 'docker-compose down'" -ForegroundColor $Blue
    Write-Host "  Restart:        ssh -i $KeyPath $InstanceUser@$InstanceIP 'docker-compose restart'" -ForegroundColor $Blue
} else {
    Write-Error-Custom "Deployment failed on EC2"
    Write-Host "Check EC2 instance logs for details" -ForegroundColor $Yellow
    exit 1
}
