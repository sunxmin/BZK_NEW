# Phase 1 Optimization Quick Verification Script
# BZK Query System - Quick Phase 1 Verification

param(
    [string]$Port = "5000"
)

$BaseUrl = "http://localhost:$Port"

Write-Host ""
Write-Host "Phase 1 Optimization Quick Verification" -ForegroundColor Cyan
Write-Host "URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# Quick test function
function Quick-Test {
    param($Url, $Name)
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop
        Write-Host "OK   $Name" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "FAIL $Name" -ForegroundColor Red
        return $false
    }
}

# Core verification items
$tests = @(
    @{ Name = "Application Status"; Url = "$BaseUrl" },
    @{ Name = "Basic Health Check"; Url = "$BaseUrl/health" },
    @{ Name = "Detailed Health Check"; Url = "$BaseUrl/health/ready" },
    @{ Name = "Liveness Check"; Url = "$BaseUrl/health/live" }
)

$successCount = 0
$totalCount = $tests.Count

foreach ($test in $tests) {
    if (Quick-Test -Url $test.Url -Name $test.Name) {
        $successCount++
    }
    Start-Sleep -Milliseconds 500
}

Write-Host ""
Write-Host "Result: $successCount/$totalCount passed" -ForegroundColor $(if ($successCount -eq $totalCount) { "Green" } else { "Yellow" })

if ($successCount -eq $totalCount) {
    Write-Host "SUCCESS: Phase 1 optimizations are working properly!" -ForegroundColor Green
} else {
    Write-Host "WARNING: Some functions need checking. Run full verification script." -ForegroundColor Yellow
}

Write-Host "" 