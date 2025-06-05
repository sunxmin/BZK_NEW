# Fix web.config for ASP.NET Core compatibility
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

Write-Host "Fixing web.config for ASP.NET Core compatibility..." -ForegroundColor Yellow

$webConfigPath = "$SitePath\web.config"

# Backup original web.config
if (Test-Path $webConfigPath) {
    Copy-Item $webConfigPath "$webConfigPath.backup" -Force
    Write-Host "Original web.config backed up" -ForegroundColor Green
}

# Create fixed web.config content
$webConfigContent = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModule" resourceType="Unspecified" />
      </handlers>

      <aspNetCore processPath="dotnet"
                  arguments=".\BZKQuerySystem.Web.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="outofprocess">

        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>

      <security>
        <requestFiltering>
          <requestLimits maxAllowedContentLength="52428800" />
        </requestFiltering>
      </security>

      <httpProtocol>
        <customHeaders>
          <add name="X-Content-Type-Options" value="nosniff" />
          <add name="X-Frame-Options" value="SAMEORIGIN" />
          <add name="X-XSS-Protection" value="1; mode=block" />
        </customHeaders>
      </httpProtocol>

      <staticContent>
        <remove fileExtension=".woff" />
        <remove fileExtension=".woff2" />
        <mimeMap fileExtension=".woff" mimeType="application/font-woff" />
        <mimeMap fileExtension=".woff2" mimeType="font/woff2" />
      </staticContent>

    </system.webServer>
  </location>
</configuration>
'@

# Write fixed web.config
try {
    $webConfigContent | Out-File -FilePath $webConfigPath -Encoding UTF8 -Force
    Write-Host "web.config updated successfully" -ForegroundColor Green
}
catch {
    Write-Host "Failed to update web.config: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Restart application pool
Import-Module WebAdministration
try {
    Restart-WebAppPool -Name "BZKQuerySystem"
    Write-Host "Application pool restarted" -ForegroundColor Green
}
catch {
    Write-Host "Failed to restart application pool: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Fix completed! Please test the website at http://localhost:8080" -ForegroundColor Cyan
Write-Host ""
Write-Host "If you still get errors, please:" -ForegroundColor Yellow
Write-Host "1. Install ASP.NET Core 8.0 Hosting Bundle from:" -ForegroundColor White
Write-Host "   https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
Write-Host "2. Restart your computer" -ForegroundColor White
Write-Host "3. Test the website again" -ForegroundColor White

Read-Host "Press Enter to exit"
