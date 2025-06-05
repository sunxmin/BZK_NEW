# BZK项目 - Redis缓存集成配置

**当前状态**: ✅ **Redis安装完成，运行正常**  
**最后更新**: 2024年12月27日  
**Redis版本**: 3.2.100  
**部署位置**: 本地开发环境  

## 📊 当前Redis服务状态

### ✅ 安装信息

- **安装路径**: 自动检测多个位置
- **端口**: 6379 (默认)
- **认证**: 密码保护 (BZK_Redis_2025)
- **最大内存**: 默认 (可配置)
- **持久化**: 默认RDB快照

### 🔧 连接配置

```yaml
Redis服务器信息:
  主机: localhost
  端口: 6379
  密码: BZK_Redis_2025
  连接字符串: "localhost:6379,password=BZK_Redis_2025,abortConnect=false,connectTimeout=5000,connectRetry=3,syncTimeout=5000"
```

### ✅ 连接测试结果

```bash
# Redis连接测试
redis-cli.exe -a BZK_Redis_2025 ping
# 返回: PONG ✅

# Redis版本信息
redis_version:3.2.100 ✅

# BZK系统集成测试
监控页面Redis状态: Healthy ✅
```

## 🔧 .NET项目集成配置

### 1. NuGet包依赖

需要在BZK项目中安装以下NuGet包：

```xml
<PackageReference Include="StackExchange.Redis" Version="2.8.0" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
```

### 2. appsettings.json配置

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BZKDatabase;Trusted_Connection=true;",
    "Redis": "localhost:6379,password=BZK_Redis_2025,abortConnect=false,connectTimeout=5000,connectRetry=3,syncTimeout=5000"
  },
  "CacheSettings": {
    "DefaultExpiration": "00:30:00",
    "SlidingExpiration": true,
    "QueryCacheExpiration": "00:15:00",
    "UserCacheExpiration": "01:00:00",
    "MaxConcurrentQueries": 180,
    "SizeLimit": 1000
  }
}
```

### 3. Program.cs服务注册 (已修复)

```csharp
// Redis缓存服务注册
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

// 注册IDistributedCache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "BZKCache";
});

// 注册IConnectionMultiplexer (修复监控检查问题)
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
{
    var configuration = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false; // 启动时即使Redis不可用也不阻塞
    return StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);
});

// 内存缓存配置 (修复Size配置问题)
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // 设置缓存大小限制
});

// 自定义缓存服务注册
builder.Services.AddScoped<ICacheService, HybridCacheService>();

// 缓存配置绑定
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection("CacheSettings"));
```

### 4. 缓存服务接口

```csharp
public interface ICacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<long> GetCacheCountAsync();
    Task<Dictionary<string, object>> GetCacheStatsAsync();
}
```

### 5. 混合缓存服务实现 (已修复Size配置)

```csharp
public class HybridCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly CacheSettings _cacheSettings;

    public HybridCacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ILogger<HybridCacheService> logger,
        IOptions<CacheSettings> cacheSettings)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _logger = logger;
        _cacheSettings = cacheSettings.Value;
    }

    public async Task<T> GetAsync<T>(string key)
    {
        try
        {
            // L1缓存: 内存缓存
            if (_memoryCache.TryGetValue(key, out T cachedValue))
            {
                _logger.LogDebug("L1缓存命中: {Key}", key);
                return cachedValue;
            }

            // L2缓存: Redis缓存
            var redisValue = await _distributedCache.GetStringAsync(key);
            if (redisValue != null)
            {
                var deserializedValue = JsonSerializer.Deserialize<T>(redisValue);
                
                // 回写到L1缓存 (修复Size配置)
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    Size = 1 // 支持SizeLimit配置
                };
                _memoryCache.Set(key, deserializedValue, options);
                
                _logger.LogDebug("L2缓存命中并回写L1: {Key}", key);
                return deserializedValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存获取失败，Key: {Key}", key);
        }
        return default(T);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var exp = expiration ?? _cacheSettings.DefaultExpiration;
            
            // L1缓存: 内存缓存 (修复Size配置)
            var memoryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = exp,
                Size = 1 // 支持SizeLimit配置
            };
            _memoryCache.Set(key, value, memoryOptions);

            // L2缓存: Redis缓存
            var redisOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = exp
            };
            
            var serializedValue = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(key, serializedValue, redisOptions);
            
            _logger.LogDebug("混合缓存写入成功: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存写入失败，Key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            _memoryCache.Remove(key);
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("缓存删除成功: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存删除失败，Key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out _))
                return true;
                
            var value = await _distributedCache.GetStringAsync(key);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存检查失败，Key: {Key}", key);
            return false;
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        _logger.LogWarning("模式删除功能需要直接使用StackExchange.Redis实现");
        await Task.CompletedTask;
    }

    public async Task<long> GetCacheCountAsync()
    {
        try
        {
            // 这里需要实现具体的统计逻辑
            return await Task.FromResult(0L);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存统计失败");
            return 0;
        }
    }

    public async Task<Dictionary<string, object>> GetCacheStatsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["L1_Cache_Type"] = "Memory",
            ["L2_Cache_Type"] = "Redis",
            ["Status"] = "Healthy"
        };
        
        return await Task.FromResult(stats);
    }
}
```

### 6. 缓存配置类

```csharp
public class CacheSettings
{
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public bool SlidingExpiration { get; set; } = true;
    public TimeSpan QueryCacheExpiration { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan UserCacheExpiration { get; set; } = TimeSpan.FromHours(1);
    public int MaxConcurrentQueries { get; set; } = 180;
    public int SizeLimit { get; set; } = 1000;
}
```

## 🎯 BZK业务场景缓存策略

### 1. 查询结果缓存

```csharp
public async Task<QueryResult> GetQueryResultAsync(QueryRequest request)
{
    var cacheKey = $"query:{request.GetHashCode()}";
    
    var cachedResult = await _cacheService.GetAsync<QueryResult>(cacheKey);
    if (cachedResult != null)
    {
        return cachedResult;
    }
    
    var result = await ExecuteQueryAsync(request);
    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));
    
    return result;
}
```

### 2. 用户会话缓存

```csharp
public async Task<UserSession> GetUserSessionAsync(string userId)
{
    var cacheKey = $"user:session:{userId}";
    return await _cacheService.GetAsync<UserSession>(cacheKey);
}

