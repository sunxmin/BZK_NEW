# BZK查询系统 - Redis缓存服务自动化安装脚本
# 版本: v1.0
# 创建日期: 2025年6月3日
# 适用环境: Windows 10/11

param(
    [string]$InstallPath = "D:\Redis",
    [string]$Password = "BZK_Redis_2025",
    [string]$Port = "6379",
    [int]$MaxMemory = 1024,  # MB
    [switch]$StartService = $true,
    [switch]$VerboseOutput = $false
)

# 设置错误处理
$ErrorActionPreference = "Stop"

# 输出控制
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    
    if ($VerboseOutput) {
        Write-Host $Message -ForegroundColor $Color
    }
}

function Write-StepHeader {
    param([string]$Step)
    Write-Host "`n===================================================" -ForegroundColor Cyan
    Write-Host "? $Step" -ForegroundColor Yellow
    Write-Host "===================================================" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "?? $Message" -ForegroundColor Yellow
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Red
}

Write-Host @"
╔══════════════════════════════════════════════════════════════╗
║                    BZK Redis 缓存服务安装器                  ║
║                         版本: v1.0                          ║
╚══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Magenta

# 步骤1: 环境检查
Write-StepHeader "步骤1: 环境检查"

try {
    # 检查管理员权限
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    if (-not $isAdmin) {
        Write-Warning "检测到非管理员权限，某些功能可能受限"
        Write-Warning "建议以管理员身份运行PowerShell"
    } else {
        Write-Success "管理员权限检查通过"
    }

    # 检查.NET版本
    $dotnetVersion = dotnet --version 2>$null
    if ($dotnetVersion) {
        Write-Success ".NET版本: $dotnetVersion"
    } else {
        Write-Warning ".NET未安装或未配置PATH"
    }

    # 检查端口占用
    $portInUse = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    if ($portInUse) {
        Write-Error-Custom "端口 $Port 已被占用，请检查或更换端口"
        return
    } else {
        Write-Success "端口 $Port 可用"
    }

    # 检查磁盘空间
    $drive = Split-Path $InstallPath -Qualifier
    $freeSpace = (Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DeviceID -eq $drive}).FreeSpace / 1GB
    if ($freeSpace -lt 1) {
        Write-Warning "磁盘剩余空间较少: $([math]::Round($freeSpace, 2)) GB"
    } else {
        Write-Success "磁盘空间充足: $([math]::Round($freeSpace, 2)) GB"
    }

    Write-Success "环境检查完成"
}
catch {
    Write-Error-Custom "环境检查失败: $($_.Exception.Message)"
    return
}

# 步骤2: 下载Redis
Write-StepHeader "步骤2: 下载Redis"

try {
    $redisUrl = "https://github.com/microsoftarchive/redis/releases/download/win-3.2.100/Redis-x64-3.2.100.zip"
    $zipPath = "$env:TEMP\Redis-x64-3.2.100.zip"
    
    Write-ColorOutput "正在下载Redis..." "Yellow"
    Write-ColorOutput "下载地址: $redisUrl" "Gray"
    Write-ColorOutput "保存路径: $zipPath" "Gray"
    
    # 使用WebClient下载
    $webClient = New-Object System.Net.WebClient
    $webClient.DownloadFile($redisUrl, $zipPath)
    
    # 验证下载文件
    if (Test-Path $zipPath) {
        $fileSize = (Get-Item $zipPath).Length / 1MB
        Write-Success "Redis下载完成，文件大小: $([math]::Round($fileSize, 2)) MB"
    } else {
        throw "下载文件不存在"
    }
}
catch {
    Write-Error-Custom "Redis下载失败: $($_.Exception.Message)"
    Write-Warning "尝试手动下载: $redisUrl"
    return
}

# 步骤3: 解压安装
Write-StepHeader "步骤3: 解压安装"

