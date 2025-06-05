using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BZKQuerySystem.Services
{
    public interface IQueryPerformanceService
    {
        Task<QueryPerformanceResult> ExecuteWithMonitoringAsync<T>(
            string queryId,
            Func<Task<T>> queryFunc);
        
        QueryPerformanceStats GetPerformanceStats();
        List<QueryPerformanceResult> GetSlowQueries(int count = 10);
        void ClearStats();
    }

    public class QueryPerformanceResult
    {
        public string QueryId { get; set; } = "";
        public DateTime ExecutedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int? ResultCount { get; set; }
        public long MemoryUsage { get; set; }
    }

    public class QueryPerformanceStats
    {
        public int TotalQueries { get; set; }
        public int SuccessfulQueries { get; set; }
        public int FailedQueries { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public TimeSpan MaxExecutionTime { get; set; }
        public TimeSpan MinExecutionTime { get; set; }
        public DateTime LastQueryTime { get; set; }
    }

    public class QueryPerformanceService : IQueryPerformanceService
    {
        private readonly ILogger<QueryPerformanceService> _logger;
        private readonly ConcurrentQueue<QueryPerformanceResult> _queryHistory;
        private readonly object _statsLock = new object();
        private QueryPerformanceStats _stats;
        private const int MaxHistoryCount = 1000;

        public QueryPerformanceService(ILogger<QueryPerformanceService> logger)
        {
            _logger = logger;
            _queryHistory = new ConcurrentQueue<QueryPerformanceResult>();
            _stats = new QueryPerformanceStats
            {
                MinExecutionTime = TimeSpan.MaxValue
            };
        }

        public async Task<QueryPerformanceResult> ExecuteWithMonitoringAsync<T>(
            string queryId,
            Func<Task<T>> queryFunc)
        {
            var startMemory = GC.GetTotalMemory(false);
            var stopwatch = Stopwatch.StartNew();
            var result = new QueryPerformanceResult
            {
                QueryId = queryId,
                ExecutedAt = DateTime.Now
            };

            try
            {
                _logger.LogInformation("开始执行查询: {QueryId}", queryId);
                
                var queryResult = await queryFunc();
                
                result.IsSuccess = true;
                
                // 尝试获取结果数量
                if (queryResult is IEnumerable<object> enumerable)
                {
                    result.ResultCount = enumerable.Count();
                }

                _logger.LogInformation("查询执行成功: {QueryId}, 耗时: {Duration}ms", 
                    queryId, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                
                _logger.LogError(ex, "查询执行失败: {QueryId}, 耗时: {Duration}ms", 
                    queryId, stopwatch.ElapsedMilliseconds);
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.MemoryUsage = GC.GetTotalMemory(false) - startMemory;
                
                // 记录查询结果
                RecordQueryResult(result);
            }

            return result;
        }

        private void RecordQueryResult(QueryPerformanceResult result)
        {
            // 添加到历史记录
            _queryHistory.Enqueue(result);
            
            // 保持历史记录数量在限制内
            while (_queryHistory.Count > MaxHistoryCount)
            {
                _queryHistory.TryDequeue(out _);
            }

            // 更新统计信息
            lock (_statsLock)
            {
                _stats.TotalQueries++;
                if (result.IsSuccess)
                {
                    _stats.SuccessfulQueries++;
                }
                else
                {
                    _stats.FailedQueries++;
                }

                // 更新执行时间统计
                if (result.Duration > _stats.MaxExecutionTime)
                {
                    _stats.MaxExecutionTime = result.Duration;
                }

                if (result.Duration < _stats.MinExecutionTime)
                {
                    _stats.MinExecutionTime = result.Duration;
                }

                // 计算平均执行时间
                var totalDuration = _queryHistory
                    .Where(q => q.IsSuccess)
                    .Sum(q => q.Duration.TotalMilliseconds);
                
                if (_stats.SuccessfulQueries > 0)
                {
                    _stats.AverageExecutionTime = TimeSpan.FromMilliseconds(
                        totalDuration / _stats.SuccessfulQueries);
                }

                _stats.LastQueryTime = result.ExecutedAt;
            }
        }

        public QueryPerformanceStats GetPerformanceStats()
        {
            lock (_statsLock)
            {
                return new QueryPerformanceStats
                {
                    TotalQueries = _stats.TotalQueries,
                    SuccessfulQueries = _stats.SuccessfulQueries,
                    FailedQueries = _stats.FailedQueries,
                    AverageExecutionTime = _stats.AverageExecutionTime,
                    MaxExecutionTime = _stats.MaxExecutionTime,
                    MinExecutionTime = _stats.MinExecutionTime == TimeSpan.MaxValue 
                        ? TimeSpan.Zero : _stats.MinExecutionTime,
                    LastQueryTime = _stats.LastQueryTime
                };
            }
        }

        public List<QueryPerformanceResult> GetSlowQueries(int count = 10)
        {
            return _queryHistory
                .Where(q => q.IsSuccess)
                .OrderByDescending(q => q.Duration)
                .Take(count)
                .ToList();
        }

        public void ClearStats()
        {
            lock (_statsLock)
            {
                while (_queryHistory.TryDequeue(out _)) { }
                
                _stats = new QueryPerformanceStats
                {
                    MinExecutionTime = TimeSpan.MaxValue
                };
            }
        }
    }
} 