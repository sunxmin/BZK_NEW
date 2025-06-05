# Redis缓存服务部署配置指南

**文档版本**: v1.0  
**创建日期**: 2025年6月3日  
**适用项目**: BZK专病多维度查询系统  
**文档类型**: 部署配置指南  
**保密级别**: 内部文档

## ? 部署架构分析

### ?? **服务器部署选择建议**

#### 方案一：Redis部署在应用服务器 ????? **推荐**
```
┌─────────────────┐    ┌─────────────────┐
│   数据库服务器   │    │   应用服务器     │
│                │    │                │
│  ? SQL Server   │?───┤  ? IIS + .NET   │
│  ? 数据存储     │    │  ? Redis缓存    │
│                │    │  ? Web应用      │
└─────────────────┘    └─────────────────┘
```

**优势**:
- ? **低延迟**: 应用与缓存在同一服务器，网络延迟最小
- ? **易维护**: 应用和缓存统一管理，部署简单
- ? **成本低**: 无需额外服务器资源
- ? **性能好**: 本地访问速度最快

**适用场景**: 中小型应用，单应用服务器部署

#### 方案二：Redis部署在数据库服务器 ???
```
┌─────────────────┐    ┌─────────────────┐
│   数据库服务器   │    │   应用服务器     │
│                │    │                │
│  ? SQL Server   │    │  ? IIS + .NET   │
│  ? Redis缓存    │?───┤  ? Web应用      │
│  ? 数据层服务   │    │                │
└─────────────────┘    └─────────────────┘
```

**优势**:
- ? **数据一致性**: 缓存与数据库在同一服务器，数据同步方便
- ? **安全性**: 减少网络传输，降低数据泄露风险
- ? **资源利用**: 数据库服务器通常内存较大

**劣势**:
- ?? **网络延迟**: 应用访问缓存需跨服务器
- ? **数据库负载**: 增加数据库服务器负担

#### 方案三：Redis独立部署 ????
```
┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│ 数据库服务器 │  │ Redis服务器  │  │ 应用服务器   │
│            │  │            │  │            │
│ ? SQL Server │  │ ? Redis集群 │  │ ? IIS + .NET │
│            │  │ ? 专用缓存  │  │ ? Web应用    │
└─────────────┘  └─────────────┘  └─────────────┘
```

**优势**:
- ? **专业化**: Redis专用服务器，性能最优
- ? **可扩展**: 支持Redis集群，易于横向扩展
- ?? **负载均衡**: 不影响应用和数据库性能

**适用场景**: 大型应用，高并发需求

### ? **BZK项目推荐方案**

**推荐选择**: **方案一 - Redis部署在应用服务器**

**理由**:
1. ? BZK是查询密集型系统，缓存访问频繁
2. ? 单应用服务器架构，部署维护简单
3. ? 本地缓存访问速度最快，用户体验最佳
4. ? 成本控制，无需额外服务器资源

## ? Redis下载安装指南

### Windows版本下载

#### 官方推荐下载源
1. **Microsoft官方Redis** (推荐)
   - 下载地址: https://github.com/microsoftarchive/redis/releases
   - 版本: Redis 3.2.100 for Windows
   - 大小: ~5MB

2. **Memurai** (商业版，兼容Redis)
   - 下载地址: https://www.memurai.com/
   - 版本: Memurai for Windows
   - 特点: 官方Windows支持

3. **WSL Redis** (Linux子系统)
   - 适用于Windows 10/11
   - 安装命令: `sudo apt-get install redis-server`

### ? **推荐安装方案**

我们使用**Microsoft官方Redis 3.2.100**，这是最稳定的Windows版本。

#### 自动化安装脚本
我将为您创建自动化安装脚本，包含：
- ? 自动下载Redis
- ? 自动安装和配置
- ? 服务注册
- ? 基础配置优化
- ? 启动验证

## ?? Redis配置详解

### 基础配置文件 (redis.windows.conf)

```ini
# Redis服务器配置
bind 127.0.0.1          # 绑定本地地址（生产环境可配置为服务器IP）
port 6379               # Redis端口
timeout 0               # 客户端连接超时时间

# 内存配置
maxmemory 1gb           # 最大内存使用量
maxmemory-policy allkeys-lru  # 内存淘汰策略

# 持久化配置
save 900 1              # 900秒内至少1次变更时保存
save 300 10             # 300秒内至少10次变更时保存
save 60 10000           # 60秒内至少10000次变更时保存

# 日志配置
loglevel notice         # 日志级别
logfile "redis.log"     # 日志文件路径

# 安全配置
requirepass BZK_Redis_2025  # Redis访问密码
```

### 性能优化配置

