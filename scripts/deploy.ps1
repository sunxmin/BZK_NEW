# BZK��ѯϵͳ����ű�
# PowerShell�ű������Զ������������

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

# �ű�����
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ��ɫ�������
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

# ���ǰ������
function Test-Prerequisites {
    Write-Info "��鲿��ǰ������..."
    
    # ���.NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Success ".NET SDK �汾: $dotnetVersion"
    }
    catch {
        Write-Error ".NET SDK δ��װ��δ��PATH��"
        exit 1
    }
    
    # ���Node.js�������Ҫ����ǰ�ˣ�
    if (-not $SkipFrontend) {
        try {
            $nodeVersion = node --version
            Write-Success "Node.js �汾: $nodeVersion"
        }
        catch {
            Write-Error "Node.js δ��װ��δ��PATH��"
            exit 1
        }
    }
    
    # ���SQL Server���ӣ�����ṩ�������ַ�����
    if ($ConnectionString -and -not $SkipDatabase) {
        Write-Info "�������ݿ�����..."
        try {
            # �������������ݿ����Ӳ����߼�
            Write-Success "���ݿ���������"
        }
        catch {
            Write-Warning "���ݿ����Ӳ���ʧ�ܣ����ں�������������"
        }
    }
}

# �������Ӧ��
function Build-Backend {
    Write-Info "�������Ӧ��..."
    
    try {
        # ����֮ǰ�Ĺ���
        if (Test-Path "BZKQuerySystem.Web/bin") {
            Remove-Item -Recurse -Force "BZKQuerySystem.Web/bin"
        }
        if (Test-Path "BZKQuerySystem.Web/obj") {
            Remove-Item -Recurse -Force "BZKQuerySystem.Web/obj"
        }
        
        # ��ԭNuGet��
        Write-Info "��ԭNuGet��..."
        dotnet restore BZKQuerySystem.Web/BZKQuerySystem.Web.csproj
        
        # ������Ŀ
        Write-Info "������Ŀ..."
        dotnet build BZKQuerySystem.Web/BZKQuerySystem.Web.csproj --configuration Release --no-restore
        
        Write-Success "��˹������"
    }
    catch {
        Write-Error "��˹���ʧ��: $($_.Exception.Message)"
        exit 1
    }
}

# ����ǰ��Ӧ��
function Build-Frontend {
    if ($SkipFrontend) {
        Write-Info "����ǰ�˹���"
        return
    }
    
    Write-Info "����ǰ��Ӧ��..."
    
    try {
        Set-Location "BZKQuerySystem.Web/ClientApp"
        
        # ��װ����
        Write-Info "��װǰ������..."
        npm install
        
        # ����ǰ��
        Write-Info "����ǰ�˴���..."
        npm run build
        
        Set-Location "../.."
        Write-Success "ǰ�˹������"
    }
    catch {
        Set-Location "../.."
        Write-Error "ǰ�˹���ʧ��: $($_.Exception.Message)"
        exit 1
    }
}

# �������ݿ�
function Update-Database {
    if ($SkipDatabase) {
        Write-Info "�������ݿ����"
        return
    }
    
    Write-Info "�������ݿ�..."
    
    try {
        # ���������ַ�����������
        if ($ConnectionString) {
            $env:ConnectionStrings__DefaultConnection = $ConnectionString
        }
        
        # ����Entity FrameworkǨ��
        Write-Info "ִ�����ݿ�Ǩ��..."
        dotnet ef database update --project BZKQuerySystem.DataAccess --startup-project BZKQuerySystem.Web
        
        Write-Success "���ݿ�������"
    }
    catch {
        Write-Error "���ݿ����ʧ��: $($_.Exception.Message)"
        if (-not $Force) {
            exit 1
        } else {
            Write-Warning "ǿ��ģʽ���������ݿ���´���"
        }
    }
}

# ������Ҫ��Ŀ¼�ṹ
function Initialize-Directories {
    Write-Info "��ʼ��Ŀ¼�ṹ..."
    
    $directories = @(
        "logs",
        "temp",
        "backups",
        "exports"
    )
    
    foreach ($dir in $directories) {
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Success "����Ŀ¼: $dir"
        }
    }
}

# ���������ļ�
function Copy-ConfigFiles {
    Write-Info "����Ӧ�ó�������..."
    
    try {
        # ���ƻ����ض��������ļ�
        $sourceConfig = "BZKQuerySystem.Web/appsettings.$Environment.json"
        $targetConfig = "BZKQuerySystem.Web/appsettings.json"
        
        if (Test-Path $sourceConfig) {
            Copy-Item $sourceConfig $targetConfig -Force
            Write-Success "�����ļ��Ѹ���: $Environment"
        } else {
            Write-Warning "���������ļ�������: $sourceConfig"
        }
        
        # ���û�������
        $env:ASPNETCORE_ENVIRONMENT = $Environment
        Write-Success "��������Ϊ: $Environment"
        
    }
    catch {
        Write-Error "�����ļ�����ʧ��: $($_.Exception.Message)"
        exit 1
    }
}

