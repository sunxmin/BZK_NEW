# 第一阶段优化自动验证脚本
# 专病多维度查询系统 - Phase 1 Optimization Verification

param(
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$DetailedOutput = $false
)

Write-Host "🔍 开始第一阶段优化验证..." -ForegroundColor Green
Write-Host "📍 基础URL: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

# 验证结果收集
$results = @()

function Test-Endpoint {
    param(
        [string]$Url,
        [string]$Description,
        [string]$ExpectedContent = $null,
        [int]$ExpectedStatusCode = 200
    )
    
    try {
        Write-Host "🧪 测试: $Description" -ForegroundColor Cyan
        Write-Host "   URL: $Url" -ForegroundColor Gray
        
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 30 -UseBasicParsing
        
        $success = $response.StatusCode -eq $ExpectedStatusCode
        
        if ($ExpectedContent -and $success) {
            $success = $response.Content -like "*$ExpectedContent*"
        }
        
        if ($success) {
            Write-Host "   ✅ 成功 (状态码: $($response.StatusCode))" -ForegroundColor Green
            if ($DetailedOutput) {
                Write-Host "   📄 响应内容: $($response.Content.Substring(0, [Math]::Min(200, $response.Content.Length)))..." -ForegroundColor Gray
            }
        } else {
            Write-Host "   ❌ 失败 (状态码: $($response.StatusCode))" -ForegroundColor Red
        }
        
        return @{
            Test = $Description
            Url = $Url
            Success = $success
            StatusCode = $response.StatusCode
            ResponseLength = $response.Content.Length
            Details = if ($success) { "OK" } else { "Failed" }
        }
    }
    catch {
        Write-Host "   ❌ 异常: $($_.Exception.Message)" -ForegroundColor Red
        return @{
            Test = $Description
            Url = $Url
            Success = $false
            StatusCode = $null
            ResponseLength = 0
            Details = $_.Exception.Message
        }
    }
    finally {
        Write-Host ""
    }
}

function Test-ConfigFile {
    param(
        [string]$FilePath,
        [string]$Description,
        [string[]]$ExpectedContent
    )
    
    Write-Host "📁 检查配置文件: $Description" -ForegroundColor Cyan
    Write-Host "   文件: $FilePath" -ForegroundColor Gray
    
    try {
        if (Test-Path $FilePath) {
            $content = Get-Content $FilePath -Raw
            $allFound = $true
            
            foreach ($expected in $ExpectedContent) {
                if ($content -notlike "*$expected*") {
                    $allFound = $false
                    Write-Host "   ❌ 未找到: $expected" -ForegroundColor Red
                }
            }
            
            if ($allFound) {
                Write-Host "   ✅ 配置正确" -ForegroundColor Green
            }
            
            return @{
                Test = $Description
                Success = $allFound
                Details = if ($allFound) { "All configurations found" } else { "Some configurations missing" }
            }
        } else {
            Write-Host "   ❌ 文件不存在" -ForegroundColor Red
            return @{
                Test = $Description
                Success = $false
                Details = "File not found"
            }
        }
    }
    catch {
        Write-Host "   ❌ 异常: $($_.Exception.Message)" -ForegroundColor Red
        return @{
            Test = $Description
            Success = $false
            Details = $_.Exception.Message
        }
    }
    finally {
        Write-Host ""
    }
}

# ==================== 开始验证 ====================

Write-Host "📋 第一部分：配置文件验证" -ForegroundColor Magenta
Write-Host "=" * 50

# 验证1：数据库连接池配置
$configResult1 = Test-ConfigFile -FilePath "BZKQuerySystem.Web/appsettings.json" -Description "数据库连接池配置" -ExpectedContent @(
    "Pooling=true",
    "Min Pool Size=5",
    "Max Pool Size=100",
    "Connection Timeout=30",
    "Command Timeout=30"
)
$results += $configResult1

# 验证2：错误处理配置
$configResult2 = Test-ConfigFile -FilePath "BZKQuerySystem.Web/appsettings.json" -Description "错误处理配置" -ExpectedContent @(
    "ErrorHandling",
    "EnableDetailedErrors",
    "LogErrorDetails",
    "RetryAttempts"
)
$results += $configResult2

# 验证3：健康检查配置
$configResult3 = Test-ConfigFile -FilePath "BZKQuerySystem.Web/appsettings.json" -Description "健康检查配置" -ExpectedContent @(
    "HealthChecks",
    "EnableDatabaseCheck",
    "CheckIntervalSeconds",
    "TimeoutSeconds"
)
$results += $configResult3

Write-Host "📋 第二部分：API端点验证" -ForegroundColor Magenta
Write-Host "=" * 50

# 等待应用程序启动
Write-Host "⏳ 等待应用程序启动..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 验证4：基础健康检查
$apiResult1 = Test-Endpoint -Url "$BaseUrl/health" -Description "基础健康检查" -ExpectedContent "Healthy"
$results += $apiResult1

# 验证5：详细健康检查
$apiResult2 = Test-Endpoint -Url "$BaseUrl/health/ready" -Description "详细健康检查" -ExpectedContent "status"
$results += $apiResult2

# 验证6：存活检查
$apiResult3 = Test-Endpoint -Url "$BaseUrl/health/live" -Description "存活检查"
$results += $apiResult3

