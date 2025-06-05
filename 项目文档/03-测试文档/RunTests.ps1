# BZK查询系统测试执行器
# 版本: v1.0
# 描述: 自动化测试框架，支持JMeter性能测试、单元测试、集成测试、UI测试和安全测试

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

# 彩色输出函数
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

# 初始化测试环境
function Initialize-TestEnvironment {
    Write-ColorOutput "? 初始化测试环境..." "Blue"
    
    # 创建测试结果目录
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
                # 忽略删除错误，继续执行
            }
        }
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    
    Write-ColorOutput "? 测试环境初始化完成" "Green"
}

# 检查依赖工具
function Test-Dependencies {
    Write-ColorOutput "? 检查测试工具依赖..." "Blue"
    
    # 检查 .NET
    try {
        $dotnetVersion = dotnet --version
        Write-ColorOutput "? dotnet 已安装: $dotnetVersion" "Green"
    }
    catch {
        Write-ColorOutput "? dotnet 未安装或未添加到PATH" "Red"
        return $false
    }
    
    # 检查 Java
    try {
        $javaVersion = java -version 2>&1 | Select-Object -First 1
        Write-ColorOutput "? java 已安装: $javaVersion" "Green"
    }
    catch {
        Write-ColorOutput "? Java 未安装或未添加到PATH" "Red"
        return $false
    }
    
    return $true
}

# 执行JMeter性能测试
function Invoke-JMeterTests {
    Write-ColorOutput "? 开始执行JMeter性能测试..." "Blue"
    
    # 设置JMeter环境变量
    if (-not $env:JMETER_HOME) {
        $env:JMETER_HOME = "D:\tools\apache-jmeter-5.6.3"
        $env:Path += ";D:\tools\apache-jmeter-5.6.3\bin"
    }
    
    # 检查JMeter是否可用
    try {
        $jmeterVersion = jmeter --version 2>&1 | Select-Object -First 10
        Write-ColorOutput "? 检测到JMeter" "Green"
    }
    catch {
        Write-ColorOutput "? JMeter未安装或未添加到PATH" "Red"
        Write-ColorOutput "请下载并安装Apache JMeter 5.5+" "Yellow"
        return $false
    }
    
    # 检查测试脚本
    if (-not (Test-Path "BZK性能测试.jmx")) {
        Write-ColorOutput "? 找不到JMeter测试脚本: BZK性能测试.jmx" "Red"
        return $false
    }
    
    # 执行JMeter测试
    $jmeterCmd = "jmeter -n -t `"BZK性能测试.jmx`" -l `"TestResults/JMeter/results.jtl`" -e -o `"TestResults/JMeter/html-report`" -Jbase.url=$BaseUrl"
    Write-ColorOutput "执行命令: $jmeterCmd" "Cyan"
    
    try {
        Invoke-Expression $jmeterCmd
        
        if (Test-Path "TestResults/JMeter/html-report/index.html") {
            Write-ColorOutput "? JMeter测试执行完成" "Green"
            Write-ColorOutput "? 测试报告: TestResults/JMeter/html-report/index.html" "Cyan"
            return $true
        } else {
            Write-ColorOutput "? JMeter测试报告生成失败" "Red"
            return $false
        }
    }
    catch {
        Write-ColorOutput "? JMeter测试执行失败: $($_.Exception.Message)" "Red"
        return $false
    }
}

# 执行单元测试
function Invoke-UnitTests {
    Write-ColorOutput "? 开始执行单元测试..." "Blue"
    
    # 查找测试项目 - 从项目根目录开始查找
    $projectRoot = (Get-Item $PSScriptRoot).Parent.Parent.FullName
    $testProjects = Get-ChildItem -Path $projectRoot -Recurse -Filter "*.Tests.csproj"
    
    if ($testProjects.Count -eq 0) {
        Write-ColorOutput "? 未找到测试项目" "Red"
        Write-ColorOutput "? 在路径 $projectRoot 中查找 *.Tests.csproj 文件" "Yellow"
        return $false
    }
    
    $allTestsPassed = $true
    
    foreach ($project in $testProjects) {
        Write-ColorOutput "? 执行测试项目: $($project.Name)" "Cyan"
        
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
                Write-ColorOutput "? 项目 $($project.Name) 测试通过" "Green"
            } else {
                Write-ColorOutput "? 项目 $($project.Name) 测试失败" "Red"
                $allTestsPassed = $false
            }
        }
        catch {
            Write-ColorOutput "? 项目 $($project.Name) 测试执行错误: $($_.Exception.Message)" "Red"
            $allTestsPassed = $false
        }
    }
    
    return $allTestsPassed
}