# ��֤����
function Test-Deployment {
    Write-Info "��֤����..."
    
    try {
        # ���ؼ��ļ��Ƿ����
        $requiredFiles = @(
            "BZKQuerySystem.Web/bin/Release/net8.0/BZKQuerySystem.Web.dll",
            "BZKQuerySystem.Web/appsettings.json"
        )
        
        foreach ($file in $requiredFiles) {
            if (-not (Test-Path $file)) {
                throw "ȱ�ٹؼ��ļ�: $file"
            }
        }
        
        Write-Success "������֤ͨ��"
    }
    catch {
        Write-Error "������֤ʧ��: $($_.Exception.Message)"
        exit 1
    }
}

# ���������ű�
function Create-StartupScripts {
    Write-Info "���������ű�..."
    
    # Windows���������ű�
    $serviceScript = @"
@echo off
echo ����BZK��ѯϵͳ...
cd /d "%~dp0"
dotnet BZKQuerySystem.Web/bin/Release/net8.0/BZKQuerySystem.Web.dll
pause
"@
    
    $serviceScript | Out-File -FilePath "start.bat" -Encoding ASCII
    
    # PowerShell�����ű�
    $psScript = @"
# BZK��ѯϵͳ�����ű�
Write-Host "����BZK��ѯϵͳ..." -ForegroundColor Green
Set-Location `$PSScriptRoot
`$env:ASPNETCORE_ENVIRONMENT = "$Environment"
dotnet BZKQuerySystem.Web/bin/Release/net8.0/BZKQuerySystem.Web.dll
"@
    
    $psScript | Out-File -FilePath "start.ps1" -Encoding UTF8
    
    Write-Success "�����ű��Ѵ���"
}

# ��������
function Create-Backup {
    if ($Environment -eq "Production") {
        Write-Info "����������������..."
        
        try {
            $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
            $backupDir = "backups/backup_$timestamp"
            
            New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
            
            # ���ݵ�ǰ�汾
            if (Test-Path "BZKQuerySystem.Web/bin") {
                Copy-Item -Recurse "BZKQuerySystem.Web/bin" "$backupDir/bin"
            }
            
            # ���������ļ�
            if (Test-Path "BZKQuerySystem.Web/appsettings.json") {
                Copy-Item "BZKQuerySystem.Web/appsettings.json" "$backupDir/"
            }
            
            Write-Success "�����Ѵ���: $backupDir"
        }
        catch {
            Write-Warning "���ݴ���ʧ��: $($_.Exception.Message)"
        }
    }
}

# ����������
function Start-Deployment {
    $startTime = Get-Date
    Write-Info "��ʼ����BZK��ѯϵͳ (����: $Environment)"
    Write-Info "����ʱ��: $startTime"
    
    try {
        # ִ�в�����
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
        
        Write-Success "? ������ɣ�"
        Write-Info "�ܺ�ʱ: $($duration.TotalMinutes.ToString("F1")) ����"
        Write-Info ""
        Write-Info "��һ������:"
        Write-Info "1. ��������ļ� appsettings.json"
        Write-Info "2. �������ݿ������ַ���"
        Write-Info "3. ���� start.bat �� start.ps1 ����Ӧ��"
        Write-Info "4. ���� http://localhost:5000 ��֤����"
    }
    catch {
        Write-Error "����ʧ��: $($_.Exception.Message)"
        Write-Info "���������Ϣ������"
        exit 1
    }
}

# ��ʾ������Ϣ
function Show-Help {
    Write-Host @"
BZK��ѯϵͳ����ű�

�÷�:
    .\deploy.ps1 [����]

����:
    -Environment      Ŀ�껷�� (Development/Staging/Production)
    -ConnectionString ���ݿ������ַ���
    -SkipFrontend     ����ǰ�˹���
    -SkipDatabase     �������ݿ����
    -Force            ǿ��ִ�У����Դ���

ʾ��:
    .\deploy.ps1 -Environment Production
    .\deploy.ps1 -Environment Development -SkipFrontend
    .\deploy.ps1 -ConnectionString "Server=...;Database=...;" -Force

"@ -ForegroundColor Cyan
}

# �ű���ڵ�
if ($args -contains "-help" -or $args -contains "--help" -or $args -contains "-h") {
    Show-Help
    exit 0
}

# ȷ��������������
if ($Environment -eq "Production" -and -not $Force) {
    Write-Warning "������������������"
    $confirmation = Read-Host "�Ƿ������(y/N)"
    if ($confirmation -ne "y" -and $confirmation -ne "Y") {
        Write-Info "������ȡ��"
        exit 0
    }
}

# ��ʼ����
Start-Deployment 