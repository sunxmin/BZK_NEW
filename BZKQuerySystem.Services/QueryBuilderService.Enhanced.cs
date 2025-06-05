using BZKQuerySystem.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace BZKQuerySystem.Services
{
    public class EnhancedQueryBuilderService : IQueryBuilderService
    {
        private readonly BZKQueryDbContext _dbContext;
        private readonly string _connectionString;
        private readonly UserService _userService;
        private readonly ILogger<EnhancedQueryBuilderService> _logger;
        private readonly IMemoryCache _cache;

        // SQL关键字白名单 - 只允许查询相关的关键字
        private static readonly HashSet<string> _allowedSqlKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "AND", "OR", "ORDER", "BY", "GROUP", "HAVING", 
            "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "ON", "AS", "DISTINCT",
            "TOP", "ASC", "DESC", "IN", "NOT", "LIKE", "BETWEEN", "IS", "NULL",
            "COUNT", "SUM", "AVG", "MIN", "MAX", "CASE", "WHEN", "THEN", "ELSE", "END"
        };

        // 危险的SQL关键字黑名单
        private static readonly HashSet<string> _dangerousSqlKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "TRUNCATE", 
            "EXEC", "EXECUTE", "SP_", "XP_", "OPENROWSET", "OPENDATASOURCE",
            "BULK", "BACKUP", "RESTORE", "SHUTDOWN", "KILL"
        };

        public EnhancedQueryBuilderService(
            BZKQueryDbContext dbContext, 
            string connectionString, 
            UserService userService,
            ILogger<EnhancedQueryBuilderService> logger,
            IMemoryCache cache)
        {
            _dbContext = dbContext;
            _connectionString = connectionString;
            _userService = userService;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// 安全的SQL查询执行 - 使用参数化查询防止SQL注入
        /// </summary>
        public async Task<DataTable> ExecuteSecureQueryAsync(string userId, QueryRequest request)
        {
            try
            {
                // 1. 验证用户权限
                if (!await ValidateUserTableAccess(userId, request.Tables))
                {
                    throw new UnauthorizedAccessException("用户没有访问指定表的权限");
                }

                // 2. 构建安全的参数化SQL
                var (sql, parameters) = BuildParameterizedQuery(request);
                
                // 3. SQL安全检查
                ValidateSqlSafety(sql);

                // 4. 记录查询日志
                _logger.LogInformation("用户 {UserId} 执行查询: {Sql}", userId, sql);

                // 5. 执行查询
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new SqlCommand(sql, connection);
                command.CommandTimeout = 30; // 设置超时时间
                
                // 添加参数
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }

                using var adapter = new SqlDataAdapter(command);
                var dataTable = new DataTable();
                
                // 限制返回行数防止内存溢出
                adapter.Fill(dataTable);
                
                if (dataTable.Rows.Count > 10000)
                {
                    _logger.LogWarning("查询返回大量数据: {RowCount} 行", dataTable.Rows.Count);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行查询时发生错误，用户: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 构建参数化SQL查询
        /// </summary>
        private (string sql, List<SqlParameter> parameters) BuildParameterizedQuery(QueryRequest request)
        {
            var sql = new StringBuilder();
            var parameters = new List<SqlParameter>();
            var parameterIndex = 0;

            // 构建SELECT子句
            sql.Append("SELECT ");
            if (request.IsDistinct)
                sql.Append("DISTINCT ");

            if (request.TopCount.HasValue)
                sql.Append($"TOP {request.TopCount} ");

            sql.AppendJoin(", ", request.Columns.Select(c => $"[{c.TableName}].[{c.ColumnName}]"));

            // 构建FROM子句
            sql.Append($" FROM [{request.Tables.First()}] ");

            // 构建JOIN子句
            foreach (var join in request.Joins)
            {
                sql.Append($"{join.JoinType} JOIN [{join.RightTable}] ON ");
                sql.Append($"[{join.LeftTable}].[{join.LeftColumn}] = [{join.RightTable}].[{join.RightColumn}] ");
            }

            // 构建WHERE子句
            if (request.Filters.Any())
            {
                sql.Append("WHERE ");
                var filterParts = new List<string>();

                foreach (var filter in request.Filters)
                {
                    var paramName = $"@param{parameterIndex++}";
                    var filterSql = BuildFilterExpression(filter, paramName);
                    filterParts.Add(filterSql);
                    
                    parameters.Add(new SqlParameter(paramName, filter.Value ?? DBNull.Value));
                }

                sql.Append(string.Join(" AND ", filterParts));
            }

            // 构建ORDER BY子句
            if (request.OrderBy.Any())
            {
                sql.Append(" ORDER BY ");
                sql.AppendJoin(", ", request.OrderBy.Select(o => 
                    $"[{o.TableName}].[{o.ColumnName}] {o.Direction}"));
            }

            return (sql.ToString(), parameters);
        }

        /// <summary>
        /// 构建安全的过滤表达式
        /// </summary>
        private string BuildFilterExpression(FilterCondition filter, string paramName)
        {
            var columnName = $"[{filter.TableName}].[{filter.ColumnName}]";
            
            return filter.Operator.ToUpper() switch
            {
                "=" => $"{columnName} = {paramName}",
                "!=" => $"{columnName} != {paramName}",
                ">" => $"{columnName} > {paramName}",
                "<" => $"{columnName} < {paramName}",
                ">=" => $"{columnName} >= {paramName}",
                "<=" => $"{columnName} <= {paramName}",
                "LIKE" => $"{columnName} LIKE {paramName}",
                "NOT LIKE" => $"{columnName} NOT LIKE {paramName}",
                "IN" => $"{columnName} IN ({paramName})",
                "NOT IN" => $"{columnName} NOT IN ({paramName})",
                "IS NULL" => $"{columnName} IS NULL",
                "IS NOT NULL" => $"{columnName} IS NOT NULL",
                _ => throw new ArgumentException($"不支持的操作符: {filter.Operator}")
            };
        }

        /// <summary>
        /// SQL安全性验证
        /// </summary>
        private void ValidateSqlSafety(string sql)
        {
            // 检查危险关键字
            var words = Regex.Split(sql, @"\W+", RegexOptions.IgnoreCase)
                           .Where(w => !string.IsNullOrWhiteSpace(w));

            foreach (var word in words)
            {
                if (_dangerousSqlKeywords.Contains(word))
                {
                    throw new SecurityException($"SQL包含危险关键字: {word}");
                }
            }

            // 检查SQL注入特征
            var patterns = new[]
            {
                @";\s*(drop|delete|update|insert)",
                @"union\s+select",
                @"\/\*.*\*\/",
                @"--",
                @"xp_",
                @"sp_password"
            };

            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase))
                {
                    throw new SecurityException("检测到SQL注入攻击尝试");
                }
            }
        }

        /// <summary>
        /// 验证用户表访问权限
        /// </summary>
        private async Task<bool> ValidateUserTableAccess(string userId, IEnumerable<string> tables)
        {
            // 缓存权限检查结果
            var cacheKey = $"user_table_access_{userId}";
            if (!_cache.TryGetValue(cacheKey, out HashSet<string> allowedTables))
            {
                allowedTables = await GetUserAllowedTables(userId);
                _cache.Set(cacheKey, allowedTables, TimeSpan.FromMinutes(10));
            }

            return tables.All(table => allowedTables.Contains(table));
        }

        private async Task<HashSet<string>> GetUserAllowedTables(string userId)
        {
            try
            {
                // 检查是否是管理员
                if (await _userService.HasPermissionAsync(userId, SystemPermissions.SystemAdmin) ||
                    await _userService.HasPermissionAsync(userId, SystemPermissions.ViewAllTables))
                {
                    // 返回所有非系统表
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();
                    var tables = await GetAllDatabaseTables(connection);
                    return new HashSet<string>(tables.Where(t => !IsSystemTable(t)));
                }

                // 返回用户明确授权的表
                var allowedTableNames = await _dbContext.AllowedTables
                    .Where(at => at.UserId == userId && at.CanRead)
                    .Select(at => at.TableName)
                    .ToListAsync();

                return new HashSet<string>(allowedTableNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户 {UserId} 的表权限时发生错误", userId);
                return new HashSet<string>();
            }
        }

        private async Task<List<string>> GetAllDatabaseTables(SqlConnection connection)
        {
            var tables = new List<string>();
            var sql = @"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo'
                UNION
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.VIEWS 
                WHERE TABLE_SCHEMA = 'dbo'";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }

        private bool IsSystemTable(string tableName)
        {
            var systemTables = new[]
            {
                "__EFMigrationsHistory", "AllowedTables", "ColumnInfos", 
                "DatabaseConnections", "QueryShares", "RoleClaims", 
                "Roles", "SavedQueries", "TableInfos", "UserRoles", "Users"
            };

            return systemTables.Contains(tableName, StringComparer.OrdinalIgnoreCase);
        }
    }

    // 查询请求模型
    public class QueryRequest
    {
        public List<string> Tables { get; set; } = new();
        public List<ColumnSelection> Columns { get; set; } = new();
        public List<JoinCondition> Joins { get; set; } = new();
        public List<FilterCondition> Filters { get; set; } = new();
        public List<OrderByClause> OrderBy { get; set; } = new();
        public bool IsDistinct { get; set; }
        public int? TopCount { get; set; }
    }

    public class ColumnSelection
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string Alias { get; set; }
    }

    public class JoinCondition
    {
        public string JoinType { get; set; } // INNER, LEFT, RIGHT, FULL
        public string LeftTable { get; set; }
        public string LeftColumn { get; set; }
        public string RightTable { get; set; }
        public string RightColumn { get; set; }
    }

    public class FilterCondition
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        public string LogicalOperator { get; set; } = "AND"; // AND, OR
    }

    public class OrderByClause
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string Direction { get; set; } = "ASC"; // ASC, DESC
    }

    public interface IQueryBuilderService
    {
        Task<DataTable> ExecuteSecureQueryAsync(string userId, QueryRequest request);
    }

    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
    }
} 