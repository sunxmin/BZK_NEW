@echo off
title BZK Redis服务器启动
echo ========================================
echo BZK Redis服务器启动脚本
echo ========================================

echo 检查Redis安装位置...

REM 检查常见的Redis安装位置
if exist "D:\Redis\redis-server.exe" (
    set REDIS_PATH=D:\Redis
    goto :start_redis
)

if exist "C:\Program Files\Redis\redis-server.exe" (
    set REDIS_PATH=C:\Program Files\Redis
    goto :start_redis
)

if exist "C:\Redis\redis-server.exe" (
    set REDIS_PATH=C:\Redis
    goto :start_redis
)

REM 检查是否通过Chocolatey安装
if exist "C:\ProgramData\chocolatey\lib\redis-64\tools\redis-server.exe" (
    set REDIS_PATH=C:\ProgramData\chocolatey\lib\redis-64\tools
    goto :start_redis
)

echo ❌ 未找到Redis安装！
echo 请先安装Redis或检查安装路径
echo.
echo 建议使用以下命令安装：
echo choco install redis-64
echo.
goto :end

:start_redis
echo ✓ 找到Redis安装位置: %REDIS_PATH%
echo.
echo 正在启动Redis服务器...
echo 端口: 6379
echo 密码: BZK_Redis_2025
echo.
echo ⚠️  保持此窗口打开，Redis服务将在此运行
echo    关闭窗口将停止Redis服务
echo.

cd /d "%REDIS_PATH%"

REM 启动Redis服务器，使用密码保护
redis-server.exe --port 6379 --requirepass BZK_Redis_2025

:end
echo.
echo Redis服务已停止
pause