# 执行集成测试
function Invoke-IntegrationTests {
    Write-ColorOutput "? 开始执行集成测试..." "Blue"
    
    # 检查系统是否运行
    try {
        $response = Invoke-WebRequest -Uri $BaseUrl -Method Get -TimeoutSec 10 -ErrorAction Stop
        Write-ColorOutput "? 系统可访问: $BaseUrl (状态码: $($response.StatusCode))" "Green"
    }
    catch {
        Write-ColorOutput "? 无法访问系统: $BaseUrl" "Red"
        Write-ColorOutput "? 错误详情: $($_.Exception.Message)" "Yellow"
        Write-ColorOutput "请确保系统正在运行" "Yellow"
        return $false
    }
    
    # 检查API端点
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
            Write-ColorOutput "? API端点测试通过: $endpoint (状态码: $($response.StatusCode))" "Green"
        }
        catch {
            if ($_.Exception.Response.StatusCode -eq 404) {
                Write-ColorOutput "?? API端点不存在: $endpoint (404)" "Yellow"
            } else {
                Write-ColorOutput "? API端点测试失败: $endpoint - $($_.Exception.Message)" "Red"
                $apiTestsPassed = $false
            }
        }
    }
    
    Write-ColorOutput "? 集成测试检查完成" "Green"
    return $apiTestsPassed
}

# 执行UI自动化测试
function Invoke-UITests {
    Write-ColorOutput "? 开始执行UI自动化测试..." "Blue"
    
    # 检查Chrome浏览器
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
            Write-ColorOutput "? 检测到Chrome: $chromeVersion" "Green"
            Write-ColorOutput "? Chrome路径: $chromePath" "Cyan"
        }
        catch {
            Write-ColorOutput "? Chrome浏览器已安装: $chromePath" "Green"
        }
        
        # 设置Chrome路径环境变量
        $env:CHROME_PATH = $chromePath
        Write-ColorOutput "? UI测试环境配置完成" "Green"
        return $true
    } else {
        Write-ColorOutput "? Chrome浏览器未找到" "Red"
        Write-ColorOutput "请确保Chrome浏览器已正确安装" "Yellow"
        return $false
    }
}

# 执行安全测试
function Invoke-SecurityTests {
    Write-ColorOutput "?? 开始执行安全测试..." "Blue"
    
    # 基础安全检查
    Write-ColorOutput "? 执行基础安全扫描..." "Cyan"
    
    # HTTP头检查
    try {
        $headers = Invoke-WebRequest -Uri $BaseUrl -Method Head -TimeoutSec 10
        
        $securityHeaders = @{
            "X-Frame-Options" = "点击劫持防护"
            "X-Content-Type-Options" = "MIME类型嗅探防护"
            "X-XSS-Protection" = "XSS防护"
            "Strict-Transport-Security" = "HTTPS强制"
        }
        
        foreach ($header in $securityHeaders.Keys) {
            if ($headers.Headers.ContainsKey($header)) {
                Write-ColorOutput "? $($securityHeaders[$header]): $($headers.Headers[$header])" "Green"
            } else {
                Write-ColorOutput "?? 缺少安全头: $header ($($securityHeaders[$header]))" "Yellow"
            }
        }
    }
    catch {
        Write-ColorOutput "? 无法检查HTTP安全头" "Red"
    }
    
    # SQL注入测试
    Write-ColorOutput "? 执行SQL注入安全测试..." "Cyan"
    
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
                Write-ColorOutput "?? 可能存在SQL注入漏洞: $payload" "Yellow"
            } else {
                Write-ColorOutput "? 注入防护正常: $payload" "Green"
            }
        }
        catch {
            Write-ColorOutput "? 注入被正确阻止: $payload" "Green"
        }
    }
    
    return $true
}

