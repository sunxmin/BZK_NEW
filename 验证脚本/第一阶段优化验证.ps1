# BZK查询系统 - 第一阶段优化验证脚本
# 执行时间: $(Get-Date)

param(
    [string]$BaseUrl = "https://localhost:7056",
    [switch]$Detailed = $false
)

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "          BZK查询系统 - 第一阶段优化验证" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# 测试结果收集
$TestResults = @()

function Test-Endpoint {
    param(
        [string]$Url,
        [string]$Description,
        [string]$ExpectedContent = $null
    )
    
    try {
        Write-Host "测试: $Description" -ForegroundColor Yellow
        
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 30
        $stopwatch.Stop()
        $responseTime = $stopwatch.ElapsedMilliseconds
        
        $success = $true
        $message = "成功"
        
        if ($ExpectedContent -and $response -notlike "*$ExpectedContent*") {
            $success = $false
            $message = "响应内容不符合预期"
        }
        
        if ($success) {
            Write-Host "Success $Description - 响应时间: ${responseTime}ms" -ForegroundColor Green
        } else {
            Write-Host "Failed $Description - $message" -ForegroundColor Red
        }
        
        return @{
            Test = $Description
            Success = $success
            ResponseTime = $responseTime
            Message = $message
            Url = $Url
        }
    }
    catch {
        Write-Host "Failed $Description - 错误: $($_.Exception.Message)" -ForegroundColor Red
        return @{
            Test = $Description
            Success = $false
            ResponseTime = 0
            Message = $_.Exception.Message
            Url = $Url
        }
    }
}

function Test-Configuration {
    param(
        [string]$FilePath,
        [string]$Description,
        [array]$ExpectedSettings
    )
    
    try {
        Write-Host "配置检查: $Description" -ForegroundColor Yellow
        
        if (-not (Test-Path $FilePath)) {
            throw "配置文件不存在: $FilePath"
        }
        
        $content = Get-Content $FilePath -Raw
        $allFound = $true
        $missingSettings = @()
        
        foreach ($setting in $ExpectedSettings) {
            if ($content -notlike "*$setting*") {
                $allFound = $false
                $missingSettings += $setting
            }
        }
        
        if ($allFound) {
            Write-Host "Success $Description - 所有必需配置已找到" -ForegroundColor Green
            return @{
                Test = $Description
                Success = $true
                Message = "配置正确"
                FilePath = $FilePath
            }
        } else {
            $message = "缺少配置: $($missingSettings -join ', ')"
            Write-Host "Failed $Description - $message" -ForegroundColor Red
            return @{
                Test = $Description
                Success = $false
                Message = $message
                FilePath = $FilePath
            }
        }
    }
    catch {
        Write-Host "Failed $Description - 错误: $($_.Exception.Message)" -ForegroundColor Red
        return @{
            Test = $Description
            Success = $false
            Message = $_.Exception.Message
            FilePath = $FilePath
        }
    }
}

# 第一阶段优化验证项目
Write-Host "开始第一阶段优化验证..." -ForegroundColor Cyan
Write-Host ""

# 1. Redis缓存配置验证
Write-Host "1. Redis缓存配置验证" -ForegroundColor Blue
$configResult1 = Test-Configuration -FilePath "BZKQuerySystem.Web/appsettings.json" -Description "Redis缓存启用配置" -ExpectedSettings @(
    '"UseRedis": true',
    '"EnableRedisCheck": true',
    '"Redis": "localhost:6379"'
)
$TestResults += $configResult1

# 2. 健康检查端点验证
Write-Host ""
Write-Host "2. 健康检查端点验证" -ForegroundColor Blue

# 基础健康检查
$healthResult1 = Test-Endpoint -Url "$BaseUrl/health" -Description "基础健康检查" -ExpectedContent "Healthy"
$TestResults += $healthResult1

# 详细健康检查
$healthResult2 = Test-Endpoint -Url "$BaseUrl/health/ready" -Description "详细健康检查" -ExpectedContent "status"
$TestResults += $healthResult2

# 存活检查
$healthResult3 = Test-Endpoint -Url "$BaseUrl/health/live" -Description "存活状态检查" -ExpectedContent "status"
$TestResults += $healthResult3

# 3. 监控API验证
Write-Host ""
Write-Host "3. 监控API验证" -ForegroundColor Blue

# 监控仪表板
$monitorResult1 = Test-Endpoint -Url "$BaseUrl/api/Monitoring/Dashboard" -Description "监控仪表板API"
$TestResults += $monitorResult1

