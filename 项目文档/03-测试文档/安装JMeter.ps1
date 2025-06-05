# Apache JMeter 自动安装脚本
# 版本: v1.0
# 支持自动下载、解压、配置环境变量和中文设置

param(
    [Parameter(Mandatory=$false)]
    [string]$InstallPath = "D:\tools",
    
    [Parameter(Mandatory=$false)]
    [switch]$SetChineseLanguage = $true
)

# 脚本配置
$JMeterVersion = "5.6.3"
$JMeterFileName = "apache-jmeter-$JMeterVersion"
$JMeterZipFile = "$JMeterFileName.zip"
$DownloadUrl = "https://dlcdn.apache.org/jmeter/binaries/$JMeterZipFile"
$JMeterHome = Join-Path $InstallPath $JMeterFileName

# 颜色输出函数
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# 检查管理员权限
function Test-AdminRights {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# 检查Java安装
function Test-JavaInstallation {
    try {
        $javaVersion = java -version 2>&1
        if ($javaVersion -match "version") {
            Write-ColorOutput "? Java已安装: $($javaVersion[0])" "Green"
            return $true
        }
    }
    catch {
        Write-ColorOutput "? Java未安装或未添加到PATH" "Red"
        Write-ColorOutput "请先安装Java 8或更高版本: https://www.oracle.com/java/technologies/downloads/" "Yellow"
        return $false
    }
}

# 下载JMeter
function Download-JMeter {
    param([string]$Url, [string]$OutputPath)
    
    Write-ColorOutput "? 开始下载Apache JMeter $JMeterVersion..." "Blue"
    Write-ColorOutput "下载地址: $Url" "Cyan"
    Write-ColorOutput "保存位置: $OutputPath" "Cyan"
    
    try {
        # 检查是否已存在
        if (Test-Path $OutputPath) {
            Write-ColorOutput "文件已存在，跳过下载" "Yellow"
            return $true
        }
        
        # 使用Invoke-WebRequest下载，显示进度
        $ProgressPreference = 'Continue'
        Invoke-WebRequest -Uri $Url -OutFile $OutputPath -UseBasicParsing
        
        Write-ColorOutput "? 下载完成!" "Green"
        return $true
    }
    catch {
        Write-ColorOutput "? 下载失败: $($_.Exception.Message)" "Red"
        Write-ColorOutput "请手动下载: $Url" "Yellow"
        return $false
    }
}

# 解压JMeter
function Extract-JMeter {
    param([string]$ZipPath, [string]$ExtractPath)
    
    Write-ColorOutput "? 解压JMeter文件..." "Blue"
    
    try {
        if (Test-Path $ExtractPath) {
            Write-ColorOutput "目标目录已存在，删除旧版本..." "Yellow"
            Remove-Item $ExtractPath -Recurse -Force
        }
        
        # 使用内置的Expand-Archive
        Expand-Archive -Path $ZipPath -DestinationPath (Split-Path $ExtractPath) -Force
        
        Write-ColorOutput "? 解压完成!" "Green"
        return $true
    }
    catch {
        Write-ColorOutput "? 解压失败: $($_.Exception.Message)" "Red"
        return $false
    }
}

# 设置环境变量
function Set-EnvironmentVariables {
    param([string]$JMeterPath)
    
    Write-ColorOutput "?? 配置环境变量..." "Blue"
    
    try {
        # 设置JMETER_HOME
        [Environment]::SetEnvironmentVariable("JMETER_HOME", $JMeterPath, "Machine")
        Write-ColorOutput "? 设置JMETER_HOME: $JMeterPath" "Green"
        
        # 添加到PATH
        $currentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
        $jmeterBinPath = Join-Path $JMeterPath "bin"
        
        if ($currentPath -notlike "*$jmeterBinPath*") {
            $newPath = "$currentPath;$jmeterBinPath"
            [Environment]::SetEnvironmentVariable("Path", $newPath, "Machine")
            Write-ColorOutput "? 添加到PATH: $jmeterBinPath" "Green"
        } else {
            Write-ColorOutput "PATH中已存在JMeter路径" "Yellow"
        }
        
        # 刷新当前会话的环境变量
        $env:JMETER_HOME = $JMeterPath
        $env:Path += ";$jmeterBinPath"
        
        return $true
    }
    catch {
        Write-ColorOutput "? 设置环境变量失败: $($_.Exception.Message)" "Red"
        Write-ColorOutput "请手动设置环境变量" "Yellow"
        return $false
    }
}

# 设置中文界面
function Set-ChineseLanguage {
    param([string]$JMeterPath)
    
    if (-not $SetChineseLanguage) {
        return
    }
    
    Write-ColorOutput "?? 设置中文界面..." "Blue"
    
    $propertiesFile = Join-Path $JMeterPath "bin\jmeter.properties"
    
    try {
        if (Test-Path $propertiesFile) {
            $content = Get-Content $propertiesFile -Encoding UTF8
            $newContent = @()
            
            foreach ($line in $content) {
                if ($line -match "^#?language=") {
                    $newContent += "language=zh_CN"
                    Write-ColorOutput "? 设置语言为中文" "Green"
                } else {
                    $newContent += $line
                }
            }
            
            $newContent | Set-Content $propertiesFile -Encoding UTF8
        }
    }
    catch {
        Write-ColorOutput "? 设置中文界面失败: $($_.Exception.Message)" "Red"
    }
}

# 验证安装
function Test-Installation {
    param([string]$JMeterPath)
    
    Write-ColorOutput "? 验证安装..." "Blue"
    
    $jmeterBat = Join-Path $JMeterPath "bin\jmeter.bat"
    $jmeterJar = Join-Path $JMeterPath "bin\ApacheJMeter.jar"
    
    if ((Test-Path $jmeterBat) -and (Test-Path $jmeterJar)) {
        Write-ColorOutput "? JMeter安装验证成功!" "Green"
        
        # 尝试获取版本信息
        try {
            $versionInfo = & $jmeterBat -version 2>$null
            if ($versionInfo) {
                Write-ColorOutput "JMeter版本信息获取成功" "Green"
            }
        }
        catch {
            Write-ColorOutput "?? 版本信息获取失败，但文件存在" "Yellow"
        }
        
        return $true
    } else {
        Write-ColorOutput "? JMeter安装验证失败" "Red"
        return $false
    }
}

# 创建桌面快捷方式
function New-DesktopShortcut {
    param([string]$JMeterPath)
    
    Write-ColorOutput "?? 创建桌面快捷方式..." "Blue"
    
    try {
        $WshShell = New-Object -ComObject WScript.Shell
        $Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Apache JMeter.lnk")
        $Shortcut.TargetPath = Join-Path $JMeterPath "bin\jmeter.bat"
        $Shortcut.WorkingDirectory = Join-Path $JMeterPath "bin"
        $Shortcut.Description = "Apache JMeter Performance Testing Tool"
        $Shortcut.Save()
        
        Write-ColorOutput "? 桌面快捷方式创建成功!" "Green"
    }
    catch {
        Write-ColorOutput "?? 桌面快捷方式创建失败: $($_.Exception.Message)" "Yellow"
    }
}

# 主安装函数
function Install-JMeter {
    Write-ColorOutput "? Apache JMeter $JMeterVersion 自动安装程序" "Magenta"
    Write-ColorOutput "=============================================" "Magenta"
    
    # 检查管理员权限
    if (-not (Test-AdminRights)) {
        Write-ColorOutput "?? 建议以管理员身份运行以正确设置环境变量" "Yellow"
        $continue = Read-Host "是否继续? (y/N)"
        if ($continue -ne "y" -and $continue -ne "Y") {
            exit 1
        }
    }
    
    # 检查Java
    if (-not (Test-JavaInstallation)) {
        exit 1
    }
    
    # 创建安装目录
    if (-not (Test-Path $InstallPath)) {
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
        Write-ColorOutput "? 创建安装目录: $InstallPath" "Green"
    }
    
    # 下载
    $zipPath = Join-Path $InstallPath $JMeterZipFile
    if (-not (Download-JMeter $DownloadUrl $zipPath)) {
        exit 1
    }
    
    # 解压
    if (-not (Extract-JMeter $zipPath $JMeterHome)) {
        exit 1
    }
    
    # 设置环境变量
    if (Test-AdminRights) {
        Set-EnvironmentVariables $JMeterHome
    } else {
        Write-ColorOutput "?? 无管理员权限，跳过环境变量设置" "Yellow"
        Write-ColorOutput "请手动设置环境变量:" "Yellow"
        Write-ColorOutput "JMETER_HOME = $JMeterHome" "Cyan"
        Write-ColorOutput "PATH 添加 = $JMeterHome\bin" "Cyan"
    }
    
    # 设置中文
    Set-ChineseLanguage $JMeterHome
    
    # 验证安装
    if (Test-Installation $JMeterHome) {
        # 创建快捷方式
        New-DesktopShortcut $JMeterHome
        
        # 清理下载文件
        Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
        
        Write-ColorOutput "=============================================" "Magenta"
        Write-ColorOutput "? Apache JMeter 安装完成!" "Green"
        Write-ColorOutput "安装路径: $JMeterHome" "Cyan"
        Write-ColorOutput "启动方式:" "Cyan"
        Write-ColorOutput "1. 双击桌面快捷方式" "White"
        Write-ColorOutput "2. 命令行输入: jmeter" "White"
        Write-ColorOutput "3. 运行: $JMeterHome\bin\jmeter.bat" "White"
        Write-ColorOutput "=============================================" "Magenta"
        
        # 询问是否立即启动
        $launch = Read-Host "是否立即启动JMeter? (y/N)"
        if ($launch -eq "y" -or $launch -eq "Y") {
            Start-Process -FilePath (Join-Path $JMeterHome "bin\jmeter.bat")
        }
    } else {
        Write-ColorOutput "? 安装过程中出现错误" "Red"
        exit 1
    }
}

# 脚本入口
try {
    Install-JMeter
}
catch {
    Write-ColorOutput "? 安装脚本执行失败: $($_.Exception.Message)" "Red"
    exit 1
} 