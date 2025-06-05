# BZK系统Redis服务管理工具集

**文档编号**: Redis-Tools-001  
**创建日期**: 2024年12月27日  
**文档版本**: v1.0  
**维护者**: 系统管理员  
**适用范围**: BZK系统Redis缓存服务  

## 📋 工具集概述

本工具集提供了BZK系统Redis缓存服务的完整管理解决方案，包括安装、启动、连接、测试和故障排除等功能。所有工具均经过实际环境验证，可直接用于生产环境。

## 🛠️ 工具清单

### 1. Redis安装工具

#### **install_redis.ps1**

- **功能**: 自动化Redis安装和配置
- **支持平台**: Windows 10/11, Windows Server 2016+
- **安装方式**: Chocolatey、手动下载、Docker
- **位置**: 项目根目录

**使用方法**:

```powershell
# 以管理员身份运行PowerShell
powershell -ExecutionPolicy Bypass -File install_redis.ps1
```

**功能特性**:

- 自动检测系统环境
- 支持多种安装方式选择
- 自动配置密码和端口
- 安装后自动验证

### 2. Redis服务管理工具

#### **启动Redis服务.bat**

- **功能**: 启动Redis服务器
- **配置**: 端口6379，密码BZK_Redis_2025
- **位置**: 项目根目录

**使用方法**:

```batch
# 双击运行或命令行执行
启动Redis服务.bat
```

**重要说明**:

- 保持窗口打开，Redis服务在此运行
- 关闭窗口将停止Redis服务
- 支持自动检测安装位置

#### **连接Redis客户端.bat**

- **功能**: 连接Redis客户端进行管理
- **前置条件**: Redis服务必须运行
- **位置**: 项目根目录

**使用方法**:

```batch
# 双击运行或命令行执行
连接Redis客户端.bat
```

**常用命令**:

```redis
ping                    # 测试连接
set key value           # 设置键值
get key                 # 获取值
keys *                  # 查看所有键
info                    # 查看服务器信息
flushall                # 清空所有数据(谨慎使用)
exit                    # 退出客户端
```

### 3. 系统集成测试工具

#### **测试Redis连接.ps1**

- **功能**: 全面测试Redis与BZK系统集成
- **检查项目**: 端口状态、服务连接、系统集成、API响应
- **位置**: 项目根目录

**使用方法**:

```powershell
powershell -ExecutionPolicy Bypass -File "测试Redis连接.ps1"
```

**测试内容**:

1. Redis服务器端口6379状态检查
2. BZK系统启动状态验证
3. 监控API响应测试
4. Redis直接连接测试
5. 缓存功能集成验证

### 4. 历史工具(已整合)

#### **验证Redis缓存集成.ps1**

- **位置**: 项目文档/04-运维支持文档/
- **状态**: 已被新版测试工具替代
- **说明**: 保留作为参考，建议使用新版测试工具

#### **安装Redis缓存服务.ps1**

- **位置**: 项目文档/04-运维支持文档/
- **状态**: 已被新版安装工具替代
- **说明**: 保留作为参考，建议使用新版安装工具

## 🚀 快速部署指南

### 新环境部署流程

#### **步骤1: 安装Redis**

```powershell
# 1. 以管理员身份打开PowerShell
# 2. 导航到项目根目录
cd "D:\米帝信息科技\病种库相关\BZK_NEW"

# 3. 运行安装脚本
powershell -ExecutionPolicy Bypass -File install_redis.ps1

# 4. 选择安装方式(推荐选择1-Chocolatey)
```

#### **步骤2: 启动Redis服务**

```batch
# 1. 双击运行 "启动Redis服务.bat"
# 2. 确认看到 "Server started. Redis version 3.2.100"
# 3. 确认看到 "The server is now ready to accept connections on port 6379"
# 4. 保持窗口打开
```

#### **步骤3: 验证安装**

```powershell
# 运行测试脚本
powershell -ExecutionPolicy Bypass -File "测试Redis连接.ps1"

# 预期结果:
# ✓ Redis服务器正在端口6379运行
# ✓ BZK系统运行正常
# ✓ 监控API响应正常
# ✓ Redis ping测试成功
```

#### **步骤4: 启动BZK系统**

```bash
cd BZKQuerySystem.Web
dotnet run
```

