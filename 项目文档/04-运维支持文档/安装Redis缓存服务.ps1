# BZK��ѯϵͳ - Redis��������Զ�����װ�ű�
# �汾: v1.0
# ��������: 2025��6��3��
# ���û���: Windows 10/11

param(
    [string]$InstallPath = "D:\Redis",
    [string]$Password = "BZK_Redis_2025",
    [string]$Port = "6379",
    [int]$MaxMemory = 1024,  # MB
    [switch]$StartService = $true,
    [switch]$VerboseOutput = $false
)

# ���ô�����
$ErrorActionPreference = "Stop"

# �������
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
�X�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�[
�U                    BZK Redis �������װ��                  �U
�U                         �汾: v1.0                          �U
�^�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�a
"@ -ForegroundColor Magenta

# ����1: �������
Write-StepHeader "����1: �������"

try {
    # ������ԱȨ��
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    if (-not $isAdmin) {
        Write-Warning "��⵽�ǹ���ԱȨ�ޣ�ĳЩ���ܿ�������"
        Write-Warning "�����Թ���Ա�������PowerShell"
    } else {
        Write-Success "����ԱȨ�޼��ͨ��"
    }

    # ���.NET�汾
    $dotnetVersion = dotnet --version 2>$null
    if ($dotnetVersion) {
        Write-Success ".NET�汾: $dotnetVersion"
    } else {
        Write-Warning ".NETδ��װ��δ����PATH"
    }

    # ���˿�ռ��
    $portInUse = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    if ($portInUse) {
        Write-Error-Custom "�˿� $Port �ѱ�ռ�ã����������˿�"
        return
    } else {
        Write-Success "�˿� $Port ����"
    }

    # �����̿ռ�
    $drive = Split-Path $InstallPath -Qualifier
    $freeSpace = (Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DeviceID -eq $drive}).FreeSpace / 1GB
    if ($freeSpace -lt 1) {
        Write-Warning "����ʣ��ռ����: $([math]::Round($freeSpace, 2)) GB"
    } else {
        Write-Success "���̿ռ����: $([math]::Round($freeSpace, 2)) GB"
    }

    Write-Success "����������"
}
catch {
    Write-Error-Custom "�������ʧ��: $($_.Exception.Message)"
    return
}

# ����2: ����Redis
Write-StepHeader "����2: ����Redis"

try {
    $redisUrl = "https://github.com/microsoftarchive/redis/releases/download/win-3.2.100/Redis-x64-3.2.100.zip"
    $zipPath = "$env:TEMP\Redis-x64-3.2.100.zip"
    
    Write-ColorOutput "��������Redis..." "Yellow"
    Write-ColorOutput "���ص�ַ: $redisUrl" "Gray"
    Write-ColorOutput "����·��: $zipPath" "Gray"
    
    # ʹ��WebClient����
    $webClient = New-Object System.Net.WebClient
    $webClient.DownloadFile($redisUrl, $zipPath)
    
    # ��֤�����ļ�
    if (Test-Path $zipPath) {
        $fileSize = (Get-Item $zipPath).Length / 1MB
        Write-Success "Redis������ɣ��ļ���С: $([math]::Round($fileSize, 2)) MB"
    } else {
        throw "�����ļ�������"
    }
}
catch {
    Write-Error-Custom "Redis����ʧ��: $($_.Exception.Message)"
    Write-Warning "�����ֶ�����: $redisUrl"
    return
}

# ����3: ��ѹ��װ
Write-StepHeader "����3: ��ѹ��װ"

try {
    # ������װĿ¼
    if (-not (Test-Path $InstallPath)) {
        New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
        Write-Success "������װĿ¼: $InstallPath"
    }

    # ��ѹ�ļ�
    Write-ColorOutput "���ڽ�ѹRedis..." "Yellow"
    Expand-Archive -Path $zipPath -DestinationPath $InstallPath -Force
    
    # ��֤��ѹ���
    $redisExe = Join-Path $InstallPath "redis-server.exe"
    $redisCliExe = Join-Path $InstallPath "redis-cli.exe"
    
    if ((Test-Path $redisExe) -and (Test-Path $redisCliExe)) {
        Write-Success "Redis��ѹ���"
        Write-ColorOutput "Redis������: $redisExe" "Gray"
        Write-ColorOutput "Redis�ͻ���: $redisCliExe" "Gray"
    } else {
        throw "�ؼ��ļ�ȱʧ"
    }

    # ���������ļ�
    Remove-Item $zipPath -Force
    Write-ColorOutput "������ʱ�ļ�: $zipPath" "Gray"
}
catch {
    Write-Error-Custom "Redis��ѹ��װʧ��: $($_.Exception.Message)"
    return
}