try {
    # 创建安装目录
    if (-not (Test-Path $InstallPath)) {
        New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
        Write-Success "创建安装目录: $InstallPath"
    }

    # 解压文件
    Write-ColorOutput "正在解压Redis..." "Yellow"
    Expand-Archive -Path $zipPath -DestinationPath $InstallPath -Force
    
    # 验证解压结果
    $redisExe = Join-Path $InstallPath "redis-server.exe"
    $redisCliExe = Join-Path $InstallPath "redis-cli.exe"
    
    if ((Test-Path $redisExe) -and (Test-Path $redisCliExe)) {
        Write-Success "Redis解压完成"
        Write-ColorOutput "Redis服务器: $redisExe" "Gray"
        Write-ColorOutput "Redis客户端: $redisCliExe" "Gray"
    } else {
        throw "关键文件缺失"
    }

    # 清理下载文件
    Remove-Item $zipPath -Force
    Write-ColorOutput "清理临时文件: $zipPath" "Gray"
}
catch {
    Write-Error-Custom "Redis解压安装失败: $($_.Exception.Message)"
    return
}

# 步骤4: 创建配置文件
Write-StepHeader "步骤4: 创建配置文件"

try {
    $configPath = Join-Path $InstallPath "redis.windows.conf"
    
    $configContent = @"
# BZK Redis 配置文件
# 创建时间: $(Get-Date)

# 服务器配置
bind 127.0.0.1
port $Port
timeout 0

# 内存配置
maxmemory ${MaxMemory}mb
maxmemory-policy allkeys-lru

# 持久化配置
save 900 1
save 300 10
save 60 10000

# 日志配置
loglevel notice
logfile "$InstallPath\redis.log"

# 安全配置
requirepass $Password

# 网络优化
tcp-keepalive 60
tcp-backlog 511

# 内存优化
hash-max-ziplist-entries 512
hash-max-ziplist-value 64
list-max-ziplist-size -2
set-max-intset-entries 512

# 持久化优化
stop-writes-on-bgsave-error yes
rdbcompression yes
rdbchecksum yes

# Windows特定配置
service-name Redis-BZK
service-run yes
"@

    Set-Content -Path $configPath -Value $configContent -Encoding UTF8
    Write-Success "配置文件创建完成: $configPath"
    
    # 显示关键配置
    Write-ColorOutput "配置摘要:" "Cyan"
    Write-ColorOutput "  端口: $Port" "Gray"
    Write-ColorOutput "  密码: $Password" "Gray"
    Write-ColorOutput "  最大内存: ${MaxMemory}MB" "Gray"
    Write-ColorOutput "  日志文件: $InstallPath\redis.log" "Gray"
}
catch {
    Write-Error-Custom "配置文件创建失败: $($_.Exception.Message)"
    return
}

# 步骤5: 注册Windows服务
Write-StepHeader "步骤5: 注册Windows服务"