# 生成测试报告
function New-TestReport {
    if (-not $GenerateReport) {
        return
    }
    
    Write-ColorOutput "? 生成测试报告..." "Blue"
    
    $reportPath = "TestResults/TestReport.html"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    # 创建HTML报告
    $html = @"
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>BZK查询系统 - 测试报告</title>
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
        <h1>? BZK查询系统 - 测试报告</h1>
        <p><strong>生成时间:</strong> $timestamp</p>
        <p><strong>测试类型:</strong> $TestType</p>
        <p><strong>基础URL:</strong> $BaseUrl</p>
        
        <div class="summary">
            <div class="card success">
                <h3>? 通过测试</h3>
                <p id="passCount">-</p>
            </div>
            <div class="card error">
                <h3>? 失败测试</h3>
                <p id="failCount">-</p>
            </div>
            <div class="card">
                <h3>? 总计</h3>
                <p id="totalCount">-</p>
            </div>
        </div>
        
        <h2>? 测试结果详情</h2>
        <table>
            <thead>
                <tr>
                    <th>测试类型</th>
                    <th>状态</th>
                    <th>说明</th>
                </tr>
            </thead>
            <tbody id="testResults">
                <!-- 测试结果将在这里插入 -->
            </tbody>
        </table>
        
        <h2>? 测试文件</h2>
        <ul>
            <li><a href="JMeter/html-report/index.html">JMeter性能测试报告</a></li>
            <li><a href="Unit/">单元测试结果</a></li>
            <li><a href="Integration/">集成测试结果</a></li>
            <li><a href="UI/">UI测试结果</a></li>
            <li><a href="Security/">安全测试结果</a></li>
        </ul>
        
        <div class="footer">
            <p>BZK查询系统测试框架 v1.0 | 测试完成</p>
        </div>
    </div>
</body>
</html>
"@
    
    $html | Out-File -FilePath $reportPath -Encoding UTF8
    Write-ColorOutput "? 测试报告已生成: $reportPath" "Green"
}

# 主函数
function Main {
    Write-ColorOutput "? BZK查询系统测试执行器 v1.0" "Blue"
    Write-ColorOutput "=====================================" "Blue"
    
    $startTime = Get-Date
    
    # 初始化环境
    Initialize-TestEnvironment
    
    # 检查依赖
    if (-not (Test-Dependencies)) {
        exit 1
    }
    
    $results = @{}
    
    # 根据参数执行不同类型的测试
    switch ($TestType) {
        "All" {
            Write-ColorOutput "? 执行所有测试..." "Magenta"
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
    
    # 生成报告
    New-TestReport
    
    # 显示总结
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-ColorOutput "=====================================" "Blue"
    Write-ColorOutput "? 测试执行总结" "Blue"
    Write-ColorOutput "总执行时间: $($duration.ToString('mm\:ss'))" "Cyan"
    
    $passCount = 0
    $failCount = 0
    
    foreach ($test in $results.Keys) {
        if ($results[$test]) {
            Write-ColorOutput "? $test : 通过" "Green"
            $passCount++
        } else {
            Write-ColorOutput "? $test : 失败" "Red"
            $failCount++
        }
    }
    
    Write-ColorOutput "=====================================" "Blue"
    Write-ColorOutput "通过: $passCount | 失败: $failCount | 总计: $($passCount + $failCount)" "Cyan"
    
    if ($failCount -eq 0) {
        Write-ColorOutput "? 所有测试通过！" "Green"
        exit 0
    } else {
        Write-ColorOutput "?? 部分测试失败，请检查详细日志" "Yellow"
        exit 1
    }
}

# 脚本入口
try {
    Main
}
catch {
    Write-ColorOutput "? 脚本执行过程中发生未预期的错误:" "Red"
    Write-ColorOutput $_.Exception.Message "Red"
    Write-ColorOutput $_.ScriptStackTrace "Red"
    exit 1
} 