# ����4: ���������ļ�
Write-StepHeader "����4: ���������ļ�"

try {
    $configPath = Join-Path $InstallPath "redis.windows.conf"
    
    $configContent = @"
# BZK Redis �����ļ�
# ����ʱ��: $(Get-Date)

# ����������
bind 127.0.0.1
port $Port
timeout 0

# �ڴ�����
maxmemory ${MaxMemory}mb
maxmemory-policy allkeys-lru

# �־û�����
save 900 1
save 300 10
save 60 10000

# ��־����
loglevel notice
logfile "$InstallPath\redis.log"

# ��ȫ����
requirepass $Password

# �����Ż�
tcp-keepalive 60
tcp-backlog 511

# �ڴ��Ż�
hash-max-ziplist-entries 512
hash-max-ziplist-value 64
list-max-ziplist-size -2
set-max-intset-entries 512

# �־û��Ż�
stop-writes-on-bgsave-error yes
rdbcompression yes
rdbchecksum yes

# Windows�ض�����
service-name Redis-BZK
service-run yes
"@

    Set-Content -Path $configPath -Value $configContent -Encoding UTF8
    Write-Success "�����ļ��������: $configPath"
    
    # ��ʾ�ؼ�����
    Write-ColorOutput "����ժҪ:" "Cyan"
    Write-ColorOutput "  �˿�: $Port" "Gray"
    Write-ColorOutput "  ����: $Password" "Gray"
    Write-ColorOutput "  ����ڴ�: ${MaxMemory}MB" "Gray"
    Write-ColorOutput "  ��־�ļ�: $InstallPath\redis.log" "Gray"
}
catch {
    Write-Error-Custom "�����ļ�����ʧ��: $($_.Exception.Message)"
    return
}

# ����5: ע��Windows����
Write-StepHeader "����5: ע��Windows����"

