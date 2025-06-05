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
        private readonly QueryBuilderService _legacyQueryService; // ����������
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
        /// ��ȡ�û��ɷ��ʵı���ǿ�棬�����棩
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
                    "��ȡ�û����б�", 
                    $"��ȡ�� {tables.Count} ���ɷ��ʱ�",
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ�û����б�ʧ�ܣ��û�: {UserId}", userId);
                return StatusCode(500, new { message = "��ȡ���б�ʧ��", error = ex.Message });
            }
        }

        /// <summary>
        /// ��ȡ�������Ϣ����ǿ�棬�����棩
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

                // �����򻯰��ж��󣬱���ѭ������
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
                _logger.LogError(ex, "��ȡ������Ϣʧ�ܣ�����: {TableName}", tableName);
                return StatusCode(500, new { message = "��ȡ����Ϣʧ��", error = ex.Message });
            }
        }

        /// <summary>
        /// ִ�в�ѯ����ǿ�棬ʹ�ð�ȫ��������ѯ��
        /// </summary>
        [HttpPost("ExecuteQuery/{userId}")]
        [EnableRateLimiting("QueryLimit")]
        [Authorize(Policy = "ExecuteQueries")]
        public async Task<IActionResult> ExecuteQuery(string userId, [FromBody] QueryRequest request)
        {
            try
            {
                var startTime = DateTime.UtcNow;

                // ʹ����ǿ�İ�ȫ��ѯ����
                var dataTable = await _enhancedQueryService.ExecuteSecureQueryAsync(userId, request);
                
                var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // ��¼��ѯִ����־
                await _auditService.LogQueryExecutionAsync(
                    userId,
                    "��ȫ��ѯִ��",
                    dataTable.Rows.Count,
                    (long)executionTime,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                // �������ؽ��
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
                    Sql = "��ѯ�Ѱ�ȫִ��" // ������ʵ��SQL����߰�ȫ��
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ִ�в�ѯʧ�ܣ��û�: {UserId}", userId);
                
                await _auditService.LogSecurityEventAsync(
                    userId,
                    "��ѯִ��ʧ��",
                    ex.Message,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return StatusCode(500, new { message = "��ѯִ��ʧ��", error = ex.Message });
            }
        }

        /// <summary>
        /// ��ҳ��ѯ���¹��ܣ�
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
                // ������������ѯ
                var (sql, parameters) = BuildParameterizedQueryFromRequest(request);

                // ִ�з�ҳ��ѯ
                var result = await _paginatedQueryService.ExecutePaginatedQueryAsync(
                    sql, 
                    parameters, 
                    request.PageNumber, 
                    request.PageSize
                );

                // ��¼��ѯ��־
                await _auditService.LogQueryExecutionAsync(
                    userId,
                    "��ҳ��ѯִ��",
                    result.TotalCount,
                    0, // ִ��ʱ����ڷ�����¼
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ҳ��ѯʧ�ܣ��û�: {UserId}", userId);
                return StatusCode(500, new { message = "��ҳ��ѯʧ��", error = ex.Message });
            }
        }

        /// <summary>
        /// ��ȡ��ѯ������Ϣ
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
                _logger.LogError(ex, "��ȡ��ѯ������Ϣʧ�ܣ��û�: {UserId}", userId);
                return StatusCode(500, new { message = "��ȡ������Ϣʧ��", error = ex.Message });
            }
        }

        /// <summary>
        /// �����ѯ����ǿ�棩
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
                    $"�����ѯ: {request.Name}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(new { success = true, queryId = result.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�����ѯʧ��");
                return StatusCode(500, new { error = "�����ѯʧ��" });
            }
        }

        /// <summary>
        /// ��ȡ����Ĳ�ѯ
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
                _logger.LogError(ex, "��ȡ����Ĳ�ѯʧ�ܣ��û�: {UserId}", userId);
                return StatusCode(500, new { message = "��ȡ����Ĳ�ѯʧ��", error = ex.Message });
            }
        }

        /// <summary>
        /// ɾ������Ĳ�ѯ
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
                    $"ɾ����ѯID: {queryId}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ɾ����ѯʧ��");
                return StatusCode(500, new { error = "ɾ����ѯʧ��" });
            }
        }

        /// <summary>
        /// ������ѯ���
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
                    var excelBytes = _excelExportService.ExportToExcel(dataTable, request.FileName ?? "��ѯ���");

                    await _auditService.LogDataExportAsync(
                        userId, 
                        string.Join(",", request.Tables), 
                        dataTable.Rows.Count, 
                        "Excel",
                        HttpContext.Connection.RemoteIpAddress?.ToString());

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"{request.FileName ?? "��ѯ���"}.xlsx");
                }
                else if (request.Format.ToLower() == "csv")
                {
                    var csvStream = await new CsvStreamGenerator(
                        _paginatedQueryService,
                        sql,
                        parameters
                    ).GetStream();

                    return File(csvStream, "text/csv", $"{request.FileName ?? "��ѯ���"}.csv");
                }

                return BadRequest("��֧�ֵĵ�����ʽ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��������ʧ��");
                return StatusCode(500, new { error = "��������ʧ��" });
            }
        }

        /// <summary>
        /// �����ѯ
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
                    $"�����ѯID: {queryId} ���û�: {string.Join(",", request.UserIds)}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�����ѯʧ��");
                return StatusCode(500, new { error = "�����ѯʧ��" });
            }
        }

        #region ˽�з���

        private (string sql, List<Microsoft.Data.SqlClient.SqlParameter> parameters) BuildParameterizedQueryFromRequest(QueryRequest request)
        {
            // �����ʵ�֣�ʵ��Ӧ��ʹ����ǿ��ѯ����Ĺ�������
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

    #region ����ģ��

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

    #region CSV��������

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

            // д���б���
            if (dataTable.Columns.Count > 0)
            {
                var columnNames = dataTable.Columns.Cast<DataColumn>()
                    .Select(c => EscapeCsvField(c.ColumnName));
                await writer.WriteLineAsync(string.Join(",", columnNames));
            }

            // д��������
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