### 日常运维流程

#### **启动服务**

1. 启动Redis: 运行 `启动Redis服务.bat`
2. 启动BZK: `cd BZKQuerySystem.Web && dotnet run`
3. 验证状态: 访问系统监控页面

#### **停止服务**

1. 停止BZK: 在终端按 `Ctrl+C`
2. 停止Redis: 关闭Redis服务窗口

#### **状态检查**

```powershell
# 快速状态检查
netstat -an | findstr :6379  # 检查Redis端口
netstat -an | findstr :5072  # 检查BZK端口

# 详细状态检查
powershell -ExecutionPolicy Bypass -File "测试Redis连接.ps1"
```

## 🔧 故障排除指南

### 常见问题及解决方案

#### **问题1: Redis启动失败**

**症状**: 启动脚本提示"未找到Redis安装"
**解决方案**:

```powershell
# 1. 重新运行安装脚本
powershell -ExecutionPolicy Bypass -File install_redis.ps1

# 2. 或手动检查安装路径
dir "C:\Program Files\Redis"
dir "C:\ProgramData\chocolatey\lib\redis-64\tools"
```

#### **问题2: Redis连接被拒绝**

**症状**: 客户端显示"connection refused"
**解决方案**:

```batch
# 1. 确认Redis服务正在运行
netstat -an | findstr :6379

# 2. 检查防火墙设置
# 3. 重启Redis服务
```

#### **问题3: BZK系统显示Redis Warning**

**症状**: 监控页面显示Redis状态为Warning
**解决方案**:

```powershell
# 1. 运行完整测试
powershell -ExecutionPolicy Bypass -File "测试Redis连接.ps1"

# 2. 检查连接字符串配置
# 确认appsettings.json中Redis连接字符串正确

# 3. 重启BZK系统
```

#### **问题4: 监控页面数据异常**

**症状**: 监控页面显示空数据或错误信息
**解决方案**:

```bash
# 1. 重启BZK系统
cd BZKQuerySystem.Web
dotnet run

# 2. 清除浏览器缓存
# 3. 检查网络连接
```

### 日志分析指南

#### **Redis服务日志**

**位置**: Redis服务窗口输出
**关键信息**:

- `Server started`: 服务启动成功
- `Ready to accept connections`: 可接受连接
- `Client connected`: 客户端连接
- `Client disconnected`: 客户端断开

#### **BZK系统日志**

**位置**: dotnet run终端输出
**关键信息**:

- `Redis ConnectionMultiplexer服务未注册`: 配置问题
- `Redis连接检查超时`: 连接问题
- `监控服务收到停止信号`: 正常关闭

## 📊 性能监控

### 关键指标监控

#### **Redis性能指标**

- **连接数**: `info clients`
- **内存使用**: `info memory`
- **命中率**: `info stats`
- **操作统计**: `info commandstats`

#### **BZK集成指标**

- **缓存命中率**: 监控页面显示
- **响应时间**: < 2秒正常
- **错误率**: < 1%正常
- **并发连接**: 支持180用户

### 监控命令速查

```redis
# Redis服务器信息
info server

# 客户端连接信息
info clients
client list

# 内存使用信息
info memory
memory usage [key]

# 性能统计
info stats
info commandstats

# 缓存内容检查
keys BZK:*
ttl [key]
```

## 📚 最佳实践

### 安全配置

1. **密码保护**: 使用强密码 `BZK_Redis_2025`
2. **网络限制**: 仅允许本地连接
3. **定期备份**: 重要数据定期备份
4. **权限控制**: 限制客户端权限

### 性能优化

1. **内存管理**: 设置合理的最大内存限制
2. **过期策略**: 配置适当的数据过期时间
3. **连接池**: 使用连接池减少连接开销
4. **监控告警**: 设置性能阈值告警

### 运维建议

1. **定期检查**: 每日运行测试脚本
2. **日志分析**: 定期分析错误日志
3. **版本更新**: 关注Redis版本更新
4. **备份恢复**: 制定备份恢复计划

---

## 📞 技术支持

**问题反馈**: 通过系统监控页面查看详细状态  
**紧急联系**: 参考故障排除指南自行解决  
**文档更新**: 如有改进建议请记录在案  

**最后更新**: 2024年12月27日  
**下次审查**: 2025年1月27日