# 性能指标
$monitorResult2 = Test-Endpoint -Url "$BaseUrl/api/Monitoring/Performance" -Description "性能指标API"
$TestResults += $monitorResult2

# 健康状态
$monitorResult3 = Test-Endpoint -Url "$BaseUrl/api/Monitoring/Health" -Description "健康状态API"
$TestResults += $monitorResult3

# 4. 缓存服务验证
Write-Host ""
Write-Host "4. 缓存服务验证" -ForegroundColor Blue

# 检查Redis配置
try {
    $redisConfig = Get-Content "BZKQuerySystem.Web/appsettings.json" | ConvertFrom-Json
    $useRedis = $redisConfig.CacheSettings.UseRedis
    $redisConnection = $redisConfig.ConnectionStrings.Redis
    
    if ($useRedis -eq $true) {
        Write-Host "Success Redis缓存已启用 - 连接字符串: $redisConnection" -ForegroundColor Green
        $cacheResult = @{
            Test = "Redis缓存配置"
            Success = $true
            Message = "已启用"
        }
    } else {
        Write-Host "Failed Redis缓存未启用" -ForegroundColor Red
        $cacheResult = @{
            Test = "Redis缓存配置"
            Success = $false
            Message = "未启用"
        }
    }
    $TestResults += $cacheResult
}
catch {
    Write-Host "Failed 无法读取缓存配置: $($_.Exception.Message)" -ForegroundColor Red
    $TestResults += @{
        Test = "Redis缓存配置读取"
        Success = $false
        Message = $_.Exception.Message
    }
}

# 5. 性能基线测试
Write-Host "`n5️⃣ 性能基线测试" -ForegroundColor Blue

try {
    Write-Host "执行性能基线测试..." -ForegroundColor Yellow
    
    # 测试多次请求的平均响应时间
    $responseTimes = @()
    $testCount = 5
    
    for ($i = 1; $i -le $testCount; $i++) {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $null = Invoke-RestMethod -Uri "$BaseUrl/api/Monitoring/Performance" -Method Get -TimeoutSec 30
        $stopwatch.Stop()
        $responseTimes += $stopwatch.ElapsedMilliseconds
        Write-Host "  测试 $i/$testCount - 响应时间: $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Gray
    }
    
    $avgResponseTime = ($responseTimes | Measure-Object -Average).Average
    $maxResponseTime = ($responseTimes | Measure-Object -Maximum).Maximum
    $minResponseTime = ($responseTimes | Measure-Object -Minimum).Minimum
    
    Write-Host "✅ 性能基线测试完成" -ForegroundColor Green
    Write-Host "   平均响应时间: $([math]::Round($avgResponseTime, 2))ms" -ForegroundColor Green
    Write-Host "   最快响应时间: ${minResponseTime}ms" -ForegroundColor Green
    Write-Host "   最慢响应时间: ${maxResponseTime}ms" -ForegroundColor Green
    
    $performanceResult = @{
        Test = "性能基线测试"
        Success = $true
        ResponseTime = $avgResponseTime
        Message = "平均响应时间: $([math]::Round($avgResponseTime, 2))ms"
    }
    $TestResults += $performanceResult
}
catch {
    Write-Host "❌ 性能基线测试失败: $($_.Exception.Message)" -ForegroundColor Red
    $TestResults += @{
        Test = "性能基线测试"
        Success = $false
        Message = $_.Exception.Message
    }
}

# 6. 单元测试验证（如果存在）
Write-Host "`n6️⃣ 单元测试验证" -ForegroundColor Blue

try {
    if (Test-Path "BZKQuerySystem.Tests") {
        Write-Host "发现测试项目，尝试运行单元测试..." -ForegroundColor Yellow
        
        # 运行单元测试
        $testOutput = dotnet test BZKQuerySystem.Tests --verbosity minimal 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ 单元测试执行成功" -ForegroundColor Green
            $testResult = @{
                Test = "单元测试执行"
                Success = $true
                Message = "测试通过"
            }
        } else {
            Write-Host "❌ 单元测试执行失败" -ForegroundColor Red
            Write-Host $testOutput -ForegroundColor Red
            $testResult = @{
                Test = "单元测试执行"
                Success = $false
                Message = "测试失败"
            }
        }
        $TestResults += $testResult
    } else {
        Write-Host "⚠️  未找到测试项目" -ForegroundColor Yellow
        $TestResults += @{
            Test = "单元测试项目"
            Success = $false
            Message = "测试项目不存在"
        }
    }
}
catch {
    Write-Host "❌ 单元测试验证出错: $($_.Exception.Message)" -ForegroundColor Red
    $TestResults += @{
        Test = "单元测试验证"
        Success = $false
        Message = $_.Exception.Message
    }
}

