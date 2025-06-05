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

        // SQL�ؼ��ְ����� - ֻ�����ѯ��صĹؼ���
        private static readonly HashSet<string> _allowedSqlKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "AND", "OR", "ORDER", "BY", "GROUP", "HAVING", 
            "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "ON", "AS", "DISTINCT",
            "TOP", "ASC", "DESC", "IN", "NOT", "LIKE", "BETWEEN", "IS", "NULL",
            "COUNT", "SUM", "AVG", "MIN", "MAX", "CASE", "WHEN", "THEN", "ELSE", "END"
        };

        // Σ�յ�SQL�ؼ��ֺ�����
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
        /// ��ȫ��SQL��ѯִ�� - ʹ�ò�������ѯ��ֹSQLע��
        /// </summary>
        public async Task<DataTable> ExecuteSecureQueryAsync(string userId, QueryRequest request)
        {
            try
            {
                // 1. ��֤�û�Ȩ��
                if (!await ValidateUserTableAccess(userId, request.Tables))
                {
                    throw new UnauthorizedAccessException("�û�û�з���ָ�����Ȩ��");
                }

                // 2. ������ȫ�Ĳ�����SQL
                var (sql, parameters) = BuildParameterizedQuery(request);
                
                // 3. SQL��ȫ���
                ValidateSqlSafety(sql);

                // 4. ��¼��ѯ��־
                _logger.LogInformation("�û� {UserId} ִ�в�ѯ: {Sql}", userId, sql);

                // 5. ִ�в�ѯ
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new SqlCommand(sql, connection);
                command.CommandTimeout = 30; // ���ó�ʱʱ��
                
                // ��Ӳ���
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }

                using var adapter = new SqlDataAdapter(command);
                var dataTable = new DataTable();
                
                // ���Ʒ���������ֹ�ڴ����
                adapter.Fill(dataTable);
                
                if (dataTable.Rows.Count > 10000)
                {
                    _logger.LogWarning("��ѯ���ش�������: {RowCount} ��", dataTable.Rows.Count);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ִ�в�ѯʱ���������û�: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// ����������SQL��ѯ
        /// </summary>
        private (string sql, List<SqlParameter> parameters) BuildParameterizedQuery(QueryRequest request)
        {
            var sql = new StringBuilder();
            var parameters = new List<SqlParameter>();
            var parameterIndex = 0;

            // ����SELECT�Ӿ�
            sql.Append("SELECT ");
            if (request.IsDistinct)
                sql.Append("DISTINCT ");

            if (request.TopCount.HasValue)
                sql.Append($"TOP {request.TopCount} ");

            sql.AppendJoin(", ", request.Columns.Select(c => $"[{c.TableName}].[{c.ColumnName}]"));

            // ����FROM�Ӿ�
            sql.Append($" FROM [{request.Tables.First()}] ");

            // ����JOIN�Ӿ�
            foreach (var join in request.Joins)
            {
                sql.Append($"{join.JoinType} JOIN [{join.RightTable}] ON ");
                sql.Append($"[{join.LeftTable}].[{join.LeftColumn}] = [{join.RightTable}].[{join.RightColumn}] ");
            }

            // ����WHERE�Ӿ�
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

            // ����ORDER BY�Ӿ�
            if (request.OrderBy.Any())
            {
                sql.Append(" ORDER BY ");
                sql.AppendJoin(", ", request.OrderBy.Select(o => 
                    $"[{o.TableName}].[{o.ColumnName}] {o.Direction}"));
            }

            return (sql.ToString(), parameters);
        }

        /// <summary>
        /// ������ȫ�Ĺ��˱��ʽ
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
                _ => throw new ArgumentException($"��֧�ֵĲ�����: {filter.Operator}")
            };
        }

        /// <summary>
        /// SQL��ȫ����֤
        /// </summary>
        private void ValidateSqlSafety(string sql)
        {
            // ���Σ�չؼ���
            var words = Regex.Split(sql, @"\W+", RegexOptions.IgnoreCase)
                           .Where(w => !string.IsNullOrWhiteSpace(w));

            foreach (var word in words)
            {
                if (_dangerousSqlKeywords.Contains(word))
                {
                    throw new SecurityException($"SQL����Σ�չؼ���: {word}");
                }
            }

            // ���SQLע������
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
                    throw new SecurityException("��⵽SQLע�빥������");
                }
            }
        }

        /// <summary>
        /// ��֤�û������Ȩ��
        /// </summary>
        private async Task<bool> ValidateUserTableAccess(string userId, IEnumerable<string> tables)
        {
            // ����Ȩ�޼����
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
                // ����Ƿ��ǹ���Ա
                if (await _userService.HasPermissionAsync(userId, SystemPermissions.SystemAdmin) ||
                    await _userService.HasPermissionAsync(userId, SystemPermissions.ViewAllTables))
                {
                    // �������з�ϵͳ��
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();
                    var tables = await GetAllDatabaseTables(connection);
                    return new HashSet<string>(tables.Where(t => !IsSystemTable(t)));
                }

                // �����û���ȷ��Ȩ�ı�
                var allowedTableNames = await _dbContext.AllowedTables
                    .Where(at => at.UserId == userId && at.CanRead)
                    .Select(at => at.TableName)
                    .ToListAsync();

                return new HashSet<string>(allowedTableNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ�û� {UserId} �ı�Ȩ��ʱ��������", userId);
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

    // ��ѯ����ģ��
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