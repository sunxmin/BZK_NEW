# 简化的第一阶段优化验证脚本
param(
    [string]$BaseUrl = "https://localhost:7297"
)

Write-Host "开始第一阶段优化验证..." -ForegroundColor Cyan
Write-Host ""

# 验证结果数组
$results = @()

# 函数：测试端点
function Test-SimpleEndpoint {
    param([string]$Url, [string]$Name)
    
    try {
        Write-Host "测试: $Name" -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 10
        Write-Host "成功: $Name" -ForegroundColor Green
        return @{ Name = $Name; Success = $true; Message = "OK" }
    }
    catch {
        Write-Host "失败: $Name - $($_.Exception.Message)" -ForegroundColor Red
        return @{ Name = $Name; Success = $false; Message = $_.Exception.Message }
    }
}

# 1. 检查配置文件
Write-Host "1. 检查Redis配置" -ForegroundColor Blue
try {
    $config = Get-Content "BZKQuerySystem.Web/appsettings.json" | ConvertFrom-Json
    if ($config.CacheSettings.UseRedis -eq $true) {
        Write-Host "成功: Redis已启用" -ForegroundColor Green
        $results += @{ Name = "Redis配置"; Success = $true; Message = "已启用" }
    } else {
        Write-Host "失败: Redis未启用" -ForegroundColor Red
        $results += @{ Name = "Redis配置"; Success = $false; Message = "未启用" }
    }
}
catch {
    Write-Host "失败: 无法读取配置" -ForegroundColor Red
    $results += @{ Name = "配置读取"; Success = $false; Message = $_.Exception.Message }
}

# 2. 测试健康检查端点
Write-Host "`n2. 测试健康检查端点" -ForegroundColor Blue
$results += Test-SimpleEndpoint -Url "$BaseUrl/health" -Name "基础健康检查"
$results += Test-SimpleEndpoint -Url "$BaseUrl/health/ready" -Name "就绪检查"
$results += Test-SimpleEndpoint -Url "$BaseUrl/health/live" -Name "存活检查"

# 3. 测试监控API
Write-Host "`n3. 测试监控API" -ForegroundColor Blue
$results += Test-SimpleEndpoint -Url "$BaseUrl/api/Monitoring/Health" -Name "监控健康API"
$results += Test-SimpleEndpoint -Url "$BaseUrl/api/Monitoring/Performance" -Name "监控性能API"

# 4. 检查单元测试
Write-Host "`n4. 检查单元测试" -ForegroundColor Blue
try {
    if (Test-Path "BZKQuerySystem.Tests/Services/CacheServiceTests.cs") {
        Write-Host "成功: 单元测试文件存在" -ForegroundColor Green
        $results += @{ Name = "单元测试"; Success = $true; Message = "文件存在" }
    } else {
        Write-Host "失败: 单元测试文件不存在" -ForegroundColor Red
        $results += @{ Name = "单元测试"; Success = $false; Message = "文件不存在" }
    }
}
catch {
    $results += @{ Name = "单元测试"; Success = $false; Message = $_.Exception.Message }
}

# 5. 检查监控界面
Write-Host "`n5. 检查监控界面" -ForegroundColor Blue
if (Test-Path "BZKQuerySystem.Web/Views/Monitoring/Dashboard.cshtml") {
    Write-Host "成功: 监控界面文件存在" -ForegroundColor Green
    $results += @{ Name = "监控界面"; Success = $true; Message = "文件存在" }
} else {
    Write-Host "失败: 监控界面文件不存在" -ForegroundColor Red
    $results += @{ Name = "监控界面"; Success = $false; Message = "文件不存在" }
}

# 生成报告
Write-Host "`n第一阶段优化验证报告" -ForegroundColor Cyan
Write-Host "=" * 50

$successCount = ($results | Where-Object { $_.Success }).Count
$totalCount = $results.Count
$successRate = [math]::Round(($successCount / $totalCount) * 100, 1)

Write-Host "总测试项目: $totalCount" -ForegroundColor White
Write-Host "成功项目: $successCount" -ForegroundColor Green
Write-Host "失败项目: $($totalCount - $successCount)" -ForegroundColor Red
Write-Host "成功率: $successRate%" -ForegroundColor $(if ($successRate -ge 80) { "Green" } else { "Yellow" })

Write-Host "`n详细结果:" -ForegroundColor White
foreach ($result in $results) {
    $status = if ($result.Success) { "[成功]" } else { "[失败]" }
    $color = if ($result.Success) { "Green" } else { "Red" }
    Write-Host "$status $($result.Name): $($result.Message)" -ForegroundColor $color
}

if ($successRate -ge 80) {
    Write-Host "`n第一阶段优化验证通过！" -ForegroundColor Green
} else {
    Write-Host "`n第一阶段优化需要进一步检查" -ForegroundColor Yellow
} 