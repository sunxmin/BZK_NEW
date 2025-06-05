using Microsoft.Extensions.Logging;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace BZKQuerySystem.Services
{
    public interface IDatabaseOptimizationService
    {
        Task<List<IndexRecommendation>> GetIndexRecommendationsAsync();
        Task<List<QueryAnalysisResult>> AnalyzeSlowQueriesAsync();
        Task<DatabaseStatistics> GetDatabaseStatisticsAsync();
        Task<List<TableStatistics>> GetTableStatisticsAsync();
    }

    public class IndexRecommendation
    {
        public string TableName { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public string IndexType { get; set; } = "";
        public string Reason { get; set; } = "";
        public int Priority { get; set; }
        public string CreateScript { get; set; } = "";
    }

    public class QueryAnalysisResult
    {
        public string QueryText { get; set; } = "";
        public TimeSpan AverageExecutionTime { get; set; }
        public int ExecutionCount { get; set; }
        public string OptimizationSuggestion { get; set; } = "";
        public List<string> MissingIndexes { get; set; } = new();
    }

    public class DatabaseStatistics
    {
        public long TotalSize { get; set; }
        public long DataSize { get; set; }
        public long IndexSize { get; set; }
        public int TableCount { get; set; }
        public int IndexCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class TableStatistics
    {
        public string TableName { get; set; } = "";
        public long RowCount { get; set; }
        public long DataSize { get; set; }
        public long IndexSize { get; set; }
        public int IndexCount { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<string> Indexes { get; set; } = new();
    }

    public class DatabaseOptimizationService : IDatabaseOptimizationService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseOptimizationService> _logger;

        public DatabaseOptimizationService(
            string connectionString,
            ILogger<DatabaseOptimizationService> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<List<IndexRecommendation>> GetIndexRecommendationsAsync()
        {
            var recommendations = new List<IndexRecommendation>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // ��ѯȱʧ��������
                var sql = @"
                    SELECT 
                        migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) * (migs.user_seeks + migs.user_scans) AS improvement_measure,
                        'CREATE INDEX [IX_' + OBJECT_NAME(mid.OBJECT_ID) + '_' + 
                        ISNULL(mid.equality_columns,'') + 
                        CASE WHEN mid.inequality_columns IS NOT NULL 
                            THEN '_' + mid.inequality_columns 
                            ELSE '' END + ']' +
                        ' ON ' + mid.statement + 
                        ' (' + ISNULL(mid.equality_columns,'') +
                        CASE WHEN mid.equality_columns IS NOT NULL AND mid.inequality_columns IS NOT NULL 
                            THEN ',' ELSE '' END +
                        ISNULL(mid.inequality_columns, '') + ')' +
                        ISNULL(' INCLUDE (' + mid.included_columns + ')', '') AS create_index_statement,
                        migs.user_seeks,
                        migs.user_scans,
                        mid.statement as table_name,
                        mid.equality_columns,
                        mid.inequality_columns,
                        mid.included_columns
                    FROM sys.dm_db_missing_index_groups mig
                    INNER JOIN sys.dm_db_missing_index_group_stats migs ON migs.group_handle = mig.index_group_handle
                    INNER JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
                    WHERE migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) * (migs.user_seeks + migs.user_scans) > 10
                    ORDER BY improvement_measure DESC;";

                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var tableName = ExtractTableName(reader["table_name"].ToString() ?? "");
                    var equalityColumns = reader["equality_columns"]?.ToString() ?? "";
                    var inequalityColumns = reader["inequality_columns"]?.ToString() ?? "";
                    
                    recommendations.Add(new IndexRecommendation
                    {
                        TableName = tableName,
                        ColumnName = string.Join(", ", new[] { equalityColumns, inequalityColumns }
                            .Where(c => !string.IsNullOrEmpty(c))),
                        IndexType = "NONCLUSTERED",
                        Reason = $"��ѯƵ�ʸ� (����:{reader["user_seeks"]}, ɨ��:{reader["user_scans"]})",
                        Priority = (int)(double)reader["improvement_measure"],
                        CreateScript = reader["create_index_statement"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ��������ʧ��");
            }

            return recommendations;
        }

        public async Task<List<QueryAnalysisResult>> AnalyzeSlowQueriesAsync()
        {
            var results = new List<QueryAnalysisResult>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // ������ѯ�洢�е�����ѯ
                var sql = @"
                    SELECT TOP 10
                        qt.query_sql_text,
                        rs.avg_duration / 1000.0 as avg_duration_ms,
                        rs.count_executions,
                        rs.avg_logical_io_reads,
                        rs.avg_physical_io_reads
                    FROM sys.query_store_query q
                    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
                    JOIN sys.query_store_plan p ON q.query_id = p.query_id
                    JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
                    WHERE rs.avg_duration > 1000000 -- ����1��Ĳ�ѯ
                    ORDER BY rs.avg_duration DESC;";

                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var queryText = reader["query_sql_text"].ToString() ?? "";
                    var avgDuration = TimeSpan.FromMilliseconds((double)reader["avg_duration_ms"]);
                    var execCount = (long)reader["count_executions"];

                    results.Add(new QueryAnalysisResult
                    {
                        QueryText = queryText,
                        AverageExecutionTime = avgDuration,
                        ExecutionCount = (int)execCount,
                        OptimizationSuggestion = GenerateOptimizationSuggestion(queryText),
                        MissingIndexes = ExtractMissingIndexes(queryText)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��������ѯʧ��: {Error}", ex.Message);
                // �����ѯ�洢�����ã����ؿս��
            }

            return results;
        }

        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        SUM(size * 8.0 / 1024) as total_size_mb,
                        SUM(CASE WHEN type = 0 THEN size * 8.0 / 1024 ELSE 0 END) as data_size_mb,
                        (SELECT COUNT(*) FROM sys.tables) as table_count,
                        (SELECT COUNT(*) FROM sys.indexes WHERE type > 0) as index_count
                    FROM sys.database_files;";

                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new DatabaseStatistics
                    {
                        TotalSize = (long)((double)reader["total_size_mb"] * 1024 * 1024),
                        DataSize = (long)((double)reader["data_size_mb"] * 1024 * 1024),
                        IndexSize = (long)((double)reader["total_size_mb"] * 1024 * 1024) - 
                                   (long)((double)reader["data_size_mb"] * 1024 * 1024),
                        TableCount = (int)reader["table_count"],
                        IndexCount = (int)reader["index_count"],
                        LastUpdated = DateTime.Now
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ���ݿ�ͳ��ʧ��");
            }

            return new DatabaseStatistics { LastUpdated = DateTime.Now };
        }

        public async Task<List<TableStatistics>> GetTableStatisticsAsync()
        {
            var statistics = new List<TableStatistics>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        t.NAME as table_name,
                        p.rows as row_count,
                        (SUM(a.total_pages) * 8) / 1024.0 as total_size_mb,
                        (SUM(a.used_pages) * 8) / 1024.0 as used_size_mb,
                        (SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id = t.object_id AND i.type > 0) as index_count
                    FROM sys.tables t
                    INNER JOIN sys.partitions p ON t.object_id = p.object_id
                    INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
                    LEFT OUTER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE t.is_ms_shipped = 0
                        AND p.index_id IN (0,1) -- �ѻ�ۼ�����
                    GROUP BY t.name, p.rows, t.object_id
                    ORDER BY SUM(a.total_pages) DESC;";

                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var tableName = reader["table_name"].ToString() ?? "";
                    var totalSizeMb = (double)reader["total_size_mb"];
                    
                    statistics.Add(new TableStatistics
                    {
                        TableName = tableName,
                        RowCount = (long)reader["row_count"],
                        DataSize = (long)(totalSizeMb * 1024 * 1024),
                        IndexCount = (int)reader["index_count"],
                        LastUpdated = DateTime.Now,
                        Indexes = await GetTableIndexesAsync(connection, tableName)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ��ͳ��ʧ��");
            }

            return statistics;
        }

        private async Task<List<string>> GetTableIndexesAsync(SqlConnection connection, string tableName)
        {
            var indexes = new List<string>();
            
            try
            {
                var sql = @"
                    SELECT i.name
                    FROM sys.indexes i
                    INNER JOIN sys.tables t ON i.object_id = t.object_id
                    WHERE t.name = @tableName AND i.type > 0;";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@tableName", tableName);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    indexes.Add(reader["name"].ToString() ?? "");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ������ʧ��: {Table}", tableName);
            }

            return indexes;
        }

        private string ExtractTableName(string statement)
        {
            // �� [database].[schema].[table] ��ʽ����ȡ����
            var match = Regex.Match(statement, @"\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]");
            return match.Success ? match.Groups[3].Value : statement;
        }

        private string GenerateOptimizationSuggestion(string queryText)
        {
            var suggestions = new List<string>();

            if (queryText.Contains("SELECT *"))
                suggestions.Add("����ʹ�� SELECT *��ֻѡ����Ҫ����");

            if (queryText.Contains("WHERE") && !queryText.Contains("INDEX"))
                suggestions.Add("������WHERE�Ӿ��е����ϴ�������");

            if (queryText.Contains("ORDER BY"))
                suggestions.Add("������ORDER BY�Ӿ��е����ϴ�������");

            if (queryText.Contains("GROUP BY"))
                suggestions.Add("������GROUP BY�Ӿ��е����ϴ�������");

            if (queryText.Contains("JOIN") && !queryText.Contains("INNER"))
                suggestions.Add("����ʹ��INNER JOIN��������JOIN���ͣ�������ã�");

            return suggestions.Any() ? string.Join("; ", suggestions) : "��ѯ�ṹ����";
        }

        private List<string> ExtractMissingIndexes(string queryText)
        {
            var missingIndexes = new List<string>();

            // �򵥵�������ʽƥ��WHERE��ORDER BY�Ӿ��е���
            var whereMatches = Regex.Matches(queryText, @"WHERE\s+(\w+)", RegexOptions.IgnoreCase);
            var orderMatches = Regex.Matches(queryText, @"ORDER BY\s+(\w+)", RegexOptions.IgnoreCase);

            foreach (Match match in whereMatches)
            {
                missingIndexes.Add($"WHERE�Ӿ�: {match.Groups[1].Value}");
            }

            foreach (Match match in orderMatches)
            {
                missingIndexes.Add($"ORDER BY�Ӿ�: {match.Groups[1].Value}");
            }

            return missingIndexes.Distinct().ToList();
        }
    }
} 