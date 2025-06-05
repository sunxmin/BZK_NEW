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
        /// ��ҳ��ѯ
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
                // ���Ȼ�ȡ�ܼ�¼��
                var countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountTable";
                var totalCount = await GetTotalCountAsync(countSql, parameters, cancellationToken);

                // ������ҳSQL
                var paginatedSql = BuildPaginatedSql(sql, pageNumber, pageSize);

                // ִ�з�ҳ��ѯ
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
                _logger.LogError(ex, "ִ�з�ҳ��ѯʧ��");
                throw;
            }
        }

        /// <summary>
        /// ��ʽ���������ݼ�
        /// </summary>
        public async IAsyncEnumerable<string> StreamQueryResultsAsCsvAsync(
            string sql,
            List<SqlParameter> parameters,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 60; // ���ӳ�ʱʱ�����ڴ����ݲ�ѯ
            
            foreach (var param in parameters)
            {
                command.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
            }

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            // ���CSVͷ��
            var headers = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                headers.Add(EscapeCsvValue(reader.GetName(i)));
            }
            yield return string.Join(",", headers);

            // ��ʽ���������
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

                // ���������Լ����ڴ�ʹ��
                if (currentBatch >= batchSize)
                {
                    yield return csvBuilder.ToString();
                    csvBuilder.Clear();
                    currentBatch = 0;

                    // ���ȡ������
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            // ����ʣ�������
            if (csvBuilder.Length > 0)
            {
                yield return csvBuilder.ToString();
            }
        }

        /// <summary>
        /// ��ȡ��ѯ����
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
        /// ������ҳSQL
        /// </summary>
        private string BuildPaginatedSql(string originalSql, int pageNumber, int pageSize)
        {
            var offset = (pageNumber - 1) * pageSize;
            
            // ����Ƿ�����ORDER BY�Ӿ�
            if (!originalSql.ToUpper().Contains("ORDER BY"))
            {
                // ���û��ORDER BY�����һ��Ĭ�ϵ�����
                originalSql += " ORDER BY (SELECT NULL)";
            }

            return $@"
                {originalSql}
                OFFSET {offset} ROWS
                FETCH NEXT {pageSize} ROWS ONLY";
        }

        /// <summary>
        /// ת��CSVֵ
        /// </summary>
        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // ����������š�˫���Ż��з�����Ҫ��˫���Ű�Χ
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                // ת��˫����
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }

        /// <summary>
        /// ��ȡ��ѯִ�мƻ�������ͳ��
        /// </summary>
        public async Task<QueryPerformanceInfo> GetQueryPerformanceAsync(string sql, List<SqlParameter> parameters)
        {
            var performanceInfo = new QueryPerformanceInfo();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // ����ͳ����Ϣ�ռ�
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

                // ִ�в�ѯ������ȡ�����ֻ��ȡ������Ϣ
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
                _logger.LogError(ex, "��ȡ��ѯ������Ϣʧ��");
                performanceInfo.ErrorMessage = ex.Message;
                return performanceInfo;
            }
        }

        /// <summary>
        /// ִ�в�ѯ������DataTable���
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
                
                command.CommandTimeout = 300; // 5���ӳ�ʱ
                
                using var adapter = new SqlDataAdapter(command);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                
                stopwatch.Stop();
                _logger.LogInformation("��ѯִ����ɣ����� {RowCount} �����ݣ���ʱ {Duration}ms", 
                    dataTable.Rows.Count, stopwatch.ElapsedMilliseconds);
                    
                return dataTable;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "��ѯִ��ʧ�ܣ���ʱ {Duration}ms", stopwatch.ElapsedMilliseconds);
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