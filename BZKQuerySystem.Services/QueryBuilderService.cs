using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using BZKQuerySystem.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BZKQuerySystem.Services
{
    public class QueryBuilderService
    {
        private readonly BZKQueryDbContext _dbContext;
        private readonly string _connectionString;
        private readonly UserService? _userService;
        // 第二阶段优化：集成实时通信服务
        private readonly IRealTimeNotificationService? _notificationService;

        // 系统表名列表，这些表不应该向前端显示
        private static readonly List<string> _systemTables = new List<string>
        {
            "__EFMigrationsHistory",
            "AllowedTables",
            "ColumnInfos",
            "DatabaseConnections",
            "QueryShares",
            "RoleClaims",
            "Roles",
            "SavedQueries",
            "TableInfos",
            "UserRoles",
            "Users"
        };

        public QueryBuilderService(
            BZKQueryDbContext dbContext,
            string connectionString,
            UserService? userService = null,
            IRealTimeNotificationService? notificationService = null)
        {
            _dbContext = dbContext;
            _connectionString = connectionString;
            _userService = userService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// 获取数据库中的所有表
        /// </summary>
        public async Task<List<TableInfo>> GetAllTablesAsync()
        {
            try
            {
                return await _dbContext.TableInfos
                    .Include(t => t.Columns)
                    .Where(t => !_systemTables.Contains(t.TableName)) // 系统表
                    .OrderBy(t => t.TableName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // 打印异常信息
                Console.WriteLine($"获取信息失败: {ex.Message}");
                return new List<TableInfo>();
            }
        }

        /// <summary>
        /// 获取用户允许查看的表
        /// </summary>
        public async Task<List<TableInfo>> GetAllowedTablesForUserAsync(string userId)
        {
            try
            {
                // 用户是否为管理员或可以查看所有表
                if (_userService != null)
                {
                    try
                    {
                        bool isAdmin = await _userService.HasPermissionAsync(userId, SystemPermissions.SystemAdmin);
                        bool canViewAllTables = await _userService.HasPermissionAsync(userId, SystemPermissions.ViewAllTables);

                        if (isAdmin || canViewAllTables)
                        {
                            // 通过直接SQL查询获取表
                            using (var connection = new SqlConnection(_connectionString))
                            {
                                await connection.OpenAsync();
                                var tables = await GetDatabaseTablesAsync(connection);
                                var views = await GetDatabaseViewsAsync(connection);
                                var result = new List<TableInfo>();

                                // 添加表
                                foreach (var tableName in tables)
                                {
                                    result.Add(new TableInfo
                                    {
                                        TableName = tableName,
                                        DisplayName = tableName,
                                        Description = $"数据表: {tableName}",
                                        IsView = false,
                                        Columns = new List<ColumnInfo>()
                                    });
                                }

                                // 添加视图
                                foreach (var viewName in views)
                                {
                                    result.Add(new TableInfo
                                    {
                                        TableName = viewName,
                                        DisplayName = viewName,
                                        Description = $"预览视图: {viewName}",
                                        IsView = true,
                                        Columns = new List<ColumnInfo>()
                                    });
                                }

                                return result;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"权限获取失败: {ex.Message}");
                        /* 权限获取中可能出现的异常 */
                    }
                }

                try
                {
                    // 获取用户允许的表
                    var allowedTableNames = await _dbContext.AllowedTables
                        .Where(at => at.UserId == userId && at.CanRead && !_systemTables.Contains(at.TableName)) // 系统表
                        .Select(at => at.TableName)
                        .ToListAsync();

                    // 用户没有权限时，返回空列表
                    if (allowedTableNames == null || !allowedTableNames.Any())
                    {
                        // 用户没有权限时，返回空列表
                        Console.WriteLine($"用户 {userId} 没有权限");
                        return new List<TableInfo>();
                    }

                    // 获取表的详细信息
                    try
                    {
                        return await _dbContext.TableInfos
                            .Include(t => t.Columns)
                            .Where(t => allowedTableNames.Contains(t.TableName) && !_systemTables.Contains(t.TableName)) // 系统表
                            .OrderBy(t => t.TableName)
                            .ToListAsync();
                    }
                    catch
                    {
                        // TableInfos可能不存在，需要确认表是否为视图
                        var result = new List<TableInfo>();

                        // 使用数据库连接直接查询系统表
                        using (var connection = new SqlConnection(_connectionString))
                        {
                            await connection.OpenAsync();

                            // 获取数据库中的表
                            var allTables = await GetDatabaseTablesAsync(connection);
                            // 获取数据库中的视图
                            var allViews = await GetDatabaseViewsAsync(connection);

                            foreach (var tableName in allowedTableNames)
                            {
                                // 系统表
                                if (!_systemTables.Contains(tableName))
                                {
                                    // 确认是否为视图
                                    bool isView = allViews.Contains(tableName);

                                    result.Add(new TableInfo
                                    {
                                        TableName = tableName,
                                        DisplayName = tableName,
                                        Description = isView ? $"预览视图: {tableName}" : $"数据表: {tableName}",
                                        IsView = isView,
                                        Columns = new List<ColumnInfo>()
                                    });
                                }
                            }
                        }
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"获取用户权限失败: {ex.Message}");
                    // 权限查询失败，返回空列表
                    // 默认返回空列表
                    Console.WriteLine("权限查询失败，返回空列表");
                    return new List<TableInfo>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllowedTablesForUserAsync失败: {ex.Message}");
                // 可能出现的异常
                return new List<TableInfo>();
            }
        }

        /// <summary>
        /// 获取表的列信息
        /// </summary>
        public async Task<List<ColumnInfo>> GetColumnsForTableAsync(string tableName)
        {
            try
            {
                // 获取表的详细信息
                var table = await _dbContext.TableInfos
                    .Include(t => t.Columns)
                    .FirstOrDefaultAsync(t => t.TableName == tableName);

                if (table != null && table.Columns != null && table.Columns.Any())
                {
                    return table.Columns.OrderBy(c => c.ColumnName).ToList();
                }

                // 表不存在，直接从数据库查询
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var columns = await GetTableColumnsAsync(connection, tableName);

                    // 转换为ColumnInfo
                    var result = new List<ColumnInfo>();
                    int tempId = 1; // 临时ID从1开始

                    foreach (var column in columns)
                    {
                        result.Add(new ColumnInfo
                        {
                            Id = tempId++, // 临时ID
                            TableId = 0, // 临时值
                            ColumnName = column.ColumnName,
                            DisplayName = column.ColumnName,
                            DataType = column.DataType,
                            IsPrimaryKey = column.IsPrimaryKey,
                            IsNullable = column.IsNullable,
                            Description = $": {column.ColumnName}"
                        });
                    }

                    // 可能出现的异常
                    try
                    {
                        if (columns.Any())
                        {
                            // 添加表信息
                            var tableInfo = new TableInfo
                            {
                                TableName = tableName,
                                DisplayName = tableName,
                                Description = $"数据表: {tableName}"
                            };

                            _dbContext.TableInfos.Add(tableInfo);
                            await _dbContext.SaveChangesAsync();

                            // 更新表信息ID
                            foreach (var columnInfo in result)
                            {
                                columnInfo.TableId = tableInfo.Id;
                                _dbContext.ColumnInfos.Add(columnInfo);
                            }

                            await _dbContext.SaveChangesAsync();

                            // 获取表信息
                            return await _dbContext.ColumnInfos
                                .Where(c => c.TableId == tableInfo.Id)
                                .OrderBy(c => c.ColumnName)
                                .ToListAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"获取列信息失败: {ex.Message}");
                        // 使用临时信息
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取{tableName}信息失败: {ex.Message}");
                return new List<ColumnInfo>();
            }
        }

        /// <summary>
        /// 执行数据库查询
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(
            List<string> tables,
            List<string> columns,
            List<string> joinConditions,
            List<string> whereConditions,
            List<string> orderBy,
            Dictionary<string, object> parameters,
            int? pageSize = null,
            int? pageNumber = null)
        {
            // 生成查询ID用于实时通知
            string queryId = Guid.NewGuid().ToString();
            string userId = _userService?.GetCurrentUserId() ?? "anonymous";

            try
            {
                // 第二阶段优化：发送查询开始通知
                if (_notificationService != null)
                {
                    await _notificationService.SendQueryProgressAsync(userId, new QueryProgressUpdate
                    {
                        QueryId = queryId,
                        Status = "开始执行",
                        ProgressPercentage = 0,
                        Message = $"开始执行查询，涉及 {tables.Count} 个表"
                    });
                }

                if (tables == null || tables.Count == 0)
                {
                    throw new ArgumentException("必须提供至少一个表");
                }

                // 第二阶段优化：发送SQL构建进度
                if (_notificationService != null)
                {
                    await _notificationService.SendQueryProgressAsync(userId, new QueryProgressUpdate
                    {
                        QueryId = queryId,
                        Status = "构建SQL",
                        ProgressPercentage = 25,
                        Message = "正在构建SQL查询语句"
                    });
                }

                // 构建SQL查询
                string sqlQuery = BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                // 处理分页 - OFFSET/FETCH 需要 ORDER BY 子句
                if (pageSize.HasValue && pageNumber.HasValue && pageSize.Value > 0 && pageNumber.Value > 0)
                {
                    // 检查是否有ORDER BY子句，如果没有则添加默认排序
                    if (orderBy == null || orderBy.Count == 0)
                    {
                        // 为分页添加默认排序（使用第一个表的主键或第一列）
                        Console.WriteLine("分页查询缺少ORDER BY子句，添加默认排序");

                        // 尝试使用表的第一列作为排序字段
                        string defaultOrderColumn = $"[{tables[0]}].[{GetFirstColumnOfTable(tables[0])}]";
                        sqlQuery += $"\r\nORDER BY {defaultOrderColumn}";
                        Console.WriteLine($"使用默认排序字段: {defaultOrderColumn}");
                    }

                    int offset = (pageNumber.Value - 1) * pageSize.Value;
                    sqlQuery += $"\r\nOFFSET {offset} ROWS FETCH NEXT {pageSize.Value} ROWS ONLY";

                    Console.WriteLine($"添加分页: OFFSET {offset} ROWS FETCH NEXT {pageSize.Value} ROWS ONLY");
                }

                // 第二阶段优化：发送执行前准备完成通知
                if (_notificationService != null)
                {
                    await _notificationService.SendQueryProgressAsync(userId, new QueryProgressUpdate
                    {
                        QueryId = queryId,
                        Status = "准备执行",
                        ProgressPercentage = 50,
                        Message = "SQL语句构建完成，准备执行查询"
                    });
                }

                // 记录查询开始时间
                var startTime = DateTime.Now;

                // 执行查询
                var dataTable = await ExecuteSqlQueryAsync(sqlQuery, parameters);

                // 计算执行时间
                var executionTime = DateTime.Now - startTime;

                // 第二阶段优化：发送查询完成通知
                if (_notificationService != null)
                {
                    await _notificationService.SendQueryCompletedAsync(userId, new QueryCompletionResult
                    {
                        QueryId = queryId,
                        IsSuccess = true,
                        RecordCount = dataTable.Rows.Count,
                        ExecutionTime = executionTime,
                        CompletedAt = DateTime.Now
                    });
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                // 第二阶段优化：发送查询失败通知
                if (_notificationService != null)
                {
                    await _notificationService.SendQueryCompletedAsync(userId, new QueryCompletionResult
                    {
                        QueryId = queryId,
                        IsSuccess = false,
                        RecordCount = 0,
                        ExecutionTime = TimeSpan.Zero,
                        ErrorMessage = ex.Message,
                        CompletedAt = DateTime.Now
                    });
                }

                throw;
            }
        }

        /// <summary>
        /// 获取查询总数
        /// </summary>
        /// <param name="tables">表列表</param>
        /// <param name="columns">列列表</param>
        /// <param name="joinConditions">JOIN条件</param>
        /// <param name="whereConditions">WHERE条件</param>
        /// <param name="parameters">查询参数</param>
        /// <returns>查询结果</returns>
        public async Task<int> GetTotalRowsAsync(
            List<string> tables,
            List<string> columns,
            List<string> joinConditions,
            List<string> whereConditions,
            Dictionary<string, object> parameters)
        {
            try
            {
                // 详细的调试信息
                Console.WriteLine("=== GetTotalRowsAsync 调试信息 ===");
                Console.WriteLine($"Tables: {string.Join(", ", tables ?? new List<string>())}");
                Console.WriteLine($"Columns: {string.Join(", ", columns ?? new List<string>())}");
                Console.WriteLine($"JoinConditions: {string.Join(" | ", joinConditions ?? new List<string>())}");
                Console.WriteLine($"WhereConditions: {string.Join(" | ", whereConditions ?? new List<string>())}");
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        Console.WriteLine($"Parameter: {param.Key} = {param.Value}");
                    }
                }
                Console.WriteLine("================================");

                // 使用SQL构建COUNT查询
                string countSql = "SELECT COUNT(*) FROM " + FormatSqlIdentifier(tables[0]);

                // JOIN部分
                if (joinConditions != null && joinConditions.Count > 0)
                {
                    foreach (var joinCondition in joinConditions)
                    {
                        try
                        {
                            string formattedJoinCondition = ProcessJoinConditionForCount(joinCondition);
                            if (!string.IsNullOrEmpty(formattedJoinCondition))
                            {
                                countSql += " " + formattedJoinCondition;
                            }
                        }
                        catch (Exception joinEx)
                        {
                            Console.WriteLine($"处理JOIN条件失败: {joinCondition}, 错误: {joinEx.Message}");
                            // 跳过有问题的JOIN条件
                            continue;
                        }
                    }
                }

                // 添加WHERE条件
                if (whereConditions != null && whereConditions.Count > 0)
                {
                    countSql += " WHERE ";

                    for (int i = 0; i < whereConditions.Count; i++)
                    {
                        try
                        {
                            if (i > 0)
                            {
                                // 添加连接符
                                string connector = ExtractConnectorFromCondition(whereConditions[i]);
                                if (string.IsNullOrEmpty(connector))
                                {
                                    connector = "AND"; // 默认使用AND
                                }
                                countSql += $" {connector} ";
                            }

                            // 处理WHERE条件
                            string processedCondition = ProcessWhereCondition(whereConditions[i]);
                            countSql += processedCondition;
                        }
                        catch (Exception whereEx)
                        {
                            Console.WriteLine($"处理WHERE条件失败: {whereConditions[i]}, 错误: {whereEx.Message}");
                            // 跳过有问题的WHERE条件
                            continue;
                        }
                    }
                }

                // 输出调试信息
                Console.WriteLine($"生成的COUNT SQL: {countSql}");

                // 执行查询
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(countSql, connection))
                    {
                        // 添加参数
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                try
                                {
                                    string paramName = param.Key;

                                    // 确保参数名格式正确
                                    if (!paramName.StartsWith("@"))
                                    {
                                        paramName = "@" + paramName;
                                    }

                                    // 清理参数名中的特殊字符
                                    paramName = paramName.Replace(" ", "_")
                                                      .Replace("#", "_")
                                                      .Replace("%", "_")
                                                      .Replace("$", "_")
                                                      .Replace("|", "_")
                                                      .Replace("-", "_");

                                    // 确保只有一个@前缀
                                    if (paramName.StartsWith("@@"))
                                    {
                                        paramName = paramName.Substring(1);
                                    }

                                    command.Parameters.AddWithValue(paramName, param.Value ?? DBNull.Value);
                                    Console.WriteLine($"添加参数: {paramName} = {param.Value}");
                                }
                                catch (Exception paramEx)
                                {
                                    Console.WriteLine($"添加参数失败: {param.Key} = {param.Value}, 错误: {paramEx.Message}");
                                }
                            }
                        }

                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取总行数失败: {ex.Message}");
                Console.WriteLine($"错误堆栈: {ex.StackTrace}");
                return -1;
            }
        }

        /// <summary>
        /// 获取表的第一列名称（用于默认排序）
        /// </summary>
        private string GetFirstColumnOfTable(string tableName)
        {
            try
            {
                // 尝试从缓存的表信息中获取第一列
                var table = _dbContext.TableInfos
                    .Include(t => t.Columns)
                    .FirstOrDefault(t => t.TableName == tableName);

                if (table?.Columns?.Any() == true)
                {
                    var firstColumn = table.Columns.OrderBy(c => c.ColumnName).FirstOrDefault();
                    if (firstColumn != null)
                    {
                        return firstColumn.ColumnName;
                    }
                }

                // 如果没有找到，使用通用的第一列名
                // 可以通过直接查询数据库获取
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand($@"
                        SELECT TOP 1 COLUMN_NAME
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = @tableName
                        ORDER BY ORDINAL_POSITION", connection))
                    {
                        command.Parameters.AddWithValue("@tableName", tableName);
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            return result.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取表 {tableName} 第一列失败: {ex.Message}");
            }

            // 如果都失败了，返回一个通用的列名
            return "1"; // 使用列序号作为后备选项
        }

        /// <summary>
        /// 格式化SQL标识符
        /// 使用正确的格式化方式来处理SQL中的标识符
        /// </summary>
        private string FormatSqlIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            identifier = identifier.Trim();

            // 检查是否已经是正确格式的标识符
            if (identifier.StartsWith("[") && identifier.EndsWith("]"))
            {
                return identifier;
            }

            // 去掉现有的方括号
            identifier = identifier.Replace("[", string.Empty).Replace("]", string.Empty);

            if (identifier.Contains('.'))
            {
                var parts = identifier.Split(new[] { '.' }, 2);
                string part1 = parts[0].Trim();
                string part2 = parts[1].Trim();

                // 检查是否两部分都有内容
                if (!string.IsNullOrEmpty(part1) && !string.IsNullOrEmpty(part2))
                {
                    return $"[{part1}].[{part2}]";
                }
                else if (!string.IsNullOrEmpty(part2)) // 如果只有第二部分（例如 ".ColumnName"）
                {
                    return $"[{part2}]";
                }
                else if (!string.IsNullOrEmpty(part1)) // 如果只有第一部分（例如 "TableName."）
                {
                    return $"[{part1}]";
                }
                else
                {
                    // 如果是空的点，返回错误标识符
                    throw new ArgumentException($"无效的SQL标识符: '{identifier}'");
                }
            }

            // 如果没有点，则直接格式化
            return $"[{identifier}]";
        }

        /// <summary>
        /// 构建SQL查询
        /// </summary>
        public string BuildSqlQuery(
            List<string> tables,
            List<string> columns, // 原始表名和列名格式为 "Table.Column" 或 "Column"
            List<string> joinConditions, // JOIN条件格式为JOIN ... ON ...
            List<string> whereConditions, // WHERE条件格式为WHERE ...
            List<string> orderBy) // ORDER BY条件格式为ORDER BY ...
        {
            if (tables == null || tables.Count == 0)
            {
                throw new ArgumentException("必须提供至少一个表");
            }

            var sqlBuilder = new StringBuilder();

            // SELECT部分
            sqlBuilder.Append("SELECT ");

            // 如果columns为空，则使用SELECT *
            if (columns == null || columns.Count == 0)
            {
                sqlBuilder.Append("*");
            }
            else
            {
                var selectColumnExpressions = new List<string>();

                // 检查是否为相同列
                var columnShortNames = new Dictionary<string, int>();
                foreach (var colIdentifier in columns)
                {
                    string shortName = colIdentifier.Contains('.') ? colIdentifier.Split(new[] { '.' }, 2).Last() : colIdentifier;
                    columnShortNames[shortName] = columnShortNames.ContainsKey(shortName) ? columnShortNames[shortName] + 1 : 1;
                }

                foreach (var colIdentifier in columns) // 例如 "Table.125j_选择"
                {
                    string actualIdentifier = colIdentifier;
                    string shortName = colIdentifier.Contains('.') ? colIdentifier.Split(new[] { '.' }, 2).Last() : colIdentifier;

                    // 格式化SQL标识符
                    string alias;
                    if (tables.Count > 1)
                    {
                        // 使用下划线作为别名前缀
                        if (colIdentifier.Contains('.'))
                        {
                            // 使用原始别名
                            string rawAlias = colIdentifier.Replace('.', '_');
                            alias = $"[{rawAlias}]";
                        }
                        else
                        {
                            // 使用第一个表名作为前缀
                            // 使用下划线作为分隔符
                            string rawAlias = $"{tables[0]}_{colIdentifier}";
                            alias = $"[{rawAlias}]";
                        }
                    }
                    else if (columnShortNames[shortName] > 1 && colIdentifier.Contains('.'))
                    {
                        // 使用相同列名作为别名
                        string rawAlias = colIdentifier.Replace('.', '_');
                        alias = $"[{rawAlias}]";
                    }
                    else
                    {
                        // 使用原始列名作为别名
                        alias = $"[{shortName}]";
                    }

                    // 使用FormatSqlIdentifier格式化SQL标识符
                    selectColumnExpressions.Add($"{FormatSqlIdentifier(actualIdentifier)} AS {alias}");
                }
                sqlBuilder.Append(string.Join(", ", selectColumnExpressions));
            }
            sqlBuilder.Append("\r\n");

            // FROM部分
            sqlBuilder.Append("FROM ");
            sqlBuilder.Append(FormatSqlIdentifier(tables[0])); // 表名
            sqlBuilder.Append("\r\n");

            // JOIN部分
            if (joinConditions != null && joinConditions.Count > 0)
            {
                foreach (var joinCondition in joinConditions)
                {
                    // 确定JOIN条件，使用正确的格式
                    string formattedJoinCondition = ProcessJoinConditionForCount(joinCondition);
                    sqlBuilder.Append(formattedJoinCondition);
                    sqlBuilder.Append("\r\n");
                }
            }

            // WHERE部分
            if (whereConditions != null && whereConditions.Count > 0)
            {
                sqlBuilder.Append("WHERE ");

                // 处理第一个条件
                var processedWhereConditions = whereConditions.Select(ProcessWhereCondition).ToList();

                if (processedWhereConditions.Count > 0)
                {
                    sqlBuilder.Append(processedWhereConditions[0]);

                    // 使用动态条件
                    for (int i = 1; i < processedWhereConditions.Count; i++)
                    {
                        // 提取当前条件和前一个条件的连接符
                        string connector = ExtractConnectorFromCondition(whereConditions[i]);
                        sqlBuilder.Append($" {connector} ");
                        sqlBuilder.Append(processedWhereConditions[i]);
                    }
                }

                sqlBuilder.Append("\r\n");
            }

            // ORDER BY部分
            if (orderBy != null && orderBy.Count > 0)
            {
                sqlBuilder.Append("ORDER BY ");
                // ProcessOrderByCondition 函数处理ORDER BY条件
                var processedOrderBy = orderBy.Select(ProcessOrderByCondition).ToList();
                sqlBuilder.Append(string.Join(", ", processedOrderBy));
                sqlBuilder.Append("\r\n");
            }

            return sqlBuilder.ToString();
        }

        /// <summary>
        /// 处理WHERE条件中的标识符
        /// </summary>
        private string ProcessWhereCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return condition;

            try
            {
                // 如果是JSON格式，尝试提取SQL部分
                if (condition.StartsWith("{") && condition.Contains("\"sql\""))
                {
                    using (var document = System.Text.Json.JsonDocument.Parse(condition))
                    {
                        var root = document.RootElement;
                        if (root.TryGetProperty("sql", out var sqlElement))
                        {
                            var sqlCondition = sqlElement.GetString();
                            if (!string.IsNullOrEmpty(sqlCondition))
                            {
                                return sqlCondition; // 直接返回SQL条件
                            }
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // JSON解析失败，使用原始的SQL语句
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理WHERE条件失败: {condition}, 错误: {ex.Message}");
            }

            // 直接返回原始条件
            return condition;
        }

        /// <summary>
        /// 提取WHERE条件中的连接符
        /// </summary>
        private string ExtractConnectorFromCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return "AND";

            // 使用JSON格式处理
            try
            {
                // 提取connector信息
                if (condition.Contains("\"connector\"") || condition.StartsWith("{"))
                {
                    using (var document = System.Text.Json.JsonDocument.Parse(condition))
                    {
                        var root = document.RootElement;
                        if (root.TryGetProperty("connector", out var connectorElement))
                        {
                            var connector = connectorElement.GetString();
                            return string.IsNullOrEmpty(connector) ? "AND" : connector.ToUpper();
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // JSON解析失败，使用原始的SQL语句
            }
            catch (Exception)
            {
                // 出现异常，使用原始的SQL语句
            }

            // 默认使用AND
            return "AND";
        }

        /// <summary>
        /// 处理ORDER BY条件中的标识符
        /// </summary>
        private string ProcessOrderByCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return condition;

            // 检查是否为ASC或DESC
            bool hasDirection = condition.EndsWith(" ASC", StringComparison.OrdinalIgnoreCase) ||
                               condition.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase);

            if (hasDirection)
            {
                // 提取字段和方向
                int lastSpaceIndex = condition.LastIndexOf(' ');
                string field = condition.Substring(0, lastSpaceIndex).Trim();
                string direction = condition.Substring(lastSpaceIndex).Trim();

                // 格式化SQL标识符
                return FormatSqlIdentifier(field) + " " + direction;
            }

            // 如果没有选择列，则返回原始的SQL标识符
            return FormatSqlIdentifier(condition);
        }

        /// <summary>
        /// 构建JOIN条件
        /// </summary>
        public string BuildJoinCondition(string joinType, string table, string onCondition)
        {
            if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(onCondition))
            {
                throw new ArgumentException("JOIN条件不能为空");
            }

            // 检查JOIN类型
            string validJoinType;
            switch (joinType.ToUpper())
            {
                case "INNER":
                    validJoinType = "INNER JOIN";
                    break;
                case "LEFT":
                    validJoinType = "LEFT JOIN";
                    break;
                case "RIGHT":
                    validJoinType = "RIGHT JOIN";
                    break;
                case "FULL":
                    validJoinType = "FULL JOIN";
                    break;
                default:
                    validJoinType = "INNER JOIN";
                    break;
            }

            // 格式化表名
            string formattedTable = FormatSqlIdentifier(table);

            // 格式化ON条件
            string formattedOnCondition = ProcessJoinOnCondition(onCondition);

            return $"{validJoinType} {formattedTable} ON {formattedOnCondition}";
        }

        /// <summary>
        /// 处理JOIN条件中的ON部分
        /// </summary>
        private string ProcessJoinOnCondition(string onCondition)
        {
            if (string.IsNullOrEmpty(onCondition))
                return onCondition;

            // 使用JSON格式处理
            // 使用JSON格式处理
            var comparisonOperators = new[] { ">=", "<=", "<>", "!=", "=", ">", "<" };

            foreach (var op in comparisonOperators)
            {
                int opIndex = onCondition.IndexOf($" {op} ");
                if (opIndex > 0)
                {
                    // 提取左边部分和右边部分
                    string leftPart = onCondition.Substring(0, opIndex).Trim();
                    string rightPart = onCondition.Substring(opIndex + op.Length + 2).Trim(); // +2 for spaces around operator

                    // 格式化左边部分
                    string formattedLeftPart = FormatSqlIdentifier(leftPart);
                    string formattedRightPart = FormatSqlIdentifier(rightPart);

                    // 使用操作符
                    return $"{formattedLeftPart} {op} {formattedRightPart}";
                }
            }

            // 如果为空，则返回原始的SQL标识符
            foreach (var op in comparisonOperators)
            {
                int opIndex = onCondition.IndexOf(op);
                if (opIndex > 0)
                {
                    // 提取左边部分和右边部分
                    string leftPart = onCondition.Substring(0, opIndex).Trim();
                    string rightPart = onCondition.Substring(opIndex + op.Length).Trim();

                    // 格式化左边部分
                    string formattedLeftPart = FormatSqlIdentifier(leftPart);
                    string formattedRightPart = FormatSqlIdentifier(rightPart);

                    // 使用操作符
                    return $"{formattedLeftPart} {op} {formattedRightPart}";
                }
            }

            // 如果为空，则返回原始的SQL标识符
            return FormatSqlIdentifier(onCondition);
        }

        /// <summary>
        /// 为计数查询处理JOIN条件
        /// </summary>
        private string ProcessJoinConditionForCount(string joinCondition)
        {
            if (string.IsNullOrEmpty(joinCondition))
                return string.Empty;

            try
            {
                Console.WriteLine($"原始JOIN条件: {joinCondition}");

                // 使用更直接的方法处理JOIN条件
                // 示例输入: "INNER JOIN 19_住院诊断 ON 18_门诊诊断.患者主索引 = 19_住院诊断.患者主索引"

                string processedCondition = joinCondition;

                // 第一步：格式化JOIN后的表名
                if (processedCondition.Contains(" JOIN "))
                {
                    var joinParts = processedCondition.Split(new[] { " ON " }, 2, StringSplitOptions.None);
                    if (joinParts.Length == 2)
                    {
                        string joinPart = joinParts[0]; // "INNER JOIN 19_住院诊断"
                        string onPart = joinParts[1];   // "18_门诊诊断.患者主索引 = 19_住院诊断.患者主索引"

                        // 处理JOIN部分中的表名
                        var joinWords = joinPart.Split(' ');
                        for (int i = 0; i < joinWords.Length; i++)
                        {
                            if (joinWords[i].ToUpper() == "JOIN" && i + 1 < joinWords.Length)
                            {
                                // 格式化JOIN后面的表名
                                string tableName = joinWords[i + 1];
                                joinWords[i + 1] = FormatSqlIdentifier(tableName);
                                break;
                            }
                        }
                        joinPart = string.Join(" ", joinWords);

                        // 第二步：处理ON条件中的所有 table.column 格式
                        // 分割ON条件以找到所有的标识符
                        string[] operators = { " = ", " <> ", " != ", " > ", " < ", " >= ", " <= " };
                        string formattedOnPart = onPart;

                        foreach (var op in operators)
                        {
                            if (formattedOnPart.Contains(op))
                            {
                                var opParts = formattedOnPart.Split(new[] { op }, StringSplitOptions.None);
                                for (int i = 0; i < opParts.Length; i++)
                                {
                                    string part = opParts[i].Trim();
                                    if (part.Contains('.'))
                                    {
                                        // 格式化 table.column
                                        opParts[i] = FormatSqlIdentifier(part);
                                    }
                                }
                                formattedOnPart = string.Join(op, opParts);
                                break; // 只处理第一个找到的操作符
                            }
                        }

                        processedCondition = joinPart + " ON " + formattedOnPart;
                    }
                }

                Console.WriteLine($"处理后JOIN条件: {processedCondition}");
                return processedCondition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理JOIN条件失败: {joinCondition}, 错误: {ex.Message}");
                return joinCondition; // 返回原始条件
            }
        }

        /// <summary>
        /// 执行SQL查询
        /// </summary>
        private async Task<DataTable> ExecuteSqlQueryAsync(string sql, Dictionary<string, object> parameters)
        {
            // 确定SQL的结束部分，为WITH部分
            if (!sql.TrimEnd().EndsWith(";"))
            {
                sql = sql.TrimEnd() + ";";
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(sql, connection))
                {
                    // 添加参数
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            // 确定参数格式
                            string paramName = param.Key;
                            if (!paramName.StartsWith("@"))
                            {
                                paramName = "@" + paramName;
                            }

                            // 替换SQL中的特殊字符
                            paramName = paramName.Replace(" ", "_")
                                              .Replace("@", "_")
                                              .Replace("#", "_")
                                              .Replace("%", "_")
                                              .Replace("$", "_")
                                              .Replace("|", "_")
                                              .Replace("-", "_");

                            // 添加参数前缀@
                            if (!paramName.StartsWith("@"))
                            {
                                paramName = "@" + paramName;
                            }

                            command.Parameters.AddWithValue(paramName, param.Value ?? DBNull.Value);
                        }
                    }

                    // 执行查询
                    var dataTable = new DataTable();
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }

                    return dataTable;
                }
            }
        }

        /// <summary>
        /// 获取保存的查询
        /// </summary>
        public async Task<List<SavedQuery>> GetSavedQueriesAsync(string userId)
        {
            try
            {
                Console.WriteLine($"开始获取用户ID为{userId}的查询");

                var queries = new List<SavedQuery>();

                // 使用ADO.NET直接查询用户保存的查询，EF Core作为后备
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 获取用户保存的查询
                    var userQueryText = @"
                        SELECT Id, UserId, Name, Description, SqlQuery,
                               TablesIncluded, ColumnsIncluded, FilterConditions,
                               SortOrder, CreatedAt, UpdatedAt, CreatedBy, IsShared,
                               CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                                                WHERE TABLE_NAME = 'SavedQueries'
                                                AND COLUMN_NAME = 'JoinConditions')
                                    THEN JoinConditions
                                    ELSE '[]' END AS JoinConditions
                        FROM SavedQueries
                        WHERE UserId = @userId
                        ORDER BY UpdatedAt DESC";

                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(userQueryText, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    // JoinConditions是否存在
                                    bool hasJoinConditions = false;
                                    try
                                    {
                                        hasJoinConditions = reader.GetOrdinal("JoinConditions") >= 0;
                                        Console.WriteLine($"查询ID {reader.GetInt32(reader.GetOrdinal("Id"))}: JoinConditions存在: {hasJoinConditions}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"获取JoinConditions失败: {ex.Message}");
                                        hasJoinConditions = false;
                                    }

                                    var query = new SavedQuery
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                        UserId = reader.GetString(reader.GetOrdinal("UserId")),
                                        Name = reader.GetString(reader.GetOrdinal("Name")),
                                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                        SqlQuery = reader.GetString(reader.GetOrdinal("SqlQuery")),
                                        TablesIncluded = reader.GetString(reader.GetOrdinal("TablesIncluded")),
                                        ColumnsIncluded = reader.GetString(reader.GetOrdinal("ColumnsIncluded")),
                                        FilterConditions = reader.GetString(reader.GetOrdinal("FilterConditions")),
                                        SortOrder = reader.GetString(reader.GetOrdinal("SortOrder")),
                                        // 添加JoinConditions
                                        JoinConditions = hasJoinConditions && !reader.IsDBNull(reader.GetOrdinal("JoinConditions"))
                                            ? reader.GetString(reader.GetOrdinal("JoinConditions"))
                                            : "[]",
                                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                                        CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? "未知用户" : reader.GetString(reader.GetOrdinal("CreatedBy")),
                                        IsShared = reader.GetBoolean(reader.GetOrdinal("IsShared"))
                                    };

                                    Console.WriteLine($"获取查询: ID={query.Id}, 名称={query.Name}, 创建者={query.CreatedBy}, JoinConditions长度={query.JoinConditions?.Length ?? 0}");
                                    queries.Add(query);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"获取查询失败: {ex.Message}");
                                }
                            }
                        }
                    }

                    Console.WriteLine($"数据库中查询数量: {queries.Count}");

                    // 尝试获取共享查询
                    try
                    {
                        // 检查QueryShares是否存在
                        var commandText = @"
                            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QueryShares' AND TABLE_SCHEMA = 'dbo')
                            BEGIN
                                SELECT q.Id, q.UserId, q.Name, q.Description, q.SqlQuery,
                                       q.TablesIncluded, q.ColumnsIncluded, q.FilterConditions,
                                       q.SortOrder, q.CreatedAt, q.UpdatedAt, q.CreatedBy, q.IsShared,
                                       CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                                                        WHERE TABLE_NAME = 'SavedQueries'
                                                        AND COLUMN_NAME = 'JoinConditions')
                                            THEN q.JoinConditions
                                            ELSE '[]' END AS JoinConditions
                                FROM SavedQueries q
                                INNER JOIN QueryShares qs ON q.Id = qs.QueryId
                                WHERE qs.UserId = @userId
                                ORDER BY q.UpdatedAt DESC
                            END";

                        using (var command = new Microsoft.Data.SqlClient.SqlCommand(commandText, connection))
                        {
                            command.Parameters.AddWithValue("@userId", userId);
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    try
                                    {
                                        // JoinConditions是否存在
                                        bool hasJoinConditions = false;
                                        try
                                        {
                                            hasJoinConditions = reader.GetOrdinal("JoinConditions") >= 0;
                                        }
                                        catch
                                        {
                                            // 不存在，返回false
                                            hasJoinConditions = false;
                                        }

                                        var query = new SavedQuery
                                        {
                                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                            UserId = reader.GetString(reader.GetOrdinal("UserId")),
                                            Name = reader.GetString(reader.GetOrdinal("Name")),
                                            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                            SqlQuery = reader.GetString(reader.GetOrdinal("SqlQuery")),
                                            TablesIncluded = reader.GetString(reader.GetOrdinal("TablesIncluded")),
                                            ColumnsIncluded = reader.GetString(reader.GetOrdinal("ColumnsIncluded")),
                                            FilterConditions = reader.GetString(reader.GetOrdinal("FilterConditions")),
                                            SortOrder = reader.GetString(reader.GetOrdinal("SortOrder")),
                                            // 添加JoinConditions
                                            JoinConditions = hasJoinConditions && !reader.IsDBNull(reader.GetOrdinal("JoinConditions"))
                                                ? reader.GetString(reader.GetOrdinal("JoinConditions"))
                                                : "[]",
                                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                                            CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? "未知用户" : reader.GetString(reader.GetOrdinal("CreatedBy")),
                                            IsShared = reader.GetBoolean(reader.GetOrdinal("IsShared"))
                                        };

                                        // 检查是否已存在该查询
                                        if (!queries.Any(q => q.Id == query.Id))
                                        {
                                            Console.WriteLine($"获取共享查询: ID={query.Id}, 名称={query.Name}, 创建者={query.CreatedBy}");
                                            queries.Add(query);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"获取共享查询失败: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"获取共享查询失败: {ex.Message}");
                        // 可能出现异常，返回空列表
                    }
                }

                var orderedQueries = queries.OrderByDescending(q => q.UpdatedAt).ToList();
                Console.WriteLine($"查询数量: {orderedQueries.Count}");
                return orderedQueries;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取保存的查询失败: {ex.Message}");
                return new List<SavedQuery>();
            }
        }

        /// <summary>
        /// 通过ID获取查询
        /// </summary>
        public async Task<SavedQuery> GetSavedQueryByIdAsync(int queryId)
        {
            try
            {
                return await _dbContext.SavedQueries
                    .Include(q => q.SharedUsers)
                    .FirstOrDefaultAsync(q => q.Id == queryId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"通过ID获取查询失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查用户是否已存在相同的查询
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="queryName">查询名称</param>
        /// <param name="currentQueryId">当前查询ID（在编辑查询时使用）</param>
        /// <returns>如果存在相同的查询，则返回true，否则返回false</returns>
        public async Task<bool> CheckQueryNameExistsAsync(string userId, string queryName, int currentQueryId = 0)
        {
            try
            {
                // 检查是否存在相同的查询名称
                var exists = await _dbContext.SavedQueries
                    .AnyAsync(q => q.UserId == userId
                               && q.Name == queryName
                               && q.Id != currentQueryId);

                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查查询名称失败: {ex.Message}");
                // 返回false表示用户无法创建查询
                return false;
            }
        }

        /// <summary>
        /// 保存查询
        /// </summary>
        public async Task<SavedQuery> SaveQueryAsync(SavedQuery query)
        {
            if (query.Id == 0)
            {
                // 添加
                _dbContext.SavedQueries.Add(query);
            }
            else
            {
                // 更新
                var existingQuery = await _dbContext.SavedQueries.FindAsync(query.Id);
                if (existingQuery == null)
                {
                    throw new Exception("查询不存在");
                }

                _dbContext.Entry(existingQuery).CurrentValues.SetValues(query);
            }

            await _dbContext.SaveChangesAsync();
            return query;
        }

        /// <summary>
        /// 删除查询
        /// </summary>
        public async Task DeleteSavedQueryAsync(int queryId, string userId)
        {
            var query = await _dbContext.SavedQueries
                .FirstOrDefaultAsync(q => q.Id == queryId && q.UserId == userId);

            if (query != null)
            {
                _dbContext.SavedQueries.Remove(query);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 刷新数据库架构
        /// </summary>
        public async Task RefreshDatabaseSchemaAsync()
        {
            try
            {
                // 确保架构表存在
                await EnsureSchemaTablesExistAsync();

                Console.WriteLine("开始刷新数据库...");
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 获取表列表
                    var tables = await GetDatabaseTablesAsync(connection);
                    Console.WriteLine($"表数量: {tables.Count}");

                    // 获取视图列表
                    var views = await GetDatabaseViewsAsync(connection);
                    Console.WriteLine($"视图数量: {views.Count}");

                    // 遍历表
                    foreach (var tableName in tables)
                    {
                        try
                        {
                            // 检查是否存在表
                            var existingTable = await _dbContext.TableInfos
                                .FirstOrDefaultAsync(t => t.TableName == tableName);

                            if (existingTable == null)
                            {
                                // 添加表
                                var tableInfo = new TableInfo
                                {
                                    TableName = tableName,
                                    DisplayName = tableName,
                                    Description = $"数据表: {tableName}",
                                    IsView = false
                                };

                                _dbContext.TableInfos.Add(tableInfo);
                                await _dbContext.SaveChangesAsync(); // 获取ID

                                // 获取表的列
                                var columns = await GetTableColumnsAsync(connection, tableName);
                                foreach (var column in columns)
                                {
                                    var columnInfo = new ColumnInfo
                                    {
                                        TableId = tableInfo.Id,
                                        ColumnName = column.ColumnName,
                                        DisplayName = column.ColumnName,
                                        DataType = column.DataType,
                                        IsNullable = column.IsNullable,
                                        IsPrimaryKey = column.IsPrimaryKey,
                                        Description = $": {column.ColumnName}, 类型: {column.DataType}",
                                        Table = tableInfo
                                    };

                                    _dbContext.ColumnInfos.Add(columnInfo);
                                }

                                await _dbContext.SaveChangesAsync();
                                Console.WriteLine($"更新表: {tableName}");
                            }
                            else
                            {
                                // 检查IsView是否正确
                                if (existingTable.IsView)
                                {
                                    existingTable.IsView = false;
                                    await _dbContext.SaveChangesAsync();
                                }

                                // 检查是否存在列
                                var existingColumns = await _dbContext.ColumnInfos
                                    .Where(c => c.TableId == existingTable.Id)
                                    .ToListAsync();

                                var currentColumns = await GetTableColumnsAsync(connection, tableName);

                                // 更新表的列
                                foreach (var column in currentColumns)
                                {
                                    if (!existingColumns.Any(c => c.ColumnName == column.ColumnName))
                                    {
                                        // 添加列
                                        var columnInfo = new ColumnInfo
                                        {
                                            TableId = existingTable.Id,
                                            ColumnName = column.ColumnName,
                                            DisplayName = column.ColumnName,
                                            DataType = column.DataType,
                                            IsNullable = column.IsNullable,
                                            IsPrimaryKey = column.IsPrimaryKey,
                                            Description = $": {column.ColumnName}, 类型: {column.DataType}",
                                            Table = existingTable
                                        };

                                        _dbContext.ColumnInfos.Add(columnInfo);
                                    }
                                }

                                // 删除多余的列
                                foreach (var existingColumn in existingColumns)
                                {
                                    if (!currentColumns.Any(c => c.ColumnName == existingColumn.ColumnName))
                                    {
                                        // 删除列
                                        _dbContext.ColumnInfos.Remove(existingColumn);
                                    }
                                }

                                await _dbContext.SaveChangesAsync();
                                Console.WriteLine($"更新表: {tableName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{tableName}时出现错误: {ex.Message}");
                        }
                    }

                    // 遍历视图
                    foreach (var viewName in views)
                    {
                        try
                        {
                            // 检查是否存在视图
                            var existingView = await _dbContext.TableInfos
                                .FirstOrDefaultAsync(t => t.TableName == viewName);

                            if (existingView == null)
                            {
                                // 添加视图
                                var viewInfo = new TableInfo
                                {
                                    TableName = viewName,
                                    DisplayName = viewName,
                                    Description = $"预览视图: {viewName}",
                                    IsView = true
                                };

                                _dbContext.TableInfos.Add(viewInfo);
                                await _dbContext.SaveChangesAsync(); // 获取ID

                                // 获取视图的列
                                var columns = await GetTableColumnsAsync(connection, viewName);
                                foreach (var column in columns)
                                {
                                    var columnInfo = new ColumnInfo
                                    {
                                        TableId = viewInfo.Id,
                                        ColumnName = column.ColumnName,
                                        DisplayName = column.ColumnName,
                                        DataType = column.DataType,
                                        IsNullable = column.IsNullable,
                                        IsPrimaryKey = column.IsPrimaryKey,
                                        Description = $"视图: {column.ColumnName}, 类型: {column.DataType}",
                                        Table = viewInfo
                                    };

                                    _dbContext.ColumnInfos.Add(columnInfo);
                                }

                                await _dbContext.SaveChangesAsync();
                                Console.WriteLine($"更新视图: {viewName}");
                            }
                            else
                            {
                                // 检查IsView是否正确
                                if (!existingView.IsView)
                                {
                                    existingView.IsView = true;
                                    await _dbContext.SaveChangesAsync();
                                }

                                // 检查是否存在列
                                var existingColumns = await _dbContext.ColumnInfos
                                    .Where(c => c.TableId == existingView.Id)
                                    .ToListAsync();

                                var currentColumns = await GetTableColumnsAsync(connection, viewName);

                                // 更新表的列
                                foreach (var column in currentColumns)
                                {
                                    if (!existingColumns.Any(c => c.ColumnName == column.ColumnName))
                                    {
                                        // 添加列
                                        var columnInfo = new ColumnInfo
                                        {
                                            TableId = existingView.Id,
                                            ColumnName = column.ColumnName,
                                            DisplayName = column.ColumnName,
                                            DataType = column.DataType,
                                            IsNullable = column.IsNullable,
                                            IsPrimaryKey = column.IsPrimaryKey,
                                            Description = $"视图: {column.ColumnName}, 类型: {column.DataType}",
                                            Table = existingView
                                        };

                                        _dbContext.ColumnInfos.Add(columnInfo);
                                    }
                                }

                                // 删除多余的列
                                foreach (var existingColumn in existingColumns)
                                {
                                    if (!currentColumns.Any(c => c.ColumnName == existingColumn.ColumnName))
                                    {
                                        // 删除列
                                        _dbContext.ColumnInfos.Remove(existingColumn);
                                    }
                                }

                                await _dbContext.SaveChangesAsync();
                                Console.WriteLine($"更新视图: {viewName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{viewName}时出现错误: {ex.Message}");
                        }
                    }

                    // 删除表的共享列
                    var allExistingObjects = await _dbContext.TableInfos.ToListAsync();
                    foreach (var existingObject in allExistingObjects)
                    {
                        if (existingObject.IsView && !views.Contains(existingObject.TableName))
                        {
                            // 删除表的共享列
                            var columns = await _dbContext.ColumnInfos
                                .Where(c => c.TableId == existingObject.Id)
                                .ToListAsync();

                            _dbContext.ColumnInfos.RemoveRange(columns);
                            _dbContext.TableInfos.Remove(existingObject);

                            Console.WriteLine($"删除表的共享列: {existingObject.TableName}");
                        }
                        else if (!existingObject.IsView && !tables.Contains(existingObject.TableName))
                        {
                            // 检查是否为系统表
                            if (!_systemTables.Contains(existingObject.TableName))
                            {
                                // 删除表的列
                                var columns = await _dbContext.ColumnInfos
                                    .Where(c => c.TableId == existingObject.Id)
                                    .ToListAsync();

                                _dbContext.ColumnInfos.RemoveRange(columns);
                                _dbContext.TableInfos.Remove(existingObject);

                                Console.WriteLine($"删除表的列: {existingObject.TableName}");
                            }
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                }

                Console.WriteLine("数据库刷新完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"刷新数据库失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 确保架构表存在
        /// </summary>
        private async Task EnsureSchemaTablesExistAsync()
        {
            try
            {
                // 检查TableInfos和ColumnInfos是否存在
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    bool tablesExist = false;

                    using (var command = new SqlCommand(
                        @"SELECT COUNT(*)
                          FROM INFORMATION_SCHEMA.TABLES
                          WHERE TABLE_NAME = 'TableInfos' AND TABLE_SCHEMA = 'dbo'", connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        tablesExist = (int)result > 0;
                    }

                    if (!tablesExist)
                    {
                        // 如果不存在，使用SQL语句创建架构
                        using (var command = new SqlCommand(@"
                            -- 创建TableInfos表
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TableInfos' AND TABLE_SCHEMA = 'dbo')
                            BEGIN
                                CREATE TABLE [dbo].[TableInfos](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [TableName] [nvarchar](128) NOT NULL,
                                    [DisplayName] [nvarchar](128) NOT NULL,
                                    [Description] [nvarchar](500) NULL
                                )
                            END

                            -- 创建ColumnInfos表
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ColumnInfos' AND TABLE_SCHEMA = 'dbo')
                            BEGIN
                                CREATE TABLE [dbo].[ColumnInfos](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [TableId] [int] NOT NULL,
                                    [ColumnName] [nvarchar](128) NOT NULL,
                                    [DisplayName] [nvarchar](128) NOT NULL,
                                    [DataType] [nvarchar](50) NOT NULL,
                                    [IsPrimaryKey] [bit] NOT NULL,
                                    [IsNullable] [bit] NOT NULL,
                                    [Description] [nvarchar](500) NULL,
                                    CONSTRAINT [FK_ColumnInfos_TableInfos] FOREIGN KEY([TableId]) REFERENCES [dbo].[TableInfos] ([Id]) ON DELETE CASCADE
                                )
                            END

                            -- AllowedTables
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AllowedTables' AND TABLE_SCHEMA = 'dbo')
                            BEGIN
                                CREATE TABLE [dbo].[AllowedTables](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [UserId] [nvarchar](450) NOT NULL,
                                    [TableName] [nvarchar](128) NOT NULL,
                                    [CanRead] [bit] NOT NULL,
                                    [CanExport] [bit] NOT NULL
                                )
                            END

                            -- SavedQueries
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SavedQueries' AND TABLE_SCHEMA = 'dbo')
                            BEGIN
                                CREATE TABLE [dbo].[SavedQueries](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [UserId] [nvarchar](450) NOT NULL,
                                    [Name] [nvarchar](128) NOT NULL,
                                    [Description] [nvarchar](500) NULL,
                                    [QueryData] [nvarchar](max) NOT NULL,
                                    [CreatedAt] [datetime2](7) NOT NULL,
                                    [UpdatedAt] [datetime2](7) NOT NULL
                                )
                            END

                            -- QueryShares
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QueryShares' AND TABLE_SCHEMA = 'dbo')
                            BEGIN
                                CREATE TABLE [dbo].[QueryShares](
                                    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                    [QueryId] [int] NOT NULL,
                                    [UserId] [nvarchar](450) NOT NULL,
                                    [SharedAt] [datetime2](7) NOT NULL,
                                    [SharedBy] [nvarchar](450) NOT NULL,
                                    CONSTRAINT [FK_QueryShares_SavedQueries] FOREIGN KEY([QueryId]) REFERENCES [dbo].[SavedQueries] ([Id]) ON DELETE CASCADE
                                )
                            END", connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"确保架构表存在失败: {ex.Message}");
                throw; // 确保架构表存在失败
            }
        }

        /// <summary>
        /// 获取数据库中的所有表
        /// </summary>
        private async Task<List<string>> GetDatabaseTablesAsync(SqlConnection connection)
        {
            var tables = new List<string>();

            try
            {
                // 使用SQL查询获取所有表
                string query = @"
                    SELECT t.name
                    FROM sys.tables t
                    JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = 'dbo' AND t.name NOT LIKE 'sys%' AND t.name NOT LIKE 'dt%'
                    ORDER BY t.name";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string tableName = reader.GetString(0);

                            if (!_systemTables.Contains(tableName))
                            {
                                tables.Add(tableName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取数据库中的所有表失败: {ex.Message}");
            }

            return tables;
        }

        // 获取数据库中的所有视图
        private async Task<List<string>> GetDatabaseViewsAsync(SqlConnection connection)
        {
            var views = new List<string>();

            try
            {
                // 使用SQL查询获取所有视图
                string query = @"
                    SELECT v.name
                    FROM sys.views v
                    JOIN sys.schemas s ON v.schema_id = s.schema_id
                    WHERE s.name = 'dbo' AND v.name NOT LIKE 'sys%'
                    ORDER BY v.name";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string viewName = reader.GetString(0);
                            views.Add(viewName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取数据库中的所有视图失败: {ex.Message}");
            }

            return views;
        }

        /// <summary>
        /// 获取表的列信息
        /// </summary>
        private async Task<List<ColumnModel>> GetTableColumnsAsync(SqlConnection connection, string tableName)
        {
            var columns = new List<ColumnModel>();

            // 获取表的主键列
            var primaryKeys = new HashSet<string>();
            using (var command = new SqlCommand(
                @"SELECT COLUMN_NAME
                  FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                  WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                  AND TABLE_NAME = @TableName", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        primaryKeys.Add(reader.GetString(0));
                    }
                }
            }

            // 获取表的列信息
            using (var command = new SqlCommand(
                @"SELECT
                    COLUMN_NAME,
                    DATA_TYPE,
                    IS_NULLABLE
                  FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_NAME = @TableName
                  ORDER BY ORDINAL_POSITION", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        var isNullable = reader.GetString(2) == "YES";

                        columns.Add(new ColumnModel
                        {
                            ColumnName = columnName,
                            DataType = dataType,
                            IsNullable = isNullable,
                            IsPrimaryKey = primaryKeys.Contains(columnName)
                        });
                    }
                }
            }

            return columns;
        }

        /// <summary>
        /// 获取查询共享用户
        /// </summary>
        public async Task<List<(string UserId, string UserName)>> GetQueryShareUsersAsync(int queryId)
        {
            var result = new List<(string UserId, string UserName)>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 获取查询共享用户
                    using (var command = new SqlCommand(
                        @"SELECT u.Id, u.UserName, u.DisplayName
                          FROM QueryShares qs
                          JOIN Users u ON qs.UserId = u.Id
                          WHERE qs.QueryId = @QueryId
                          ORDER BY u.UserName", connection))
                    {
                        command.Parameters.AddWithValue("@QueryId", queryId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string userId = reader.GetString(0);
                                string userName = reader.GetString(1);
                                // DisplayName处理
                                string displayName = !reader.IsDBNull(2) ? reader.GetString(2) : userName;

                                result.Add((userId, string.IsNullOrEmpty(displayName) ? userName : displayName));
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取查询共享用户失败: {ex.Message}");
                return new List<(string UserId, string UserName)>();
            }
        }

        /// <summary>
        /// 共享查询
        /// </summary>
        public async Task ShareQueryAsync(int queryId, string currentUserId, List<string> targetUserNames)
        {
            try
            {
                // 检查查询是否存在
                var query = await _dbContext.SavedQueries
                    .FirstOrDefaultAsync(q => q.Id == queryId);

                if (query == null)
                {
                    throw new Exception("查询不存在");
                }

                // 检查当前用户是否有权限共享查询
                if (query.CreatedBy != currentUserId && query.UserId != currentUserId)
                {
                    throw new Exception("当前用户没有权限共享查询");
                }

                // 使用ADO.NET直接更新查询共享状态
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 获取目标用户列表
                    // ȡĿûʹüݵķʽѯ
                    List<(string Id, string UserName)> targetUsers = new List<(string Id, string UserName)>();

                    foreach (string userName in targetUserNames)
                    {
                        using (var command = new SqlCommand(
                            "SELECT Id, UserName FROM Users WHERE UserName = @UserName", connection))
                        {
                            command.Parameters.AddWithValue("@UserName", userName);

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    string userId = reader.GetString(0);
                                    targetUsers.Add((userId, userName));
                                }
                            }
                        }
                    }

                    // ȡзûб
                    List<(string Id, string UserName)> existingShares = new List<(string Id, string UserName)>();
                    using (var command = new SqlCommand(
                        @"SELECT u.Id, u.UserName
                          FROM QueryShares qs
                          JOIN Users u ON qs.UserId = u.Id
                          WHERE qs.QueryId = @QueryId", connection))
                    {
                        command.Parameters.AddWithValue("@QueryId", queryId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string userId = reader.GetString(0);
                                string userName = reader.GetString(1);
                                existingShares.Add((userId, userName));
                            }
                        }
                    }

                    // ²ѯ״̬κηû
                    bool hasShares = targetUsers.Count > 0;
                    using (var updateCommand = new SqlCommand(
                        @"UPDATE SavedQueries
                          SET IsShared = @IsShared, UpdatedAt = @UpdatedAt
                          WHERE Id = @QueryId;", connection))
                    {
                        updateCommand.Parameters.AddWithValue("@IsShared", hasShares ? 1 : 0);
                        updateCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                        updateCommand.Parameters.AddWithValue("@QueryId", queryId);
                        await updateCommand.ExecuteNonQueryAsync();
                    }

                    // ķ
                    foreach (var user in targetUsers)
                    {
                        // ǰû
                        if (user.Id == currentUserId)
                        {
                            continue;
                        }

                        // Ƿѷ
                        bool alreadyShared = existingShares.Any(s => s.Id == user.Id);

                        if (!alreadyShared)
                        {
                            // ·¼
                            using (var insertCommand = new SqlCommand(
                                @"INSERT INTO QueryShares (QueryId, UserId, SharedAt, SharedBy)
                                  VALUES (@QueryId, @UserId, @SharedAt, @SharedBy);", connection))
                            {
                                insertCommand.Parameters.AddWithValue("@QueryId", queryId);
                                insertCommand.Parameters.AddWithValue("@UserId", user.Id);
                                insertCommand.Parameters.AddWithValue("@SharedAt", DateTime.Now);
                                insertCommand.Parameters.AddWithValue("@SharedBy", currentUserId);
                                await insertCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // ҪƳķ
                    var targetUserIds = targetUsers.Select(u => u.Id).ToList();
                    var sharesToRemove = existingShares.Where(s => !targetUserIds.Contains(s.Id)).ToList();

                    foreach (var share in sharesToRemove)
                    {
                        // ɾ¼
                        using (var deleteCommand = new SqlCommand(
                            @"DELETE FROM QueryShares
                              WHERE QueryId = @QueryId AND UserId = @UserId", connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@QueryId", queryId);
                            deleteCommand.Parameters.AddWithValue("@UserId", share.Id);
                            await deleteCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ѯʧ: {ex.Message}");
                throw;
            }
        }

        private class ColumnModel
        {
            public string ColumnName { get; set; } = string.Empty;
            public string DataType { get; set; } = string.Empty;
            public bool IsNullable { get; set; }
            public bool IsPrimaryKey { get; set; }
        }
    }
}
