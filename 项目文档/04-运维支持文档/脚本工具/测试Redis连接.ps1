# BZK系统Redis连接测试脚本
Write-Host "=== BZK系统Redis连接测试 ===" -ForegroundColor Green

# 检查Redis服务器状态
Write-Host "`n1. 检查Redis服务器状态..." -ForegroundColor Yellow

# 检查6379端口
$redisPortCheck = netstat -an | findstr ":6379"
if ($redisPortCheck) {
    Write-Host "✓ Redis服务器正在端口6379运行" -ForegroundColor Green
    Write-Host "  端口状态: $redisPortCheck" -ForegroundColor Cyan
}
else {
    Write-Host "✗ Redis服务器未在端口6379运行" -ForegroundColor Red
    Write-Host "请先运行 '启动Redis服务.bat'" -ForegroundColor Yellow
    exit 1
}

# 等待BZK系统启动
Write-Host "`n2. 等待BZK系统启动..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# 检查BZK系统状态
Write-Host "`n3. 检查BZK系统状态..." -ForegroundColor Yellow
try {
    $testResponse = Invoke-RestMethod -Uri "http://localhost:5072/api/Monitoring/test" -Method Get -TimeoutSec 3
    Write-Host "✓ BZK系统运行正常" -ForegroundColor Green
    Write-Host "  响应: $($testResponse.message)" -ForegroundColor Cyan
}
catch {
    Write-Host "✗ BZK系统未启动或无响应" -ForegroundColor Red
    Write-Host "  错误: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 测试监控API
Write-Host "`n4. 测试监控API..." -ForegroundColor Yellow
try {
    $monitorResponse = Invoke-RestMethod -Uri "http://localhost:5072/api/Monitoring/dashboard" -Method Get -TimeoutSec 10
    Write-Host "✓ 监控API响应正常" -ForegroundColor Green

    # 检查Redis状态
    if ($monitorResponse.redis -and $monitorResponse.redis.status) {
        $redisStatus = $monitorResponse.redis.status
        Write-Host "  Redis状态: $redisStatus" -ForegroundColor $(if ($redisStatus -eq "Healthy") { "Green" } elseif ($redisStatus -eq "Warning") { "Yellow" } else { "Red" })

        if ($monitorResponse.redis.message) {
            Write-Host "  Redis消息: $($monitorResponse.redis.message)" -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "  ⚠️  监控响应中未找到Redis状态信息" -ForegroundColor Yellow
    }

    # 检查缓存状态
    if ($monitorResponse.cache) {
        Write-Host "  缓存配置:" -ForegroundColor Cyan
        if ($monitorResponse.cache.redis) {
            Write-Host "    Redis缓存: $($monitorResponse.cache.redis)" -ForegroundColor Cyan
        }
        if ($monitorResponse.cache.memory) {
            Write-Host "    内存缓存: $($monitorResponse.cache.memory)" -ForegroundColor Cyan
        }
    }

}
catch {
    Write-Host "✗ 监控API请求失败" -ForegroundColor Red
    Write-Host "  错误: $($_.Exception.Message)" -ForegroundColor Red
}

# 直接测试Redis连接
Write-Host "`n5. 直接测试Redis连接..." -ForegroundColor Yellow

# 查找redis-cli.exe路径
$redisCliPaths = @(
    "D:\Redis\redis-cli.exe",
    "C:\Program Files\Redis\redis-cli.exe",
    "C:\Redis\redis-cli.exe",
    "C:\ProgramData\chocolatey\lib\redis-64\tools\redis-cli.exe"
)

$redisCliPath = $null
foreach ($path in $redisCliPaths) {
    if (Test-Path $path) {
        $redisCliPath = $path
        break
    }
}

if ($redisCliPath) {
    Write-Host "  找到redis-cli: $redisCliPath" -ForegroundColor Cyan

    try {
        # 测试Redis ping命令
        $pingResult = & $redisCliPath -p 6379 -a "BZK_Redis_2025" ping 2>$null
        if ($pingResult -eq "PONG") {
            Write-Host "✓ Redis ping测试成功" -ForegroundColor Green
        }
        else {
            Write-Host "✗ Redis ping测试失败" -ForegroundColor Red
        }

        # 测试Redis info命令
        $infoResult = & $redisCliPath -p 6379 -a "BZK_Redis_2025" info server 2>$null
        if ($infoResult -match "redis_version") {
            Write-Host "✓ Redis服务器信息获取成功" -ForegroundColor Green
        }

    }
    catch {
        Write-Host "✗ Redis客户端测试失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}
else {
    Write-Host "  ⚠️  未找到redis-cli工具" -ForegroundColor Yellow
}

Write-Host "`n=== 测试完成 ===" -ForegroundColor Green
Write-Host "如果Redis状态仍然显示Warning，请检查:" -ForegroundColor Yellow
Write-Host "1. Redis服务器是否正确启动并设置密码" -ForegroundColor White
Write-Host "2. BZK系统的Redis连接字符串配置" -ForegroundColor White
Write-Host "3. 防火墙或网络连接问题" -ForegroundColor White
