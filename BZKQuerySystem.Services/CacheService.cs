using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace BZKQuerySystem.Services
{
    /// <summary>
    /// ��������ģ��
    /// </summary>
    public class CacheSettings
    {
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
        public bool SlidingExpiration { get; set; } = true;
        public TimeSpan QueryCacheExpiration { get; set; } = TimeSpan.FromMinutes(15);
        public TimeSpan UserCacheExpiration { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan DictionaryCacheExpiration { get; set; } = TimeSpan.FromHours(24);
        public bool UseRedis { get; set; } = true;
        public bool UseMemoryCache { get; set; } = true;
        public string CacheKeyPrefix { get; set; } = "BZK:";
        public bool CompressLargeValues { get; set; } = true;
        public int CompressionThreshold { get; set; } = 1024;
    }

    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
        Task<bool> ExistsAsync(string key);
        string GenerateCacheKey(params object[] keyParts);
        Task<CacheStatistics> GetCacheStatisticsAsync();
    }

    /// <summary>
    /// ����ͳ����Ϣ
    /// </summary>
    public class CacheStatistics
    {
        public long TotalKeys { get; set; }
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public double HitRatio => TotalKeys > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
        public long MemoryUsage { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Redis������� - ר������Redis�ĸ�����ʵ��
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly CacheSettings _cacheSettings;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(
            IDistributedCache distributedCache,
            ILogger<RedisCacheService> logger,
            IOptions<CacheSettings> cacheSettings)
        {
            _distributedCache = distributedCache;
            _logger = logger;
            _cacheSettings = cacheSettings.Value;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T> GetAsync<T>(string key)
        {
            try
            {
                var prefixedKey = $"{_cacheSettings.CacheKeyPrefix}{key}";
                var cachedBytes = await _distributedCache.GetAsync(prefixedKey);

                if (cachedBytes != null && cachedBytes.Length > 0)
                {
                    _logger.LogDebug("Redis��������: {Key}", key);
                    var cachedValue = Encoding.UTF8.GetString(cachedBytes);
                    return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
                }

                _logger.LogDebug("Redis����δ����: {Key}", key);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis�����ȡʧ��: {Key}", key);
                return default(T);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var prefixedKey = $"{_cacheSettings.CacheKeyPrefix}{key}";
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                var serializedBytes = Encoding.UTF8.GetBytes(serializedValue);

                var options = new DistributedCacheEntryOptions();
                var effectiveExpiration = expiration ?? _cacheSettings.DefaultExpiration;

                if (_cacheSettings.SlidingExpiration)
                {
                    options.SetSlidingExpiration(effectiveExpiration);
                }
                else
                {
                    options.SetAbsoluteExpiration(effectiveExpiration);
                }

                await _distributedCache.SetAsync(prefixedKey, serializedBytes, options);
                _logger.LogDebug("Redis�������óɹ�: {Key}, ����ʱ��: {Expiration}", key, effectiveExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis��������ʧ��: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var prefixedKey = $"{_cacheSettings.CacheKeyPrefix}{key}";
                await _distributedCache.RemoveAsync(prefixedKey);
                _logger.LogDebug("Redis�����Ƴ��ɹ�: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis�����Ƴ�ʧ��: {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var prefixedKey = $"{_cacheSettings.CacheKeyPrefix}{key}";
                var value = await _distributedCache.GetAsync(prefixedKey);
                return value != null && value.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis������ʧ��: {Key}", key);
                return false;
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            // ע�⣺��׼��IDistributedCache�ӿڲ�֧��ģʽɾ��
            // �����ṩһ������ʵ�ּ�¼
            _logger.LogWarning("Redisģʽɾ����Ҫֱ��ʹ��StackExchange.Redisʵ��: {Pattern}", pattern);
            await Task.CompletedTask;
        }

        public string GenerateCacheKey(params object[] keyParts)
        {
            var combined = string.Join(":", keyParts);

            // ���ɶ̵ġ�һ�µĻ����
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            var hash = Convert.ToBase64String(hashBytes)[..8]; // ȡǰ8���ַ�

            var readablePart = combined.Length > 30 ? combined[..30] + "..." : combined;
            return $"{readablePart}_{hash}";
        }

        public async Task<CacheStatistics> GetCacheStatisticsAsync()
        {
            // ����ͳ����Ϣ������ϸ��ͳ����Ҫֱ��ʹ��StackExchange.Redis
            return new CacheStatistics
            {
                LastUpdated = DateTime.UtcNow,
                // ע�⣺�ֲ�ʽ����ӿ��޷�ֱ�ӻ�ȡͳ����Ϣ
                TotalKeys = -1, // ��ʾ������
                HitCount = -1,
                MissCount = -1,
                MemoryUsage = -1
            };
        }
    }

    public class HybridCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<HybridCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // L1���棨�ڴ棩����ʱ�����ý϶�
        private readonly TimeSpan _l1CacheExpiration = TimeSpan.FromMinutes(5);
        // L2���棨�ֲ�ʽ������ʱ�����ýϳ�
        private readonly TimeSpan _l2CacheExpiration = TimeSpan.FromMinutes(30);

        public HybridCacheService(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            ILogger<HybridCacheService> logger)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T> GetAsync<T>(string key)
        {
            try
            {
                // ���ȳ��Դ�L1���棨�ڴ棩��ȡ
                if (_memoryCache.TryGetValue(key, out T cachedValue))
                {
                    _logger.LogDebug("��L1��������: {Key}", key);
                    return cachedValue;
                }

                // Ȼ���Դ�L2���棨�ֲ�ʽ����ȡ
                var distributedBytes = await _distributedCache.GetAsync(key);
                if (distributedBytes != null && distributedBytes.Length > 0)
                {
                    _logger.LogDebug("��L2��������: {Key}", key);
                    var distributedValue = Encoding.UTF8.GetString(distributedBytes);
                    var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, _jsonOptions);

                    // 将数据放回L1缓存 - 修复Size问题
                    var l1Options = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _l1CacheExpiration,
                        Size = 1
                    };
                    _memoryCache.Set(key, deserializedValue, l1Options);

                    return deserializedValue;
                }

                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�����ȡʧ��: {Key}", key);
                return default(T);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var effectiveExpiration = expiration ?? _l2CacheExpiration;

                // 设置L1缓存（内存）- 修复Size问题
                var l1Expiration = effectiveExpiration > _l1CacheExpiration ? _l1CacheExpiration : effectiveExpiration;
                var memoryCacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = l1Expiration,
                    Size = 1 // 设置缓存条目大小
                };
                _memoryCache.Set(key, value, memoryCacheOptions);

                // 设置L2缓存（分布式）
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                var serializedBytes = Encoding.UTF8.GetBytes(serializedValue);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = effectiveExpiration
                };

                await _distributedCache.SetAsync(key, serializedBytes, options);

                _logger.LogDebug("缓存设置成功: {Key}, 过期时间: {Expiration}", key, effectiveExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "缓存设置失败: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                // ��L1�����Ƴ�
                _memoryCache.Remove(key);

                // ��L2�����Ƴ�
                await _distributedCache.RemoveAsync(key);

                _logger.LogDebug("�����Ƴ��ɹ�: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�����Ƴ�ʧ��: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                // ע�⣺����ֻ��������֪�Ļ��������Ϊ�ֲ�ʽ����ͨ����֧��ģʽƥ��
                // ��ʵ��Ӧ���У�����ά��һ�������������
                _logger.LogWarning("ģʽ�Ƴ����湦����Ҫ����ļ�����֧��: {Pattern}", pattern);

                // �����ڴ滺�棬���ǿ���ͨ�������ȡ���м������ⲻ���Ƽ�������
                // ���õķ�ʽ��ά��������ķ������
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ģʽ�Ƴ�����ʧ��: {Pattern}", pattern);
            }
        }

        public string GenerateCacheKey(params object[] keyParts)
        {
            var combined = string.Join(":", keyParts);

            // Ϊ��ȷ���������һ���ԺͰ�ȫ�ԣ��Լ����й�ϣ����
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            var hash = Convert.ToBase64String(hashBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

            // ����ԭʼ���Ŀɶ��ԣ��ضϳ��ȣ�
            var readablePart = combined.Length > 50 ? combined.Substring(0, 50) + "..." : combined;

            return $"{readablePart}:{hash}";
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                // ���ȼ��L1����
                if (_memoryCache.TryGetValue(key, out _))
                {
                    return true;
                }

                // Ȼ����L2����
                var distributedBytes = await _distributedCache.GetAsync(key);
                return distributedBytes != null && distributedBytes.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ʧ��: {Key}", key);
                return false;
            }
        }

        public async Task<CacheStatistics> GetCacheStatisticsAsync()
        {
            // ���ڻ�ϻ��棬���ػ���ͳ����Ϣ
            return new CacheStatistics
            {
                LastUpdated = DateTime.UtcNow,
                // ע�⣺��ϻ����ͳ����Ϣ����
                TotalKeys = -1, // ��ʾ������
                HitCount = -1,
                MissCount = -1,
                MemoryUsage = -1
            };
        }
    }

    /// <summary>
    /// ר�����ڲ�ѯ�������ķ���
    /// </summary>
    public class QueryCacheService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<QueryCacheService> _logger;
        private const string QUERY_CACHE_PREFIX = "query_result";
        private const string TABLE_SCHEMA_PREFIX = "table_schema";
        private const string USER_PERMISSION_PREFIX = "user_permission";

        public QueryCacheService(ICacheService cacheService, ILogger<QueryCacheService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// �����ѯ���
        /// </summary>
        public async Task<T> GetOrSetQueryResultAsync<T>(
            string sql,
            object parameters,
            string userId,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            var cacheKey = _cacheService.GenerateCacheKey(QUERY_CACHE_PREFIX, sql, parameters, userId);

            var cachedResult = await _cacheService.GetAsync<T>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("��ѯ�����������: {UserId}", userId);
                return cachedResult;
            }

            _logger.LogDebug("ִ�в�ѯ��������: {UserId}", userId);
            var result = await factory();

            // ��ѯ�������ʱ��϶̣��������ݹ���
            var cacheExpiration = expiration ?? TimeSpan.FromMinutes(5);
            await _cacheService.SetAsync(cacheKey, result, cacheExpiration);

            return result;
        }

        /// <summary>
        /// ������ṹ��Ϣ
        /// </summary>
        public async Task<T> GetOrSetTableSchemaAsync<T>(
            string tableName,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            var cacheKey = _cacheService.GenerateCacheKey(TABLE_SCHEMA_PREFIX, tableName);

            var cachedSchema = await _cacheService.GetAsync<T>(cacheKey);
            if (cachedSchema != null)
            {
                return cachedSchema;
            }

            var schema = await factory();

            // ���ṹ����ʱ��ϳ�����Ϊ�ṹ�仯��Ƶ��
            var cacheExpiration = expiration ?? TimeSpan.FromHours(2);
            await _cacheService.SetAsync(cacheKey, schema, cacheExpiration);

            return schema;
        }

        /// <summary>
        /// �����û�Ȩ����Ϣ
        /// </summary>
        public async Task<T> GetOrSetUserPermissionAsync<T>(
            string userId,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            var cacheKey = _cacheService.GenerateCacheKey(USER_PERMISSION_PREFIX, userId);

            var cachedPermission = await _cacheService.GetAsync<T>(cacheKey);
            if (cachedPermission != null)
            {
                return cachedPermission;
            }

            var permission = await factory();

            // Ȩ����Ϣ����ʱ���е�
            var cacheExpiration = expiration ?? TimeSpan.FromMinutes(15);
            await _cacheService.SetAsync(cacheKey, permission, cacheExpiration);

            return permission;
        }

        /// <summary>
        /// �����û���صĻ���
        /// </summary>
        public async Task InvalidateUserCacheAsync(string userId)
        {
            await _cacheService.RemoveByPatternAsync($"{USER_PERMISSION_PREFIX}:{userId}");
            await _cacheService.RemoveByPatternAsync($"{QUERY_CACHE_PREFIX}:*:{userId}");

            _logger.LogInformation("�����û�����: {UserId}", userId);
        }

        /// <summary>
        /// ��������صĻ���
        /// </summary>
        public async Task InvalidateTableCacheAsync(string tableName)
        {
            await _cacheService.RemoveByPatternAsync($"{TABLE_SCHEMA_PREFIX}:{tableName}");

            _logger.LogInformation("����������: {TableName}", tableName);
        }
    }

    /// <summary>
    /// �򵥵��ڴ滺�����ʵ��
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T> GetAsync<T>(string key)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out T cachedValue))
                {
                    _logger.LogDebug("��������: {Key}", key);
                    return cachedValue;
                }
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�����ȡʧ��: {Key}", key);
                return default(T);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new MemoryCacheEntryOptions();
                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                }

                // 添加Size设置以支持SizeLimit
                options.Size = 1;

                _memoryCache.Set(key, value, options);
                _logger.LogDebug("缓存设置成功: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "缓存设置失败: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _memoryCache.Remove(key);
                _logger.LogDebug("�����Ƴ��ɹ�: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�����Ƴ�ʧ��: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            // ��ʵ�֣��ڴ滺�治֧��ģʽƥ��
            _logger.LogWarning("�ڴ滺�治֧��ģʽƥ���Ƴ�: {Pattern}", pattern);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return _memoryCache.TryGetValue(key, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ʧ��: {Key}", key);
                return false;
            }
        }

        public string GenerateCacheKey(params object[] keyParts)
        {
            return string.Join(":", keyParts);
        }

        public async Task<CacheStatistics> GetCacheStatisticsAsync()
        {
            // �����ڴ滺�棬���ػ���ͳ����Ϣ
            return new CacheStatistics
            {
                LastUpdated = DateTime.UtcNow,
                // ע�⣺IMemoryCache�ӿ��޷�ֱ�ӻ�ȡͳ����Ϣ
                TotalKeys = -1, // ��ʾ������
                HitCount = -1,
                MissCount = -1,
                MemoryUsage = -1
            };
        }
    }
}