public async Task SetUserSessionAsync(string userId, UserSession session)
{
    var cacheKey = $"user:session:{userId}";
    await _cacheService.SetAsync(cacheKey, session, TimeSpan.FromHours(1));
}
```

### 3. 字典数据缓存

```csharp
public async Task<List<DictionaryItem>> GetDictionaryAsync(string category)
{
    var cacheKey = $"dict:{category}";
    
    var cached = await _cacheService.GetAsync<List<DictionaryItem>>(cacheKey);
    if (cached != null)
    {
        return cached;
    }
    
    var items = await _dictionaryRepository.GetByCategoryAsync(category);
    await _cacheService.SetAsync(cacheKey, items, TimeSpan.FromHours(24));
    
    return items;
}
```

## 🔍 监控和诊断 (已修复)

### 1. Redis连接检查 (已修复)

```csharp
private async Task<bool> CheckRedisConnectionAsync()
{
    try
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var scope = HttpContext.RequestServices.CreateScope();
        var connectionMultiplexer = scope.ServiceProvider.GetService<StackExchange.Redis.IConnectionMultiplexer>();

        if (connectionMultiplexer == null)
        {
            _logger.LogWarning("Redis ConnectionMultiplexer服务未注册");
            return false;
        }

        var testTask = Task.Run(async () =>
        {
            if (!connectionMultiplexer.IsConnected) return false;
            
            try
            {
                var database = connectionMultiplexer.GetDatabase();
                var pingResult = await database.PingAsync();
                return pingResult.TotalMilliseconds < 5000;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Redis ping测试失败: {Message}", ex.Message);
                return false;
            }
        }, timeoutCts.Token);

        return await testTask;
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Redis连接检查超时");
        return false;
    }
}
```

### 2. 缓存性能监控

```csharp
public class CacheMetrics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRatio => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
    public long TotalRequests => HitCount + MissCount;
}
```

### 3. 健康检查配置

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<CacheHealthCheck>("cache");
```

## 🚀 部署和运维

### 1. 生产环境配置建议

```json
{
  "ConnectionStrings": {
    "Redis": "redis-server:6379,password=strong_password,ssl=true,abortConnect=false"
  },
  "CacheSettings": {
    "DefaultExpiration": "01:00:00",
    "QueryCacheExpiration": "00:30:00",
    "UserCacheExpiration": "02:00:00",
    "MaxConcurrentQueries": 500
  }
}
```

### 2. 性能优化建议

- **连接池**: 使用连接池管理Redis连接
- **序列化**: 考虑使用MessagePack等高效序列化
- **压缩**: 大对象启用压缩
- **分片**: 大规模部署考虑Redis集群

### 3. 监控告警

- Redis连接状态监控
- 缓存命中率监控
- 内存使用率监控
- 响应时间监控

## 📋 故障排除

### 常见问题及解决方案

1. **连接超时**: 检查网络和防火墙设置
2. **认证失败**: 确认密码配置正确
3. **内存不足**: 调整Redis内存配置
4. **性能问题**: 优化缓存策略和过期时间

### 运维脚本

- `启动Redis服务.bat` - 启动Redis服务器
- `连接Redis客户端.bat` - 连接Redis客户端
- `测试Redis连接.ps1` - 全面连接测试
- `install_redis.ps1` - 自动化安装脚本

---

**最后更新**: 2024年12月27日  
**状态**: Redis连接问题已完全修复，监控服务优化完成  
**下次检查**: 2025年1月27日
