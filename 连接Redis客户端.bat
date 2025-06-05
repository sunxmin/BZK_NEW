@echo off
title BZK Redis客户端连接
echo ========================================
echo BZK Redis客户端连接脚本
echo ========================================

echo 检查Redis服务器是否运行...

REM 检查6379端口是否有服务监听
netstat -an | findstr :6379 >nul
if %errorlevel% == 0 (
    echo ✓ Redis服务器正在运行
) else (
    echo ❌ Redis服务器未运行！
    echo.
    echo 请先运行 "启动Redis服务.bat" 启动Redis服务器
    echo 或者使用以下命令启动Redis：
    echo redis-server.exe --port 6379 --requirepass BZK_Redis_2025
    echo.
    goto :end
)

echo.
echo 正在连接到Redis服务器...
echo 服务器: 127.0.0.1:6379
echo 密码: BZK_Redis_2025
echo.
echo 连接成功后可以使用以下命令：
echo   ping                    # 测试连接
echo   set key value           # 设置键值
echo   get key                 # 获取值
echo   keys *                  # 查看所有键
echo   info                    # 查看服务器信息
echo   exit                    # 退出
echo.

REM 检查Redis安装位置
if exist "D:\Redis\redis-cli.exe" (
    cd /d "D:\Redis"
    redis-cli.exe -p 6379 -a BZK_Redis_2025
    goto :end
)

if exist "C:\Program Files\Redis\redis-cli.exe" (
    cd /d "C:\Program Files\Redis"
    redis-cli.exe -p 6379 -a BZK_Redis_2025
    goto :end
)

if exist "C:\Redis\redis-cli.exe" (
    cd /d "C:\Redis"
    redis-cli.exe -p 6379 -a BZK_Redis_2025
    goto :end
)

if exist "C:\ProgramData\chocolatey\lib\redis-64\tools\redis-cli.exe" (
    cd /d "C:\ProgramData\chocolatey\lib\redis-64\tools"
    redis-cli.exe -p 6379 -a BZK_Redis_2025
    goto :end
)

echo ❌ 未找到redis-cli.exe客户端工具
echo 请检查Redis安装

:end
echo.
echo 连接已断开
pause
