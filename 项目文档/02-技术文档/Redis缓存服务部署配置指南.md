# Redis�������������ָ��

**�ĵ��汾**: v1.0  
**��������**: 2025��6��3��  
**������Ŀ**: BZKר����ά�Ȳ�ѯϵͳ  
**�ĵ�����**: ��������ָ��  
**���ܼ���**: �ڲ��ĵ�

## ? ����ܹ�����

### ?? **����������ѡ����**

#### ����һ��Redis������Ӧ�÷����� ????? **�Ƽ�**
```
��������������������������������������    ��������������������������������������
��   ���ݿ������   ��    ��   Ӧ�÷�����     ��
��                ��    ��                ��
��  ? SQL Server   ��?��������  ? IIS + .NET   ��
��  ? ���ݴ洢     ��    ��  ? Redis����    ��
��                ��    ��  ? WebӦ��      ��
��������������������������������������    ��������������������������������������
```

**����**:
- ? **���ӳ�**: Ӧ���뻺����ͬһ�������������ӳ���С
- ? **��ά��**: Ӧ�úͻ���ͳһ���������
- ? **�ɱ���**: ��������������Դ
- ? **���ܺ�**: ���ط����ٶ����

**���ó���**: ��С��Ӧ�ã���Ӧ�÷���������

#### ��������Redis���������ݿ������ ???
```
��������������������������������������    ��������������������������������������
��   ���ݿ������   ��    ��   Ӧ�÷�����     ��
��                ��    ��                ��
��  ? SQL Server   ��    ��  ? IIS + .NET   ��
��  ? Redis����    ��?��������  ? WebӦ��      ��
��  ? ���ݲ����   ��    ��                ��
��������������������������������������    ��������������������������������������
```

**����**:
- ? **����һ����**: ���������ݿ���ͬһ������������ͬ������
- ? **��ȫ��**: �������紫�䣬��������й¶����
- ? **��Դ����**: ���ݿ������ͨ���ڴ�ϴ�

**����**:
- ?? **�����ӳ�**: Ӧ�÷��ʻ�����������
- ? **���ݿ⸺��**: �������ݿ����������

#### ��������Redis�������� ????
```
������������������������������  ������������������������������  ������������������������������
�� ���ݿ������ ��  �� Redis������  ��  �� Ӧ�÷�����   ��
��            ��  ��            ��  ��            ��
�� ? SQL Server ��  �� ? Redis��Ⱥ ��  �� ? IIS + .NET ��
��            ��  �� ? ר�û���  ��  �� ? WebӦ��    ��
������������������������������  ������������������������������  ������������������������������
```

**����**:
- ? **רҵ��**: Redisר�÷���������������
- ? **����չ**: ֧��Redis��Ⱥ�����ں�����չ
- ?? **���ؾ���**: ��Ӱ��Ӧ�ú����ݿ�����

**���ó���**: ����Ӧ�ã��߲�������

### ? **BZK��Ŀ�Ƽ�����**

**�Ƽ�ѡ��**: **����һ - Redis������Ӧ�÷�����**

**����**:
1. ? BZK�ǲ�ѯ�ܼ���ϵͳ���������Ƶ��
2. ? ��Ӧ�÷������ܹ�������ά����
3. ? ���ػ�������ٶ���죬�û��������
4. ? �ɱ����ƣ���������������Դ

## ? Redis���ذ�װָ��

### Windows�汾����

#### �ٷ��Ƽ�����Դ
1. **Microsoft�ٷ�Redis** (�Ƽ�)
   - ���ص�ַ: https://github.com/microsoftarchive/redis/releases
   - �汾: Redis 3.2.100 for Windows
   - ��С: ~5MB

2. **Memurai** (��ҵ�棬����Redis)
   - ���ص�ַ: https://www.memurai.com/
   - �汾: Memurai for Windows
   - �ص�: �ٷ�Windows֧��

3. **WSL Redis** (Linux��ϵͳ)
   - ������Windows 10/11
   - ��װ����: `sudo apt-get install redis-server`

### ? **�Ƽ���װ����**

����ʹ��**Microsoft�ٷ�Redis 3.2.100**���������ȶ���Windows�汾��

#### �Զ�����װ�ű�
�ҽ�Ϊ�������Զ�����װ�ű���������
- ? �Զ�����Redis
- ? �Զ���װ������
- ? ����ע��
- ? ���������Ż�
- ? ������֤

## ?? Redis�������

### ���������ļ� (redis.windows.conf)

```ini
# Redis����������
bind 127.0.0.1          # �󶨱��ص�ַ����������������Ϊ������IP��
port 6379               # Redis�˿�
timeout 0               # �ͻ������ӳ�ʱʱ��

# �ڴ�����
maxmemory 1gb           # ����ڴ�ʹ����
maxmemory-policy allkeys-lru  # �ڴ���̭����

# �־û�����
save 900 1              # 900��������1�α��ʱ����
save 300 10             # 300��������10�α��ʱ����
save 60 10000           # 60��������10000�α��ʱ����

# ��־����
loglevel notice         # ��־����
logfile "redis.log"     # ��־�ļ�·��

# ��ȫ����
requirepass BZK_Redis_2025  # Redis��������
```