```ini
# 网络优化
tcp-keepalive 60        # TCP连接保活时间
tcp-backlog 511         # TCP监听队列长度

# 内存优化
hash-max-ziplist-entries 512
hash-max-ziplist-value 64
list-max-ziplist-size -2
set-max-intset-entries 512

# 持久化优化
stop-writes-on-bgsave-error yes
rdbcompression yes
rdbchecksum yes
```

## ? .NET应用集成配置

### 1. NuGet包安装
在BZK项目中安装Redis客户端：

```xml
<PackageReference Include="StackExchange.Redis" Version="2.6.122" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
```

### 2. appsettings.json配置

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BZKDatabase;Trusted_Connection=true;",
    "Redis": "localhost:6379,password=BZK_Redis_2025"
  },
  "CacheSettings": {
    "DefaultExpiration": "00:30:00",  // 30分钟
    "SlidingExpiration": true,
    "QueryCacheExpiration": "00:15:00",  // 查询缓存15分钟
    "UserCacheExpiration": "01:00:00"    // 用户缓存1小时
  }
}
```

### 3. Program.cs服务注册

```csharp
// Redis缓存服务注册
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "BZKCache";
});

// 自定义缓存服务注册
builder.Services.AddScoped<ICacheService, RedisCacheService>();
```

### 4. Redis缓存服务实现

```csharp
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly CacheSettings _cacheSettings;

    public RedisCacheService(
        IDistributedCache distributedCache,
        ILogger<RedisCacheService> logger,
        IOptions<CacheSettings> cacheSettings)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _cacheSettings = cacheSettings.Value;
    }

    public async Task<T> GetAsync<T>(string key)
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key);
            if (cachedValue != null)
            {
                return JsonSerializer.Deserialize<T>(cachedValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis缓存读取失败，Key: {Key}", key);
        }
        return default(T);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                options.SetSlidingExpiration(_cacheSettings.DefaultExpiration);
            }

            var serializedValue = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(key, serializedValue, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis缓存写入失败，Key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis缓存删除失败，Key: {Key}", key);
        }
    }
}
```

## ?? 安全配置

### 1. 网络安全
```ini
# 生产环境配置
bind 192.168.1.100      # 绑定内网IP
protected-mode yes      # 启用保护模式
requirepass BZK_Redis_Strong_Password_2025!  # 强密码
```

### 2. 访问控制
```ini
# 禁用危险命令
rename-command FLUSHDB ""
rename-command FLUSHALL ""
rename-command SHUTDOWN ""
rename-command CONFIG ""
```

### 3. 防火墙配置
```powershell
# Windows防火墙规则
New-NetFirewallRule -DisplayName "Redis-BZK" -Direction Inbound -Protocol TCP -LocalPort 6379 -Action Allow -RemoteAddress 192.168.1.0/24
```

## ? 监控和维护

### 1. Redis性能监控
- **内存使用**: `INFO memory`
- **连接数**: `INFO clients`
- **命令统计**: `INFO commandstats`
- **键空间**: `INFO keyspace`

### 2. 健康检查脚本
```powershell
# Redis健康检查
function Test-RedisHealth {
    try {
        $result = redis-cli.exe ping
        if ($result -eq "PONG") {
            Write-Host "? Redis服务正常" -ForegroundColor Green
            return $true
        }
    }
    catch {
        Write-Host "? Redis服务异常: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}
```

### 3. 自动备份脚本
```powershell
# Redis数据备份
function Backup-RedisData {
    $backupPath = "D:\Redis\Backup\$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    New-Item -Path $backupPath -ItemType Directory -Force
    
    # 执行BGSAVE
    redis-cli.exe BGSAVE
    
    # 复制RDB文件
    Copy-Item "D:\Redis\dump.rdb" "$backupPath\dump.rdb"
    Write-Host "? Redis数据备份完成: $backupPath" -ForegroundColor Green
}
```

## ? 部署checklist

### 安装前检查
- [ ] 确认服务器选择（推荐：应用服务器）
- [ ] 检查端口6379是否可用
- [ ] 确认内存资源（建议预留1-2GB）
- [ ] 准备安全密码

### 安装配置
- [ ] 下载Redis安装包
- [ ] 执行自动化安装脚本
- [ ] 配置redis.conf文件
- [ ] 注册Windows服务
- [ ] 启动Redis服务

### 应用集成
- [ ] 安装.NET Redis包
- [ ] 配置连接字符串
- [ ] 实现缓存服务类
- [ ] 更新依赖注入配置
- [ ] 执行集成测试

### 验证测试
- [ ] Redis服务启动验证
- [ ] 连接性测试
- [ ] 缓存读写测试
- [ ] 性能基准测试
- [ ] 故障恢复测试

---

**下一步**: 我将为您创建自动化安装配置脚本，帮助您在当前本地环境快速部署Redis缓存服务。

**文档维护**: 本文档将根据实际部署经验持续更新优化
**技术支持**: 如有问题，请联系项目技术团队
**版本记录**: v1.0 - 初始版本，包含完整部署指南 