# 验证7：数据库连接诊断
$apiResult4 = Test-Endpoint -Url "$BaseUrl/api/Diagnostics/db-connection" -Description "数据库连接诊断" -ExpectedContent "canConnect"
$results += $apiResult4

# 验证8：第一阶段优化成果展示（需要登录权限，可能返回401）
$apiResult5 = Test-Endpoint -Url "$BaseUrl/api/SystemQuality/phase1-achievements" -Description "第一阶段优化成果展示" -ExpectedStatusCode 401
if ($apiResult5.StatusCode -eq 401) {
    $apiResult5.Success = $true
    $apiResult5.Details = "需要身份验证（预期行为）"
}
$results += $apiResult5

# 验证9：性能监控API（需要登录权限，可能返回401）
$apiResult6 = Test-Endpoint -Url "$BaseUrl/api/Performance/stats" -Description "性能监控API" -ExpectedStatusCode 401
if ($apiResult6.StatusCode -eq 401) {
    $apiResult6.Success = $true
    $apiResult6.Details = "需要身份验证（预期行为）"
}
$results += $apiResult6

Write-Host "📋 第三部分：项目文件验证" -ForegroundColor Magenta
Write-Host "=" * 50

# 验证10：检查新增的NuGet包
$projectFileResult = Test-ConfigFile -FilePath "BZKQuerySystem.Web/BZKQuerySystem.Web.csproj" -Description "健康检查NuGet包" -ExpectedContent @(
    "Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore",
    "AspNetCore.HealthChecks.Redis",
    "AspNetCore.HealthChecks.UI"
)
$results += $projectFileResult

# 验证11：检查新增的控制器
$controllerResult = Test-ConfigFile -FilePath "BZKQuerySystem.Web/Controllers/SystemQualityController.cs" -Description "系统质量监控控制器" -ExpectedContent @(
    "SystemQualityController",
    "第一阶段优化",
    "phase1-achievements"
)
$results += $controllerResult

# ==================== 结果汇总 ====================

Write-Host "📊 验证结果汇总" -ForegroundColor Magenta
Write-Host "=" * 60

$successCount = ($results | Where-Object { $_.Success }).Count
$totalCount = $results.Count
$successRate = [math]::Round(($successCount / $totalCount) * 100, 2)

Write-Host ""
Write-Host "🎯 总体验证结果：" -ForegroundColor White
Write-Host "   ✅ 成功: $successCount 项" -ForegroundColor Green
Write-Host "   ❌ 失败: $($totalCount - $successCount) 项" -ForegroundColor Red
Write-Host "   📈 成功率: $successRate%" -ForegroundColor $(if ($successRate -ge 80) { "Green" } else { "Yellow" })
Write-Host ""

# 详细结果表格
Write-Host "📋 详细验证结果：" -ForegroundColor White
Write-Host "-" * 60
$results | ForEach-Object {
    $status = if ($_.Success) { "✅" } else { "❌" }
    $truncatedDetails = if ($_.Details.Length -gt 30) { $_.Details.Substring(0, 30) + "..." } else { $_.Details }
    Write-Host "$status $($_.Test.PadRight(35)) $truncatedDetails" -ForegroundColor $(if ($_.Success) { "Green" } else { "Red" })
}

Write-Host ""

# 失败项目的详细信息
$failedResults = $results | Where-Object { -not $_.Success }
if ($failedResults.Count -gt 0) {
    Write-Host "🔍 失败项目详细信息：" -ForegroundColor Yellow
    Write-Host "-" * 60
    $failedResults | ForEach-Object {
        Write-Host "❌ $($_.Test)" -ForegroundColor Red
        Write-Host "   详情: $($_.Details)" -ForegroundColor Gray
        if ($_.Url) {
            Write-Host "   URL: $($_.Url)" -ForegroundColor Gray
        }
        Write-Host ""
    }
}

# 最终评估
Write-Host "🏆 第一阶段优化验证评估：" -ForegroundColor White
if ($successRate -ge 90) {
    Write-Host "   🎉 优秀！第一阶段优化完全成功！" -ForegroundColor Green
} elseif ($successRate -ge 70) {
    Write-Host "   👍 良好！大部分优化项目成功，少数需要调整。" -ForegroundColor Yellow
} else {
    Write-Host "   ⚠️  需要关注！多个优化项目需要检查和修复。" -ForegroundColor Red
}

Write-Host ""
Write-Host "📝 建议后续行动：" -ForegroundColor Cyan
if ($successRate -ge 90) {
    Write-Host "   • 可以开始第二阶段优化：自动化测试体系建设" -ForegroundColor Green
    Write-Host "   • 监控系统运行状况，收集性能数据" -ForegroundColor Green
} else {
    Write-Host "   • 修复失败的验证项目" -ForegroundColor Yellow
    Write-Host "   • 重新运行验证脚本确认修复效果" -ForegroundColor Yellow
    Write-Host "   • 必要时回滚有问题的更改" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🔍 验证完成时间: $(Get-Date)" -ForegroundColor Gray
Write-Host "💡 运行 '$PSCommandPath -DetailedOutput' 获取详细输出" -ForegroundColor Gray 