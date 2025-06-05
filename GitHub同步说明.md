# BZK病种库查询系统 - GitHub同步说明

## 仓库信息

- **GitHub仓库**: <https://github.com/sunxmin/BZK_NEW>
- **用户名**: sunxmin
- **邮箱**: <zick.sun@qq.com>

## 同步状态

✅ **本地仓库已初始化完成**
✅ **部署文件已添加并提交**
🔄 **等待网络连接稳定后推送到GitHub**

## 新增的部署文件

本次部署过程中新增了以下重要文件：

### 部署脚本

- `deploy-to-iis.bat` - IIS部署批处理脚本
- `deploy-iis-simple.ps1` - 简化版PowerShell部署脚本
- `fix-webconfig.ps1` - web.config修复脚本
- `restore-webconfig.ps1` - web.config恢复脚本
- `push-to-github.ps1` - GitHub推送脚本

### 配置文件

- `web.config` - IIS部署配置文件
- `appsettings.Production.json` - 生产环境配置
- `.gitignore` - Git忽略文件配置

### 文档

- `IIS部署指南.md` - 完整的IIS部署指南
- `GitHub同步说明.md` - 本文档

### 发布文件

- `publish/` - 项目发布输出文件夹
- `logs/` - 日志文件夹结构

## 手动同步步骤

如果自动推送脚本失败，可以手动执行以下步骤：

### 1. 检查远程仓库

```bash
git remote -v
```

### 2. 拉取远程更新（如果有的话）

```bash
git pull origin main --rebase
```

### 3. 推送本地提交

```bash
git push origin master
```

### 4. 如果需要认证

可能需要配置GitHub Personal Access Token或SSH密钥。

## 自动同步

运行以下脚本进行自动同步：

```powershell
.\push-to-github.ps1
```

## 提交记录

最新提交包含：

- IIS部署脚本和配置文件
- 生产环境配置优化
- 完整的部署文档
- Git配置和忽略规则

## 注意事项

1. 确保网络连接稳定
2. 可能需要GitHub访问令牌进行身份验证
3. 首次推送可能需要设置上游分支
4. 所有敏感信息已通过.gitignore排除

## 联系信息

如有问题，请联系：

- GitHub: sunxmin
- Email: <zick.sun@qq.com>
