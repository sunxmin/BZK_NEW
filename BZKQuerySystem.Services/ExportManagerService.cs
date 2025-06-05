using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BZKQuerySystem.Services
{
    public enum ExportFormat
    {
        Excel,
        PDF,
        CSV,
        Word
    }

    public enum ExportStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public class ExportTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = "";
        public string FileName { get; set; } = "";
        public ExportFormat Format { get; set; }
        public ExportStatus Status { get; set; } = ExportStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int Progress { get; set; } = 0;
        public string? ErrorMessage { get; set; }
        public string? FilePath { get; set; }
        public long? FileSizeBytes { get; set; }
        public DataTable? Data { get; set; }
        public Dictionary<string, object>? Options { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }
    }

    public class ExportHistory
    {
        public string Id { get; set; } = "";
        public string UserId { get; set; } = "";
        public string FileName { get; set; } = "";
        public ExportFormat Format { get; set; }
        public DateTime ExportedAt { get; set; }
        public bool IsSuccess { get; set; }
        public int RecordCount { get; set; }
        public long FileSizeBytes { get; set; }
        public TimeSpan Duration { get; set; }
        public string? DownloadPath { get; set; }
    }

    public interface IExportManagerService
    {
        Task<string> QueueExportAsync(string userId, DataTable data, string fileName, ExportFormat format, Dictionary<string, object>? options = null);
        Task<ExportTask?> GetExportTaskAsync(string taskId);
        Task<List<ExportTask>> GetUserExportTasksAsync(string userId);
        Task<bool> CancelExportAsync(string taskId, string userId);
        Task<List<ExportHistory>> GetExportHistoryAsync(string userId, int pageSize = 20, int pageNumber = 1);
        Task CleanupOldFilesAsync(TimeSpan maxAge);
    }

    public class ExportManagerService : IExportManagerService
    {
        private readonly ILogger<ExportManagerService> _logger;
        private readonly ExcelExportService _excelExportService;
        private readonly PdfExportService _pdfExportService;
        private readonly IRealTimeNotificationService? _notificationService;
        private readonly string _exportPath;
        
        // 内存中的任务队列（生产环境应使用持久化存储）
        private readonly ConcurrentDictionary<string, ExportTask> _activeTasks = new();
        private readonly List<ExportHistory> _exportHistory = new();
        private readonly SemaphoreSlim _exportSemaphore;

        public ExportManagerService(
            ILogger<ExportManagerService> logger,
            ExcelExportService excelExportService,
            PdfExportService pdfExportService,
            IRealTimeNotificationService? notificationService = null,
            string exportPath = "wwwroot/exports")
        {
            _logger = logger;
            _excelExportService = excelExportService;
            _pdfExportService = pdfExportService;
            _notificationService = notificationService;
            _exportPath = exportPath;
            
            // 限制并发导出任务数量
            _exportSemaphore = new SemaphoreSlim(3, 3);
            
            // 确保导出目录存在
            if (!Directory.Exists(_exportPath))
            {
                Directory.CreateDirectory(_exportPath);
            }
        }

        /// <summary>
        /// 将导出任务加入队列
        /// </summary>
        public async Task<string> QueueExportAsync(
            string userId, 
            DataTable data, 
            string fileName, 
            ExportFormat format,
            Dictionary<string, object>? options = null)
        {
            var task = new ExportTask
            {
                UserId = userId,
                FileName = fileName,
                Format = format,
                Data = data,
                Options = options ?? new Dictionary<string, object>(),
                CancellationTokenSource = new CancellationTokenSource()
            };

            _activeTasks[task.Id] = task;

            // 异步处理导出任务
            _ = ProcessExportTaskAsync(task);

            _logger.LogInformation("导出任务已加入队列：{TaskId}, 用户：{UserId}, 格式：{Format}", 
                task.Id, userId, format);

            return task.Id;
        }

        /// <summary>
        /// 处理导出任务
        /// </summary>
        private async Task ProcessExportTaskAsync(ExportTask task)
        {
            await _exportSemaphore.WaitAsync(task.CancellationTokenSource!.Token);
            
            try
            {
                task.Status = ExportStatus.Processing;
                task.StartedAt = DateTime.Now;

                // 发送开始通知
                if (_notificationService != null)
                {
                    await _notificationService.SendSystemNotificationAsync(task.UserId, new SystemNotification
                    {
                        Title = "导出开始",
                        Message = $"正在导出 {task.FileName}.{task.Format.ToString().ToLower()}",
                        Type = NotificationType.Info
                    });
                }

                byte[] fileData;
                string fileExtension;

                // 根据格式选择导出方法
                switch (task.Format)
                {
                    case ExportFormat.Excel:
                        task.Progress = 25;
                        fileData = _excelExportService.ExportToExcel(task.Data!, task.FileName);
                        fileExtension = ".xlsx";
                        break;

                    case ExportFormat.PDF:
                        task.Progress = 25;
                        var pdfOptions = ExtractPdfOptions(task.Options);
                        fileData = await _pdfExportService.ExportToPdfAsync(task.Data!, pdfOptions);
                        fileExtension = ".pdf";
                        break;

                    case ExportFormat.CSV:
                        task.Progress = 25;
                        fileData = ExportToCsv(task.Data!);
                        fileExtension = ".csv";
                        break;

                    default:
                        throw new NotSupportedException($"不支持的导出格式：{task.Format}");
                }

                task.Progress = 75;

                // 保存文件
                var fileName = $"{task.FileName}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
                var filePath = Path.Combine(_exportPath, fileName);
                
                await File.WriteAllBytesAsync(filePath, fileData, task.CancellationTokenSource.Token);

                task.FilePath = filePath;
                task.FileSizeBytes = fileData.Length;
                task.Status = ExportStatus.Completed;
                task.CompletedAt = DateTime.Now;
                task.Progress = 100;

                // 添加到历史记录
                var history = new ExportHistory
                {
                    Id = task.Id,
                    UserId = task.UserId,
                    FileName = task.FileName,
                    Format = task.Format,
                    ExportedAt = task.CompletedAt.Value,
                    IsSuccess = true,
                    RecordCount = task.Data?.Rows.Count ?? 0,
                    FileSizeBytes = task.FileSizeBytes.Value,
                    Duration = task.CompletedAt.Value - task.StartedAt!.Value,
                    DownloadPath = filePath
                };
                
                _exportHistory.Add(history);

                // 发送完成通知
                if (_notificationService != null)
                {
                    await _notificationService.SendSystemNotificationAsync(task.UserId, new SystemNotification
                    {
                        Title = "导出完成",
                        Message = $"{task.FileName} 导出成功，文件大小：{FormatFileSize(task.FileSizeBytes.Value)}",
                        Type = NotificationType.Success,
                        AutoHideDelay = TimeSpan.FromSeconds(10)
                    });
                }

                _logger.LogInformation("导出任务完成：{TaskId}, 文件：{FilePath}", task.Id, filePath);
            }
            catch (OperationCanceledException)
            {
                task.Status = ExportStatus.Cancelled;
                task.CompletedAt = DateTime.Now;
                
                _logger.LogInformation("导出任务已取消：{TaskId}", task.Id);
            }
            catch (Exception ex)
            {
                task.Status = ExportStatus.Failed;
                task.ErrorMessage = ex.Message;
                task.CompletedAt = DateTime.Now;

                // 发送失败通知
                if (_notificationService != null)
                {
                    await _notificationService.SendSystemNotificationAsync(task.UserId, new SystemNotification
                    {
                        Title = "导出失败",
                        Message = $"{task.FileName} 导出失败：{ex.Message}",
                        Type = NotificationType.Error,
                        AutoHideDelay = TimeSpan.FromSeconds(15)
                    });
                }

                _logger.LogError(ex, "导出任务失败：{TaskId}", task.Id);
            }
            finally
            {
                // 清理数据引用以释放内存
                task.Data = null;
                _exportSemaphore.Release();
            }
        }

        /// <summary>
        /// 获取导出任务状态
        /// </summary>
        public Task<ExportTask?> GetExportTaskAsync(string taskId)
        {
            _activeTasks.TryGetValue(taskId, out var task);
            return Task.FromResult(task);
        }

        /// <summary>
        /// 获取用户的导出任务列表
        /// </summary>
        public Task<List<ExportTask>> GetUserExportTasksAsync(string userId)
        {
            var userTasks = new List<ExportTask>();
            
            foreach (var task in _activeTasks.Values)
            {
                if (task.UserId == userId)
                {
                    // 创建副本，避免返回敏感数据
                    var taskCopy = new ExportTask
                    {
                        Id = task.Id,
                        UserId = task.UserId,
                        FileName = task.FileName,
                        Format = task.Format,
                        Status = task.Status,
                        CreatedAt = task.CreatedAt,
                        StartedAt = task.StartedAt,
                        CompletedAt = task.CompletedAt,
                        Progress = task.Progress,
                        ErrorMessage = task.ErrorMessage,
                        FileSizeBytes = task.FileSizeBytes
                    };
                    userTasks.Add(taskCopy);
                }
            }

            return Task.FromResult(userTasks);
        }

        /// <summary>
        /// 取消导出任务
        /// </summary>
        public Task<bool> CancelExportAsync(string taskId, string userId)
        {
            if (_activeTasks.TryGetValue(taskId, out var task) && task.UserId == userId)
            {
                if (task.Status == ExportStatus.Pending || task.Status == ExportStatus.Processing)
                {
                    task.CancellationTokenSource?.Cancel();
                    return Task.FromResult(true);
                }
            }
            
            return Task.FromResult(false);
        }

        /// <summary>
        /// 获取导出历史
        /// </summary>
        public Task<List<ExportHistory>> GetExportHistoryAsync(string userId, int pageSize = 20, int pageNumber = 1)
        {
            var userHistory = _exportHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ExportedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult(userHistory);
        }

        /// <summary>
        /// 清理旧文件
        /// </summary>
        public async Task CleanupOldFilesAsync(TimeSpan maxAge)
        {
            try
            {
                var cutoffDate = DateTime.Now - maxAge;
                var files = Directory.GetFiles(_exportPath)
                    .Where(f => File.GetCreationTime(f) < cutoffDate);

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogInformation("已删除过期导出文件：{FilePath}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除过期文件失败：{FilePath}", file);
                    }
                }

                // 清理已完成的任务（保留最近24小时的）
                var taskCutoffDate = DateTime.Now - TimeSpan.FromHours(24);
                var tasksToRemove = _activeTasks.Values
                    .Where(t => t.CompletedAt.HasValue && t.CompletedAt < taskCutoffDate)
                    .ToList();

                foreach (var task in tasksToRemove)
                {
                    _activeTasks.TryRemove(task.Id, out _);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理旧文件时出错");
            }
        }

        /// <summary>
        /// 提取PDF导出选项
        /// </summary>
        private PdfExportOptions ExtractPdfOptions(Dictionary<string, object> options)
        {
            var pdfOptions = new PdfExportOptions();

            if (options.TryGetValue("Title", out var title))
                pdfOptions.Title = title?.ToString() ?? pdfOptions.Title;

            if (options.TryGetValue("Author", out var author))
                pdfOptions.Author = author?.ToString() ?? pdfOptions.Author;

            if (options.TryGetValue("IncludeTimestamp", out var includeTimestamp))
                if (bool.TryParse(includeTimestamp?.ToString(), out var includeTimestampBool))
                    pdfOptions.IncludeTimestamp = includeTimestampBool;

            return pdfOptions;
        }

        /// <summary>
        /// 导出为CSV格式
        /// </summary>
        private byte[] ExportToCsv(DataTable data)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8);

            // 写入表头
            if (data.Columns.Count > 0)
            {
                var columnNames = data.Columns.Cast<DataColumn>()
                    .Select(c => EscapeCsvField(c.ColumnName));
                writer.WriteLine(string.Join(",", columnNames));
            }

            // 写入数据行
            foreach (DataRow row in data.Rows)
            {
                var values = row.ItemArray.Select(field => EscapeCsvField(field?.ToString() ?? ""));
                writer.WriteLine(string.Join(",", values));
            }

            writer.Flush();
            return memoryStream.ToArray();
        }

        /// <summary>
        /// 转义CSV字段
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int order = 0;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }
    }
} 