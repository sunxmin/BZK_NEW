@echo off
echo ================================================
echo            SQL Server 服务启动脚本
echo ================================================
echo.

echo 检查当前用户权限...
net session >nul 2>&1
if %errorLevel% == 0 (
    echo ✓ 检测到管理员权限
) else (
    echo ✗ 需要管理员权限！
    echo.
    echo 请右键点击此批处理文件，选择"以管理员身份运行"
    echo.
    pause
    exit /b 1
)

echo.
echo 正在启动SQL Server相关服务...
echo.

echo 1. 启动 SQL Server 主服务...
net start MSSQLSERVER
if %errorLevel% == 0 (
    echo ✓ SQL Server 主服务启动成功
) else (
    echo ✗ SQL Server 主服务启动失败 - 错误代码: %errorLevel%
)

echo.
echo 2. 启动 SQL Server 代理服务...
net start SQLSERVERAGENT
if %errorLevel% == 0 (
    echo ✓ SQL Server 代理服务启动成功
) else (
    echo ✗ SQL Server 代理服务启动失败 - 错误代码: %errorLevel%
)

echo.
echo 3. 启动 SQL Server 浏览器服务...
net start SQLBrowser
if %errorLevel% == 0 (
    echo ✓ SQL Server 浏览器服务启动成功
) else (
    echo ✗ SQL Server 浏览器服务启动失败 - 错误代码: %errorLevel%
)

echo.
echo ================================================
echo              服务启动状态检查
echo ================================================
echo.

echo 检查服务状态：
sc query MSSQLSERVER | findstr STATE
sc query SQLSERVERAGENT | findstr STATE
sc query SQLBrowser | findstr STATE

echo.
echo 测试数据库连接...
sqlcmd -S localhost -U sa -P 123 -Q "SELECT 'SQL Server 连接成功！' AS 状态, @@VERSION AS 版本" -h -1
if %errorLevel% == 0 (
    echo ✓ 数据库连接测试成功！
) else (
    echo ✗ 数据库连接测试失败
    echo.
    echo 可能的原因：
    echo - sa用户未启用或密码不正确
    echo - SQL Server未配置为混合身份验证模式
    echo - 需要重启SQL Server服务以应用配置更改
    echo.
    echo 解决方案：
    echo 1. 打开SQL Server Management Studio
    echo 2. 使用Windows身份验证连接到localhost
    echo 3. 启用sa用户并设置密码为：123
    echo 4. 启用混合身份验证模式
    echo 5. 重启SQL Server服务
)

echo.
echo 现在可以运行您的应用程序了：
echo cd /d "D:\CursorProject\BZK_NEW\BZKQuerySystem.Web"
echo dotnet run
echo.

pause 