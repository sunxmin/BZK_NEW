# Restore web.config with AspNetCoreModuleV2 after Hosting Bundle installation
# Run as Administrator

param(
    [string]$SitePath = "C:\inetpub\wwwroot\BZKQuerySystem"
)

# Check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Host "[ERROR] Please run this script as Administrator!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "  Restoring web.config after Hosting Bundle installation" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

$webConfigPath = "$SitePath\web.config"

# Check if backup exists
$backupPath = "$webConfigPath.backup"
if (Test-Path $backupPath) {
    Write-Host "Found backup file, using it..." -ForegroundColor Green
    Copy-Item $backupPath $webConfigPath -Force
}
else {
    Write-Host "No backup found, creating new optimized web.config..." -ForegroundColor Yellow

    # Create optimized web.config for AspNetCoreModuleV2
    $webConfigContent = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <!-- ASP.NET Core Module V2 配置 -->
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>

      <aspNetCore processPath="dotnet"
                  arguments=".\BZKQuerySystem.Web.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">

        <!-- 环境变量 -->
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="ASPNETCORE_URLS" value="http://*:8080" />
        </environmentVariables>
      </aspNetCore>

      <!-- 安全配置 -->
      <security>
        <requestFiltering>
          <!-- 允许上传大文件 -->
          <requestLimits maxAllowedContentLength="52428800" />
          <!-- 允许的HTTP动词 -->
          <verbs allowUnlisted="true">
            <add verb="GET" allowed="true" />
            <add verb="POST" allowed="true" />
            <add verb="PUT" allowed="true" />
            <add verb="DELETE" allowed="true" />
            <add verb="OPTIONS" allowed="true" />
          </verbs>
        </requestFiltering>
      </security>

      <!-- HTTP响应头 -->
      <httpProtocol>
        <customHeaders>
          <!-- 安全头 -->
          <add name="X-Content-Type-Options" value="nosniff" />
          <add name="X-Frame-Options" value="SAMEORIGIN" />
          <add name="X-XSS-Protection" value="1; mode=block" />
          <add name="Referrer-Policy" value="strict-origin-when-cross-origin" />
        </customHeaders>
      </httpProtocol>

      <!-- 静态内容配置 -->
      <staticContent>
        <!-- 移除默认的扩展映射并添加新的 -->
        <remove fileExtension=".woff" />
        <remove fileExtension=".woff2" />
        <mimeMap fileExtension=".woff" mimeType="application/font-woff" />
        <mimeMap fileExtension=".woff2" mimeType="font/woff2" />

        <!-- 设置静态文件缓存 -->
        <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
      </staticContent>

      <!-- 错误页面 -->
      <httpErrors errorMode="Custom" defaultResponseMode="ExecuteURL">
        <remove statusCode="404" subStatusCode="-1" />
        <error statusCode="404" path="/Home/NotFound" responseMode="ExecuteURL" />
        <remove statusCode="500" subStatusCode="-1" />
        <error statusCode="500" path="/Home/Error" responseMode="ExecuteURL" />
      </httpErrors>

      <!-- 压缩配置 -->
      <httpCompression directory="%SystemDrive%\inetpub\temp\IIS Temporary Compressed Files">
        <scheme name="gzip" dll="%Windir%\system32\inetsrv\gzip.dll" />
        <dynamicTypes>
          <add mimeType="text/*" enabled="true" />
          <add mimeType="message/*" enabled="true" />
          <add mimeType="application/javascript" enabled="true" />
          <add mimeType="application/json" enabled="true" />
          <add mimeType="*/*" enabled="false" />
        </dynamicTypes>
        <staticTypes>
          <add mimeType="text/*" enabled="true" />
          <add mimeType="message/*" enabled="true" />
          <add mimeType="application/javascript" enabled="true" />
          <add mimeType="*/*" enabled="false" />
        </staticTypes>
      </httpCompression>

    </system.webServer>
  </location>
</configuration>
'@

    # Write the web.config
    try {
        $webConfigContent | Out-File -FilePath $webConfigPath -Encoding UTF8 -Force
        Write-Host "[SUCCESS] New web.config created with AspNetCoreModuleV2" -ForegroundColor Green
    }
    catch {
        Write-Host "[ERROR] Failed to create web.config: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Import WebAdministration module and restart application pool
Write-Host "[INFO] Restarting application pool..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop

    # Stop and start application pool
    Stop-WebAppPool -Name "BZKQuerySystem" -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    Start-WebAppPool -Name "BZKQuerySystem"

    Write-Host "[SUCCESS] Application pool restarted" -ForegroundColor Green
}
catch {
    Write-Host "[WARNING] Failed to restart application pool: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Please manually restart the application pool in IIS Manager" -ForegroundColor Cyan
}

# Test if AspNetCoreModuleV2 is available
Write-Host "[INFO] Checking AspNetCoreModuleV2 availability..." -ForegroundColor Yellow
try {
    $modules = Get-WebGlobalModule | Where-Object Name -like "*AspNetCore*"
    if ($modules) {
        Write-Host "[SUCCESS] ASP.NET Core modules found:" -ForegroundColor Green
        $modules | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor White }
    }
    else {
        Write-Host "[WARNING] No ASP.NET Core modules found" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "[WARNING] Could not check modules (requires elevated permissions)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "            Configuration Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Test the website: http://localhost:8080" -ForegroundColor White
Write-Host "2. Check application logs at: $SitePath\logs\" -ForegroundColor White
Write-Host "3. If still having issues, check Windows Event Logs" -ForegroundColor White
Write-Host ""

# Test site accessibility
Write-Host "[INFO] Testing site accessibility..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080" -TimeoutSec 15 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "[SUCCESS] Site is accessible! Status: $($response.StatusCode)" -ForegroundColor Green
    }
    else {
        Write-Host "[INFO] Site responded with status: $($response.StatusCode)" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "[INFO] Site test: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Please test manually in browser: http://localhost:8080" -ForegroundColor Cyan
}

Read-Host "Press Enter to exit"
