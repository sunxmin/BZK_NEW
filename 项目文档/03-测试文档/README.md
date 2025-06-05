# ? BZK查询系统 - 测试文档使用指南

**版本**: v1.0  
**创建日期**: 2025年1月3日  
**最后更新**: 2025年1月3日  

## ? 文档概述

本目录包含BZK查询系统的完整测试套件，包括性能测试、功能测试、安全测试等全方位的测试解决方案。

## ? 文件结构

```
03-测试文档/
├── README.md                          # 本文件 - 使用指南
├── Apache_JMeter_安装指南.md           # JMeter详细安装指南
├── 安装JMeter.ps1                     # JMeter自动安装脚本
├── JMeter性能测试脚本配置指南.md        # JMeter配置和使用说明
├── BZK性能测试.jmx                    # JMeter测试脚本
├── users.csv                          # 测试用户数据
├── 功能自动化测试代码.md                # 自动化测试代码和框架
├── 详细测试用例文档.md                 # 完整的测试用例集合
├── 安全测试检查清单.md                 # 安全测试清单和方法
└── RunTests.ps1                       # 测试执行脚本
```

## ? 快速开始

### 1. 环境准备

#### 必需工具
```bash
# .NET SDK 8.0+
dotnet --version

# Java 8+ (JMeter需要)
java -version
```

#### Apache JMeter安装 (性能测试必需)

**方式一：一键自动安装（推荐）**
```powershell
# 以管理员身份运行PowerShell
.\安装JMeter.ps1

# 自定义安装路径
.\安装JMeter.ps1 -InstallPath "E:\tools"

# 不设置中文界面
.\安装JMeter.ps1 -SetChineseLanguage:$false
```

