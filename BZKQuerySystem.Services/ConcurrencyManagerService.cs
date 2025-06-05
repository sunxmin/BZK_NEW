using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace BZKQuerySystem.Services
{
    public class ConcurrencySettings
    {
        public int MaxConcurrentQueries { get; set; } = 180;
        public int MaxConcurrentConnections { get; set; } = 200;
        public int ThreadPoolSize { get; set; } = 100;
        public int ConnectionPoolSize { get; set; } = 50;
        public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool EnableThrottling { get; set; } = true;
    }

    public class ConcurrencyStats
    {
        public int ActiveQueries { get; set; }
        public int QueuedQueries { get; set; }
        public int TotalConnections { get; set; }
        public int AvailableConnections { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageMB { get; set; }
    }

    public interface IConcurrencyManagerService
    {
        Task<bool> TryAcquireQuerySlotAsync(string userId, CancellationToken cancellationToken = default);
        Task ReleaseQuerySlotAsync(string userId);
        Task<ConcurrencyStats> GetConcurrencyStatsAsync();
        Task<bool> IsSystemHealthyAsync();
        Task OptimizePerformanceAsync();
    }

    public class ConcurrencyManagerService : IConcurrencyManagerService
    {
        private readonly ILogger<ConcurrencyManagerService> _logger;
        private readonly ConcurrencySettings _settings;
        private readonly SemaphoreSlim _querySemaphore;
        private readonly ConcurrentDictionary<string, DateTime> _activeQueries;
        private readonly ConcurrentDictionary<string, int> _userQueryCounts;

        public ConcurrencyManagerService(
            ILogger<ConcurrencyManagerService> logger,
            IOptions<ConcurrencySettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
            _querySemaphore = new SemaphoreSlim(_settings.MaxConcurrentQueries, _settings.MaxConcurrentQueries);
            _activeQueries = new ConcurrentDictionary<string, DateTime>();
            _userQueryCounts = new ConcurrentDictionary<string, int>();
        }

        public async Task<bool> TryAcquireQuerySlotAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 检查用户是否超过单用户限制
                var userQueryCount = _userQueryCounts.GetOrAdd(userId, 0);
                if (userQueryCount >= 5) // 单用户最多5个并发查询
                {
                    _logger.LogWarning("用户 {UserId} 超过单用户并发限制", userId);
                    return false;
                }

                // 检查系统健康状态
                if (!await IsSystemHealthyAsync())
                {
                    _logger.LogWarning("系统负载过高，拒绝新查询请求");
                    return false;
                }

                // 尝试获取查询槽位
                var acquired = await _querySemaphore.WaitAsync(0, cancellationToken);
                if (acquired)
                {
                    var queryId = Guid.NewGuid().ToString();
                    _activeQueries.TryAdd(queryId, DateTime.UtcNow);
                    _userQueryCounts.AddOrUpdate(userId, 1, (key, count) => count + 1);

                    _logger.LogInformation("用户 {UserId} 获取查询槽位，当前活跃查询: {ActiveCount}",
                        userId, _activeQueries.Count);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取查询槽位时发生错误");
                return false;
            }
        }

        public async Task ReleaseQuerySlotAsync(string userId)
        {
            try
            {
                _querySemaphore.Release();
                _userQueryCounts.AddOrUpdate(userId, 0, (key, count) => Math.Max(0, count - 1));

                _logger.LogInformation("用户 {UserId} 释放查询槽位，剩余活跃查询: {ActiveCount}",
                    userId, _activeQueries.Count);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放查询槽位时发生错误");
            }
        }

        public async Task<ConcurrencyStats> GetConcurrencyStatsAsync()
        {
            try
            {
                var stats = new ConcurrencyStats
                {
                    ActiveQueries = _settings.MaxConcurrentQueries - _querySemaphore.CurrentCount,
                    QueuedQueries = 0, // 当前实现中没有队列
                    TotalConnections = _settings.MaxConcurrentConnections,
                    AvailableConnections = _settings.MaxConcurrentConnections - _activeQueries.Count,
                    LastUpdated = DateTime.UtcNow
                };

                // 获取系统性能指标
                var process = System.Diagnostics.Process.GetCurrentProcess();
                stats.MemoryUsageMB = process.WorkingSet64 / 1024 / 1024;

                // CPU使用率需要采样计算
                stats.CpuUsagePercent = await GetCpuUsageAsync();

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取并发统计时发生错误");
                return new ConcurrencyStats();
            }
        }

        public async Task<bool> IsSystemHealthyAsync()
        {
            try
            {
                var stats = await GetConcurrencyStatsAsync();

                // 检查内存使用情况（超过4GB认为不健康）
                if (stats.MemoryUsageMB > 4096)
                {
                    return false;
                }

                // 检查CPU使用率（超过90%认为不健康）
                if (stats.CpuUsagePercent > 90)
                {
                    return false;
                }

                // 检查活跃连接数（超过95%认为不健康）
                var connectionUsagePercent = (double)stats.ActiveQueries / _settings.MaxConcurrentQueries * 100;
                if (connectionUsagePercent > 95)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查系统健康状态时发生错误");
                return false;
            }
        }

        public async Task OptimizePerformanceAsync()
        {
            try
            {
                // 清理超时的查询
                var timeout = TimeSpan.FromMinutes(10);
                var cutoffTime = DateTime.UtcNow - timeout;

                var expiredQueries = _activeQueries
                    .Where(kv => kv.Value < cutoffTime)
                    .ToList();

                foreach (var expired in expiredQueries)
                {
                    _activeQueries.TryRemove(expired.Key, out _);
                    _logger.LogWarning("清理超时查询: {QueryId}", expired.Key);
                }

                // 触发垃圾回收（在高负载时）
                var stats = await GetConcurrencyStatsAsync();
                if (stats.MemoryUsageMB > 2048)
                {
                    GC.Collect(2, GCCollectionMode.Optimized);
                    _logger.LogInformation("执行垃圾回收优化内存使用");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "性能优化时发生错误");
            }
        }

        private async Task<double> GetCpuUsageAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

                await Task.Delay(100); // 短暂延迟用于采样

                var endTime = DateTime.UtcNow;
                var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return cpuUsageTotal * 100;
            }
            catch
            {
                return 0;
            }
        }
    }
}
