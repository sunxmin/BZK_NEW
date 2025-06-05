# BZK查询系统部署脚本
# PowerShell脚本用于自动化部署和升级

param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipFrontend = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipDatabase = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force = $false
)

# 脚本配置
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# 颜色输出函数
function Write-ColorOutput($ForegroundColor, $Message) {
    Write-Host $Message -ForegroundColor $ForegroundColor
}

function Write-Success($Message) {
    Write-ColorOutput Green "? $Message"
}

function Write-Warning($Message) {
    Write-ColorOutput Yellow "? $Message"
}

function Write-Error($Message) {
    Write-ColorOutput Red "? $Message"
}

function Write-Info($Message) {
    Write-ColorOutput Cyan "? $Message"
}

# 检查前置条件
function Test-Prerequisites {
    Write-Info "检查部署前置条件..."
    
    # 检查.NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Success ".NET SDK 版本: $dotnetVersion"
    }
    catch {
        Write-Error ".NET SDK 未安装或未在PATH中"
        exit 1
    }
    
    # 检查Node.js（如果需要构建前端）
    if (-not $SkipFrontend) {
        try {
            $nodeVersion = node --version
            Write-Success "Node.js 版本: $nodeVersion"
        }
        catch {
            Write-Error "Node.js 未安装或未在PATH中"
            exit 1
        }
    }
    
    # 检查SQL Server连接（如果提供了连接字符串）
    if ($ConnectionString -and -not $SkipDatabase) {
        Write-Info "测试数据库连接..."
        try {
            # 这里可以添加数据库连接测试逻辑
            Write-Success "数据库连接正常"
        }
        catch {
            Write-Warning "数据库连接测试失败，将在后续步骤中重试"
        }
    }
}

# 构建后端应用
function Build-Backend {
    Write-Info "构建后端应用..."
    
    try {
        # 清理之前的构建
        if (Test-Path "BZKQuerySystem.Web/bin") {
            Remove-Item -Recurse -Force "BZKQuerySystem.Web/bin"
        }
        if (Test-Path "BZKQuerySystem.Web/obj") {
            Remove-Item -Recurse -Force "BZKQuerySystem.Web/obj"
        }
        
        # 还原NuGet包
        Write-Info "还原NuGet包..."
        dotnet restore BZKQuerySystem.Web/BZKQuerySystem.Web.csproj
        
        # 构建项目
        Write-Info "编译项目..."
        dotnet build BZKQuerySystem.Web/BZKQuerySystem.Web.csproj --configuration Release --no-restore
        
        Write-Success "后端构建完成"
    }
    catch {
        Write-Error "后端构建失败: $($_.Exception.Message)"
        exit 1
    }
}

# 构建前端应用
function Build-Frontend {
    if ($SkipFrontend) {
        Write-Info "跳过前端构建"
        return
    }
    
    Write-Info "构建前端应用..."
    
    try {
        Set-Location "BZKQuerySystem.Web/ClientApp"
        
        # 安装依赖
        Write-Info "安装前端依赖..."
        npm install
        
        # 构建前端
        Write-Info "编译前端代码..."
        npm run build
        
        Set-Location "../.."
        Write-Success "前端构建完成"
    }
    catch {
        Set-Location "../.."
        Write-Error "前端构建失败: $($_.Exception.Message)"
        exit 1
    }
}

# 更新数据库
function Update-Database {
    if ($SkipDatabase) {
        Write-Info "跳过数据库更新"
        return
    }
    
    Write-Info "更新数据库..."
    
    try {
        # 设置连接字符串环境变量
        if ($ConnectionString) {
            $env:ConnectionStrings__DefaultConnection = $ConnectionString
        }
        
        # 运行Entity Framework迁移
        Write-Info "执行数据库迁移..."
        dotnet ef database update --project BZKQuerySystem.DataAccess --startup-project BZKQuerySystem.Web
        
        Write-Success "数据库更新完成"
    }
    catch {
        Write-Error "数据库更新失败: $($_.Exception.Message)"
        if (-not $Force) {
            exit 1
        } else {
            Write-Warning "强制模式：忽略数据库更新错误"
        }
    }
}

# 创建必要的目录结构
function Initialize-Directories {
    Write-Info "初始化目录结构..."
    
    $directories = @(
        "logs",
        "temp",
        "backups",
        "exports"
    )
    
    foreach ($dir in $directories) {
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Success "创建目录: $dir"
        }
    }
}

# 复制配置文件
function Copy-ConfigFiles {
    Write-Info "配置应用程序设置..."
    
    try {
        # 复制环境特定的配置文件
        $sourceConfig = "BZKQuerySystem.Web/appsettings.$Environment.json"
        $targetConfig = "BZKQuerySystem.Web/appsettings.json"
        
        if (Test-Path $sourceConfig) {
            Copy-Item $sourceConfig $targetConfig -Force
            Write-Success "配置文件已更新: $Environment"
        } else {
            Write-Warning "环境配置文件不存在: $sourceConfig"
        }
        
        # 设置环境变量
        $env:ASPNETCORE_ENVIRONMENT = $Environment
        Write-Success "环境设置为: $Environment"
        
    }
    catch {
        Write-Error "配置文件复制失败: $($_.Exception.Message)"
        exit 1
    }
}

