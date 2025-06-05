# Apache JMeter �Զ���װ�ű�
# �汾: v1.0
# ֧���Զ����ء���ѹ�����û�����������������

param(
    [Parameter(Mandatory=$false)]
    [string]$InstallPath = "D:\tools",
    
    [Parameter(Mandatory=$false)]
    [switch]$SetChineseLanguage = $true
)

# �ű�����
$JMeterVersion = "5.6.3"
$JMeterFileName = "apache-jmeter-$JMeterVersion"
$JMeterZipFile = "$JMeterFileName.zip"
$DownloadUrl = "https://dlcdn.apache.org/jmeter/binaries/$JMeterZipFile"
$JMeterHome = Join-Path $InstallPath $JMeterFileName

# ��ɫ�������
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# ������ԱȨ��
function Test-AdminRights {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# ���Java��װ
function Test-JavaInstallation {
    try {
        $javaVersion = java -version 2>&1
        if ($javaVersion -match "version") {
            Write-ColorOutput "? Java�Ѱ�װ: $($javaVersion[0])" "Green"
            return $true
        }
    }
    catch {
        Write-ColorOutput "? Javaδ��װ��δ��ӵ�PATH" "Red"
        Write-ColorOutput "���Ȱ�װJava 8����߰汾: https://www.oracle.com/java/technologies/downloads/" "Yellow"
        return $false
    }
}

# ����JMeter
function Download-JMeter {
    param([string]$Url, [string]$OutputPath)
    
    Write-ColorOutput "? ��ʼ����Apache JMeter $JMeterVersion..." "Blue"
    Write-ColorOutput "���ص�ַ: $Url" "Cyan"
    Write-ColorOutput "����λ��: $OutputPath" "Cyan"
    
    try {
        # ����Ƿ��Ѵ���
        if (Test-Path $OutputPath) {
            Write-ColorOutput "�ļ��Ѵ��ڣ���������" "Yellow"
            return $true
        }
        
        # ʹ��Invoke-WebRequest���أ���ʾ����
        $ProgressPreference = 'Continue'
        Invoke-WebRequest -Uri $Url -OutFile $OutputPath -UseBasicParsing
        
        Write-ColorOutput "? �������!" "Green"
        return $true
    }
    catch {
        Write-ColorOutput "? ����ʧ��: $($_.Exception.Message)" "Red"
        Write-ColorOutput "���ֶ�����: $Url" "Yellow"
        return $false
    }
}

# ��ѹJMeter
function Extract-JMeter {
    param([string]$ZipPath, [string]$ExtractPath)
    
    Write-ColorOutput "? ��ѹJMeter�ļ�..." "Blue"
    
    try {
        if (Test-Path $ExtractPath) {
            Write-ColorOutput "Ŀ��Ŀ¼�Ѵ��ڣ�ɾ���ɰ汾..." "Yellow"
            Remove-Item $ExtractPath -Recurse -Force
        }
        
        # ʹ�����õ�Expand-Archive
        Expand-Archive -Path $ZipPath -DestinationPath (Split-Path $ExtractPath) -Force
        
        Write-ColorOutput "? ��ѹ���!" "Green"
        return $true
    }
    catch {
        Write-ColorOutput "? ��ѹʧ��: $($_.Exception.Message)" "Red"
        return $false
    }
}

# ���û�������
function Set-EnvironmentVariables {
    param([string]$JMeterPath)
    
    Write-ColorOutput "?? ���û�������..." "Blue"
    
    try {
        # ����JMETER_HOME
        [Environment]::SetEnvironmentVariable("JMETER_HOME", $JMeterPath, "Machine")
        Write-ColorOutput "? ����JMETER_HOME: $JMeterPath" "Green"
        
        # ��ӵ�PATH
        $currentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
        $jmeterBinPath = Join-Path $JMeterPath "bin"
        
        if ($currentPath -notlike "*$jmeterBinPath*") {
            $newPath = "$currentPath;$jmeterBinPath"
            [Environment]::SetEnvironmentVariable("Path", $newPath, "Machine")
            Write-ColorOutput "? ��ӵ�PATH: $jmeterBinPath" "Green"
        } else {
            Write-ColorOutput "PATH���Ѵ���JMeter·��" "Yellow"
        }
        
        # ˢ�µ�ǰ�Ự�Ļ�������
        $env:JMETER_HOME = $JMeterPath
        $env:Path += ";$jmeterBinPath"
        
        return $true
    }
    catch {
        Write-ColorOutput "? ���û�������ʧ��: $($_.Exception.Message)" "Red"
        Write-ColorOutput "���ֶ����û�������" "Yellow"
        return $false
    }
}

# �������Ľ���
function Set-ChineseLanguage {
    param([string]$JMeterPath)
    
    if (-not $SetChineseLanguage) {
        return
    }
    
    Write-ColorOutput "?? �������Ľ���..." "Blue"
    
    $propertiesFile = Join-Path $JMeterPath "bin\jmeter.properties"
    
    try {
        if (Test-Path $propertiesFile) {
            $content = Get-Content $propertiesFile -Encoding UTF8
            $newContent = @()
            
            foreach ($line in $content) {
                if ($line -match "^#?language=") {
                    $newContent += "language=zh_CN"
                    Write-ColorOutput "? ��������Ϊ����" "Green"
                } else {
                    $newContent += $line
                }
            }
            
            $newContent | Set-Content $propertiesFile -Encoding UTF8
        }
    }
    catch {
        Write-ColorOutput "? �������Ľ���ʧ��: $($_.Exception.Message)" "Red"
    }
}

# ��֤��װ
function Test-Installation {
    param([string]$JMeterPath)
    
    Write-ColorOutput "? ��֤��װ..." "Blue"
    
    $jmeterBat = Join-Path $JMeterPath "bin\jmeter.bat"
    $jmeterJar = Join-Path $JMeterPath "bin\ApacheJMeter.jar"
    
    if ((Test-Path $jmeterBat) -and (Test-Path $jmeterJar)) {
        Write-ColorOutput "? JMeter��װ��֤�ɹ�!" "Green"
        
        # ���Ի�ȡ�汾��Ϣ
        try {
            $versionInfo = & $jmeterBat -version 2>$null
            if ($versionInfo) {
                Write-ColorOutput "JMeter�汾��Ϣ��ȡ�ɹ�" "Green"
            }
        }
        catch {
            Write-ColorOutput "?? �汾��Ϣ��ȡʧ�ܣ����ļ�����" "Yellow"
        }
        
        return $true
    } else {
        Write-ColorOutput "? JMeter��װ��֤ʧ��" "Red"
        return $false
    }
}

# ���������ݷ�ʽ
function New-DesktopShortcut {
    param([string]$JMeterPath)
    
    Write-ColorOutput "?? ���������ݷ�ʽ..." "Blue"
    
    try {
        $WshShell = New-Object -ComObject WScript.Shell
        $Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Apache JMeter.lnk")
        $Shortcut.TargetPath = Join-Path $JMeterPath "bin\jmeter.bat"
        $Shortcut.WorkingDirectory = Join-Path $JMeterPath "bin"
        $Shortcut.Description = "Apache JMeter Performance Testing Tool"
        $Shortcut.Save()
        
        Write-ColorOutput "? �����ݷ�ʽ�����ɹ�!" "Green"
    }
    catch {
        Write-ColorOutput "?? �����ݷ�ʽ����ʧ��: $($_.Exception.Message)" "Yellow"
    }
}

# ����װ����
function Install-JMeter {
    Write-ColorOutput "? Apache JMeter $JMeterVersion �Զ���װ����" "Magenta"
    Write-ColorOutput "=============================================" "Magenta"
    
    # ������ԱȨ��
    if (-not (Test-AdminRights)) {
        Write-ColorOutput "?? �����Թ���Ա�����������ȷ���û�������" "Yellow"
        $continue = Read-Host "�Ƿ����? (y/N)"
        if ($continue -ne "y" -and $continue -ne "Y") {
            exit 1
        }
    }
    
    # ���Java
    if (-not (Test-JavaInstallation)) {
        exit 1
    }
    
    # ������װĿ¼
    if (-not (Test-Path $InstallPath)) {
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
        Write-ColorOutput "? ������װĿ¼: $InstallPath" "Green"
    }
    
    # ����
    $zipPath = Join-Path $InstallPath $JMeterZipFile
    if (-not (Download-JMeter $DownloadUrl $zipPath)) {
        exit 1
    }
    
    # ��ѹ
    if (-not (Extract-JMeter $zipPath $JMeterHome)) {
        exit 1
    }
    
    # ���û�������
    if (Test-AdminRights) {
        Set-EnvironmentVariables $JMeterHome
    } else {
        Write-ColorOutput "?? �޹���ԱȨ�ޣ�����������������" "Yellow"
        Write-ColorOutput "���ֶ����û�������:" "Yellow"
        Write-ColorOutput "JMETER_HOME = $JMeterHome" "Cyan"
        Write-ColorOutput "PATH ��� = $JMeterHome\bin" "Cyan"
    }
    
    # ��������
    Set-ChineseLanguage $JMeterHome
    
    # ��֤��װ
    if (Test-Installation $JMeterHome) {
        # ������ݷ�ʽ
        New-DesktopShortcut $JMeterHome
        
        # ���������ļ�
        Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
        
        Write-ColorOutput "=============================================" "Magenta"
        Write-ColorOutput "? Apache JMeter ��װ���!" "Green"
        Write-ColorOutput "��װ·��: $JMeterHome" "Cyan"
        Write-ColorOutput "������ʽ:" "Cyan"
        Write-ColorOutput "1. ˫�������ݷ�ʽ" "White"
        Write-ColorOutput "2. ����������: jmeter" "White"
        Write-ColorOutput "3. ����: $JMeterHome\bin\jmeter.bat" "White"
        Write-ColorOutput "=============================================" "Magenta"
        
        # ѯ���Ƿ���������
        $launch = Read-Host "�Ƿ���������JMeter? (y/N)"
        if ($launch -eq "y" -or $launch -eq "Y") {
            Start-Process -FilePath (Join-Path $JMeterHome "bin\jmeter.bat")
        }
    } else {
        Write-ColorOutput "? ��װ�����г��ִ���" "Red"
        exit 1
    }
}

# �ű����
try {
    Install-JMeter
}
catch {
    Write-ColorOutput "? ��װ�ű�ִ��ʧ��: $($_.Exception.Message)" "Red"
    exit 1
} 