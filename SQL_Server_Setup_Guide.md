# SQL Server 设置指南

## 问题诊断

您遇到的错误 `Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool` 表明应用程序无法连接到SQL Server数据库。

根据检查，您的系统中SQL Server服务处于停止状态，这是问题的根源。

## 解决方案

### 方案1：启动SQL Server服务（推荐）

#### 1. 使用管理员权限启动服务

**方法A：通过Windows服务管理器**
1. 按 `Win + R` 打开运行对话框
2. 输入 `services.msc` 并按回车
3. 在服务列表中找到 `SQL Server (MSSQLSERVER)`
4. 右键点击该服务，选择"启动"
5. 如果启动成功，服务状态将变为"正在运行"

**方法B：通过命令行**
1. 以管理员身份打开命令提示符
   - 按 `Win + X`，选择"Windows PowerShell(管理员)"
   - 或搜索"cmd"，右键选择"以管理员身份运行"
2. 运行以下命令：
   ```cmd
   net start MSSQLSERVER
   ```

#### 2. 启动SQL Server代理（可选但推荐）
```cmd
net start SQLSERVERAGENT
```

#### 3. 验证服务状态
```powershell
Get-Service MSSQLSERVER
```

### 方案2：配置SQL Server身份验证

启动SQL Server服务后，需要确保sa用户可以登录：

#### 1. 打开SQL Server Management Studio (SSMS)
- 如果没有安装SSMS，可以从微软官网下载

#### 2. 使用Windows身份验证连接
- 服务器名称：`localhost` 或 `(local)`
- 身份验证：Windows身份验证

#### 3. 启用混合身份验证模式
1. 右键点击服务器名称 → 属性
2. 选择"安全性"页面
3. 在"服务器身份验证"下选择"SQL Server和Windows身份验证模式"
4. 点击"确定"

#### 4. 启用sa用户并设置密码
1. 展开"安全性" → "登录名"
2. 右键点击"sa" → 属性
3. 在"常规"页面，设置密码为：`123`
4. 在"状态"页面：
   - 授权：已授权
   - 登录：已启用
5. 点击"确定"

#### 5. 重启SQL Server服务使更改生效
```cmd
net stop MSSQLSERVER
net start MSSQLSERVER
```

### 方案3：测试连接

启动服务并配置身份验证后，可以测试连接：

```cmd
sqlcmd -S localhost -U sa -P 123 -Q "SELECT @@VERSION"
```

如果连接成功，应该会显示SQL Server版本信息。

## 现在运行应用程序

完成上述设置后，返回到项目目录并重新运行：

```cmd
cd D:\CursorProject\BZK_NEW\BZKQuerySystem.Web
dotnet run
```

应用程序现在应该能够成功连接到数据库并启动。

## 常见问题

### Q: 为什么需要管理员权限？
A: Windows服务管理需要管理员权限。SQL Server是系统级服务。

### Q: 如果忘记sa密码怎么办？
A: 可以使用Windows身份验证连接，然后重置sa密码。

### Q: 服务启动失败怎么办？
A: 检查Windows事件查看器中的错误日志，可能需要检查SQL Server安装或配置。

### Q: 应用程序仍然无法连接？
A: 
1. 确认SQL Server服务正在运行
2. 检查防火墙设置
3. 验证TCP/IP协议已启用（SQL Server配置管理器）
4. 确认端口1433可用

## 自动化脚本

为了方便，可以创建一个批处理文件来自动启动服务：

创建 `start-sql-server.bat` 文件：
```bat
@echo off
echo 正在启动SQL Server服务...
net start MSSQLSERVER
net start SQLSERVERAGENT
echo SQL Server服务启动完成。
pause
```

右键以管理员身份运行此批处理文件。

## 服务器状态检查

当前检测到的SQL Server服务状态：
- ✅ SQL Server Integration Services 15.0: 正在运行
- ✅ SQL Full-text Filter Daemon Launcher: 正在运行  
- ❌ **SQL Server (MSSQLSERVER): 已停止** ← 需要启动
- ✅ SQL Server Analysis Services: 正在运行
- ❌ SQL Server 代理: 已停止 ← 建议启动
- ❌ SQL Server Browser: 已停止 ← 建议启动

主要需要启动的是 **MSSQLSERVER** 服务。 