try {
    $serviceName = "Redis-BZK"
    $redisServerExe = Join-Path $InstallPath "redis-server.exe"
    $configPath = Join-Path $InstallPath "redis.windows.conf"
    
    # �������Ƿ��Ѵ���
    $existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Warning "���� $serviceName �Ѵ��ڣ���ֹͣ��ɾ��"
        Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
        sc.exe delete $serviceName | Out-Null
        Start-Sleep -Seconds 2
    }
    
    # ע���·���
    if ($isAdmin) {
        Write-ColorOutput "����ע��Windows����..." "Yellow"
        $serviceCmd = "`"$redisServerExe`" `"$configPath`""
        sc.exe create $serviceName binPath= $serviceCmd start= auto DisplayName= "BZK Redis�������" | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Windows����ע��ɹ�: $serviceName"
        } else {
            throw "����ע��ʧ�ܣ��������: $LASTEXITCODE"
        }
    } else {
        Write-Warning "û�й���ԱȨ�ޣ���������ע��"
        Write-Warning "�ֶ�ע������: sc.exe create $serviceName binPath= `"$redisServerExe $configPath`""
    }
}
catch {
    Write-Error-Custom "Windows����ע��ʧ��: $($_.Exception.Message)"
    Write-Warning "�����ֶ�����: $redisServerExe $configPath"
}

# ����6: ���û�������
Write-StepHeader "����6: ���û�������"

try {
    # ���Redis��PATH
    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($currentPath -notlike "*$InstallPath*") {
        Write-ColorOutput "�������Redis��PATH��������..." "Yellow"
        $newPath = "$currentPath;$InstallPath"
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
        
        # ���µ�ǰ�Ự��PATH
        $env:Path += ";$InstallPath"
        Write-Success "PATH���������������"
    } else {
        Write-Success "Redis����PATH����������"
    }
    
    # ����REDIS_HOME
    [Environment]::SetEnvironmentVariable("REDIS_HOME", $InstallPath, "User")
    $env:REDIS_HOME = $InstallPath
    Write-Success "REDIS_HOME���������������: $InstallPath"
}
catch {
    Write-Error-Custom "������������ʧ��: $($_.Exception.Message)"
    Write-Warning "���ֶ���� $InstallPath ��PATH��������"
}

# ����7: ����Redis����
Write-StepHeader "����7: ����Redis����"

try {
    if ($StartService) {
        $serviceName = "Redis-BZK"
        $redisServerExe = Join-Path $InstallPath "redis-server.exe"
        $configPath = Join-Path $InstallPath "redis.windows.conf"
        
        if ($isAdmin -and (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
            Write-ColorOutput "��������Redis����..." "Yellow"
            Start-Service -Name $serviceName
            Start-Sleep -Seconds 3
            
            $service = Get-Service -Name $serviceName
            if ($service.Status -eq "Running") {
                Write-Success "Redis���������ɹ�"
            } else {
                throw "����״̬�쳣: $($service.Status)"
            }
        } else {
            Write-Warning "�Խ��̷�ʽ����Redis���Ƿ���ģʽ��"
            Write-ColorOutput "��������: $redisServerExe $configPath" "Gray"
            
            # �ں�̨����Redis����
            Start-Process -FilePath $redisServerExe -ArgumentList $configPath -WindowStyle Minimized
            Start-Sleep -Seconds 3
            Write-Success "Redis�����������"
        }
    } else {
        Write-Warning "����Redis��������"
    }
}
catch {
    Write-Error-Custom "Redis��������ʧ��: $($_.Exception.Message)"
    Write-Warning "�����ֶ�����: $redisServerExe $configPath"
}

# ����8: ���Ӳ���
Write-StepHeader "����8: ���Ӳ���"

try {
    Start-Sleep -Seconds 2
    $redisCliExe = Join-Path $InstallPath "redis-cli.exe"
    
    # ��������
    Write-ColorOutput "���ڲ���Redis����..." "Yellow"
    
    # Ping����
    $pingResult = & $redisCliExe -p $Port -a $Password ping 2>$null
    if ($pingResult -eq "PONG") {
        Write-Success "Redis���Ӳ��Գɹ�"
    } else {
        throw "Ping����ʧ��: $pingResult"
    }
    
    # �汾��Ϣ
    $versionInfo = & $redisCliExe -p $Port -a $Password info server 2>$null | Select-String "redis_version"
    if ($versionInfo) {
        Write-Success "Redis�汾: $($versionInfo.Line.Split(':')[1])"
    }
    
    # �ڴ���Ϣ
    $memoryInfo = & $redisCliExe -p $Port -a $Password info memory 2>$null | Select-String "used_memory_human"
    if ($memoryInfo) {
        Write-Success "�ڴ�ʹ��: $($memoryInfo.Line.Split(':')[1])"
    }
    
}
catch {
    Write-Error-Custom "Redis���Ӳ���ʧ��: $($_.Exception.Message)"
    Write-Warning "����Redis����״̬������"
}

# ����9: ��������ű�
Write-StepHeader "����9: ��������ű�"

try {
    # �������������ű�
    $startScriptPath = Join-Path $InstallPath "����Redis.bat"
    $startScriptContent = @"
@echo off
echo ����BZK Redis�������...
cd /d "$InstallPath"
redis-server.exe redis.windows.conf
pause
"@
    Set-Content -Path $startScriptPath -Value $startScriptContent -Encoding UTF8
    
    # �����������ӽű�
    $connectScriptPath = Join-Path $InstallPath "����Redis.bat"
    $connectScriptContent = @"
@echo off
echo ����BZK Redis�������...
cd /d "$InstallPath"
redis-cli.exe -p $Port -a $Password
pause
"@
    Set-Content -Path $connectScriptPath -Value $connectScriptContent -Encoding UTF8
    
    # ����PowerShell����ű�
    $managementScriptPath = Join-Path $InstallPath "����Redis.ps1"
    $managementScriptContent = @"
# BZK Redis ����ű�
param([string]`$Action = "status")

`$redisPath = "$InstallPath"
`$serviceName = "Redis-BZK"

