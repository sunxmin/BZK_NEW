# BZK系统Redis工具使用说明

**创建日期**: 2024年12月27日  
**文档版本**: v1.0  
**适用范围**: BZK系统Redis缓存服务管理  

## 📋 工具概述

本目录包含BZK系统Redis缓存服务的关键管理工具，这些工具经过实际验证，可直接用于日常运维。

## 🛠️ 可用工具

### 1. Redis服务管理

#### **启动Redis服务.bat**

**用途**: 启动Redis服务器  
**配置**: 端口6379，密码BZK_Redis_2025  
**使用**: 双击运行，保持窗口打开  

#### **连接Redis客户端.bat**

**用途**: 连接Redis客户端进行管理  
**前置**: Redis服务必须运行  
**使用**: 双击运行，进入命令行模式  

### 2. 系统安装与测试

#### **install_redis.ps1**

**用途**: 自动化Redis安装和配置  
**支持**: Windows 10/11, Windows Server  
**使用**: 以管理员身份运行PowerShell  

```powershell
powershell -ExecutionPolicy Bypass -File install_redis.ps1
```

#### **测试Redis连接.ps1**

**用途**: 全面测试Redis与BZK系统集成  
**功能**: 端口检查、连接测试、API验证  
**使用**:

```powershell
powershell -ExecutionPolicy Bypass -File "测试Redis连接.ps1"
```

### 3. 技术文档

#### **监控服务关闭优化说明.md**

**用途**: 记录监控服务优化的技术细节  
**内容**: TaskCanceledException修复过程  
**状态**: 临时文档，已整合到正式技术文档  

## 🚀 快速启动流程

### 新环境部署

1. 运行 `install_redis.ps1` 安装Redis
2. 运行 `启动Redis服务.bat` 启动服务
3. 运行 `测试Redis连接.ps1` 验证安装
4. 启动BZK系统: `cd BZKQuerySystem.Web && dotnet run`

### 日常运维

1. 启动Redis: `启动Redis服务.bat`
2. 启动BZK: `cd BZKQuerySystem.Web && dotnet run`
3. 连接管理: `连接Redis客户端.bat`
4. 状态检查: `测试Redis连接.ps1`

## 📚 详细文档

完整的技术文档和故障排除指南请参考：

- `项目文档/02-技术文档/20241227_003_Redis连接优化与监控服务修复报告.md`
- `项目文档/04-运维支持文档/Redis服务管理工具集.md`

## ⚠️ 重要说明

1. **Redis服务窗口**: 启动Redis后请保持窗口打开
2. **密码配置**: 系统使用密码 `BZK_Redis_2025`
3. **端口占用**: Redis使用端口6379，确保无冲突
4. **管理员权限**: 安装脚本需要管理员权限
5. **防火墙**: 确保端口6379未被防火墙阻止

---

**技术支持**: 参考项目文档中的故障排除指南  
**更新记录**: 与项目版本同步更新
