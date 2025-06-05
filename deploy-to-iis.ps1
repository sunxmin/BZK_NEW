# BZK病种库查询系统 - IIS部署脚本 (PowerShell版本)
# 需要以管理员身份运行

param(
    [string]$SiteName = "BZKQuerySystem",
    [string]$Port = "8080",
    [string]$SitePath = "C:\inetpub\wwwroot\BZKQuerySystem"
)

# 检查管理员权限
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Host "[ERROR] 请以管理员身份运行此脚本！" -ForegroundColor Red
    Write-Host "右键点击PowerShell，选择'以管理员身份运行'" -ForegroundColor Yellow
    Read-Host "按回车键退出"
    exit 1
}

Write-Host "=======================================================" -ForegroundColor Green
Write-Host "       BZK病种库查询系统 - IIS部署脚本" -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host ""

# 配置变量
$SourcePath = Join-Path $PSScriptRoot "BZKQuerySystem.Web\publish"

Write-Host "[INFO] 配置信息:" -ForegroundColor Cyan
Write-Host "        站点名称: $SiteName"
Write-Host "        站点路径: $SitePath"
Write-Host "        源路径: $SourcePath"
Write-Host "        端口: $Port"
Write-Host ""

# 检查源文件是否存在
if (-not (Test-Path $SourcePath)) {
    Write-Host "[ERROR] 发布文件不存在: $SourcePath" -ForegroundColor Red
    Write-Host "请先运行: dotnet publish -c Release -o publish" -ForegroundColor Yellow
    Read-Host "按回车键退出"
    exit 1
}

# 检查并安装IIS功能
Write-Host "[INFO] 检查IIS是否已安装..." -ForegroundColor Yellow

try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "[INFO] IIS已安装" -ForegroundColor Green
}
catch {
    Write-Host "[INFO] 安装IIS功能..." -ForegroundColor Yellow

    $features = @(
        "IIS-WebServerRole",
        "IIS-WebServer",
        "IIS-CommonHttpFeatures",
        "IIS-HttpErrors",
        "IIS-HttpLogging",
        "IIS-RequestFiltering",
        "IIS-StaticContent",
        "IIS-DefaultDocument",
        "IIS-DirectoryBrowsing",
        "IIS-ASPNET45",
        "IIS-NetFxExtensibility45",
        "IIS-ISAPIExtensions",
        "IIS-ISAPIFilter",
        "IIS-ManagementConsole"
    )

    foreach ($feature in $features) {
        try {
            Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart | Out-Null
            Write-Host "  ✓ $feature" -ForegroundColor Green
        }
        catch {
            Write-Host "  ✗ $feature" -ForegroundColor Red
        }
    }

    Write-Host "[INFO] IIS安装完成，请重启计算机后重新运行脚本" -ForegroundColor Green
    Read-Host "按回车键退出"
    exit 0
}

# 检查ASP.NET Core Hosting Bundle
Write-Host "[INFO] 检查ASP.NET Core Hosting Bundle..." -ForegroundColor Yellow
$hostingBundle = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*" |
Where-Object { $_.DisplayName -like "*ASP.NET Core*Hosting Bundle*" -and $_.DisplayName -like "*8.0*" }

if (-not $hostingBundle) {
    Write-Host "[WARNING] 未检测到ASP.NET Core 8.0 Hosting Bundle" -ForegroundColor Yellow
    Write-Host "[INFO] 请从以下链接下载并安装:" -ForegroundColor Cyan
    Write-Host "        https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Cyan
    Write-Host "[INFO] 安装后请重启服务器，然后重新运行此脚本" -ForegroundColor Yellow

    $continue = Read-Host "是否继续部署? (y/N)"
    if ($continue -ne "y" -and $continue -ne "Y") {
        exit 0
    }
}
else {
    Write-Host "[INFO] ASP.NET Core Hosting Bundle已安装" -ForegroundColor Green
}

# 停止现有站点和应用程序池
Write-Host "[INFO] 停止现有站点..." -ForegroundColor Yellow
try {
    Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
    Stop-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}
catch {
    # 忽略错误，可能是因为站点不存在
}

