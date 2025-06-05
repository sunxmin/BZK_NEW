# Test Stage 1 Optimization
param([string]$BaseUrl = "https://localhost:7297")

Write-Host "Stage 1 Optimization Verification" -ForegroundColor Cyan

# Test Redis configuration
try {
    $config = Get-Content "BZKQuerySystem.Web/appsettings.json" | ConvertFrom-Json
    if ($config.CacheSettings.UseRedis -eq $true) {
        Write-Host "[SUCCESS] Redis is enabled" -ForegroundColor Green
    } else {
        Write-Host "[FAILED] Redis is not enabled" -ForegroundColor Red
    }
} catch {
    Write-Host "[ERROR] Cannot read config: $($_.Exception.Message)" -ForegroundColor Red
}

# Test files exist
$files = @(
    "BZKQuerySystem.Web/Controllers/MonitoringController.cs",
    "BZKQuerySystem.Tests/Services/CacheServiceTests.cs",
    "BZKQuerySystem.Web/Views/Monitoring/Dashboard.cshtml"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "[SUCCESS] File exists: $file" -ForegroundColor Green
    } else {
        Write-Host "[FAILED] File missing: $file" -ForegroundColor Red
    }
}

Write-Host "Verification completed" -ForegroundColor Cyan 