# 第一阶段优化快速验证脚本
# 专病多维度查询系统 - Quick Phase 1 Verification

param(
    [string]$Port = "5000"
)

$BaseUrl = "http://localhost:$Port"

Write-Host ""
Write-Host "⚡ 第一阶段优化快速验证" -ForegroundColor Cyan
Write-Host "🌐 URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "⏰ $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# 快速测试函数
function Quick-Test {
    param($Url, $Name)
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop
        Write-Host "✅ $Name" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ $Name" -ForegroundColor Red
        return $false
    }
}

# 核心验证项目
$tests = @(
    @{ Name = "应用程序运行状态"; Url = "$BaseUrl" },
    @{ Name = "基础健康检查"; Url = "$BaseUrl/health" },
    @{ Name = "详细健康检查"; Url = "$BaseUrl/health/ready" },
    @{ Name = "存活状态检查"; Url = "$BaseUrl/health/live" }
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
Write-Host "📊 验证结果: $successCount/$totalCount 通过" -ForegroundColor $(if ($successCount -eq $totalCount) { "Green" } else { "Yellow" })

if ($successCount -eq $totalCount) {
    Write-Host "🎉 第一阶段优化运行正常！" -ForegroundColor Green
} else {
    Write-Host "⚠️  部分功能需要检查，建议运行完整验证脚本" -ForegroundColor Yellow
}

Write-Host "" 