# 删除现有站点和应用程序池
Write-Host "[INFO] 删除现有站点..." -ForegroundColor Yellow
try {
    Remove-Website -Name $SiteName -ErrorAction SilentlyContinue
    Remove-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
}
catch {
    # 忽略错误
}

# 创建站点目录
Write-Host "[INFO] 创建站点目录..." -ForegroundColor Yellow
if (Test-Path $SitePath) {
    Remove-Item $SitePath -Recurse -Force
}
New-Item -ItemType Directory -Path $SitePath -Force | Out-Null

# 复制文件
Write-Host "[INFO] 复制应用程序文件..." -ForegroundColor Yellow
try {
    Copy-Item -Path "$SourcePath\*" -Destination $SitePath -Recurse -Force
    Write-Host "[INFO] 文件复制完成" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] 文件复制失败: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "按回车键退出"
    exit 1
}

# 创建应用程序池
Write-Host "[INFO] 创建应用程序池..." -ForegroundColor Yellow
try {
    New-WebAppPool -Name $SiteName -Force
    Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
    Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name "processModel.idleTimeout" -Value "00:00:00"
    Set-ItemProperty -Path "IIS:\AppPools\$SiteName" -Name "recycling.periodicRestart.time" -Value "00:00:00"
    Write-Host "[INFO] 应用程序池创建完成" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] 应用程序池创建失败: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "按回车键退出"
    exit 1
}

# 创建站点
Write-Host "[INFO] 创建IIS站点..." -ForegroundColor Yellow
try {
    New-Website -Name $SiteName -PhysicalPath $SitePath -Port $Port -ApplicationPool $SiteName
    Write-Host "[INFO] IIS站点创建完成" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] IIS站点创建失败: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "按回车键退出"
    exit 1
}

# 设置权限
Write-Host "[INFO] 设置文件夹权限..." -ForegroundColor Yellow
try {
    $acl = Get-Acl $SitePath

    # 为IIS_IUSRS设置权限
    $accessRule1 = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule1)

    # 为IUSR设置权限
    $accessRule2 = New-Object System.Security.AccessControl.FileSystemAccessRule("IUSR", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule2)

    Set-Acl -Path $SitePath -AclObject $acl
    Write-Host "[INFO] 权限设置完成" -ForegroundColor Green
}
catch {
    Write-Host "[WARNING] 权限设置失败: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 启动应用程序池和站点
Write-Host "[INFO] 启动应用程序池..." -ForegroundColor Yellow
try {
    Start-WebAppPool -Name $SiteName
    Write-Host "[INFO] 应用程序池启动成功" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] 应用程序池启动失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "[INFO] 启动站点..." -ForegroundColor Yellow
try {
    Start-Website -Name $SiteName
    Write-Host "[INFO] 站点启动成功" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] 站点启动失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 部署完成
Write-Host ""
Write-Host "=======================================================" -ForegroundColor Green
Write-Host "               部署完成！" -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host ""
Write-Host "站点已成功部署到IIS" -ForegroundColor Cyan
Write-Host "访问地址: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "站点路径: $SitePath" -ForegroundColor Cyan
Write-Host ""
Write-Host "请确保:" -ForegroundColor Yellow
Write-Host "1. SQL Server服务正在运行" -ForegroundColor White
Write-Host "2. Redis服务正在运行" -ForegroundColor White
Write-Host "3. 防火墙允许端口 $Port" -ForegroundColor White
Write-Host ""
Write-Host "如果遇到问题，请检查:" -ForegroundColor Yellow
Write-Host "1. IIS管理器中的站点状态" -ForegroundColor White
Write-Host "2. Windows事件日志" -ForegroundColor White
Write-Host "3. 应用程序日志文件: $SitePath\logs\" -ForegroundColor White
Write-Host ""

# 测试站点是否可访问
Write-Host "[INFO] 测试站点访问..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$Port" -TimeoutSec 10 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "[SUCCESS] 站点可正常访问！" -ForegroundColor Green
    }
    else {
        Write-Host "[WARNING] 站点响应状态码: $($response.StatusCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "[WARNING] 站点测试失败: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "请手动在浏览器中测试: http://localhost:$Port" -ForegroundColor Cyan
}

Read-Host "按回车键退出"
