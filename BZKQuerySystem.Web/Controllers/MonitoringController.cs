using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BZKQuerySystem.Services;
using BZKQuerySystem.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;

namespace BZKQuerySystem.Web.Controllers
{
    /// <summary>
    /// 系统监控控制器 - 第一部分
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ViewPerformanceMetrics")]
    public class MonitoringController : ControllerBase
    {
        private readonly IAdvancedMonitoringService _monitoringService;
        private readonly IConcurrencyManagerService _concurrencyManager;
        private readonly ILogger<MonitoringController> _logger;

        public MonitoringController(
            IAdvancedMonitoringService monitoringService,
            IConcurrencyManagerService concurrencyManager,
            ILogger<MonitoringController> logger)
        {
            _monitoringService = monitoringService;
            _concurrencyManager = concurrencyManager;
            _logger = logger;
        }

        /// <summary>
        /// 获取系统健康状态概览
        /// </summary>
        [HttpGet("health-summary")]
        public async Task<IActionResult> GetHealthSummary()
        {
            try
            {
                var summary = await _monitoringService.GetSystemHealthSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统健康状态概览时发生错误");
                return StatusCode(500, new { error = "获取系统健康状态失败" });
            }
        }

        /// <summary>
        /// 获取当前系统监控指标
        /// </summary>
        [HttpGet("current-metrics")]
        public async Task<IActionResult> GetCurrentMetrics()
        {
            try
            {
                var metrics = await _monitoringService.GetCurrentMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前监控指标时发生错误");
                return StatusCode(500, new { error = "获取监控指标失败" });
            }
        }

        /// <summary>
        /// 获取历史监控数据
        /// </summary>
        [HttpGet("metrics-history")]
        public async Task<IActionResult> GetMetricsHistory(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var start = startTime ?? DateTime.UtcNow.AddHours(-1);
                var end = endTime ?? DateTime.UtcNow;

                var history = await _monitoringService.GetMetricsHistoryAsync(start, end);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取历史监控数据时发生错误");
                return StatusCode(500, new { error = "获取历史数据失败" });
            }
        }

        /// <summary>
        /// 获取活跃告警
        /// </summary>
        [HttpGet("active-alerts")]
        public async Task<IActionResult> GetActiveAlerts()
        {
            try
            {
                var alerts = await _monitoringService.GetActiveAlertsAsync();
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活跃告警时发生错误");
                return StatusCode(500, new { error = "获取告警信息失败" });
            }
        }