**方式二：手动安装**
1. 访问 [Apache JMeter官网](https://jmeter.apache.org/download_jmeter.cgi)
2. 下载 `apache-jmeter-5.6.3.zip` (Windows) 或 `apache-jmeter-5.6.3.tgz` (Linux/macOS)
3. 解压到合适目录（如：`D:\tools\apache-jmeter-5.6.3`）
4. 设置环境变量：
   - `JMETER_HOME` = `D:\tools\apache-jmeter-5.6.3`
   - 添加 `%JMETER_HOME%\bin` 到 `PATH`
5. 可选：设置中文界面（修改 `bin\jmeter.properties` 中的 `language=zh_CN`）

**详细安装指南**: ? [Apache_JMeter_安装指南.md](Apache_JMeter_安装指南.md)

#### 可选工具
```bash
# Chrome浏览器 (UI测试需要)
# OWASP ZAP (安全测试需要)
# Visual Studio 2022 (开发和调试)
```

### 2. 验证安装

```powershell
# 验证JMeter安装
jmeter -version

# 测试脚本验证
.\RunTests.ps1 -TestType "Unit"
```

### 3. 执行测试

#### 方式一：使用PowerShell脚本（推荐）
```powershell
# 执行所有测试
.\RunTests.ps1

# 执行特定类型的测试
.\RunTests.ps1 -TestType JMeter
.\RunTests.ps1 -TestType Unit
.\RunTests.ps1 -TestType Integration
.\RunTests.ps1 -TestType UI
.\RunTests.ps1 -TestType Security

# 指定不同的基础URL
.\RunTests.ps1 -BaseUrl "https://test.example.com"

# 详细输出模式
.\RunTests.ps1 -VerboseOutput

# 跳过报告生成
.\RunTests.ps1 -GenerateReport:$false
```

#### 方式二：手动执行各类测试

**JMeter性能测试**:
```bash
jmeter -n -t BZK性能测试.jmx -l results.jtl -e -o html-report
```

**单元测试**:
```bash
dotnet test --filter "Category=Unit" --logger "trx"
```

**集成测试**:
```bash
dotnet test --filter "Category=Integration" --logger "trx"
```

## ? 测试类型说明

### 1. ? JMeter性能测试
- **目标**: 验证系统性能指标
- **测试内容**: 并发用户、响应时间、吞吐量
- **文档**: `JMeter性能测试脚本配置指南.md`
- **脚本**: `BZK性能测试.jmx`

### 2. ? 功能自动化测试
- **目标**: 验证系统功能正确性
- **测试内容**: 单元测试、集成测试、UI测试
- **文档**: `功能自动化测试代码.md`
- **框架**: xUnit + Selenium + MSTest

### 3. ? 手动测试用例
- **目标**: 系统化的手动测试验证
- **测试内容**: 登录、查询、导出、权限等
- **文档**: `详细测试用例文档.md`
- **用例数**: 50+ 个详细测试场景

### 4. ?? 安全测试
- **目标**: 验证系统安全性
- **测试内容**: OWASP Top 10、SQL注入、XSS等
- **文档**: `安全测试检查清单.md`
- **工具**: OWASP ZAP、手动测试

## ? 测试报告

### 自动生成报告
执行测试脚本后会自动生成HTML格式的综合测试报告：
- **位置**: `TestResults/TestReport.html`
- **内容**: 测试统计、结果详情、链接到各类测试报告

### 各类专项报告
- **JMeter报告**: `TestResults/JMeter/html-report/index.html`
- **单元测试**: `TestResults/Unit/`
- **集成测试**: `TestResults/Integration/`
- **UI测试**: `TestResults/UI/`
- **安全测试**: `TestResults/Security/`

## ? 配置说明

### 测试数据配置
```csv
# users.csv - 测试用户数据
username,password
testuser01,Password123!
testuser02,Password123!
...
```

### 环境变量
```bash
# 系统基础URL
BASE_URL=http://localhost:5000

# 浏览器设置（UI测试）
BROWSER=Chrome
HEADLESS=true

# 数据库连接（集成测试）
TEST_CONNECTION_STRING=Server=...
```

## ? 测试策略

### 测试金字塔
```
    ? UI测试 (少量)
   -------- 端到端功能验证
  ?? 集成测试 (适量)
 ------------ API和服务集成
??? 单元测试 (大量)
-------------- 业务逻辑验证
```

### 测试分层
1. **单元测试**: 覆盖业务逻辑、数据访问、服务层
2. **集成测试**: 验证API接口、数据库交互、外部服务
3. **UI测试**: 关键用户流程的端到端验证
4. **性能测试**: 并发用户、响应时间、系统稳定性
5. **安全测试**: 漏洞扫描、渗透测试、配置审查

## ? 测试检查清单

### 功能测试检查
- [ ] 用户登录和认证
- [ ] 查询构建器功能
- [ ] 数据导出功能
- [ ] 权限控制机制
- [ ] 错误处理和恢复

### 性能测试检查
- [ ] 并发用户支持 (目标: 120用户)
- [ ] 查询响应时间 (目标: <5秒)
- [ ] 导出性能测试 (目标: 10K条<60秒)
- [ ] 系统稳定性验证
- [ ] 资源使用监控

### 安全测试检查
- [ ] SQL注入防护
- [ ] XSS攻击防护
- [ ] 权限控制验证
- [ ] 数据脱敏检查
- [ ] 会话安全性

## ? 故障排除

### 常见问题

**Q: JMeter测试脚本无法找到**
```
A: 确保JMeter已安装并添加到系统PATH
   验证命令: jmeter -v
```

**Q: 单元测试项目未找到**
```
A: 确保测试项目文件名包含'.Tests.csproj'
   检查项目结构和命名规范
```

**Q: 集成测试连接失败**
```
A: 确保目标系统正在运行
   验证URL: http://localhost:5000
   检查防火墙和网络配置
```

**Q: UI测试Chrome驱动问题**
```
A: 更新ChromeDriver到与Chrome版本匹配
   设置正确的PATH环境变量
```

### 调试建议

1. **启用详细日志**:
   ```powershell
   .\RunTests.ps1 -Verbose
   ```

2. **单独执行问题测试**:
   ```powershell
   .\RunTests.ps1 -TestType Unit
   ```

3. **检查系统状态**:
   ```bash
   curl -I http://localhost:5000/health
   ```

4. **查看详细错误**:
   ```bash
   dotnet test --logger "console;verbosity=detailed"
   ```

## ? 相关文档

### 项目文档
- [系统需求规格说明书](../02-技术文档/系统需求规格说明书.md)
- [系统架构设计文档](../02-技术文档/系统架构设计文档.md)
- [企业级项目文档索引](../企业级项目文档索引.md)

### 外部资源
- [Apache JMeter用户手册](https://jmeter.apache.org/usermanual/index.html)
- [xUnit.net文档](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Selenium WebDriver文档](https://selenium-python.readthedocs.io/)
- [OWASP测试指南](https://owasp.org/www-project-web-security-testing-guide/)

## ? 支持与反馈

### 联系方式
- **技术支持**: 开发团队
- **测试支持**: 测试工程师
- **文档维护**: 技术文档团队

### 问题报告
请在发现问题时提供以下信息：
1. 操作系统和版本
2. 测试类型和具体场景
3. 错误信息和日志
4. 重现步骤
5. 期望结果

---

**文档维护**: 测试工程师  
**创建日期**: 2025年1月3日  
**适用版本**: BZK查询系统 v2.0+  
**许可证**: 内部文档 