try {
    $serviceName = "Redis-BZK"
    $redisServerExe = Join-Path $InstallPath "redis-server.exe"
    $configPath = Join-Path $InstallPath "redis.windows.conf"
    
    # 检查服务是否已存在
    $existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Warning "服务 $serviceName 已存在，先停止并删除"
        Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
        sc.exe delete $serviceName | Out-Null
        Start-Sleep -Seconds 2
    }
    
    # 注册新服务
    if ($isAdmin) {
        Write-ColorOutput "正在注册Windows服务..." "Yellow"
        $serviceCmd = "`"$redisServerExe`" `"$configPath`""
        sc.exe create $serviceName binPath= $serviceCmd start= auto DisplayName= "BZK Redis缓存服务" | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Windows服务注册成功: $serviceName"
        } else {
            throw "服务注册失败，错误代码: $LASTEXITCODE"
        }
    } else {
        Write-Warning "没有管理员权限，跳过服务注册"
        Write-Warning "手动注册命令: sc.exe create $serviceName binPath= `"$redisServerExe $configPath`""
    }
}
catch {
    Write-Error-Custom "Windows服务注册失败: $($_.Exception.Message)"
    Write-Warning "可以手动运行: $redisServerExe $configPath"
}

# 步骤6: 配置环境变量
Write-StepHeader "步骤6: 配置环境变量"

try {
    # 添加Redis到PATH
    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($currentPath -notlike "*$InstallPath*") {
        Write-ColorOutput "正在添加Redis到PATH环境变量..." "Yellow"
        $newPath = "$currentPath;$InstallPath"
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
        
        # 更新当前会话的PATH
        $env:Path += ";$InstallPath"
        Write-Success "PATH环境变量配置完成"
    } else {
        Write-Success "Redis已在PATH环境变量中"
    }
    
    # 设置REDIS_HOME
    [Environment]::SetEnvironmentVariable("REDIS_HOME", $InstallPath, "User")
    $env:REDIS_HOME = $InstallPath
    Write-Success "REDIS_HOME环境变量设置完成: $InstallPath"
}
catch {
    Write-Error-Custom "环境变量配置失败: $($_.Exception.Message)"
    Write-Warning "请手动添加 $InstallPath 到PATH环境变量"
}

# 步骤7: 启动Redis服务
Write-StepHeader "步骤7: 启动Redis服务"

try {
    if ($StartService) {
        $serviceName = "Redis-BZK"
        $redisServerExe = Join-Path $InstallPath "redis-server.exe"
        $configPath = Join-Path $InstallPath "redis.windows.conf"
        
        if ($isAdmin -and (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
            Write-ColorOutput "正在启动Redis服务..." "Yellow"
            Start-Service -Name $serviceName
            Start-Sleep -Seconds 3
            
            $service = Get-Service -Name $serviceName
            if ($service.Status -eq "Running") {
                Write-Success "Redis服务启动成功"
            } else {
                throw "服务状态异常: $($service.Status)"
            }
        } else {
            Write-Warning "以进程方式启动Redis（非服务模式）"
            Write-ColorOutput "启动命令: $redisServerExe $configPath" "Gray"
            
            # 在后台启动Redis进程
            Start-Process -FilePath $redisServerExe -ArgumentList $configPath -WindowStyle Minimized
            Start-Sleep -Seconds 3
            Write-Success "Redis进程启动完成"
        }
    } else {
        Write-Warning "跳过Redis服务启动"
    }
}
catch {
    Write-Error-Custom "Redis服务启动失败: $($_.Exception.Message)"
    Write-Warning "可以手动启动: $redisServerExe $configPath"
}

# 步骤8: 连接测试
Write-StepHeader "步骤8: 连接测试"

try {
    Start-Sleep -Seconds 2
    $redisCliExe = Join-Path $InstallPath "redis-cli.exe"
    
    # 测试连接
    Write-ColorOutput "正在测试Redis连接..." "Yellow"
    
    # Ping测试
    $pingResult = & $redisCliExe -p $Port -a $Password ping 2>$null
    if ($pingResult -eq "PONG") {
        Write-Success "Redis连接测试成功"
    } else {
        throw "Ping测试失败: $pingResult"
    }
    
    # 版本信息
    $versionInfo = & $redisCliExe -p $Port -a $Password info server 2>$null | Select-String "redis_version"
    if ($versionInfo) {
        Write-Success "Redis版本: $($versionInfo.Line.Split(':')[1])"
    }
    
    # 内存信息
    $memoryInfo = & $redisCliExe -p $Port -a $Password info memory 2>$null | Select-String "used_memory_human"
    if ($memoryInfo) {
        Write-Success "内存使用: $($memoryInfo.Line.Split(':')[1])"
    }
    
}
catch {
    Write-Error-Custom "Redis连接测试失败: $($_.Exception.Message)"
    Write-Warning "请检查Redis服务状态和配置"
}

# 步骤9: 创建管理脚本
Write-StepHeader "步骤9: 创建管理脚本"

try {
    # 创建快速启动脚本
    $startScriptPath = Join-Path $InstallPath "启动Redis.bat"
    $startScriptContent = @"
@echo off
echo 启动BZK Redis缓存服务...
cd /d "$InstallPath"
redis-server.exe redis.windows.conf
pause
"@
    Set-Content -Path $startScriptPath -Value $startScriptContent -Encoding UTF8
    
    # 创建快速连接脚本
    $connectScriptPath = Join-Path $InstallPath "连接Redis.bat"
    $connectScriptContent = @"
@echo off
echo 连接BZK Redis缓存服务...
cd /d "$InstallPath"
redis-cli.exe -p $Port -a $Password
pause
"@
    Set-Content -Path $connectScriptPath -Value $connectScriptContent -Encoding UTF8
    
    # 创建PowerShell管理脚本
    $managementScriptPath = Join-Path $InstallPath "管理Redis.ps1"
    $managementScriptContent = @"
# BZK Redis 管理脚本
param([string]`$Action = "status")

