# BZK病种库查询系统 - IIS部署指南

## 概述

本指南将帮助您将BZK病种库查询系统部署到本地IIS服务器上。

## 系统要求

### 硬件要求
- **CPU**: 2核心以上
- **内存**: 4GB以上
- **硬盘**: 20GB可用空间

### 软件要求
- **操作系统**: Windows 10/11 或 Windows Server 2016+
- **IIS**: 10.0+
- **ASP.NET Core Runtime**: 8.0
- **SQL Server**: 2016+
- **Redis**: 6.0+

## 部署前准备

### 1. 确保依赖服务运行正常

#### SQL Server检查
```bash
# 检查SQL Server服务状态
net start | findstr "SQL Server"

# 测试数据库连接
sqlcmd -S localhost -U sa -P 123 -Q "SELECT @@VERSION"
```

#### Redis检查
```bash
# 启动Redis（如果未运行）
启动Redis服务.bat

# 测试Redis连接
测试Redis连接.ps1
```

### 2. 下载ASP.NET Core Hosting Bundle

如果系统未安装ASP.NET Core Runtime，请从以下地址下载：
- [ASP.NET Core 8.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)

安装后**必须重启服务器**。

## 一键部署

### 方法一：使用自动部署脚本（推荐）

1. 以**管理员身份**运行PowerShell或命令提示符
2. 导航到项目根目录
3. 执行部署脚本：

```bash
deploy-to-iis.bat
```

### 方法二：手动部署

#### 1. 发布应用程序

```bash
cd BZKQuerySystem.Web
dotnet publish -c Release -o publish
```

#### 2. 安装IIS功能

以管理员身份运行PowerShell，执行：

```powershell
# 启用IIS功能
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging
Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestFiltering
Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DefaultDocument
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DirectoryBrowsing
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45
Enable-WindowsOptionalFeature -Online -FeatureName IIS-NetFxExtensibility45
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIExtensions
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIFilter
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole
```

#### 3. 创建IIS站点

```powershell
# 设置变量
$siteName = "BZKQuerySystem"
$sitePath = "C:\inetpub\wwwroot\$siteName"
$port = 8080

# 创建目录
New-Item -ItemType Directory -Path $sitePath -Force

# 复制文件
Copy-Item -Path ".\BZKQuerySystem.Web\publish\*" -Destination $sitePath -Recurse -Force

# 创建应用程序池
New-WebAppPool -Name $siteName -Force
Set-ItemProperty -Path "IIS:\AppPools\$siteName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path "IIS:\AppPools\$siteName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"

# 创建站点
New-Website -Name $siteName -PhysicalPath $sitePath -Port $port -ApplicationPool $siteName

# 设置权限
icacls $sitePath /grant "IIS_IUSRS:(OI)(CI)F" /T
icacls $sitePath /grant "IUSR:(OI)(CI)F" /T

# 启动站点
Start-Website -Name $siteName
Start-WebAppPool -Name $siteName
```

## 部署后验证

### 1. 检查站点状态

在IIS管理器中检查：
1. 应用程序池 "BZKQuerySystem" 是否运行
2. 站点 "BZKQuerySystem" 是否启动
3. 检查绑定端口是否正确（8080）

### 2. 测试访问

打开浏览器访问：
- **主页**: http://localhost:8080
- **健康检查**: http://localhost:8080/health
- **系统监控**: http://localhost:8080/Monitoring/Dashboard

### 3. 检查日志

查看以下位置的日志文件：
- **ASP.NET Core日志**: `C:\inetpub\wwwroot\BZKQuerySystem\logs\`
- **Windows事件日志**: 事件查看器 → Windows日志 → 应用程序
- **IIS日志**: `C:\inetpub\logs\LogFiles\`

## 常见问题排查

### 问题1: 502.5错误 - 进程失败

**可能原因**:
- ASP.NET Core Runtime未安装或版本不匹配
- 应用程序权限不足

**解决方案**:
1. 安装或更新ASP.NET Core 8.0 Runtime
2. 检查应用程序池身份
3. 查看事件日志获取详细错误信息

### 问题2: 数据库连接失败

**检查步骤**:
1. 确认SQL Server服务运行正常
2. 验证连接字符串正确性
3. 检查防火墙设置
4. 验证SQL Server身份验证模式

### 问题3: Redis连接失败

**检查步骤**:
1. 确认Redis服务运行正常
2. 检查Redis配置文件
3. 验证密码设置
4. 测试网络连接

### 问题4: 权限问题

**解决方案**:
```bash
# 重新设置权限
icacls "C:\inetpub\wwwroot\BZKQuerySystem" /grant "IIS_IUSRS:(OI)(CI)F" /T
icacls "C:\inetpub\wwwroot\BZKQuerySystem" /grant "IUSR:(OI)(CI)F" /T
```

### 问题5: 静态文件不加载

**检查项目**:
1. web.config文件是否正确
2. 静态文件处理程序是否启用
3. MIME类型映射是否正确

## 性能优化建议

### 1. IIS配置优化

```xml
<!-- 在web.config中添加 -->
<system.webServer>
  <!-- 启用压缩 -->
  <urlCompression doStaticCompression="true" doDynamicCompression="true" />
  
  <!-- 输出缓存 -->
  <caching enabled="true" enableKernelCache="true">
    <profiles>
      <add extension=".css" policy="CacheUntilChange" kernelCachePolicy="CacheUntilChange" />
      <add extension=".js" policy="CacheUntilChange" kernelCachePolicy="CacheUntilChange" />
    </profiles>
  </caching>
</system.webServer>
```

### 2. 应用程序池优化

```powershell
# 设置应用程序池参数
Set-ItemProperty -Path "IIS:\AppPools\BZKQuerySystem" -Name "recycling.periodicRestart.time" -Value "00:00:00"
Set-ItemProperty -Path "IIS:\AppPools\BZKQuerySystem" -Name "processModel.idleTimeout" -Value "00:00:00"
Set-ItemProperty -Path "IIS:\AppPools\BZKQuerySystem" -Name "processModel.maxProcesses" -Value 1
```

### 3. 系统监控

定期检查以下指标：
- CPU使用率
- 内存使用情况
- 磁盘I/O
- 网络连接数
- 数据库连接池状态

## 安全考虑

### 1. 网络安全
- 配置防火墙规则
- 限制不必要的端口访问
- 考虑使用HTTPS（需要SSL证书）

### 2. 应用程序安全
- 定期更新系统和组件
- 配置适当的用户权限
- 启用审计日志

### 3. 数据安全
- 定期备份数据库
- 设置强密码策略
- 加密敏感数据

## 维护建议

### 日常维护
- 监控系统资源使用情况
- 检查错误日志
- 清理临时文件
- 更新安全补丁

### 定期维护
- 数据库维护和优化
- 日志文件归档
- 性能基准测试
- 灾难恢复测试

## 联系支持

如遇到部署问题，请提供以下信息：
1. 错误信息和堆栈跟踪
2. 系统配置信息
3. 相关日志文件
4. 部署步骤和环境描述

---

**部署完成后，您的系统将可通过 http://localhost:8080 访问** 
