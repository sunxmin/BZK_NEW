using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BZKQuerySystem.Services
{
    public interface IQueryHistoryVersionService
    {
        Task<int> SaveQueryVersionAsync(QueryHistoryVersion version);
        Task<List<QueryHistoryVersion>> GetQueryVersionsAsync(string userId, int? limit = 50);
        Task<List<QueryHistoryVersion>> GetQueryVersionsBySessionAsync(string sessionId);
        Task<QueryHistoryVersion?> GetVersionByIdAsync(int versionId);
        Task<bool> DeleteVersionAsync(int versionId, string userId);
        Task<bool> SetVersionNameAsync(int versionId, string userId, string name);
        Task<bool> SetVersionFavoriteAsync(int versionId, string userId, bool isFavorite);
        Task<List<QueryHistoryVersion>> GetFavoriteVersionsAsync(string userId);
        Task<QueryHistoryVersion?> RollbackToVersionAsync(int versionId, string userId);
        Task<List<QueryHistorySession>> GetQuerySessionsAsync(string userId);
        Task<bool> DeleteSessionAsync(string sessionId, string userId);
        Task<QueryComparisonResult> CompareVersionsAsync(int version1Id, int version2Id);
        Task<List<QueryHistoryStatistics>> GetQueryStatisticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class QueryHistoryVersion
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string? VersionName { get; set; }
        public string QueryConfigJson { get; set; } = "";
        public string GeneratedSql { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsFavorite { get; set; } = false;
        public bool IsSuccessful { get; set; } = true;
        public int? RecordCount { get; set; }
        public TimeSpan? ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ResultSummary { get; set; }
        
        // 反序列化的查询配置
        public QueryConfiguration? QueryConfig { get; set; }
        
        // 版本标签和备注
        public string? Tags { get; set; }
        public string? Notes { get; set; }
    }

    public class QueryHistorySession
    {
        public string SessionId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string? SessionName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int VersionCount { get; set; }
        public int SuccessfulQueries { get; set; }
        public int FailedQueries { get; set; }
        public TimeSpan? TotalExecutionTime { get; set; }
        public List<QueryHistoryVersion> Versions { get; set; } = new();
    }

    public class QueryComparisonResult
    {
        public QueryHistoryVersion Version1 { get; set; } = new();
        public QueryHistoryVersion Version2 { get; set; } = new();
        public List<string> ConfigDifferences { get; set; } = new();
        public List<string> SqlDifferences { get; set; } = new();
        public PerformanceComparison? Performance { get; set; }
    }

    public class PerformanceComparison
    {
        public TimeSpan? ExecutionTimeDiff { get; set; }
        public int? RecordCountDiff { get; set; }
        public string PerformanceChange { get; set; } = "";
    }

    public class QueryHistoryStatistics
    {
        public DateTime Date { get; set; }
        public int TotalQueries { get; set; }
        public int SuccessfulQueries { get; set; }
        public int FailedQueries { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public int TotalRecords { get; set; }
    }

    public class QueryHistoryVersionService : IQueryHistoryVersionService
    {
        private readonly string _connectionString;
        private readonly ILogger<QueryHistoryVersionService> _logger;

        public QueryHistoryVersionService(string connectionString, ILogger<QueryHistoryVersionService> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
            EnsureTablesExist();
        }

        private void EnsureTablesExist()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // 创建查询历史版本表
                var createVersionsTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='QueryHistoryVersions' AND xtype='U')
                CREATE TABLE QueryHistoryVersions (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    SessionId NVARCHAR(50) NOT NULL,
                    UserId NVARCHAR(255) NOT NULL,
                    VersionName NVARCHAR(255),
                    QueryConfigJson NVARCHAR(MAX) NOT NULL,
                    GeneratedSql NVARCHAR(MAX) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                    IsSuccessful BIT NOT NULL DEFAULT 1,
                    RecordCount INT,
                    ExecutionTimeMs BIGINT,
                    ErrorMessage NVARCHAR(MAX),
                    ResultSummary NVARCHAR(1000),
                    Tags NVARCHAR(500),
                    Notes NVARCHAR(1000),
                    INDEX IX_QueryHistoryVersions_UserId (UserId),
                    INDEX IX_QueryHistoryVersions_SessionId (SessionId),
                    INDEX IX_QueryHistoryVersions_CreatedAt (CreatedAt)
                )";

                using var cmd1 = new SqlCommand(createVersionsTableSql, connection);
                cmd1.ExecuteNonQuery();

                // 创建收藏版本表
                var createFavoriteVersionsTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserFavoriteVersions' AND xtype='U')
                CREATE TABLE UserFavoriteVersions (
                    UserId NVARCHAR(255) NOT NULL,
                    VersionId INT NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                    PRIMARY KEY (UserId, VersionId),
                    FOREIGN KEY (VersionId) REFERENCES QueryHistoryVersions(Id) ON DELETE CASCADE
                )";

                using var cmd2 = new SqlCommand(createFavoriteVersionsTableSql, connection);
                cmd2.ExecuteNonQuery();

                // 创建查询会话表
                var createSessionsTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='QuerySessions' AND xtype='U')
                CREATE TABLE QuerySessions (
                    SessionId NVARCHAR(50) PRIMARY KEY,
                    UserId NVARCHAR(255) NOT NULL,
                    SessionName NVARCHAR(255),
                    StartTime DATETIME2 NOT NULL DEFAULT GETDATE(),
                    EndTime DATETIME2,
                    INDEX IX_QuerySessions_UserId (UserId),
                    INDEX IX_QuerySessions_StartTime (StartTime)
                )";

                using var cmd3 = new SqlCommand(createSessionsTableSql, connection);
                cmd3.ExecuteNonQuery();

                _logger.LogInformation("查询历史版本表初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建查询历史版本表失败");
            }
        }

        public async Task<int> SaveQueryVersionAsync(QueryHistoryVersion version)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 确保会话存在
                await EnsureSessionExistsAsync(connection, version.SessionId, version.UserId);

                var sql = @"
                INSERT INTO QueryHistoryVersions 
                (SessionId, UserId, VersionName, QueryConfigJson, GeneratedSql, IsSuccessful, 
                 RecordCount, ExecutionTimeMs, ErrorMessage, ResultSummary, Tags, Notes)
                VALUES 
                (@SessionId, @UserId, @VersionName, @QueryConfigJson, @GeneratedSql, @IsSuccessful,
                 @RecordCount, @ExecutionTimeMs, @ErrorMessage, @ResultSummary, @Tags, @Notes);
                SELECT SCOPE_IDENTITY();";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@SessionId", version.SessionId);
                cmd.Parameters.AddWithValue("@UserId", version.UserId);
                cmd.Parameters.AddWithValue("@VersionName", version.VersionName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@QueryConfigJson", version.QueryConfigJson);
                cmd.Parameters.AddWithValue("@GeneratedSql", version.GeneratedSql);
                cmd.Parameters.AddWithValue("@IsSuccessful", version.IsSuccessful);
                cmd.Parameters.AddWithValue("@RecordCount", version.RecordCount ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ExecutionTimeMs", version.ExecutionTime?.TotalMilliseconds ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ErrorMessage", version.ErrorMessage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ResultSummary", version.ResultSummary ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Tags", version.Tags ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes", version.Notes ?? (object)DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                var newId = Convert.ToInt32(result);

                _logger.LogInformation("保存查询版本成功: {VersionId}, 会话: {SessionId}", newId, version.SessionId);
                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存查询版本失败: 会话 {SessionId}", version.SessionId);
                return -1;
            }
        }

        public async Task<List<QueryHistoryVersion>> GetQueryVersionsAsync(string userId, int? limit = 50)
        {
            var versions = new List<QueryHistoryVersion>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                SELECT TOP (@Limit) v.*, 
                       CASE WHEN f.UserId IS NOT NULL THEN 1 ELSE 0 END as IsFavorite
                FROM QueryHistoryVersions v
                LEFT JOIN UserFavoriteVersions f ON v.Id = f.VersionId AND f.UserId = @UserId
                WHERE v.UserId = @UserId
                ORDER BY v.CreatedAt DESC";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Limit", limit ?? 50);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var version = MapVersion(reader);
                    versions.Add(version);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取查询版本失败: {UserId}", userId);
            }

            return versions;
        }

        public async Task<List<QueryHistoryVersion>> GetQueryVersionsBySessionAsync(string sessionId)
        {
            var versions = new List<QueryHistoryVersion>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                SELECT * FROM QueryHistoryVersions 
                WHERE SessionId = @SessionId 
                ORDER BY CreatedAt ASC";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@SessionId", sessionId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var version = MapVersion(reader);
                    versions.Add(version);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取会话版本失败: {SessionId}", sessionId);
            }

            return versions;
        }

        public async Task<QueryHistoryVersion?> GetVersionByIdAsync(int versionId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT * FROM QueryHistoryVersions WHERE Id = @Id";
                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Id", versionId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapVersion(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取版本失败: {VersionId}", versionId);
            }

            return null;
        }

        public async Task<bool> DeleteVersionAsync(int versionId, string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "DELETE FROM QueryHistoryVersions WHERE Id = @Id AND UserId = @UserId";
                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Id", versionId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("删除查询版本: {VersionId}, 影响行数: {RowsAffected}", versionId, rowsAffected);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除查询版本失败: {VersionId}", versionId);
                return false;
            }
        }

        public async Task<bool> SetVersionNameAsync(int versionId, string userId, string name)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                UPDATE QueryHistoryVersions 
                SET VersionName = @Name 
                WHERE Id = @Id AND UserId = @UserId";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Id", versionId);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@UserId", userId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("设置版本名称: {VersionId}, 名称: {Name}", versionId, name);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置版本名称失败: {VersionId}", versionId);
                return false;
            }
        }

        public async Task<bool> SetVersionFavoriteAsync(int versionId, string userId, bool isFavorite)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                if (isFavorite)
                {
                    var sql = @"
                    IF NOT EXISTS (SELECT 1 FROM UserFavoriteVersions WHERE UserId = @UserId AND VersionId = @VersionId)
                    INSERT INTO UserFavoriteVersions (UserId, VersionId) VALUES (@UserId, @VersionId)";
                    
                    using var cmd = new SqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@VersionId", versionId);
                    await cmd.ExecuteNonQueryAsync();
                }
                else
                {
                    var sql = "DELETE FROM UserFavoriteVersions WHERE UserId = @UserId AND VersionId = @VersionId";
                    using var cmd = new SqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@VersionId", versionId);
                    await cmd.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("设置版本收藏状态: {VersionId}, 用户: {UserId}, 收藏: {IsFavorite}", 
                    versionId, userId, isFavorite);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置版本收藏状态失败: {VersionId}", versionId);
                return false;
            }
        }

        public async Task<List<QueryHistoryVersion>> GetFavoriteVersionsAsync(string userId)
        {
            var versions = new List<QueryHistoryVersion>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                SELECT v.*, 1 as IsFavorite
                FROM QueryHistoryVersions v
                INNER JOIN UserFavoriteVersions f ON v.Id = f.VersionId
                WHERE f.UserId = @UserId
                ORDER BY f.CreatedAt DESC";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var version = MapVersion(reader);
                    version.IsFavorite = true;
                    versions.Add(version);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取收藏版本失败: {UserId}", userId);
            }

            return versions;
        }

        public async Task<QueryHistoryVersion?> RollbackToVersionAsync(int versionId, string userId)
        {
            try
            {
                var version = await GetVersionByIdAsync(versionId);
                if (version == null || version.UserId != userId)
                {
                    return null;
                }

                // 创建新版本作为回滚版本
                var rollbackVersion = new QueryHistoryVersion
                {
                    SessionId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    VersionName = $"回滚到: {version.VersionName ?? $"版本{versionId}"}",
                    QueryConfigJson = version.QueryConfigJson,
                    GeneratedSql = version.GeneratedSql,
                    Tags = "rollback",
                    Notes = $"从版本 {versionId} 回滚"
                };

                var newVersionId = await SaveQueryVersionAsync(rollbackVersion);
                if (newVersionId > 0)
                {
                    rollbackVersion.Id = newVersionId;
                    _logger.LogInformation("回滚到版本成功: {OriginalVersionId} -> {NewVersionId}", versionId, newVersionId);
                    return rollbackVersion;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回滚到版本失败: {VersionId}", versionId);
            }

            return null;
        }

        public async Task<List<QueryHistorySession>> GetQuerySessionsAsync(string userId)
        {
            var sessions = new List<QueryHistorySession>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                SELECT s.SessionId, s.UserId, s.SessionName, s.StartTime, s.EndTime,
                       COUNT(v.Id) as VersionCount,
                       SUM(CASE WHEN v.IsSuccessful = 1 THEN 1 ELSE 0 END) as SuccessfulQueries,
                       SUM(CASE WHEN v.IsSuccessful = 0 THEN 1 ELSE 0 END) as FailedQueries,
                       SUM(v.ExecutionTimeMs) as TotalExecutionTimeMs
                FROM QuerySessions s
                LEFT JOIN QueryHistoryVersions v ON s.SessionId = v.SessionId
                WHERE s.UserId = @UserId
                GROUP BY s.SessionId, s.UserId, s.SessionName, s.StartTime, s.EndTime
                ORDER BY s.StartTime DESC";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var session = new QueryHistorySession
                    {
                        SessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                        UserId = reader.GetString(reader.GetOrdinal("UserId")),
                        SessionName = reader.IsDBNull(reader.GetOrdinal("SessionName")) ? null : reader.GetString(reader.GetOrdinal("SessionName")),
                        StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                        EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? null : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                        VersionCount = reader.GetInt32(reader.GetOrdinal("VersionCount")),
                        SuccessfulQueries = reader.GetInt32(reader.GetOrdinal("SuccessfulQueries")),
                        FailedQueries = reader.GetInt32(reader.GetOrdinal("FailedQueries"))
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("TotalExecutionTimeMs")))
                    {
                        session.TotalExecutionTime = TimeSpan.FromMilliseconds(reader.GetInt64(reader.GetOrdinal("TotalExecutionTimeMs")));
                    }

                    sessions.Add(session);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取查询会话失败: {UserId}", userId);
            }

            return sessions;
        }

        public async Task<bool> DeleteSessionAsync(string sessionId, string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                try
                {
                    // 删除版本
                    var deleteVersionsSql = "DELETE FROM QueryHistoryVersions WHERE SessionId = @SessionId AND UserId = @UserId";
                    using var cmd1 = new SqlCommand(deleteVersionsSql, connection, transaction);
                    cmd1.Parameters.AddWithValue("@SessionId", sessionId);
                    cmd1.Parameters.AddWithValue("@UserId", userId);
                    await cmd1.ExecuteNonQueryAsync();

                    // 删除会话
                    var deleteSessionSql = "DELETE FROM QuerySessions WHERE SessionId = @SessionId AND UserId = @UserId";
                    using var cmd2 = new SqlCommand(deleteSessionSql, connection, transaction);
                    cmd2.Parameters.AddWithValue("@SessionId", sessionId);
                    cmd2.Parameters.AddWithValue("@UserId", userId);
                    var rowsAffected = await cmd2.ExecuteNonQueryAsync();

                    transaction.Commit();
                    
                    _logger.LogInformation("删除查询会话: {SessionId}, 影响行数: {RowsAffected}", sessionId, rowsAffected);
                    return rowsAffected > 0;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除查询会话失败: {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<QueryComparisonResult> CompareVersionsAsync(int version1Id, int version2Id)
        {
            var result = new QueryComparisonResult();
            
            try
            {
                var version1 = await GetVersionByIdAsync(version1Id);
                var version2 = await GetVersionByIdAsync(version2Id);

                if (version1 == null || version2 == null)
                {
                    return result;
                }

                result.Version1 = version1;
                result.Version2 = version2;

                // 比较配置差异
                result.ConfigDifferences = CompareConfigurations(version1.QueryConfigJson, version2.QueryConfigJson);
                
                // 比较SQL差异
                result.SqlDifferences = CompareSql(version1.GeneratedSql, version2.GeneratedSql);

                // 比较性能
                if (version1.ExecutionTime.HasValue && version2.ExecutionTime.HasValue)
                {
                    result.Performance = new PerformanceComparison
                    {
                        ExecutionTimeDiff = version2.ExecutionTime - version1.ExecutionTime,
                        RecordCountDiff = (version2.RecordCount ?? 0) - (version1.RecordCount ?? 0)
                    };

                    if (result.Performance.ExecutionTimeDiff > TimeSpan.Zero)
                    {
                        result.Performance.PerformanceChange = "性能下降";
                    }
                    else if (result.Performance.ExecutionTimeDiff < TimeSpan.Zero)
                    {
                        result.Performance.PerformanceChange = "性能提升";
                    }
                    else
                    {
                        result.Performance.PerformanceChange = "性能相同";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "比较版本失败: {Version1Id} vs {Version2Id}", version1Id, version2Id);
            }

            return result;
        }

        public async Task<List<QueryHistoryStatistics>> GetQueryStatisticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var statistics = new List<QueryHistoryStatistics>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                SELECT CAST(CreatedAt AS DATE) as Date,
                       COUNT(*) as TotalQueries,
                       SUM(CASE WHEN IsSuccessful = 1 THEN 1 ELSE 0 END) as SuccessfulQueries,
                       SUM(CASE WHEN IsSuccessful = 0 THEN 1 ELSE 0 END) as FailedQueries,
                       AVG(ExecutionTimeMs) as AvgExecutionTimeMs,
                       SUM(ISNULL(RecordCount, 0)) as TotalRecords
                FROM QueryHistoryVersions 
                WHERE UserId = @UserId
                  AND (@StartDate IS NULL OR CreatedAt >= @StartDate)
                  AND (@EndDate IS NULL OR CreatedAt <= @EndDate)
                GROUP BY CAST(CreatedAt AS DATE)
                ORDER BY Date DESC";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@StartDate", startDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", endDate ?? (object)DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    statistics.Add(new QueryHistoryStatistics
                    {
                        Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                        TotalQueries = reader.GetInt32(reader.GetOrdinal("TotalQueries")),
                        SuccessfulQueries = reader.GetInt32(reader.GetOrdinal("SuccessfulQueries")),
                        FailedQueries = reader.GetInt32(reader.GetOrdinal("FailedQueries")),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(reader.IsDBNull(reader.GetOrdinal("AvgExecutionTimeMs")) ? 0 : reader.GetDouble(reader.GetOrdinal("AvgExecutionTimeMs"))),
                        TotalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"))
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取查询统计失败: {UserId}", userId);
            }

            return statistics;
        }

        private async Task EnsureSessionExistsAsync(SqlConnection connection, string sessionId, string userId)
        {
            var checkSql = "SELECT COUNT(*) FROM QuerySessions WHERE SessionId = @SessionId";
            using var checkCmd = new SqlCommand(checkSql, connection);
            checkCmd.Parameters.AddWithValue("@SessionId", sessionId);
            
            var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;
            if (!exists)
            {
                var insertSql = "INSERT INTO QuerySessions (SessionId, UserId) VALUES (@SessionId, @UserId)";
                using var insertCmd = new SqlCommand(insertSql, connection);
                insertCmd.Parameters.AddWithValue("@SessionId", sessionId);
                insertCmd.Parameters.AddWithValue("@UserId", userId);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        private QueryHistoryVersion MapVersion(SqlDataReader reader)
        {
            var version = new QueryHistoryVersion
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                SessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                UserId = reader.GetString(reader.GetOrdinal("UserId")),
                VersionName = reader.IsDBNull(reader.GetOrdinal("VersionName")) ? null : reader.GetString(reader.GetOrdinal("VersionName")),
                QueryConfigJson = reader.GetString(reader.GetOrdinal("QueryConfigJson")),
                GeneratedSql = reader.GetString(reader.GetOrdinal("GeneratedSql")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IsSuccessful = reader.GetBoolean(reader.GetOrdinal("IsSuccessful")),
                RecordCount = reader.IsDBNull(reader.GetOrdinal("RecordCount")) ? null : reader.GetInt32(reader.GetOrdinal("RecordCount")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("ErrorMessage")),
                ResultSummary = reader.IsDBNull(reader.GetOrdinal("ResultSummary")) ? null : reader.GetString(reader.GetOrdinal("ResultSummary")),
                Tags = reader.IsDBNull(reader.GetOrdinal("Tags")) ? null : reader.GetString(reader.GetOrdinal("Tags")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes"))
            };

            if (!reader.IsDBNull(reader.GetOrdinal("ExecutionTimeMs")))
            {
                version.ExecutionTime = TimeSpan.FromMilliseconds(reader.GetInt64(reader.GetOrdinal("ExecutionTimeMs")));
            }

            // 检查是否有收藏字段 - 使用安全的字段检查方式
            var hasIsFavoriteColumn = false;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals("IsFavorite", StringComparison.OrdinalIgnoreCase))
                {
                    hasIsFavoriteColumn = true;
                    break;
                }
            }

            if (hasIsFavoriteColumn && !reader.IsDBNull(reader.GetOrdinal("IsFavorite")))
            {
                version.IsFavorite = reader.GetBoolean(reader.GetOrdinal("IsFavorite"));
            }
            else
            {
                version.IsFavorite = false;
            }

            // 反序列化JSON配置
            try
            {
                if (!string.IsNullOrEmpty(version.QueryConfigJson))
                {
                    version.QueryConfig = JsonSerializer.Deserialize<QueryConfiguration>(version.QueryConfigJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "反序列化版本配置失败: {VersionId}", version.Id);
            }

            return version;
        }

        private List<string> CompareConfigurations(string config1Json, string config2Json)
        {
            var differences = new List<string>();
            
            try
            {
                var config1 = JsonSerializer.Deserialize<QueryConfiguration>(config1Json);
                var config2 = JsonSerializer.Deserialize<QueryConfiguration>(config2Json);

                if (config1 == null || config2 == null) return differences;

                // 比较选中的表
                if (!config1.SelectedTables.SequenceEqual(config2.SelectedTables))
                {
                    differences.Add("选择的表发生变化");
                }

                // 比较选中的字段
                if (config1.SelectedFields.Count != config2.SelectedFields.Count)
                {
                    differences.Add("选择的字段数量发生变化");
                }

                // 比较条件
                if (config1.Conditions.Count != config2.Conditions.Count)
                {
                    differences.Add("查询条件数量发生变化");
                }

                // 比较排序
                if (config1.OrderBy != config2.OrderBy)
                {
                    differences.Add("排序方式发生变化");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "比较配置失败");
                differences.Add("配置比较出错");
            }

            return differences;
        }

        private List<string> CompareSql(string sql1, string sql2)
        {
            var differences = new List<string>();
            
            if (sql1.Trim() != sql2.Trim())
            {
                differences.Add("SQL语句发生变化");
                
                // 简单的行级比较
                var lines1 = sql1.Split('\n').Select(l => l.Trim()).ToList();
                var lines2 = sql2.Split('\n').Select(l => l.Trim()).ToList();

                if (lines1.Count != lines2.Count)
                {
                    differences.Add($"SQL行数从 {lines1.Count} 变为 {lines2.Count}");
                }
            }

            return differences;
        }
    }
} 