`$redisPath = "$InstallPath"
`$serviceName = "Redis-BZK"

switch (`$Action) {
    "start" { 
        Start-Service -Name `$serviceName
        Write-Host "? Redis服务已启动" -ForegroundColor Green
    }
    "stop" { 
        Stop-Service -Name `$serviceName
        Write-Host "?? Redis服务已停止" -ForegroundColor Yellow
    }
    "restart" { 
        Restart-Service -Name `$serviceName
        Write-Host "? Redis服务已重启" -ForegroundColor Blue
    }
    "status" { 
        `$service = Get-Service -Name `$serviceName -ErrorAction SilentlyContinue
        if (`$service) {
            Write-Host "? Redis服务状态: `$(`$service.Status)" -ForegroundColor Cyan
        } else {
            Write-Host "? Redis服务未找到" -ForegroundColor Red
        }
    }
    "test" {
        & "`$redisPath\redis-cli.exe" -p $Port -a $Password ping
    }
    default { 
        Write-Host "用法: .\管理Redis.ps1 [start|stop|restart|status|test]"
    }
}
"@
    Set-Content -Path $managementScriptPath -Value $managementScriptContent -Encoding UTF8
    
    Write-Success "管理脚本创建完成:"
    Write-ColorOutput "  启动脚本: $startScriptPath" "Gray"
    Write-ColorOutput "  连接脚本: $connectScriptPath" "Gray"
    Write-ColorOutput "  管理脚本: $managementScriptPath" "Gray"
}
catch {
    Write-Error-Custom "管理脚本创建失败: $($_.Exception.Message)"
}

# 步骤10: 创建桌面快捷方式
Write-StepHeader "步骤10: 创建桌面快捷方式"

try {
    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktopPath "BZK Redis管理.lnk"
    
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = Join-Path $InstallPath "连接Redis.bat"
    $shortcut.WorkingDirectory = $InstallPath
    $shortcut.Description = "BZK Redis缓存服务管理"
    $shortcut.Save()
    
    Write-Success "桌面快捷方式创建完成: $shortcutPath"
}
catch {
    Write-Error-Custom "桌面快捷方式创建失败: $($_.Exception.Message)"
}

# 安装完成总结
Write-Host "`n" -NoNewline
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                    ? Redis安装完成！                        ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green

Write-Host "`n? 安装信息总结:" -ForegroundColor Cyan
Write-Host "  ? 安装路径: $InstallPath" -ForegroundColor White
Write-Host "  ? 端口: $Port" -ForegroundColor White
Write-Host "  ? 密码: $Password" -ForegroundColor White
Write-Host "  ? 最大内存: ${MaxMemory}MB" -ForegroundColor White
Write-Host "  ? 配置文件: $InstallPath\redis.windows.conf" -ForegroundColor White
Write-Host "  ? 日志文件: $InstallPath\redis.log" -ForegroundColor White

Write-Host "`n? 快速使用:" -ForegroundColor Cyan
Write-Host "  连接Redis: redis-cli -p $Port -a $Password" -ForegroundColor Yellow
Write-Host "  启动服务: net start Redis-BZK" -ForegroundColor Yellow
Write-Host "  停止服务: net stop Redis-BZK" -ForegroundColor Yellow
Write-Host "  管理脚本: .\管理Redis.ps1 [start|stop|restart|status|test]" -ForegroundColor Yellow

Write-Host "`n? .NET集成连接字符串:" -ForegroundColor Cyan
Write-Host "  localhost:$Port,password=$Password" -ForegroundColor Green

Write-Host "`n?? 重要提醒:" -ForegroundColor Yellow
Write-Host "  1. 请妥善保管Redis密码: $Password" -ForegroundColor White
Write-Host "  2. 生产环境请修改bind地址和增强密码" -ForegroundColor White
Write-Host "  3. 定期备份Redis数据" -ForegroundColor White
Write-Host "  4. 监控Redis内存使用情况" -ForegroundColor White

Write-Host "`n? 下一步: 更新BZK项目配置，集成Redis缓存服务" -ForegroundColor Magenta
Write-Host "安装脚本执行完成！" -ForegroundColor Green 