using BZKQuerySystem.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BZKQuerySystem.Services
{
    public interface IAuditService
    {
        Task LogUserActionAsync(string userId, string action, string details, string? ipAddress = null);
        Task LogQueryExecutionAsync(string userId, string query, int resultCount, long executionTime, string? ipAddress = null);
        Task LogDataExportAsync(string userId, string tableName, int recordCount, string format, string? ipAddress = null);
        Task LogSecurityEventAsync(string userId, string eventType, string details, string? ipAddress = null);
        Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<AuditLog>> GetSystemAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? eventType = null);
        Task CleanupOldAuditLogsAsync(int retentionDays = 90);
    }

    public class AuditService : IAuditService
    {
        private readonly BZKQueryDbContext _dbContext;
        private readonly ILogger<AuditService> _logger;

        public AuditService(BZKQueryDbContext dbContext, ILogger<AuditService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task LogUserActionAsync(string userId, string action, string details, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Details = details,
                    IpAddress = ipAddress ?? string.Empty,
                    Timestamp = DateTime.UtcNow,
                    EventType = AuditEventType.UserAction
                };

                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("用户操作记录: {UserId} - {Action}: {Details}", userId, action, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录用户操作审计日志失败");
            }
        }

        public async Task LogQueryExecutionAsync(string userId, string query, int resultCount, long executionTime, string? ipAddress = null)
        {
            try
            {
                var queryDetails = new
                {
                    Query = query.Length > 1000 ? query.Substring(0, 1000) + "..." : query,
                    ResultCount = resultCount,
                    ExecutionTimeMs = executionTime,
                    Timestamp = DateTime.UtcNow
                };

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = "ExecuteQuery",
                    Details = JsonSerializer.Serialize(queryDetails),
                    IpAddress = ipAddress ?? string.Empty,
                    Timestamp = DateTime.UtcNow,
                    EventType = AuditEventType.QueryExecution
                };

                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("查询执行记录: {UserId} - 结果数: {ResultCount}, 执行时间: {ExecutionTime}ms", 
                    userId, resultCount, executionTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录查询执行审计日志失败");
            }
        }

        public async Task LogDataExportAsync(string userId, string tableName, int recordCount, string format, string? ipAddress = null)
        {
            try
            {
                var details = new
                {
                    TableName = tableName,
                    RecordCount = recordCount,
                    Format = format,
                    Timestamp = DateTime.UtcNow
                };

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = "ExportData",
                    Details = JsonSerializer.Serialize(details),
                    IpAddress = ipAddress ?? string.Empty,
                    Timestamp = DateTime.UtcNow,
                    EventType = AuditEventType.DataExport
                };

                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("数据导出记录: {UserId} - 表: {TableName}, 记录数: {RecordCount}", 
                    userId, tableName, recordCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录数据导出审计日志失败");
            }
        }

        public async Task LogSecurityEventAsync(string userId, string eventType, string details, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = eventType,
                    Details = details,
                    IpAddress = ipAddress ?? string.Empty,
                    Timestamp = DateTime.UtcNow,
                    EventType = AuditEventType.SecurityEvent
                };

                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogWarning("安全事件记录: {UserId} - {EventType}: {Details}", userId, eventType, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录安全事件审计日志失败");
            }
        }

        public async Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _dbContext.AuditLogs.Where(a => a.UserId == userId);

                if (fromDate.HasValue)
                    query = query.Where(a => a.Timestamp >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(a => a.Timestamp <= toDate.Value);

                return await query
                    .OrderByDescending(a => a.Timestamp)
                    .Take(1000) // 限制返回数量
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户审计日志失败");
                return new List<AuditLog>();
            }
        }

        public async Task<List<AuditLog>> GetSystemAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? eventType = null)
        {
            try
            {
                var query = _dbContext.AuditLogs.AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(a => a.Timestamp >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(a => a.Timestamp <= toDate.Value);

                if (!string.IsNullOrEmpty(eventType) && Enum.TryParse<AuditEventType>(eventType, out var eventTypeEnum))
                    query = query.Where(a => a.EventType == eventTypeEnum);

                return await query
                    .OrderByDescending(a => a.Timestamp)
                    .Take(1000) // 限制返回数量
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统审计日志失败");
                return new List<AuditLog>();
            }
        }

        /// <summary>
        /// 清理旧的审计日志
        /// </summary>
        public async Task CleanupOldAuditLogsAsync(int retentionDays = 90)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var oldLogs = await _dbContext.AuditLogs
                    .Where(a => a.Timestamp < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _dbContext.AuditLogs.RemoveRange(oldLogs);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("清理了 {Count} 条旧审计日志", oldLogs.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理旧审计日志失败");
            }
        }
    }
} 