### �����Ż�����

```ini
# �����Ż�
tcp-keepalive 60        # TCP���ӱ���ʱ��
tcp-backlog 511         # TCP�������г���

# �ڴ��Ż�
hash-max-ziplist-entries 512
hash-max-ziplist-value 64
list-max-ziplist-size -2
set-max-intset-entries 512

# �־û��Ż�
stop-writes-on-bgsave-error yes
rdbcompression yes
rdbchecksum yes
```

## ? .NETӦ�ü�������

### 1. NuGet����װ
��BZK��Ŀ�а�װRedis�ͻ��ˣ�

```xml
<PackageReference Include="StackExchange.Redis" Version="2.6.122" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
```

### 2. appsettings.json����

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BZKDatabase;Trusted_Connection=true;",
    "Redis": "localhost:6379,password=BZK_Redis_2025"
  },
  "CacheSettings": {
    "DefaultExpiration": "00:30:00",  // 30����
    "SlidingExpiration": true,
    "QueryCacheExpiration": "00:15:00",  // ��ѯ����15����
    "UserCacheExpiration": "01:00:00"    // �û�����1Сʱ
  }
}
```

### 3. Program.cs����ע��

```csharp
// Redis�������ע��
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "BZKCache";
});

// �Զ��建�����ע��
builder.Services.AddScoped<ICacheService, RedisCacheService>();
```

### 4. Redis�������ʵ��

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
            _logger.LogError(ex, "Redis�����ȡʧ�ܣ�Key: {Key}", key);
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
            _logger.LogError(ex, "Redis����д��ʧ�ܣ�Key: {Key}", key);
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
            _logger.LogError(ex, "Redis����ɾ��ʧ�ܣ�Key: {Key}", key);
        }
    }
}
```

## ?? ��ȫ����

### 1. ���簲ȫ
```ini
# ������������
bind 192.168.1.100      # ������IP
protected-mode yes      # ���ñ���ģʽ
requirepass BZK_Redis_Strong_Password_2025!  # ǿ����
```

### 2. ���ʿ���
```ini
# ����Σ������
rename-command FLUSHDB ""
rename-command FLUSHALL ""
rename-command SHUTDOWN ""
rename-command CONFIG ""
```

### 3. ����ǽ����
```powershell
# Windows����ǽ����
New-NetFirewallRule -DisplayName "Redis-BZK" -Direction Inbound -Protocol TCP -LocalPort 6379 -Action Allow -RemoteAddress 192.168.1.0/24
```

## ? ��غ�ά��

### 1. Redis���ܼ��
- **�ڴ�ʹ��**: `INFO memory`
- **������**: `INFO clients`
- **����ͳ��**: `INFO commandstats`
- **���ռ�**: `INFO keyspace`

### 2. �������ű�
```powershell
# Redis�������
function Test-RedisHealth {
    try {
        $result = redis-cli.exe ping
        if ($result -eq "PONG") {
            Write-Host "? Redis��������" -ForegroundColor Green
            return $true
        }
    }
    catch {
        Write-Host "? Redis�����쳣: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}
```

### 3. �Զ����ݽű�
```powershell
# Redis���ݱ���
function Backup-RedisData {
    $backupPath = "D:\Redis\Backup\$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    New-Item -Path $backupPath -ItemType Directory -Force
    
    # ִ��BGSAVE
    redis-cli.exe BGSAVE
    
    # ����RDB�ļ�
    Copy-Item "D:\Redis\dump.rdb" "$backupPath\dump.rdb"
    Write-Host "? Redis���ݱ������: $backupPath" -ForegroundColor Green
}
```

## ? ����checklist

### ��װǰ���
- [ ] ȷ�Ϸ�����ѡ���Ƽ���Ӧ�÷�������
- [ ] ���˿�6379�Ƿ����
- [ ] ȷ���ڴ���Դ������Ԥ��1-2GB��
- [ ] ׼����ȫ����

### ��װ����
- [ ] ����Redis��װ��
- [ ] ִ���Զ�����װ�ű�
- [ ] ����redis.conf�ļ�
- [ ] ע��Windows����
- [ ] ����Redis����

### Ӧ�ü���
- [ ] ��װ.NET Redis��
- [ ] ���������ַ���
- [ ] ʵ�ֻ��������
- [ ] ��������ע������
- [ ] ִ�м��ɲ���

### ��֤����
- [ ] Redis����������֤
- [ ] �����Բ���
- [ ] �����д����
- [ ] ���ܻ�׼����
- [ ] ���ϻָ�����

---

**��һ��**: �ҽ�Ϊ�������Զ�����װ���ýű����������ڵ�ǰ���ػ������ٲ���Redis�������

**�ĵ�ά��**: ���ĵ�������ʵ�ʲ�������������Ż�
**����֧��**: �������⣬����ϵ��Ŀ�����Ŷ�
**�汾��¼**: v1.0 - ��ʼ�汾��������������ָ�� 