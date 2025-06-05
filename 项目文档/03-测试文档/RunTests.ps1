# BZK��ѯϵͳ����ִ����
# �汾: v1.0
# ����: �Զ������Կ�ܣ�֧��JMeter���ܲ��ԡ���Ԫ���ԡ����ɲ��ԡ�UI���ԺͰ�ȫ����

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("All", "JMeter", "Unit", "Integration", "UI", "Security")]
    [string]$TestType = "All",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "http://localhost:5000",
    
    [Parameter(Mandatory = $false)]
    [switch]$VerboseOutput,
    
    [Parameter(Mandatory = $false)]
    [switch]$GenerateReport = $true
)

# ��ɫ�������
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

# ��ʼ�����Ի���
function Initialize-TestEnvironment {
    Write-ColorOutput "? ��ʼ�����Ի���..." "Blue"
    
    # �������Խ��Ŀ¼
    $testDirs = @(
        "TestResults",
        "TestResults/JMeter", 
        "TestResults/Unit",
        "TestResults/Integration",
        "TestResults/UI",
        "TestResults/Security"
    )
    
    foreach ($dir in $testDirs) {
        if (Test-Path $dir) {
            try {
                Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
            }
            catch {
                # ����ɾ�����󣬼���ִ��
            }
        }
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    
    Write-ColorOutput "? ���Ի�����ʼ�����" "Green"
}

# �����������
function Test-Dependencies {
    Write-ColorOutput "? �����Թ�������..." "Blue"
    
    # ��� .NET
    try {
        $dotnetVersion = dotnet --version
        Write-ColorOutput "? dotnet �Ѱ�װ: $dotnetVersion" "Green"
    }
    catch {
        Write-ColorOutput "? dotnet δ��װ��δ��ӵ�PATH" "Red"
        return $false
    }
    
    # ��� Java
    try {
        $javaVersion = java -version 2>&1 | Select-Object -First 1
        Write-ColorOutput "? java �Ѱ�װ: $javaVersion" "Green"
    }
    catch {
        Write-ColorOutput "? Java δ��װ��δ��ӵ�PATH" "Red"
        return $false
    }
    
    return $true
}

# ִ��JMeter���ܲ���
function Invoke-JMeterTests {
    Write-ColorOutput "? ��ʼִ��JMeter���ܲ���..." "Blue"
    
    # ����JMeter��������
    if (-not $env:JMETER_HOME) {
        $env:JMETER_HOME = "D:\tools\apache-jmeter-5.6.3"
        $env:Path += ";D:\tools\apache-jmeter-5.6.3\bin"
    }
    
    # ���JMeter�Ƿ����
    try {
        $jmeterVersion = jmeter --version 2>&1 | Select-Object -First 10
        Write-ColorOutput "? ��⵽JMeter" "Green"
    }
    catch {
        Write-ColorOutput "? JMeterδ��װ��δ��ӵ�PATH" "Red"
        Write-ColorOutput "�����ز���װApache JMeter 5.5+" "Yellow"
        return $false
    }
    
    # �����Խű�
    if (-not (Test-Path "BZK���ܲ���.jmx")) {
        Write-ColorOutput "? �Ҳ���JMeter���Խű�: BZK���ܲ���.jmx" "Red"
        return $false
    }
    
    # ִ��JMeter����
    $jmeterCmd = "jmeter -n -t `"BZK���ܲ���.jmx`" -l `"TestResults/JMeter/results.jtl`" -e -o `"TestResults/JMeter/html-report`" -Jbase.url=$BaseUrl"
    Write-ColorOutput "ִ������: $jmeterCmd" "Cyan"
    
    try {
        Invoke-Expression $jmeterCmd
        
        if (Test-Path "TestResults/JMeter/html-report/index.html") {
            Write-ColorOutput "? JMeter����ִ�����" "Green"
            Write-ColorOutput "? ���Ա���: TestResults/JMeter/html-report/index.html" "Cyan"
            return $true
        } else {
            Write-ColorOutput "? JMeter���Ա�������ʧ��" "Red"
            return $false
        }
    }
    catch {
        Write-ColorOutput "? JMeter����ִ��ʧ��: $($_.Exception.Message)" "Red"
        return $false
    }
}

# ִ�е�Ԫ����
function Invoke-UnitTests {
    Write-ColorOutput "? ��ʼִ�е�Ԫ����..." "Blue"
    
    # ���Ҳ�����Ŀ - ����Ŀ��Ŀ¼��ʼ����
    $projectRoot = (Get-Item $PSScriptRoot).Parent.Parent.FullName
    $testProjects = Get-ChildItem -Path $projectRoot -Recurse -Filter "*.Tests.csproj"
    
    if ($testProjects.Count -eq 0) {
        Write-ColorOutput "? δ�ҵ�������Ŀ" "Red"
        Write-ColorOutput "? ��·�� $projectRoot �в��� *.Tests.csproj �ļ�" "Yellow"
        return $false
    }
    
    $allTestsPassed = $true
    
    foreach ($project in $testProjects) {
        Write-ColorOutput "? ִ�в�����Ŀ: $($project.Name)" "Cyan"
        
        $testArgs = @(
            "test",
            $project.FullName,
            "--configuration", $Configuration,
            "--logger", "trx",
            "--results-directory", "TestResults/Unit"
        )
        
        if ($VerboseOutput) {
            $testArgs += "--verbosity", "detailed"
        }
        
        try {
            & dotnet @testArgs
            
            if ($LASTEXITCODE -eq 0) {
                Write-ColorOutput "? ��Ŀ $($project.Name) ����ͨ��" "Green"
            } else {
                Write-ColorOutput "? ��Ŀ $($project.Name) ����ʧ��" "Red"
                $allTestsPassed = $false
            }
        }
        catch {
            Write-ColorOutput "? ��Ŀ $($project.Name) ����ִ�д���: $($_.Exception.Message)" "Red"
            $allTestsPassed = $false
        }
    }
    
    return $allTestsPassed
}

# ִ�м��ɲ���
function Invoke-IntegrationTests {
    Write-ColorOutput "? ��ʼִ�м��ɲ���..." "Blue"
    
    # ���ϵͳ�Ƿ�����
    try {
        $response = Invoke-WebRequest -Uri $BaseUrl -Method Get -TimeoutSec 10 -ErrorAction Stop
        Write-ColorOutput "? ϵͳ�ɷ���: $BaseUrl (״̬��: $($response.StatusCode))" "Green"
    }
    catch {
        Write-ColorOutput "? �޷�����ϵͳ: $BaseUrl" "Red"
        Write-ColorOutput "? ��������: $($_.Exception.Message)" "Yellow"
        Write-ColorOutput "��ȷ��ϵͳ��������" "Yellow"
        return $false
    }
    
    # ���API�˵�
    $apiEndpoints = @(
        "/api/health",
        "/api/query",
        "/"
    )
    
    $apiTestsPassed = $true
    
    foreach ($endpoint in $apiEndpoints) {
        try {
            $testUrl = "$BaseUrl$endpoint"
            $response = Invoke-WebRequest -Uri $testUrl -Method Get -TimeoutSec 5 -ErrorAction Stop
            Write-ColorOutput "? API�˵����ͨ��: $endpoint (״̬��: $($response.StatusCode))" "Green"
        }
        catch {
            if ($_.Exception.Response.StatusCode -eq 404) {
                Write-ColorOutput "?? API�˵㲻����: $endpoint (404)" "Yellow"
            } else {
                Write-ColorOutput "? API�˵����ʧ��: $endpoint - $($_.Exception.Message)" "Red"
                $apiTestsPassed = $false
            }
        }
    }
    
    Write-ColorOutput "? ���ɲ��Լ�����" "Green"
    return $apiTestsPassed
}

# ִ��UI�Զ�������
function Invoke-UITests {
    Write-ColorOutput "? ��ʼִ��UI�Զ�������..." "Blue"
    
    # ���Chrome�����
    $chromePaths = @(
        "C:\Program Files\Google\Chrome\Application\chrome.exe",
        "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
    )
    
    $chromeFound = $false
    $chromePath = ""
    
    foreach ($path in $chromePaths) {
        if (Test-Path $path) {
            $chromeFound = $true
            $chromePath = $path
            break
        }
    }
    
    if ($chromeFound) {
        try {
            $chromeVersion = & "$chromePath" --version 2>&1 | Select-Object -First 1
            Write-ColorOutput "? ��⵽Chrome: $chromeVersion" "Green"
            Write-ColorOutput "? Chrome·��: $chromePath" "Cyan"
        }
        catch {
            Write-ColorOutput "? Chrome������Ѱ�װ: $chromePath" "Green"
        }
        
        # ����Chrome·����������
        $env:CHROME_PATH = $chromePath
        Write-ColorOutput "? UI���Ի����������" "Green"
        return $true
    } else {
        Write-ColorOutput "? Chrome�����δ�ҵ�" "Red"
        Write-ColorOutput "��ȷ��Chrome���������ȷ��װ" "Yellow"
        return $false
    }
}

# ִ�а�ȫ����
function Invoke-SecurityTests {
    Write-ColorOutput "?? ��ʼִ�а�ȫ����..." "Blue"
    
    # ������ȫ���
    Write-ColorOutput "? ִ�л�����ȫɨ��..." "Cyan"
    
    # HTTPͷ���
    try {
        $headers = Invoke-WebRequest -Uri $BaseUrl -Method Head -TimeoutSec 10
        
        $securityHeaders = @{
            "X-Frame-Options" = "����ٳַ���"
            "X-Content-Type-Options" = "MIME������̽����"
            "X-XSS-Protection" = "XSS����"
            "Strict-Transport-Security" = "HTTPSǿ��"
        }
        
        foreach ($header in $securityHeaders.Keys) {
            if ($headers.Headers.ContainsKey($header)) {
                Write-ColorOutput "? $($securityHeaders[$header]): $($headers.Headers[$header])" "Green"
            } else {
                Write-ColorOutput "?? ȱ�ٰ�ȫͷ: $header ($($securityHeaders[$header]))" "Yellow"
            }
        }
    }
    catch {
        Write-ColorOutput "? �޷����HTTP��ȫͷ" "Red"
    }
    
    # SQLע�����
    Write-ColorOutput "? ִ��SQLע�밲ȫ����..." "Cyan"
    
    $injectionPayloads = @(
        "'; DROP TABLE--",
        "' OR '1'='1",
        "' UNION SELECT--",
        "'; WAITFOR DELAY--"
    )
    
    foreach ($payload in $injectionPayloads) {
        try {
            $body = @{
                "tables" = @("Patient")
                "conditions" = @(@{
                    "field" = "Name"
                    "operator" = "="
                    "value" = $payload
                })
            } | ConvertTo-Json -Depth 3
            
            $response = Invoke-WebRequest -Uri "$BaseUrl/api/query/execute" -Method POST -Body $body -ContentType "application/json" -ErrorAction SilentlyContinue
            
            if ($response.StatusCode -eq 200 -and $response.Content -match "(error|Error|ERROR)") {
                Write-ColorOutput "?? ���ܴ���SQLע��©��: $payload" "Yellow"
            } else {
                Write-ColorOutput "? ע���������: $payload" "Green"
            }
        }
        catch {
            Write-ColorOutput "? ע�뱻��ȷ��ֹ: $payload" "Green"
        }
    }
    
    return $true
}

# ���ɲ��Ա���
function New-TestReport {
    if (-not $GenerateReport) {
        return
    }
    
    Write-ColorOutput "? ���ɲ��Ա���..." "Blue"
    
    $reportPath = "TestResults/TestReport.html"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    # ����HTML����
    $html = @"
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>BZK��ѯϵͳ - ���Ա���</title>
    <style>
        body { font-family: 'Microsoft YaHei', Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
        h2 { color: #34495e; margin-top: 30px; }
        .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin: 20px 0; }
        .card { background: #ecf0f1; padding: 15px; border-radius: 6px; text-align: center; }
        .card.success { background: #d5edda; border-left: 4px solid #28a745; }
        .card.warning { background: #fff3cd; border-left: 4px solid #ffc107; }
        .card.error { background: #f8d7da; border-left: 4px solid #dc3545; }
        .card h3 { margin: 0 0 10px 0; color: #2c3e50; }
        .card p { margin: 0; font-size: 24px; font-weight: bold; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background-color: #3498db; color: white; }
        .status-pass { color: #28a745; font-weight: bold; }
        .status-fail { color: #dc3545; font-weight: bold; }
        .footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; color: #6c757d; }
    </style>
</head>
<body>
    <div class="container">
        <h1>? BZK��ѯϵͳ - ���Ա���</h1>
        <p><strong>����ʱ��:</strong> $timestamp</p>
        <p><strong>��������:</strong> $TestType</p>
        <p><strong>����URL:</strong> $BaseUrl</p>
        
        <div class="summary">
            <div class="card success">
                <h3>? ͨ������</h3>
                <p id="passCount">-</p>
            </div>
            <div class="card error">
                <h3>? ʧ�ܲ���</h3>
                <p id="failCount">-</p>
            </div>
            <div class="card">
                <h3>? �ܼ�</h3>
                <p id="totalCount">-</p>
            </div>
        </div>
        
        <h2>? ���Խ������</h2>
        <table>
            <thead>
                <tr>
                    <th>��������</th>
                    <th>״̬</th>
                    <th>˵��</th>
                </tr>
            </thead>
            <tbody id="testResults">
                <!-- ���Խ������������� -->
            </tbody>
        </table>
        
        <h2>? �����ļ�</h2>
        <ul>
            <li><a href="JMeter/html-report/index.html">JMeter���ܲ��Ա���</a></li>
            <li><a href="Unit/">��Ԫ���Խ��</a></li>
            <li><a href="Integration/">���ɲ��Խ��</a></li>
            <li><a href="UI/">UI���Խ��</a></li>
            <li><a href="Security/">��ȫ���Խ��</a></li>
        </ul>
        
        <div class="footer">
            <p>BZK��ѯϵͳ���Կ�� v1.0 | �������</p>
        </div>
    </div>
</body>
</html>
"@
    
    $html | Out-File -FilePath $reportPath -Encoding UTF8
    Write-ColorOutput "? ���Ա���������: $reportPath" "Green"
}

# ������
function Main {
    Write-ColorOutput "? BZK��ѯϵͳ����ִ���� v1.0" "Blue"
    Write-ColorOutput "=====================================" "Blue"
    
    $startTime = Get-Date
    
    # ��ʼ������
    Initialize-TestEnvironment
    
    # �������
    if (-not (Test-Dependencies)) {
        exit 1
    }
    
    $results = @{}
    
    # ���ݲ���ִ�в�ͬ���͵Ĳ���
    switch ($TestType) {
        "All" {
            Write-ColorOutput "? ִ�����в���..." "Magenta"
            $results["JMeter"] = Invoke-JMeterTests
            $results["Unit"] = Invoke-UnitTests
            $results["Integration"] = Invoke-IntegrationTests
            $results["UI"] = Invoke-UITests
            $results["Security"] = Invoke-SecurityTests
        }
        "JMeter" {
            $results["JMeter"] = Invoke-JMeterTests
        }
        "Unit" {
            $results["Unit"] = Invoke-UnitTests
        }
        "Integration" {
            $results["Integration"] = Invoke-IntegrationTests
        }
        "UI" {
            $results["UI"] = Invoke-UITests
        }
        "Security" {
            $results["Security"] = Invoke-SecurityTests
        }
    }
    
    # ���ɱ���
    New-TestReport
    
    # ��ʾ�ܽ�
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-ColorOutput "=====================================" "Blue"
    Write-ColorOutput "? ����ִ���ܽ�" "Blue"
    Write-ColorOutput "��ִ��ʱ��: $($duration.ToString('mm\:ss'))" "Cyan"
    
    $passCount = 0
    $failCount = 0
    
    foreach ($test in $results.Keys) {
        if ($results[$test]) {
            Write-ColorOutput "? $test : ͨ��" "Green"
            $passCount++
        } else {
            Write-ColorOutput "? $test : ʧ��" "Red"
            $failCount++
        }
    }
    
    Write-ColorOutput "=====================================" "Blue"
    Write-ColorOutput "ͨ��: $passCount | ʧ��: $failCount | �ܼ�: $($passCount + $failCount)" "Cyan"
    
    if ($failCount -eq 0) {
        Write-ColorOutput "? ���в���ͨ����" "Green"
        exit 0
    } else {
        Write-ColorOutput "?? ���ֲ���ʧ�ܣ�������ϸ��־" "Yellow"
        exit 1
    }
}

# �ű����
try {
    Main
}
catch {
    Write-ColorOutput "? �ű�ִ�й����з���δԤ�ڵĴ���:" "Red"
    Write-ColorOutput $_.Exception.Message "Red"
    Write-ColorOutput $_.ScriptStackTrace "Red"
    exit 1
} 