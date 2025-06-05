using BZKQuerySystem.DataAccess;
using BZKQuerySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;

namespace BZKQuerySystem.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class QueryBuilderEnhancedController : ControllerBase
    {
        private readonly IQueryBuilderService _enhancedQueryService;
        private readonly QueryBuilderService _legacyQueryService; // 保持向后兼容
        private readonly PaginatedQueryService _paginatedQueryService;
        private readonly ExcelExportService _excelExportService;
        private readonly UserService _userService;
        private readonly IAuditService _auditService;
        private readonly QueryCacheService _cacheService;
        private readonly ILogger<QueryBuilderEnhancedController> _logger;

        public QueryBuilderEnhancedController(
            IQueryBuilderService enhancedQueryService,
            QueryBuilderService legacyQueryService,
            PaginatedQueryService paginatedQueryService,
            ExcelExportService excelExportService,
            UserService userService,
            IAuditService auditService,
            QueryCacheService cacheService,
            ILogger<QueryBuilderEnhancedController> logger)
        {
            _enhancedQueryService = enhancedQueryService;
            _legacyQueryService = legacyQueryService;
            _paginatedQueryService = paginatedQueryService;
            _excelExportService = excelExportService;
            _userService = userService;
            _auditService = auditService;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// 获取用户可访问的表（增强版，带缓存）
        /// </summary>
        [HttpGet("GetUserTables/{userId}")]
        public async Task<IActionResult> GetUserTables(string userId)
        {
            try
            {
                var tables = await _cacheService.GetOrSetTableSchemaAsync(
                    $"user_tables_{userId}",
                    async () => await _legacyQueryService.GetAllowedTablesForUserAsync(userId),
                    TimeSpan.FromMinutes(15)
                );

                await _auditService.LogUserActionAsync(
                    userId, 
                    "获取用户表列表", 
                    $"获取到 {tables.Count} 个可访问表",
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户表列表失败，用户: {UserId}", userId);
                return StatusCode(500, new { message = "获取表列表失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取表的列信息（增强版，带缓存）
        /// </summary>
        [HttpGet("GetTableColumns/{tableName}")]
        public async Task<IActionResult> GetTableColumns(string tableName)
        {
            try
            {
                var columns = await _cacheService.GetOrSetTableSchemaAsync(
                    $"table_columns_{tableName}",
                    async () => await _legacyQueryService.GetColumnsForTableAsync(tableName),
                    TimeSpan.FromHours(1)
                );

                // 创建简化版列对象，避免循环引用
                var simplifiedColumns = columns.Select(c => new {
                    Id = c.Id,
                    TableId = c.TableId,
                    ColumnName = c.ColumnName,
                    DisplayName = c.DisplayName,
                    DataType = c.DataType,
                    Description = c.Description,
                    IsPrimaryKey = c.IsPrimaryKey,
                    IsNullable = c.IsNullable
                }).ToList();

                return Ok(simplifiedColumns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取表列信息失败，表名: {TableName}", tableName);
                return StatusCode(500, new { message = "获取列信息失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 执行查询（增强版，使用安全参数化查询）
        /// </summary>
        [HttpPost("ExecuteQuery/{userId}")]
        [EnableRateLimiting("QueryLimit")]
        [Authorize(Policy = "ExecuteQueries")]
        public async Task<IActionResult> ExecuteQuery(string userId, [FromBody] QueryRequest request)
        {
            try
            {
                var startTime = DateTime.UtcNow;

                // 使用增强的安全查询服务
                var dataTable = await _enhancedQueryService.ExecuteSecureQueryAsync(userId, request);
                
                var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // 记录查询执行日志
                await _auditService.LogQueryExecutionAsync(
                    userId,
                    "安全查询执行",
                    dataTable.Rows.Count,
                    (long)executionTime,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                // 构建返回结果
                var result = new
                {
                    Columns = dataTable.Columns.Cast<DataColumn>().Select(c => new
                    {
                        Name = c.ColumnName,
                        DisplayName = c.ColumnName,
                        DataType = c.DataType.Name
                    }).ToList(),
                    Data = dataTable.Rows.Cast<DataRow>().Select(row =>
                        dataTable.Columns.Cast<DataColumn>().ToDictionary(
                            col => col.ColumnName,
                            col => row[col] == DBNull.Value ? null : row[col]
                        )
                    ).ToList(),
                    TotalCount = dataTable.Rows.Count,
                    ExecutionTime = Math.Round(executionTime, 2),
                    Sql = "查询已安全执行" // 不返回实际SQL以提高安全性
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行查询失败，用户: {UserId}", userId);
                
                await _auditService.LogSecurityEventAsync(
                    userId,
                    "查询执行失败",
                    ex.Message,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return StatusCode(500, new { message = "查询执行失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 分页查询（新功能）
        /// </summary>
        [HttpPost("ExecutePaginatedQuery/{userId}")]
        [EnableRateLimiting("QueryLimit")]
        [Authorize(Policy = "ExecuteQueries")]
        public async Task<IActionResult> ExecutePaginatedQuery(
            string userId, 
            [FromBody] PaginatedQueryRequest request)
        {
            try
            {
                // 构建参数化查询
                var (sql, parameters) = BuildParameterizedQueryFromRequest(request);

                // 执行分页查询
                var result = await _paginatedQueryService.ExecutePaginatedQueryAsync(
                    sql, 
                    parameters, 
                    request.PageNumber, 
                    request.PageSize
                );

                // 记录查询日志
                await _auditService.LogQueryExecutionAsync(
                    userId,
                    "分页查询执行",
                    result.TotalCount,
                    0, // 执行时间会在服务层记录
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询失败，用户: {UserId}", userId);
                return StatusCode(500, new { message = "分页查询失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取查询性能信息
        /// </summary>
        [HttpPost("GetQueryPerformance/{userId}")]
        [Authorize(Policy = "ViewPerformanceMetrics")]
        public async Task<IActionResult> GetQueryPerformance(string userId, [FromBody] QueryRequest request)
        {
            try
            {
                var (sql, parameters) = BuildParameterizedQueryFromRequest(request);
                var performance = await _paginatedQueryService.GetQueryPerformanceAsync(sql, parameters);

                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取查询性能信息失败，用户: {UserId}", userId);
                return StatusCode(500, new { message = "获取性能信息失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 保存查询（增强版）
        /// </summary>
        [HttpPost("SaveQuery/{userId}")]
        [Authorize(Policy = "SaveQueries")]
        public async Task<IActionResult> SaveQuery(string userId, [FromBody] SaveQueryRequest request)
        {
            try
            {
                var savedQuery = new SavedQuery
                {
                    UserId = userId,
                    Name = request.Name,
                    Description = request.Description,
                    TablesIncluded = string.Join(",", request.Tables),
                    ColumnsIncluded = string.Join(",", request.Columns),
                    JoinConditions = string.Join(",", request.JoinConditions),
                    FilterConditions = string.Join(",", request.WhereConditions),
                    SortOrder = string.Join(",", request.OrderBy),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                var result = await _legacyQueryService.SaveQueryAsync(savedQuery);

                await _auditService.LogUserActionAsync(
                    userId, 
                    "SaveQuery", 
                    $"保存查询: {request.Name}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(new { success = true, queryId = result.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存查询失败");
                return StatusCode(500, new { error = "保存查询失败" });
            }
        }

        /// <summary>
        /// 获取保存的查询
        /// </summary>
        [HttpGet("GetSavedQueries/{userId}")]
        public async Task<IActionResult> GetSavedQueries(string userId)
        {
            try
            {
                var queries = await _legacyQueryService.GetSavedQueriesAsync(userId);
                return Ok(queries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取保存的查询失败，用户: {UserId}", userId);
                return StatusCode(500, new { message = "获取保存的查询失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 删除保存的查询
        /// </summary>
        [HttpDelete("DeleteSavedQuery/{queryId}")]
        [Authorize(Policy = "DeleteQueries")]
        public async Task<IActionResult> DeleteSavedQuery(int queryId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                await _legacyQueryService.DeleteSavedQueryAsync(queryId, userId);

                await _auditService.LogUserActionAsync(
                    userId, 
                    "DeleteQuery", 
                    $"删除查询ID: {queryId}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除查询失败");
                return StatusCode(500, new { error = "删除查询失败" });
            }
        }

        /// <summary>
        /// 导出查询结果
        /// </summary>
        [HttpPost("ExportQuery/{userId}")]
        [EnableRateLimiting("ExportLimit")]
        [Authorize(Policy = "ExportData")]
        public async Task<IActionResult> ExportQuery(string userId, [FromBody] ExportQueryRequest request)
        {
            try
            {
                var (sql, parameters) = BuildParameterizedQueryFromRequest(request);
                var dataTable = await _paginatedQueryService.ExecuteQueryAsync(sql, parameters.ToArray());

                if (request.Format.ToLower() == "excel")
                {
                    var excelBytes = _excelExportService.ExportToExcel(dataTable, request.FileName ?? "查询结果");

                    await _auditService.LogDataExportAsync(
                        userId, 
                        string.Join(",", request.Tables), 
                        dataTable.Rows.Count, 
                        "Excel",
                        HttpContext.Connection.RemoteIpAddress?.ToString());

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"{request.FileName ?? "查询结果"}.xlsx");
                }
                else if (request.Format.ToLower() == "csv")
                {
                    var csvStream = await new CsvStreamGenerator(
                        _paginatedQueryService,
                        sql,
                        parameters
                    ).GetStream();

                    return File(csvStream, "text/csv", $"{request.FileName ?? "查询结果"}.csv");
                }

                return BadRequest("不支持的导出格式");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出数据失败");
                return StatusCode(500, new { error = "导出数据失败" });
            }
        }

        /// <summary>
        /// 分享查询
        /// </summary>
        [HttpPost("ShareQuery/{queryId}")]
        [Authorize(Policy = "ShareQueries")]
        public async Task<IActionResult> ShareQuery(int queryId, [FromBody] ShareQueryRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                await _legacyQueryService.ShareQueryAsync(queryId, currentUserId, request.UserIds);

                await _auditService.LogUserActionAsync(
                    currentUserId, 
                    "ShareQuery", 
                    $"分享查询ID: {queryId} 给用户: {string.Join(",", request.UserIds)}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分享查询失败");
                return StatusCode(500, new { error = "分享查询失败" });
            }
        }

        #region 私有方法

        private (string sql, List<Microsoft.Data.SqlClient.SqlParameter> parameters) BuildParameterizedQueryFromRequest(QueryRequest request)
        {
            // 这里简化实现，实际应该使用增强查询服务的构建方法
            var sql = "SELECT " + string.Join(", ", request.Columns.Select(c => $"[{c.TableName}].[{c.ColumnName}]"));
            sql += " FROM " + string.Join(", ", request.Tables.Select(t => $"[{t}]"));
            
            var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>();
            
            if (request.Filters.Any())
            {
                sql += " WHERE ";
                var filterParts = new List<string>();
                int paramIndex = 0;
                
                foreach (var filter in request.Filters)
                {
                    var paramName = $"@param{paramIndex++}";
                    filterParts.Add($"[{filter.TableName}].[{filter.ColumnName}] {filter.Operator} {paramName}");
                    parameters.Add(new Microsoft.Data.SqlClient.SqlParameter(paramName, filter.Value ?? DBNull.Value));
                }
                
                sql += string.Join(" AND ", filterParts);
            }

            return (sql, parameters);
        }

        #endregion
    }

    #region 请求模型

    public class PaginatedQueryRequest : QueryRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }

    public class SaveQueryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tables { get; set; } = new();
        public List<string> Columns { get; set; } = new();
        public List<string> JoinConditions { get; set; } = new();
        public List<string> WhereConditions { get; set; } = new();
        public List<string> OrderBy { get; set; } = new();
    }

    public class ExportQueryRequest : QueryRequest
    {
        public string Format { get; set; } = "excel";
        public string FileName { get; set; } = string.Empty;
    }

    public class ShareQueryRequest
    {
        public List<string> UserIds { get; set; } = new();
    }

    #endregion

    #region CSV流生成器

    public class CsvStreamGenerator
    {
        private readonly PaginatedQueryService _queryService;
        private readonly string _sql;
        private readonly List<Microsoft.Data.SqlClient.SqlParameter> _parameters;

        public CsvStreamGenerator(PaginatedQueryService queryService, string sql, List<Microsoft.Data.SqlClient.SqlParameter> parameters)
        {
            _queryService = queryService;
            _sql = sql;
            _parameters = parameters;
        }

        public async Task<Stream> GetStream()
        {
            var dataTable = await _queryService.ExecuteQueryAsync(_sql, _parameters.ToArray());
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);

            // 写入列标题
            if (dataTable.Columns.Count > 0)
            {
                var columnNames = dataTable.Columns.Cast<DataColumn>()
                    .Select(c => EscapeCsvField(c.ColumnName));
                await writer.WriteLineAsync(string.Join(",", columnNames));
            }

            // 写入数据行
            foreach (DataRow row in dataTable.Rows)
            {
                var fields = row.ItemArray.Select(f => EscapeCsvField(f?.ToString() ?? ""));
                await writer.WriteLineAsync(string.Join(",", fields));
            }

            await writer.FlushAsync();
            stream.Position = 0;
            return stream;
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }
    }

    #endregion
} 