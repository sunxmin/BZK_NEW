@echo off
chcp 65001 > nul
echo =======================================================
echo       BZK System - IIS Deploy Script
echo =======================================================
echo.

REM Check administrator privileges
net session >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] Administrator check passed
) else (
    echo [ERROR] Please run this script as administrator!
    pause
    exit /b 1
)

REM Configuration variables
set SITE_NAME=BZKQuerySystem
set SITE_PATH=C:\inetpub\wwwroot\%SITE_NAME%
set SOURCE_PATH=%~dp0BZKQuerySystem.Web\publish
set PORT=8080

echo.
echo [INFO] Configuration:
echo         Site Name: %SITE_NAME%
echo         Site Path: %SITE_PATH%
echo         Source Path: %SOURCE_PATH%
echo         Port: %PORT%
echo.

REM Check IIS features
echo [INFO] Checking if IIS is installed...
dism /online /get-featureinfo /featurename:IIS-WebServerRole >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] IIS not installed, installing IIS and ASP.NET Core features...

    REM Install IIS basic features
    dism /online /enable-feature /featurename:IIS-WebServerRole /all /norestart
    dism /online /enable-feature /featurename:IIS-WebServer /all /norestart
    dism /online /enable-feature /featurename:IIS-CommonHttpFeatures /all /norestart
    dism /online /enable-feature /featurename:IIS-HttpErrors /all /norestart
    dism /online /enable-feature /featurename:IIS-HttpLogging /all /norestart
    dism /online /enable-feature /featurename:IIS-RequestFiltering /all /norestart
    dism /online /enable-feature /featurename:IIS-StaticContent /all /norestart
    dism /online /enable-feature /featurename:IIS-DefaultDocument /all /norestart
    dism /online /enable-feature /featurename:IIS-DirectoryBrowsing /all /norestart
    dism /online /enable-feature /featurename:IIS-ASPNET45 /all /norestart
    dism /online /enable-feature /featurename:IIS-NetFxExtensibility45 /all /norestart
    dism /online /enable-feature /featurename:IIS-ISAPIExtensions /all /norestart
    dism /online /enable-feature /featurename:IIS-ISAPIFilter /all /norestart
    dism /online /enable-feature /featurename:IIS-ManagementConsole /all /norestart

    echo [INFO] IIS installation completed
) else (
    echo [INFO] IIS is already installed
)

REM Check ASP.NET Core Hosting Bundle
echo [INFO] Checking ASP.NET Core Hosting Bundle...
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" /s /f "Microsoft ASP.NET Core*Hosting Bundle*" >nul 2>&1
if %errorLevel% neq 0 (
    echo [WARNING] ASP.NET Core Hosting Bundle not detected
    echo [INFO] Please download and install ASP.NET Core 8.0 Hosting Bundle from:
    echo         https://dotnet.microsoft.com/download/dotnet/8.0
    echo [INFO] After installation, restart the server and run this script again
    pause
)

REM Stop existing site if exists
echo [INFO] Stopping existing site...
%windir%\system32\inetsrv\appcmd stop site /site.name:"%SITE_NAME%" >nul 2>&1

REM Delete existing site if exists
echo [INFO] Deleting existing site...
%windir%\system32\inetsrv\appcmd delete site /site.name:"%SITE_NAME%" >nul 2>&1

REM Delete existing application pool if exists
echo [INFO] Deleting existing application pool...
%windir%\system32\inetsrv\appcmd delete apppool /apppool.name:"%SITE_NAME%" >nul 2>&1

REM Create site directory
echo [INFO] Creating site directory...
if exist "%SITE_PATH%" rmdir /s /q "%SITE_PATH%"
mkdir "%SITE_PATH%"

REM Copy files
echo [INFO] Copying application files...
xcopy "%SOURCE_PATH%\*" "%SITE_PATH%\" /E /I /Y >nul
if %errorLevel% neq 0 (
    echo [ERROR] File copy failed!
    pause
    exit /b 1
)

REM Create application pool
echo [INFO] Creating application pool...
%windir%\system32\inetsrv\appcmd add apppool /name:"%SITE_NAME%" /managedRuntimeVersion:"" /processModel.identityType:ApplicationPoolIdentity

REM Configure application pool
echo [INFO] Configuring application pool...
%windir%\system32\inetsrv\appcmd set apppool "%SITE_NAME%" /processModel.idleTimeout:00:00:00
%windir%\system32\inetsrv\appcmd set apppool "%SITE_NAME%" /recycling.periodicRestart.time:00:00:00

REM Create site
echo [INFO] Creating IIS site...
%windir%\system32\inetsrv\appcmd add site /name:"%SITE_NAME%" /physicalPath:"%SITE_PATH%" /bindings:http/*:%PORT%:

REM Configure site to use application pool
echo [INFO] Configuring site application pool...
%windir%\system32\inetsrv\appcmd set app "%SITE_NAME%/" /applicationPool:"%SITE_NAME%"

REM Set permissions
echo [INFO] Setting folder permissions...
icacls "%SITE_PATH%" /grant "IIS_IUSRS:(OI)(CI)F" /T >nul 2>&1
icacls "%SITE_PATH%" /grant "IUSR:(OI)(CI)F" /T >nul 2>&1

REM Start application pool and site
echo [INFO] Starting application pool...
%windir%\system32\inetsrv\appcmd start apppool /apppool.name:"%SITE_NAME%"

echo [INFO] Starting site...
%windir%\system32\inetsrv\appcmd start site /site.name:"%SITE_NAME%"

echo.
echo =======================================================
echo               Deployment Complete!
echo =======================================================
echo.
echo Site successfully deployed to IIS
echo Access URL: http://localhost:%PORT%
echo Site Path: %SITE_PATH%
echo.
echo Please ensure:
echo 1. SQL Server service is running
echo 2. Redis service is running
echo 3. Firewall allows port %PORT%
echo.
echo If you encounter issues, check:
echo 1. Site status in IIS Manager
echo 2. Windows Event Logs
echo 3. Application log files
echo.

pause
