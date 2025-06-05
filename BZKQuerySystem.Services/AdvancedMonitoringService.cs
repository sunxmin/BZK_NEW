using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace BZKQuerySystem.Services
{
    public class MonitoringSettings
    {
        public int SamplingIntervalSeconds { get; set; } = 30;
        public int HistoryRetentionMinutes { get; set; } = 1440; // 24小时
        public double CpuWarningThreshold { get; set; } = 80.0;
        public double CpuCriticalThreshold { get; set; } = 95.0;
        public long MemoryWarningThresholdMB { get; set; } = 2048;
        public long MemoryCriticalThresholdMB { get; set; } = 4096;
        public bool EnableRealTimeAlerts { get; set; } = true;
    }

    public class SystemMetrics
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageMB { get; set; }
        public long TotalMemoryMB { get; set; }
        public int ActiveConnections { get; set; }
        public int ActiveQueries { get; set; }
        public double CacheHitRatio { get; set; }
        public long CacheSizeMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public long DiskReadKBps { get; set; }
        public long DiskWriteKBps { get; set; }
        public double ResponseTimeMs { get; set; }
        public int ErrorCount { get; set; }
        public SystemHealthStatus HealthStatus { get; set; }
    }

    public enum SystemHealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Down
    }

    public class PerformanceAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public AlertSeverity Severity { get; set; }
        public string Component { get; set; } = "";
        public string Message { get; set; } = "";
        public Dictionary<string, object> MetricValues { get; set; } = new();
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical
    }

    public interface IAdvancedMonitoringService
    {
        Task<SystemMetrics> GetCurrentMetricsAsync();
        Task<List<SystemMetrics>> GetMetricsHistoryAsync(DateTime startTime, DateTime endTime);
        Task<List<PerformanceAlert>> GetActiveAlertsAsync();
        Task<List<PerformanceAlert>> GetAlertHistoryAsync(DateTime startTime, DateTime endTime);
        Task<Dictionary<string, object>> GetSystemHealthSummaryAsync();
        event EventHandler<PerformanceAlert> AlertGenerated;
    }

    public class AdvancedMonitoringService : BackgroundService, IAdvancedMonitoringService
    {
        private readonly ILogger<AdvancedMonitoringService> _logger;
        private readonly MonitoringSettings _settings;
        private readonly IConcurrencyManagerService _concurrencyManager;
        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentQueue<SystemMetrics> _metricsHistory = new();
        private readonly ConcurrentDictionary<string, PerformanceAlert> _activeAlerts = new();
        private readonly ConcurrentQueue<PerformanceAlert> _alertHistory = new();

        private readonly PerformanceCounter? _cpuCounter;
        private readonly PerformanceCounter? _diskReadCounter;
        private readonly PerformanceCounter? _diskWriteCounter;
        private readonly Process _currentProcess;

        public event EventHandler<PerformanceAlert>? AlertGenerated;

        public AdvancedMonitoringService(
            ILogger<AdvancedMonitoringService> logger,
            IOptions<MonitoringSettings> settings,
            IConcurrencyManagerService concurrencyManager,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _settings = settings.Value;
            _concurrencyManager = concurrencyManager;
            _serviceProvider = serviceProvider;
            _currentProcess = Process.GetCurrentProcess();

            // 初始化性能计数器
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
                _diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "无法初始化性能计数器，将使用替代方法");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("高级监控服务已启动");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CollectMetricsAsync();
                    await CleanupOldDataAsync();

                    await Task.Delay(TimeSpan.FromSeconds(_settings.SamplingIntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // 应用程序正在关闭，这是正常情况，不需要记录错误
                    _logger.LogInformation("监控服务收到停止信号，正在关闭...");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "监控服务执行时发生错误");

                    // 在错误情况下等待，但要检查是否被取消
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("监控服务在错误等待期间收到停止信号");
                        break;
                    }
                }
            }

            _logger.LogInformation("高级监控服务已停止");
        }

        public async Task<SystemMetrics> GetCurrentMetricsAsync()
        {
            try
            {
                var metrics = new SystemMetrics();

                // CPU使用率
                metrics.CpuUsagePercent = await GetCpuUsageAsync();

                // 内存使用情况
                metrics.MemoryUsageMB = _currentProcess.WorkingSet64 / 1024 / 1024;
                metrics.TotalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024;

                // 并发统计
                var concurrencyStats = await _concurrencyManager.GetConcurrencyStatsAsync();
                metrics.ActiveQueries = concurrencyStats.ActiveQueries;
                metrics.ActiveConnections = concurrencyStats.TotalConnections - concurrencyStats.AvailableConnections;

                // 缓存统计 - 添加超时处理
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    using var scope = _serviceProvider.CreateScope();
                    var cacheService = scope.ServiceProvider.GetService<ICacheService>();
                    if (cacheService != null)
                    {
                        var cacheStatsTask = Task.Run(async () =>
                        {
                            return await cacheService.GetCacheStatisticsAsync();
                        }, timeoutCts.Token);

                        var cacheStats = await cacheStatsTask;
                        metrics.CacheHitRatio = cacheStats.HitRatio;
                        metrics.CacheSizeMB = cacheStats.MemoryUsage / 1024 / 1024;
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("获取缓存统计超时，使用默认值");
                    metrics.CacheHitRatio = 0.0;
                    metrics.CacheSizeMB = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取缓存统计失败，使用默认值");
                    metrics.CacheHitRatio = 0.0;
                    metrics.CacheSizeMB = 0;
                }

                // 进程统计
                _currentProcess.Refresh();
                metrics.ThreadCount = _currentProcess.Threads.Count;
                metrics.HandleCount = _currentProcess.HandleCount;

                // 磁盘IO（如果性能计数器可用）
                if (_diskReadCounter != null && _diskWriteCounter != null)
                {
                    metrics.DiskReadKBps = (long)_diskReadCounter.NextValue() / 1024;
                    metrics.DiskWriteKBps = (long)_diskWriteCounter.NextValue() / 1024;
                }

                // 计算健康状态
                metrics.HealthStatus = CalculateHealthStatus(metrics);

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前监控指标时发生错误");
                return new SystemMetrics { HealthStatus = SystemHealthStatus.Down };
            }
        }

        public async Task<List<SystemMetrics>> GetMetricsHistoryAsync(DateTime startTime, DateTime endTime)
        {
            var result = _metricsHistory
                .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
                .OrderBy(m => m.Timestamp)
                .ToList();

            await Task.CompletedTask;
            return result;
        }

        public async Task<List<PerformanceAlert>> GetActiveAlertsAsync()
        {
            var result = _activeAlerts.Values
                .Where(a => !a.IsResolved)
                .OrderByDescending(a => a.Timestamp)
                .ToList();

            await Task.CompletedTask;
            return result;
        }

        public async Task<List<PerformanceAlert>> GetAlertHistoryAsync(DateTime startTime, DateTime endTime)
        {
            var result = _alertHistory
                .Where(a => a.Timestamp >= startTime && a.Timestamp <= endTime)
                .OrderByDescending(a => a.Timestamp)
                .ToList();

            await Task.CompletedTask;
            return result;
        }

        public async Task<Dictionary<string, object>> GetSystemHealthSummaryAsync()
        {
            var currentMetrics = await GetCurrentMetricsAsync();
            var activeAlerts = await GetActiveAlertsAsync();

            return new Dictionary<string, object>
            {
                ["overallHealth"] = currentMetrics.HealthStatus.ToString(),
                ["cpuUsage"] = Math.Round(currentMetrics.CpuUsagePercent, 1),
                ["memoryUsage"] = currentMetrics.MemoryUsageMB,
                ["activeQueries"] = currentMetrics.ActiveQueries,
                ["cacheHitRatio"] = Math.Round(currentMetrics.CacheHitRatio * 100, 1),
                ["activeAlerts"] = activeAlerts.Count,
                ["criticalAlerts"] = activeAlerts.Count(a => a.Severity == AlertSeverity.Critical),
                ["warningAlerts"] = activeAlerts.Count(a => a.Severity == AlertSeverity.Warning),
                ["lastUpdated"] = currentMetrics.Timestamp
            };
        }

        private async Task CollectMetricsAsync()
        {
            var metrics = await GetCurrentMetricsAsync();
            _metricsHistory.Enqueue(metrics);

            // 检查是否需要生成告警
            await CheckAlertsAsync(metrics);

            _logger.LogDebug("收集监控指标: CPU={CpuUsage:F1}%, 内存={MemoryUsage}MB, 活跃查询={ActiveQueries}",
                metrics.CpuUsagePercent, metrics.MemoryUsageMB, metrics.ActiveQueries);
        }

        private async Task CheckAlertsAsync(SystemMetrics metrics)
        {
            var alerts = new List<PerformanceAlert>();

            // CPU使用率告警
            if (metrics.CpuUsagePercent >= _settings.CpuCriticalThreshold)
            {
                alerts.Add(CreateAlert(AlertSeverity.Critical, "CPU",
                    $"CPU使用率达到关键水平: {metrics.CpuUsagePercent:F1}%",
                    new { cpuUsage = metrics.CpuUsagePercent }));
            }
            else if (metrics.CpuUsagePercent >= _settings.CpuWarningThreshold)
            {
                alerts.Add(CreateAlert(AlertSeverity.Warning, "CPU",
                    $"CPU使用率较高: {metrics.CpuUsagePercent:F1}%",
                    new { cpuUsage = metrics.CpuUsagePercent }));
            }

            // 内存使用告警
            if (metrics.MemoryUsageMB >= _settings.MemoryCriticalThresholdMB)
            {
                alerts.Add(CreateAlert(AlertSeverity.Critical, "Memory",
                    $"内存使用达到关键水平: {metrics.MemoryUsageMB}MB",
                    new { memoryUsage = metrics.MemoryUsageMB }));
            }
            else if (metrics.MemoryUsageMB >= _settings.MemoryWarningThresholdMB)
            {
                alerts.Add(CreateAlert(AlertSeverity.Warning, "Memory",
                    $"内存使用较高: {metrics.MemoryUsageMB}MB",
                    new { memoryUsage = metrics.MemoryUsageMB }));
            }

            // 处理新告警
            foreach (var alert in alerts)
            {
                var alertKey = $"{alert.Component}_{alert.Severity}";
                if (!_activeAlerts.ContainsKey(alertKey))
                {
                    _activeAlerts[alertKey] = alert;
                    _alertHistory.Enqueue(alert);

                    AlertGenerated?.Invoke(this, alert);

                    if (_settings.EnableRealTimeAlerts)
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var notificationService = scope.ServiceProvider.GetService<IRealTimeNotificationService>();
                            if (notificationService != null)
                            {
                                await notificationService.SendSystemNotificationAsync("system", new SystemNotification
                                {
                                    Title = $"{alert.Severity} 告警",
                                    Message = alert.Message,
                                    Type = alert.Severity == AlertSeverity.Critical ? NotificationType.Error : NotificationType.Warning
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "发送实时告警通知失败");
                        }
                    }

                    _logger.LogWarning("生成系统告警: {Severity} - {Component} - {Message}",
                        alert.Severity, alert.Component, alert.Message);
                }
            }

            // 检查告警恢复
            var recoveredAlerts = new List<string>();
            foreach (var activeAlert in _activeAlerts)
            {
                var shouldResolve = false;

                if (activeAlert.Value.Component == "CPU" && metrics.CpuUsagePercent < _settings.CpuWarningThreshold)
                    shouldResolve = true;
                else if (activeAlert.Value.Component == "Memory" && metrics.MemoryUsageMB < _settings.MemoryWarningThresholdMB)
                    shouldResolve = true;

                if (shouldResolve)
                {
                    activeAlert.Value.IsResolved = true;
                    activeAlert.Value.ResolvedAt = DateTime.UtcNow;
                    recoveredAlerts.Add(activeAlert.Key);

                    _logger.LogInformation("告警已恢复: {Component} - {Message}",
                        activeAlert.Value.Component, activeAlert.Value.Message);
                }
            }

            foreach (var key in recoveredAlerts)
            {
                _activeAlerts.TryRemove(key, out _);
            }
        }

        private PerformanceAlert CreateAlert(AlertSeverity severity, string component, string message, object metricValues)
        {
            return new PerformanceAlert
            {
                Severity = severity,
                Component = component,
                Message = message,
                MetricValues = new Dictionary<string, object>(
                    metricValues.GetType().GetProperties().ToDictionary(
                        p => p.Name,
                        p => p.GetValue(metricValues) ?? new object()))
            };
        }

        private SystemHealthStatus CalculateHealthStatus(SystemMetrics metrics)
        {
            if (metrics.CpuUsagePercent >= _settings.CpuCriticalThreshold ||
                metrics.MemoryUsageMB >= _settings.MemoryCriticalThresholdMB)
            {
                return SystemHealthStatus.Critical;
            }

            if (metrics.CpuUsagePercent >= _settings.CpuWarningThreshold ||
                metrics.MemoryUsageMB >= _settings.MemoryWarningThresholdMB)
            {
                return SystemHealthStatus.Warning;
            }

            return SystemHealthStatus.Healthy;
        }

        private async Task<double> GetCpuUsageAsync()
        {
            try
            {
                if (_cpuCounter != null)
                {
                    // 使用性能计数器
                    return _cpuCounter.NextValue();
                }

                // 替代方法：进程CPU时间采样
                var startTime = DateTime.UtcNow;
                var startCpuUsage = _currentProcess.TotalProcessorTime;

                await Task.Delay(100);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = _currentProcess.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return cpuUsageTotal * 100;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "获取CPU使用率时发生错误，返回默认值");
                return 0;
            }
        }

        private async Task CleanupOldDataAsync()
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-_settings.HistoryRetentionMinutes);

            // 清理过期的监控数据
            while (_metricsHistory.TryPeek(out var metrics) && metrics.Timestamp < cutoffTime)
            {
                _metricsHistory.TryDequeue(out _);
            }

            // 清理过期的告警历史
            while (_alertHistory.TryPeek(out var alert) && alert.Timestamp < cutoffTime)
            {
                _alertHistory.TryDequeue(out _);
            }

            await Task.CompletedTask;
        }

        public override void Dispose()
        {
            _cpuCounter?.Dispose();
            _diskReadCounter?.Dispose();
            _diskWriteCounter?.Dispose();
            _currentProcess?.Dispose();
            base.Dispose();
        }
    }
}
