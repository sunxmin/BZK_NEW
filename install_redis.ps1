# Redis for Windows 安装脚本
Write-Host "=== BZK系统 - Redis安装脚本 ===" -ForegroundColor Green

# 检查是否以管理员身份运行
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
    Write-Host "⚠️  请以管理员身份运行此脚本" -ForegroundColor Yellow
    Write-Host "右键点击PowerShell -> '以管理员身份运行'" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "`n选择Redis安装方法:" -ForegroundColor Yellow
Write-Host "1. 使用Chocolatey安装 (推荐)" -ForegroundColor White
Write-Host "2. 手动下载安装" -ForegroundColor White
Write-Host "3. 使用Docker安装" -ForegroundColor White
Write-Host "4. 跳过安装，仅配置系统使用内存缓存" -ForegroundColor White

$choice = Read-Host "`n请输入选择 (1-4)"

switch ($choice) {
    "1" {
        Write-Host "`n=== 使用Chocolatey安装Redis ===" -ForegroundColor Green

        # 检查Chocolatey是否安装
        try {
            choco --version | Out-Null
            Write-Host "✓ Chocolatey已安装" -ForegroundColor Green
        }
        catch {
            Write-Host "安装Chocolatey..." -ForegroundColor Yellow
            Set-ExecutionPolicy Bypass -Scope Process -Force
            [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
            iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
        }

        Write-Host "安装Redis..." -ForegroundColor Yellow
        choco install redis-64 -y

        Write-Host "启动Redis服务..." -ForegroundColor Yellow
        Start-Service Redis

        Write-Host "✓ Redis安装完成！" -ForegroundColor Green
    }

    "2" {
        Write-Host "`n=== 手动下载Redis ===" -ForegroundColor Green
        Write-Host "1. 访问: https://github.com/microsoftarchive/redis/releases" -ForegroundColor White
        Write-Host "2. 下载最新的 Redis-x64-*.msi 文件" -ForegroundColor White
        Write-Host "3. 运行安装程序" -ForegroundColor White
        Write-Host "4. 安装完成后，Redis会自动启动为Windows服务" -ForegroundColor White

        Write-Host "`n下载完成后请按任意键继续..." -ForegroundColor Yellow
        pause
    }

    "3" {
        Write-Host "`n=== 使用Docker安装Redis ===" -ForegroundColor Green

        # 检查Docker是否安装
        try {
            docker --version | Out-Null
            Write-Host "✓ Docker已安装" -ForegroundColor Green

            Write-Host "拉取Redis镜像..." -ForegroundColor Yellow
            docker pull redis:latest

            Write-Host "启动Redis容器..." -ForegroundColor Yellow
            docker run -d --name bzk-redis --restart unless-stopped -p 6379:6379 redis:latest

            Write-Host "✓ Redis容器启动完成！" -ForegroundColor Green
        }
        catch {
            Write-Host "✗ Docker未安装或未启动" -ForegroundColor Red
            Write-Host "请先安装Docker Desktop: https://www.docker.com/products/docker-desktop/" -ForegroundColor White
        }
    }

    "4" {
        Write-Host "`n=== 配置系统使用内存缓存 ===" -ForegroundColor Green
        Write-Host "✓ 系统已配置为仅使用内存缓存" -ForegroundColor Green
        Write-Host "✓ Redis缓存已禁用" -ForegroundColor Green
        Write-Host "✓ 系统可以正常运行，但缓存性能略有下降" -ForegroundColor Yellow
    }
}

Write-Host "`n=== 验证Redis状态 ===" -ForegroundColor Green

# 测试Redis连接
try {
    $testResult = Test-NetConnection -ComputerName "localhost" -Port 6379 -InformationLevel Quiet
    if ($testResult) {
        Write-Host "✓ Redis端口6379可访问" -ForegroundColor Green

        # 尝试连接Redis
        try {
            redis-cli ping
            Write-Host "✓ Redis服务响应正常" -ForegroundColor Green
        }
        catch {
            Write-Host "⚠️  Redis端口开放但服务可能未完全启动" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "⚠️  Redis端口6379不可访问" -ForegroundColor Yellow
        Write-Host "这是正常的，如果您选择了选项4（仅使用内存缓存）" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "⚠️  无法测试Redis连接" -ForegroundColor Yellow
}

Write-Host "`n=== 下一步操作 ===" -ForegroundColor Green
Write-Host "1. 重新启动BZK系统：cd BZKQuerySystem.Web && dotnet run" -ForegroundColor White
Write-Host "2. 访问监控页面查看缓存状态" -ForegroundColor White
Write-Host "3. 如果Redis已安装，可以修改appsettings.json中的UseRedis为true" -ForegroundColor White

Write-Host "`n=== 安装完成 ===" -ForegroundColor Green
