using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BZKQuerySystem.Services.Extensions
{
    /// <summary>
    /// 服务集合扩展方法，用于统一配置基础设施服务
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 配置增强的日志系统
        /// </summary>
        public static IServiceCollection AddEnhancedLogging(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                
                // 添加控制台日志
                builder.AddConsole();
                
                // 添加调试日志
                builder.AddDebug();
                
                // 配置日志级别
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                builder.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            });

            return services;
        }

        /// <summary>
        /// 配置性能监控服务
        /// </summary>
        public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
        {
            services.AddScoped<IPerformanceTracker, PerformanceTracker>();
            services.AddSingleton<IMetricsCollector, MetricsCollector>();
            
            return services;
        }

        /// <summary>
        /// 配置代码质量相关服务
        /// </summary>
        public static IServiceCollection AddCodeQualityServices(this IServiceCollection services)
        {
            services.AddScoped<ICodeAnalyzer, CodeAnalyzer>();
            services.AddScoped<IHealthChecker, HealthChecker>();
            
            return services;
        }
    }

    /// <summary>
    /// 性能跟踪器接口
    /// </summary>
    public interface IPerformanceTracker
    {
        void TrackOperation(string operationName, TimeSpan duration, bool success = true);
        void TrackException(string operationName, Exception exception);
        void TrackMetric(string metricName, double value);
    }

    /// <summary>
    /// 性能跟踪器实现
    /// </summary>
    public class PerformanceTracker : IPerformanceTracker
    {
        private readonly ILogger<PerformanceTracker> _logger;

        public PerformanceTracker(ILogger<PerformanceTracker> logger)
        {
            _logger = logger;
        }

        public void TrackOperation(string operationName, TimeSpan duration, bool success = true)
        {
            _logger.LogInformation("操作 {OperationName} 执行 {Status}，耗时 {Duration}ms", 
                operationName, success ? "成功" : "失败", duration.TotalMilliseconds);
        }

        public void TrackException(string operationName, Exception exception)
        {
            _logger.LogError(exception, "操作 {OperationName} 发生异常", operationName);
        }

        public void TrackMetric(string metricName, double value)
        {
            _logger.LogInformation("指标 {MetricName}: {Value}", metricName, value);
        }
    }

    /// <summary>
    /// 指标收集器接口
    /// </summary>
    public interface IMetricsCollector
    {
        void RecordCounter(string name, int value = 1, params (string key, string value)[] tags);
        void RecordGauge(string name, double value, params (string key, string value)[] tags);
        void RecordHistogram(string name, double value, params (string key, string value)[] tags);
    }

    /// <summary>
    /// 指标收集器实现
    /// </summary>
    public class MetricsCollector : IMetricsCollector
    {
        private readonly ILogger<MetricsCollector> _logger;

        public MetricsCollector(ILogger<MetricsCollector> logger)
        {
            _logger = logger;
        }

        public void RecordCounter(string name, int value = 1, params (string key, string value)[] tags)
        {
            var tagString = string.Join(", ", tags.Select(t => $"{t.key}={t.value}"));
            _logger.LogInformation("计数器 {Name}: {Value} [{Tags}]", name, value, tagString);
        }

        public void RecordGauge(string name, double value, params (string key, string value)[] tags)
        {
            var tagString = string.Join(", ", tags.Select(t => $"{t.key}={t.value}"));
            _logger.LogInformation("仪表 {Name}: {Value} [{Tags}]", name, value, tagString);
        }

        public void RecordHistogram(string name, double value, params (string key, string value)[] tags)
        {
            var tagString = string.Join(", ", tags.Select(t => $"{t.key}={t.value}"));
            _logger.LogInformation("直方图 {Name}: {Value} [{Tags}]", name, value, tagString);
        }
    }

    /// <summary>
    /// 代码分析器接口
    /// </summary>
    public interface ICodeAnalyzer
    {
        Task<CodeQualityReport> AnalyzeCodeQualityAsync();
        Task<List<CodeIssue>> FindCodeIssuesAsync();
    }

    /// <summary>
    /// 代码分析器实现
    /// </summary>
    public class CodeAnalyzer : ICodeAnalyzer
    {
        private readonly ILogger<CodeAnalyzer> _logger;

        public CodeAnalyzer(ILogger<CodeAnalyzer> logger)
        {
            _logger = logger;
        }

        public async Task<CodeQualityReport> AnalyzeCodeQualityAsync()
        {
            _logger.LogInformation("开始代码质量分析");
            
            // 模拟分析过程
            await Task.Delay(100);
            
            return new CodeQualityReport
            {
                TotalFiles = 50,
                TotalLines = 15000,
                CodeCoverage = 75,
                Issues = new List<CodeIssue>(),
                Metrics = new Dictionary<string, int>
                {
                    ["Complexity"] = 8,
                    ["Maintainability"] = 85
                }
            };
        }

        public async Task<List<CodeIssue>> FindCodeIssuesAsync()
        {
            _logger.LogInformation("开始查找代码问题");
            
            // 模拟查找过程
            await Task.Delay(100);
            
            return new List<CodeIssue>();
        }
    }

    /// <summary>
    /// 健康检查器接口
    /// </summary>
    public interface IHealthChecker
    {
        Task<HealthStatus> CheckDatabaseHealthAsync();
        Task<HealthStatus> CheckCacheHealthAsync();
        Task<HealthStatus> CheckSystemHealthAsync();
    }

    /// <summary>
    /// 健康检查器实现
    /// </summary>
    public class HealthChecker : IHealthChecker
    {
        private readonly ILogger<HealthChecker> _logger;

        public HealthChecker(ILogger<HealthChecker> logger)
        {
            _logger = logger;
        }

        public async Task<HealthStatus> CheckDatabaseHealthAsync()
        {
            _logger.LogInformation("检查数据库健康状态");
            
            // 模拟检查过程
            await Task.Delay(50);
            
            return new HealthStatus
            {
                IsHealthy = true,
                Status = "Healthy",
                Message = "数据库连接正常",
                ResponseTime = TimeSpan.FromMilliseconds(50),
                Details = new Dictionary<string, object>
                {
                    ["ConnectionCount"] = 5,
                    ["LastCheck"] = DateTime.UtcNow
                }
            };
        }

        public async Task<HealthStatus> CheckCacheHealthAsync()
        {
            _logger.LogInformation("检查缓存健康状态");
            
            // 模拟检查过程
            await Task.Delay(30);
            
            return new HealthStatus
            {
                IsHealthy = true,
                Status = "Healthy",
                Message = "缓存服务正常",
                ResponseTime = TimeSpan.FromMilliseconds(30),
                Details = new Dictionary<string, object>
                {
                    ["CacheHitRate"] = 0.85,
                    ["LastCheck"] = DateTime.UtcNow
                }
            };
        }

        public async Task<HealthStatus> CheckSystemHealthAsync()
        {
            _logger.LogInformation("检查系统健康状态");
            
            // 模拟检查过程
            await Task.Delay(20);
            
            return new HealthStatus
            {
                IsHealthy = true,
                Status = "Healthy",
                Message = "系统运行正常",
                ResponseTime = TimeSpan.FromMilliseconds(20),
                Details = new Dictionary<string, object>
                {
                    ["CpuUsage"] = 25.5,
                    ["MemoryUsage"] = 512,
                    ["LastCheck"] = DateTime.UtcNow
                }
            };
        }
    }

    /// <summary>
    /// 代码质量报告
    /// </summary>
    public class CodeQualityReport
    {
        public int TotalFiles { get; set; }
        public int TotalLines { get; set; }
        public int CodeCoverage { get; set; }
        public List<CodeIssue> Issues { get; set; } = new();
        public Dictionary<string, int> Metrics { get; set; } = new();
    }

    /// <summary>
    /// 代码问题
    /// </summary>
    public class CodeIssue
    {
        public string File { get; set; } = string.Empty;
        public int Line { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// 健康状态
    /// </summary>
    public class HealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }
} 