# 生成验证报告
Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "                    验证结果汇总" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

$totalTests = $TestResults.Count
$passedTests = ($TestResults | Where-Object { $_.Success -eq $true }).Count
$failedTests = $totalTests - $passedTests
$successRate = [math]::Round(($passedTests / $totalTests) * 100, 2)

Write-Host ""
Write-Host "总测试项: $totalTests" -ForegroundColor White
Write-Host "通过: $passedTests" -ForegroundColor Green
Write-Host "失败: $failedTests" -ForegroundColor Red
Write-Host "成功率: $successRate%" -ForegroundColor $(if ($successRate -ge 80) { "Green" } elseif ($successRate -ge 60) { "Yellow" } else { "Red" })

Write-Host ""
Write-Host "详细结果:" -ForegroundColor White
foreach ($result in $TestResults) {
    $status = if ($result.Success) { "Success" } else { "Failed" }
    $color = if ($result.Success) { "Green" } else { "Red" }
    
    if ($result.ResponseTime -and $result.ResponseTime -gt 0) {
        Write-Host "$status $($result.Test) - $($result.Message) ($($result.ResponseTime)ms)" -ForegroundColor $color
    } else {
        Write-Host "$status $($result.Test) - $($result.Message)" -ForegroundColor $color
    }
}

# 第一阶段优化状态评估
Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "              第一阶段优化状态评估" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

$cacheEnabled = ($TestResults | Where-Object { $_.Test -eq "Redis缓存配置" }).Success
$healthChecksWorking = ($TestResults | Where-Object { $_.Test -like "*健康检查*" -and $_.Success }).Count -ge 2
$monitoringWorking = ($TestResults | Where-Object { $_.Test -like "*监控*" -and $_.Success }).Count -ge 2
$performanceGood = ($TestResults | Where-Object { $_.Test -eq "性能基线测试" }).Success

Write-Host ""
Write-Host "第一阶段优化完成情况:" -ForegroundColor Yellow
Write-Host "Redis缓存启用: $(if ($cacheEnabled) { 'Success 已完成' } else { 'Failed 未完成' })" -ForegroundColor $(if ($cacheEnabled) { "Green" } else { "Red" })
Write-Host "健康检查系统: $(if ($healthChecksWorking) { 'Success 已完成' } else { 'Failed 未完成' })" -ForegroundColor $(if ($healthChecksWorking) { "Green" } else { "Red" })
Write-Host "监控仪表板: $(if ($monitoringWorking) { 'Success 已完成' } else { 'Failed 未完成' })" -ForegroundColor $(if ($monitoringWorking) { "Green" } else { "Red" })
Write-Host "性能基线: $(if ($performanceGood) { 'Success 已建立' } else { 'Failed 需要改进' })" -ForegroundColor $(if ($performanceGood) { "Green" } else { "Red" })

# 优化建议
if ($successRate -lt 100) {
    Write-Host "`n💡 优化建议:" -ForegroundColor Yellow
    
    if (-not $cacheEnabled) {
        Write-Host "   • 确保Redis服务已启动并可访问" -ForegroundColor White
    }
    
    if (-not $healthChecksWorking) {
        Write-Host "   • 检查健康检查端点配置" -ForegroundColor White
    }
    
    if (-not $monitoringWorking) {
        Write-Host "   • 验证监控API的依赖注入配置" -ForegroundColor White
    }
    
    if ($failedTests -gt 0) {
        Write-Host "   • 查看详细错误信息并修复失败的测试项" -ForegroundColor White
    }
}

# 下一步建议
Write-Host ""
Write-Host "下一步行动:" -ForegroundColor Cyan
if ($successRate -ge 90) {
    Write-Host "第一阶段优化成功！可以开始第二阶段用户体验优化" -ForegroundColor Green
    Write-Host "第二阶段重点：SignalR实时通信、Chart.js可视化、UI优化" -ForegroundColor Blue
} elseif ($successRate -ge 70) {
    Write-Host "第一阶段基本完成，但需要修复部分问题" -ForegroundColor Yellow
    Write-Host "建议先解决失败的测试项，然后再进入第二阶段" -ForegroundColor Yellow
} else {
    Write-Host "第一阶段需要重新评估和实施" -ForegroundColor Red
    Write-Host "建议重新检查配置和依赖项" -ForegroundColor Red
}

Write-Host ""
Write-Host "验证完成于: $(Get-Date)" -ForegroundColor Gray
Write-Host "================================================================" -ForegroundColor Cyan 