        /// <summary>
        /// 获取告警历史
        /// </summary>
        [HttpGet("alert-history")]
        public async Task<IActionResult> GetAlertHistory(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var start = startTime ?? DateTime.UtcNow.AddDays(-1);
                var end = endTime ?? DateTime.UtcNow;

                var history = await _monitoringService.GetAlertHistoryAsync(start, end);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取告警历史时发生错误");
                return StatusCode(500, new { error = "获取告警历史失败" });
            }
        }

        /// <summary>
        /// 获取并发统计信息
        /// </summary>
        [HttpGet("concurrency-stats")]
        public async Task<IActionResult> GetConcurrencyStats()
        {
            try
            {
                var stats = await _concurrencyManager.GetConcurrencyStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取并发统计时发生错误");
                return StatusCode(500, new { error = "获取并发统计失败" });
            }
        }

        /// <summary>
        /// 触发系统性能优化
        /// </summary>
        [HttpPost("optimize")]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> OptimizePerformance()
        {
            try
            {
                await _concurrencyManager.OptimizePerformanceAsync();
                _logger.LogInformation("手动触发系统性能优化");
                return Ok(new { message = "性能优化已完成" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行性能优化时发生错误");
                return StatusCode(500, new { error = "性能优化失败" });
            }
        }

        /// <summary>
        /// 检查系统健康状态
        /// </summary>
        [HttpGet("health-check")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var isHealthy = await _concurrencyManager.IsSystemHealthyAsync();
                var currentMetrics = await _monitoringService.GetCurrentMetricsAsync();

                return Ok(new
                {
                    isHealthy = isHealthy,
                    status = currentMetrics.HealthStatus.ToString(),
                    timestamp = DateTime.UtcNow,
                    details = new
                    {
                        cpuUsage = currentMetrics.CpuUsagePercent,
                        memoryUsage = currentMetrics.MemoryUsageMB,
                        activeQueries = currentMetrics.ActiveQueries,
                        cacheHitRatio = currentMetrics.CacheHitRatio
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "健康检查时发生错误");
                return StatusCode(500, new { error = "健康检查失败", isHealthy = false });
            }
        }

        /// <summary>
        /// 获取监控仪表板数据
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                // 使用更短的超时控制，快速响应
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                // 并行获取数据，但各自有独立的超时控制
                var currentMetricsTask = GetCurrentMetricsWithTimeout();
                var concurrencyStatsTask = GetConcurrencyStatsWithTimeout();
                var activeAlertsTask = GetActiveAlertsWithTimeout();
                var redisStatusTask = CheckRedisConnectionAsync();

                // 等待所有任务完成
                await Task.WhenAll(currentMetricsTask, concurrencyStatsTask, activeAlertsTask, redisStatusTask);

                var currentMetrics = await currentMetricsTask;
                var concurrencyStats = await concurrencyStatsTask;
                var activeAlerts = await activeAlertsTask;
                var redisWorking = await redisStatusTask;

                // 返回前端期望的格式
                var dashboard = new
                {
                    timestamp = DateTime.UtcNow,
                    healthStatus = currentMetrics.HealthStatus.ToString(),
                    performanceMetrics = new
                    {
                        databaseResponseTime = Math.Round(currentMetrics.ResponseTimeMs, 1),
                        memoryUsageMB = currentMetrics.MemoryUsageMB,
                        userCount = currentMetrics.ActiveConnections,
                        maxConcurrentQueries = concurrencyStats?.TotalConnections ?? 200,
                        cpuUsagePercent = Math.Round(currentMetrics.CpuUsagePercent, 1),
                        activeQueries = currentMetrics.ActiveQueries,
                        cacheHitRatio = Math.Round(currentMetrics.CacheHitRatio * 100, 1),
                        threadCount = currentMetrics.ThreadCount,
                        handleCount = currentMetrics.HandleCount
                    },
                    cacheStatus = new
                    {
                        redisEnabled = true, // 从配置中读取
                        redisActuallyWorking = redisWorking,
                        memoryCacheEnabled = true,
                        cacheHitRatio = currentMetrics.CacheHitRatio > 0 ? Math.Round(currentMetrics.CacheHitRatio * 100, 1) : 0.0,
                        cacheSizeMB = currentMetrics.CacheSizeMB,
                        defaultExpirationMinutes = 30,
                        queryResultExpirationMinutes = 15,
                        tableSchemaExpirationMinutes = 60
                    },
                    healthChecks = new[]
                    {
                        new
                        {
                            name = "数据库连接",
                            status = currentMetrics.HealthStatus.ToString(),
                            responseTime = $"{currentMetrics.ResponseTimeMs:F1}ms",
                            description = "SQL Server数据库连接状态"
                        },
                        new
                        {
                            name = "Redis缓存",
                            status = redisWorking ? "Healthy" : "Warning",
                            responseTime = redisWorking ? "< 1ms" : "超时",
                            description = redisWorking ? "Redis分布式缓存状态正常" : "Redis服务不可用或连接超时"
                        },
                        new
                        {
                            name = "系统内存",
                            status = currentMetrics.MemoryUsageMB < 2048 ? "Healthy" :
                                   currentMetrics.MemoryUsageMB < 4096 ? "Warning" : "Critical",
                            responseTime = "--",
                            description = $"当前使用 {currentMetrics.MemoryUsageMB}MB"
                        },
                        new
                        {
                            name = "CPU使用率",
                            status = currentMetrics.CpuUsagePercent < 80 ? "Healthy" :
                                   currentMetrics.CpuUsagePercent < 95 ? "Warning" : "Critical",
                            responseTime = "--",
                            description = $"当前使用 {currentMetrics.CpuUsagePercent:F1}%"
                        }
                    },
                    alerts = new
                    {
                        active = activeAlerts?.Count ?? 0,
                        critical = activeAlerts?.Count(a => a.Severity == AlertSeverity.Critical) ?? 0,
                        warning = activeAlerts?.Count(a => a.Severity == AlertSeverity.Warning) ?? 0,
                        recent = activeAlerts?.Take(5).Select(a => new
                        {
                            id = a.Id,
                            severity = a.Severity.ToString(),
                            component = a.Component,
                            message = a.Message,
                            timestamp = a.Timestamp
                        }).ToArray() ?? Array.Empty<object>()
                    }
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取监控仪表板数据时发生错误");

                // 返回基本的错误响应，而不是完全失败
                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    healthStatus = "Critical",
                    error = "数据获取失败",
                    performanceMetrics = new
                    {
                        databaseResponseTime = 0.0,
                        memoryUsageMB = 0L,
                        userCount = 0,
                        maxConcurrentQueries = 200,
                        cpuUsagePercent = 0.0,
                        activeQueries = 0,
                        cacheHitRatio = 0.0,
                        threadCount = 0,
                        handleCount = 0
                    },
                    cacheStatus = new
                    {
                        redisEnabled = true,
                        redisActuallyWorking = false,
                        memoryCacheEnabled = true,
                        cacheHitRatio = 0.0,
                        cacheSizeMB = 0L,
                        defaultExpirationMinutes = 30,
                        queryResultExpirationMinutes = 15,
                        tableSchemaExpirationMinutes = 60
                    },
                    healthChecks = new[]
                    {
                        new
                        {
                            name = "系统监控",
                            status = "Critical",
                            responseTime = "--",
                            description = "监控服务发生异常"
                        }
                    },
                    alerts = new
                    {
                        active = 0,
                        critical = 0,
                        warning = 0,
                        recent = Array.Empty<object>()
                    }
                });
            }
        }

        // 辅助方法：带超时的获取当前指标
        private async Task<SystemMetrics> GetCurrentMetricsWithTimeout()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var task = Task.Run(async () => await _monitoringService.GetCurrentMetricsAsync(), cts.Token);
                return await task;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("获取当前指标超时");
                return new SystemMetrics { HealthStatus = SystemHealthStatus.Warning };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取当前指标失败");
                return new SystemMetrics { HealthStatus = SystemHealthStatus.Critical };
            }
        }

        // 辅助方法：带超时的获取并发统计
        private async Task<ConcurrencyStats> GetConcurrencyStatsWithTimeout()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var task = Task.Run(async () => await _concurrencyManager.GetConcurrencyStatsAsync(), cts.Token);
                return await task;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("获取并发统计超时");
                return new ConcurrencyStats { TotalConnections = 200, AvailableConnections = 200, ActiveQueries = 0 };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取并发统计失败");
                return new ConcurrencyStats { TotalConnections = 200, AvailableConnections = 200, ActiveQueries = 0 };
            }
        }

        // 辅助方法：带超时的获取活跃告警
        private async Task<List<PerformanceAlert>> GetActiveAlertsWithTimeout()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var task = Task.Run(async () => await _monitoringService.GetActiveAlertsAsync(), cts.Token);
                return await task;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("获取活跃告警超时");
                return new List<PerformanceAlert>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取活跃告警失败");
                return new List<PerformanceAlert>();
            }
        }

        private async Task<bool> CheckRedisConnectionAsync()
        {
            try
            {
                // 使用极短的超时时间，避免页面卡死
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

                // 获取Redis连接多路复用器
                using var scope = HttpContext.RequestServices.CreateScope();
                var connectionMultiplexer = scope.ServiceProvider.GetService<StackExchange.Redis.IConnectionMultiplexer>();

                if (connectionMultiplexer == null)
                {
                    _logger.LogWarning("Redis ConnectionMultiplexer服务未注册");
                    return false;
                }

                // 检查连接状态并执行简单测试
                var testTask = Task.Run(async () =>
                {
                    // 检查连接状态
                    if (!connectionMultiplexer.IsConnected)
                    {
                        return false;
                    }

                    try
                    {
                        // 获取数据库实例并执行ping测试
                        var database = connectionMultiplexer.GetDatabase();
                        var pingResult = await database.PingAsync();

                        // 如果ping成功，返回true
                        return pingResult.TotalMilliseconds < 5000; // 5秒超时
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查Redis连接时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 测试API连接（无权限要求）
        /// </summary>
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult TestConnection()
        {
            return Ok(new
            {
                message = "监控API连接正常",
                timestamp = DateTime.UtcNow,
                userAuthenticated = User.Identity?.IsAuthenticated == true,
                userName = User.Identity?.Name,
                userRoles = User.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList()
            });
        }
    }
}