switch (`$Action) {
    "start" { 
        Start-Service -Name `$serviceName
        Write-Host "? Redis����������" -ForegroundColor Green
    }
    "stop" { 
        Stop-Service -Name `$serviceName
        Write-Host "?? Redis������ֹͣ" -ForegroundColor Yellow
    }
    "restart" { 
        Restart-Service -Name `$serviceName
        Write-Host "? Redis����������" -ForegroundColor Blue
    }
    "status" { 
        `$service = Get-Service -Name `$serviceName -ErrorAction SilentlyContinue
        if (`$service) {
            Write-Host "? Redis����״̬: `$(`$service.Status)" -ForegroundColor Cyan
        } else {
            Write-Host "? Redis����δ�ҵ�" -ForegroundColor Red
        }
    }
    "test" {
        & "`$redisPath\redis-cli.exe" -p $Port -a $Password ping
    }
    default { 
        Write-Host "�÷�: .\����Redis.ps1 [start|stop|restart|status|test]"
    }
}
"@
    Set-Content -Path $managementScriptPath -Value $managementScriptContent -Encoding UTF8
    
    Write-Success "����ű��������:"
    Write-ColorOutput "  �����ű�: $startScriptPath" "Gray"
    Write-ColorOutput "  ���ӽű�: $connectScriptPath" "Gray"
    Write-ColorOutput "  ����ű�: $managementScriptPath" "Gray"
}
catch {
    Write-Error-Custom "����ű�����ʧ��: $($_.Exception.Message)"
}

# ����10: ���������ݷ�ʽ
Write-StepHeader "����10: ���������ݷ�ʽ"

try {
    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktopPath "BZK Redis����.lnk"
    
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = Join-Path $InstallPath "����Redis.bat"
    $shortcut.WorkingDirectory = $InstallPath
    $shortcut.Description = "BZK Redis����������"
    $shortcut.Save()
    
    Write-Success "�����ݷ�ʽ�������: $shortcutPath"
}
catch {
    Write-Error-Custom "�����ݷ�ʽ����ʧ��: $($_.Exception.Message)"
}

# ��װ����ܽ�
Write-Host "`n" -NoNewline
Write-Host "�X�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�[" -ForegroundColor Green
Write-Host "�U                    ? Redis��װ��ɣ�                        �U" -ForegroundColor Green
Write-Host "�^�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�T�a" -ForegroundColor Green

Write-Host "`n? ��װ��Ϣ�ܽ�:" -ForegroundColor Cyan
Write-Host "  ? ��װ·��: $InstallPath" -ForegroundColor White
Write-Host "  ? �˿�: $Port" -ForegroundColor White
Write-Host "  ? ����: $Password" -ForegroundColor White
Write-Host "  ? ����ڴ�: ${MaxMemory}MB" -ForegroundColor White
Write-Host "  ? �����ļ�: $InstallPath\redis.windows.conf" -ForegroundColor White
Write-Host "  ? ��־�ļ�: $InstallPath\redis.log" -ForegroundColor White

Write-Host "`n? ����ʹ��:" -ForegroundColor Cyan
Write-Host "  ����Redis: redis-cli -p $Port -a $Password" -ForegroundColor Yellow
Write-Host "  ��������: net start Redis-BZK" -ForegroundColor Yellow
Write-Host "  ֹͣ����: net stop Redis-BZK" -ForegroundColor Yellow
Write-Host "  ����ű�: .\����Redis.ps1 [start|stop|restart|status|test]" -ForegroundColor Yellow

Write-Host "`n? .NET���������ַ���:" -ForegroundColor Cyan
Write-Host "  localhost:$Port,password=$Password" -ForegroundColor Green

Write-Host "`n?? ��Ҫ����:" -ForegroundColor Yellow
Write-Host "  1. �����Ʊ���Redis����: $Password" -ForegroundColor White
Write-Host "  2. �����������޸�bind��ַ����ǿ����" -ForegroundColor White
Write-Host "  3. ���ڱ���Redis����" -ForegroundColor White
Write-Host "  4. ���Redis�ڴ�ʹ�����" -ForegroundColor White

Write-Host "`n? ��һ��: ����BZK��Ŀ���ã�����Redis�������" -ForegroundColor Magenta
Write-Host "��װ�ű�ִ����ɣ�" -ForegroundColor Green 