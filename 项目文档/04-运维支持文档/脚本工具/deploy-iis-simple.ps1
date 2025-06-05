# BZK Query System - IIS Deployment Script
# Run as Administrator

param(
    [string]$SiteName = "BZKQuerySystem",
    [string]$Port = "8080",
    [string]$SitePath = "C:\inetpub\wwwroot\BZKQuerySystem"
)

# Check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Host "[ERROR] Please run this script as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "================================================" -ForegroundColor Green
Write-Host "    BZK Query System - IIS Deployment" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

# Configuration
$SourcePath = Join-Path $PSScriptRoot "BZKQuerySystem.Web\publish"

Write-Host "[INFO] Configuration:" -ForegroundColor Cyan
Write-Host "  Site Name: $SiteName"
Write-Host "  Site Path: $SitePath"
Write-Host "  Source Path: $SourcePath"
Write-Host "  Port: $Port"
Write-Host ""

# Check if source files exist
if (-not (Test-Path $SourcePath)) {
    Write-Host "[ERROR] Published files not found: $SourcePath" -ForegroundColor Red
    Write-Host "Please run: dotnet publish -c Release -o publish" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Import WebAdministration module
Write-Host "[INFO] Loading IIS module..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "[INFO] IIS module loaded successfully" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] Failed to load IIS module. Please install IIS first." -ForegroundColor Red
    Write-Host "Install IIS using: Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Stop existing site and app pool
Write-Host "[INFO] Stopping existing site..." -ForegroundColor Yellow
try {
    Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
    Stop-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}
catch {
    # Ignore errors
}

# Remove existing site and app pool
Write-Host "[INFO] Removing existing site..." -ForegroundColor Yellow
try {
    Remove-Website -Name $SiteName -ErrorAction SilentlyContinue
    Remove-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
}
catch {
    # Ignore errors
}

# Create site directory
Write-Host "[INFO] Creating site directory..." -ForegroundColor Yellow
if (Test-Path $SitePath) {
    Remove-Item $SitePath -Recurse -Force
}
New-Item -ItemType Directory -Path $SitePath -Force | Out-Null

# Copy files
Write-Host "[INFO] Copying application files..." -ForegroundColor Yellow
try {
    Copy-Item -Path "$SourcePath\*" -Destination $SitePath -Recurse -Force
    Write-Host "[INFO] Files copied successfully" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] File copy failed: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Create application pool
Write-Host "[INFO] Creating application pool..." -ForegroundColor Yellow
try {
    New-WebAppPool -Name $SiteName -Force
    Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
    Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name "processModel.idleTimeout" -Value "00:00:00"
    Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name "recycling.periodicRestart.time" -Value "00:00:00"
    Write-Host "[INFO] Application pool created successfully" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] Application pool creation failed: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Create website
Write-Host "[INFO] Creating IIS website..." -ForegroundColor Yellow
try {
    New-Website -Name $SiteName -PhysicalPath $SitePath -Port $Port -ApplicationPool $SiteName
    Write-Host "[INFO] IIS website created successfully" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] IIS website creation failed: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Set permissions
Write-Host "[INFO] Setting folder permissions..." -ForegroundColor Yellow
try {
    icacls $SitePath /grant "IIS_IUSRS:(OI)(CI)F" /T | Out-Null
    icacls $SitePath /grant "IUSR:(OI)(CI)F" /T | Out-Null
    Write-Host "[INFO] Permissions set successfully" -ForegroundColor Green
}
catch {
    Write-Host "[WARNING] Permission setting failed: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Start application pool and website
Write-Host "[INFO] Starting application pool..." -ForegroundColor Yellow
try {
    Start-WebAppPool -Name $SiteName
    Write-Host "[INFO] Application pool started successfully" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] Application pool startup failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "[INFO] Starting website..." -ForegroundColor Yellow
try {
    Start-Website -Name $SiteName
    Write-Host "[INFO] Website started successfully" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] Website startup failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Deployment complete
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "           Deployment Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Site successfully deployed to IIS" -ForegroundColor Cyan
Write-Host "Access URL: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "Site Path: $SitePath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Please ensure:" -ForegroundColor Yellow
Write-Host "1. SQL Server service is running" -ForegroundColor White
Write-Host "2. Redis service is running" -ForegroundColor White
Write-Host "3. Firewall allows port $Port" -ForegroundColor White
Write-Host ""
Write-Host "If you encounter issues, check:" -ForegroundColor Yellow
Write-Host "1. Site status in IIS Manager" -ForegroundColor White
Write-Host "2. Windows Event Logs" -ForegroundColor White
Write-Host "3. Application log files: $SitePath\logs\" -ForegroundColor White
Write-Host ""

# Test site accessibility
Write-Host "[INFO] Testing site access..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$Port" -TimeoutSec 10 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "[SUCCESS] Site is accessible!" -ForegroundColor Green
    }
    else {
        Write-Host "[WARNING] Site response status: $($response.StatusCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "[WARNING] Site test failed: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Please test manually in browser: http://localhost:$Port" -ForegroundColor Cyan
}

Read-Host "Press Enter to exit"
