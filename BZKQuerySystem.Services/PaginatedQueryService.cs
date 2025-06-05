using BZKQuerySystem.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BZKQuerySystem.Services
{
    public class PaginatedQueryService
    {
        private readonly string _connectionString;
        private readonly ILogger<PaginatedQueryService> _logger;

        public PaginatedQueryService(string connectionString, ILogger<PaginatedQueryService> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        public async Task<PaginatedResult<DataRow>> ExecutePaginatedQueryAsync(
            string sql, 
            List<SqlParameter> parameters,
            int pageNumber = 1, 
            int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 首先获取总记录数
                var countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountTable";
                var totalCount = await GetTotalCountAsync(countSql, parameters, cancellationToken);

                // 构建分页SQL
                var paginatedSql = BuildPaginatedSql(sql, pageNumber, pageSize);

                // 执行分页查询
                var data = new List<DataRow>();
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = new SqlCommand(paginatedSql, connection);
                command.CommandTimeout = 30;
                
                foreach (var param in parameters)
                {
                    command.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                }

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                var dataTable = new DataTable();
                dataTable.Load(reader);

                foreach (DataRow row in dataTable.Rows)
                {
                    data.Add(row);
                }

                return new PaginatedResult<DataRow>
                {
                    Data = data,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行分页查询失败");
                throw;
            }
        }

        /// <summary>
        /// 流式导出大数据集
        /// </summary>
        public async IAsyncEnumerable<string> StreamQueryResultsAsCsvAsync(
            string sql,
            List<SqlParameter> parameters,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 60; // 增加超时时间用于大数据查询
            
            foreach (var param in parameters)
            {
                command.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
            }

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            // 输出CSV头部
            var headers = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                headers.Add(EscapeCsvValue(reader.GetName(i)));
            }
            yield return string.Join(",", headers);

            // 流式输出数据行
            var batchSize = 1000;
            var currentBatch = 0;
            var csvBuilder = new StringBuilder();

            while (await reader.ReadAsync(cancellationToken))
            {
                var values = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
                    values.Add(EscapeCsvValue(value));
                }

                csvBuilder.AppendLine(string.Join(",", values));
                currentBatch++;

                // 批量返回以减少内存使用
                if (currentBatch >= batchSize)
                {
                    yield return csvBuilder.ToString();
                    csvBuilder.Clear();
                    currentBatch = 0;

                    // 检查取消令牌
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            // 返回剩余的数据
            if (csvBuilder.Length > 0)
            {
                yield return csvBuilder.ToString();
            }
        }

        /// <summary>
        /// 获取查询总数
        /// </summary>
        private async Task<int> GetTotalCountAsync(string countSql, List<SqlParameter> parameters, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(countSql, connection);
            command.CommandTimeout = 30;
            
            foreach (var param in parameters)
            {
                command.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// 构建分页SQL
        /// </summary>
        private string BuildPaginatedSql(string originalSql, int pageNumber, int pageSize)
        {
            var offset = (pageNumber - 1) * pageSize;
            
            // 检查是否已有ORDER BY子句
            if (!originalSql.ToUpper().Contains("ORDER BY"))
            {
                // 如果没有ORDER BY，添加一个默认的排序
                originalSql += " ORDER BY (SELECT NULL)";
            }

            return $@"
                {originalSql}
                OFFSET {offset} ROWS
                FETCH NEXT {pageSize} ROWS ONLY";
        }

        /// <summary>
        /// 转义CSV值
        /// </summary>
        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // 如果包含逗号、双引号或换行符，需要用双引号包围
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                // 转义双引号
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }

        /// <summary>
        /// 获取查询执行计划和性能统计
        /// </summary>
        public async Task<QueryPerformanceInfo> GetQueryPerformanceAsync(string sql, List<SqlParameter> parameters)
        {
            var performanceInfo = new QueryPerformanceInfo();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 启用统计信息收集
                using (var statsCommand = new SqlCommand("SET STATISTICS IO ON; SET STATISTICS TIME ON;", connection))
                {
                    await statsCommand.ExecuteNonQueryAsync();
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                using var command = new SqlCommand(sql, connection);
                foreach (var param in parameters)
                {
                    command.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                }

                // 执行查询但不获取结果，只获取性能信息
                using var reader = await command.ExecuteReaderAsync();
                var rowCount = 0;
                while (await reader.ReadAsync())
                {
                    rowCount++;
                }

                stopwatch.Stop();

                performanceInfo.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                performanceInfo.RowCount = rowCount;
                performanceInfo.MemoryUsageKb = GC.GetTotalMemory(false) / 1024;

                return performanceInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取查询性能信息失败");
                performanceInfo.ErrorMessage = ex.Message;
                return performanceInfo;
            }
        }

        /// <summary>
        /// 执行查询并返回DataTable结果
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string sql, params SqlParameter[] parameters)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new SqlCommand(sql, connection);
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }
                
                command.CommandTimeout = 300; // 5分钟超时
                
                using var adapter = new SqlDataAdapter(command);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                
                stopwatch.Stop();
                _logger.LogInformation("查询执行完成，返回 {RowCount} 行数据，耗时 {Duration}ms", 
                    dataTable.Rows.Count, stopwatch.ElapsedMilliseconds);
                    
                return dataTable;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "查询执行失败，耗时 {Duration}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    public class PaginatedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class QueryPerformanceInfo
    {
        public long ExecutionTimeMs { get; set; }
        public int RowCount { get; set; }
        public long MemoryUsageKb { get; set; }
        public string ErrorMessage { get; set; }
        public string SqlPlan { get; set; }
    }
} 