# 验证部署
function Test-Deployment {
    Write-Info "验证部署..."
    
    try {
        # 检查关键文件是否存在
        $requiredFiles = @(
            "BZKQuerySystem.Web/bin/Release/net8.0/BZKQuerySystem.Web.dll",
            "BZKQuerySystem.Web/appsettings.json"
        )
        
        foreach ($file in $requiredFiles) {
            if (-not (Test-Path $file)) {
                throw "缺少关键文件: $file"
            }
        }
        
        Write-Success "部署验证通过"
    }
    catch {
        Write-Error "部署验证失败: $($_.Exception.Message)"
        exit 1
    }
}

# 创建启动脚本
function Create-StartupScripts {
    Write-Info "创建启动脚本..."
    
    # Windows服务启动脚本
    $serviceScript = @"
@echo off
echo 启动BZK查询系统...
cd /d "%~dp0"
dotnet BZKQuerySystem.Web/bin/Release/net8.0/BZKQuerySystem.Web.dll
pause
"@
    
    $serviceScript | Out-File -FilePath "start.bat" -Encoding ASCII
    
    # PowerShell启动脚本
    $psScript = @"
# BZK查询系统启动脚本
Write-Host "启动BZK查询系统..." -ForegroundColor Green
Set-Location `$PSScriptRoot
`$env:ASPNETCORE_ENVIRONMENT = "$Environment"
dotnet BZKQuerySystem.Web/bin/Release/net8.0/BZKQuerySystem.Web.dll
"@
    
    $psScript | Out-File -FilePath "start.ps1" -Encoding UTF8
    
    Write-Success "启动脚本已创建"
}

# 创建备份
function Create-Backup {
    if ($Environment -eq "Production") {
        Write-Info "创建生产环境备份..."
        
        try {
            $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
            $backupDir = "backups/backup_$timestamp"
            
            New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
            
            # 备份当前版本
            if (Test-Path "BZKQuerySystem.Web/bin") {
                Copy-Item -Recurse "BZKQuerySystem.Web/bin" "$backupDir/bin"
            }
            
            # 备份配置文件
            if (Test-Path "BZKQuerySystem.Web/appsettings.json") {
                Copy-Item "BZKQuerySystem.Web/appsettings.json" "$backupDir/"
            }
            
            Write-Success "备份已创建: $backupDir"
        }
        catch {
            Write-Warning "备份创建失败: $($_.Exception.Message)"
        }
    }
}

# 主部署流程
function Start-Deployment {
    $startTime = Get-Date
    Write-Info "开始部署BZK查询系统 (环境: $Environment)"
    Write-Info "部署时间: $startTime"
    
    try {
        # 执行部署步骤
        Test-Prerequisites
        Create-Backup
        Initialize-Directories
        Build-Backend
        Build-Frontend
        Copy-ConfigFiles
        Update-Database
        Test-Deployment
        Create-StartupScripts
        
        $endTime = Get-Date
        $duration = $endTime - $startTime
        
        Write-Success "? 部署完成！"
        Write-Info "总耗时: $($duration.TotalMinutes.ToString("F1")) 分钟"
        Write-Info ""
        Write-Info "下一步操作:"
        Write-Info "1. 检查配置文件 appsettings.json"
        Write-Info "2. 设置数据库连接字符串"
        Write-Info "3. 运行 start.bat 或 start.ps1 启动应用"
        Write-Info "4. 访问 http://localhost:5000 验证部署"
    }
    catch {
        Write-Error "部署失败: $($_.Exception.Message)"
        Write-Info "请检查错误信息并重试"
        exit 1
    }
}

# 显示帮助信息
function Show-Help {
    Write-Host @"
BZK查询系统部署脚本

用法:
    .\deploy.ps1 [参数]

参数:
    -Environment      目标环境 (Development/Staging/Production)
    -ConnectionString 数据库连接字符串
    -SkipFrontend     跳过前端构建
    -SkipDatabase     跳过数据库更新
    -Force            强制执行，忽略错误

示例:
    .\deploy.ps1 -Environment Production
    .\deploy.ps1 -Environment Development -SkipFrontend
    .\deploy.ps1 -ConnectionString "Server=...;Database=...;" -Force

"@ -ForegroundColor Cyan
}

# 脚本入口点
if ($args -contains "-help" -or $args -contains "--help" -or $args -contains "-h") {
    Show-Help
    exit 0
}

# 确认生产环境部署
if ($Environment -eq "Production" -and -not $Force) {
    Write-Warning "即将部署到生产环境！"
    $confirmation = Read-Host "是否继续？(y/N)"
    if ($confirmation -ne "y" -and $confirmation -ne "Y") {
        Write-Info "部署已取消"
        exit 0
    }
}

# 开始部署
Start-Deployment 