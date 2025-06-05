using BZKQuerySystem.DataAccess;
using BZKQuerySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BZKQuerySystem.Web.Controllers
{
    [Authorize]
    public class QueryBuilderController : Controller
    {
        private readonly QueryBuilderService _queryBuilderService;
        private readonly ExcelExportService _excelExportService;
        private readonly PdfExportService _pdfExportService;
        private readonly UserService _userService;

        public QueryBuilderController(
            QueryBuilderService queryBuilderService,
            ExcelExportService excelExportService,
            PdfExportService pdfExportService,
            UserService userService)
        {
            _queryBuilderService = queryBuilderService;
            _excelExportService = excelExportService;
            _pdfExportService = pdfExportService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            Console.WriteLine($"Index: 用户已登录，userId: {userId}");
            
            // 获取用户允许访问的表
            var tables = await _queryBuilderService.GetAllowedTablesForUserAsync(userId);
            Console.WriteLine($"Index: 获取到tables.Count个表");
            
            // 获取用户保存的所有查询
            var savedQueries = await _queryBuilderService.GetSavedQueriesAsync(userId);
            
            // 获取共享查询的用户
            Dictionary<int, List<string>> sharedQueryUsers = new Dictionary<int, List<string>>();
            foreach (var query in savedQueries)
            {
                // 获取共享查询的用户
                if (query.CreatedBy == User.Identity.Name)
                {
                    var sharedUsers = await _queryBuilderService.GetQueryShareUsersAsync(query.Id);
                    if (sharedUsers.Any())
                    {
                        sharedQueryUsers[query.Id] = sharedUsers.Select(u => u.UserName).ToList();
                    }
                }
            }
            
            // 输出用户保存的所有查询
            Console.WriteLine($"Index: 获取到savedQueries.Count个查询");
            foreach (var query in savedQueries)
            {
                Console.WriteLine($"查询ID: {query.Id}, 查询名称: {query.Name}, 创建者: {query.CreatedBy}");
            }
            
            // 检查用户是否有权限保存查询、共享查询和导出数据
            bool canSaveQueries = await _userService.HasPermissionAsync(userId, SystemPermissions.SaveQueries);
            bool canShareQueries = await _userService.HasPermissionAsync(userId, SystemPermissions.ShareQueries);
            bool canExportData = await _userService.HasPermissionAsync(userId, SystemPermissions.ExportData);
            
            Console.WriteLine($"用户权限: CanSaveQueries={canSaveQueries}, CanShareQueries={canShareQueries}, CanExportData={canExportData}");
            
            ViewBag.Tables = tables;
            ViewBag.SavedQueries = savedQueries;
            ViewBag.CanSaveQueries = canSaveQueries;
            ViewBag.CanShareQueries = canShareQueries;
            ViewBag.CanExportData = canExportData;
            ViewBag.SharedQueryUsers = sharedQueryUsers;
            
            Console.WriteLine("Index: 用户登录成功，返回视图");
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTables()
        {
            string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            var tables = await _queryBuilderService.GetAllowedTablesForUserAsync(userId);
            return Json(tables);
        }

        [HttpGet]
        public async Task<IActionResult> GetColumns(string tableName)
        {
            try
            {
                var columns = await _queryBuilderService.GetColumnsForTableAsync(tableName);
                
                // 简化Columns数据
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
                
                return Json(simplifiedColumns);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"获取Columns时发生错误: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteQuery([FromBody] QueryViewModel query)
        {
            try
            {
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                // 检查用户是否有权限导出数据
                bool canExport = await _userService.HasPermissionAsync(userId, SystemPermissions.ExportData);
                ViewBag.CanExport = canExport;

                // 确保SQL查询语句以分号结尾
                if (!string.IsNullOrEmpty(query.SqlQuery) && !query.SqlQuery.TrimEnd().EndsWith(";"))
                {
                    query.SqlQuery = query.SqlQuery.TrimEnd() + ";";
                }

                // 获取查询总行数
                int totalRows = await _queryBuilderService.GetTotalRowsAsync(
                    query.Tables,
                    query.Columns,
                    query.JoinConditions ?? new List<string>(),
                    query.WhereConditions ?? new List<string>(),
                    query.Parameters ?? new Dictionary<string, object>()
                );

                // 执行查询
                var dataTable = await _queryBuilderService.ExecuteQueryAsync(
                    query.Tables,
                    query.Columns,
                    query.JoinConditions ?? new List<string>(),
                    query.WhereConditions ?? new List<string>(),
                    query.OrderBy ?? new List<string>(),
                    query.Parameters ?? new Dictionary<string, object>(),
                    query.PageSize,
                    query.PageNumber
                );

                // 获取查询结果
                int pageSize = query.PageSize ?? 0;
                int totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalRows / pageSize) : 1;
                
                // 获取完整的SQL查询语句
                string sqlQuery = _queryBuilderService.BuildSqlQuery(
                    query.Tables,
                    query.Columns,
                    query.JoinConditions ?? new List<string>(),
                    query.WhereConditions ?? new List<string>(),
                    query.OrderBy ?? new List<string>()
                );

                // 构建结果对象
                var result = new
                {
                    Columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(),
                    Rows = dataTable.Rows.Cast<DataRow>().Select(row => 
                        dataTable.Columns.Cast<DataColumn>().Select(col => 
                            row[col] == DBNull.Value ? null : row[col].ToString()
                        ).ToList()
                    ).ToList(),
                    SqlQuery = sqlQuery,
                    TotalRows = totalRows,
                    CurrentPage = query.PageNumber ?? 1,
                    PageSize = query.PageSize ?? totalRows,
                    TotalPages = totalPages
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Policy = "CanExportData")]
        public async Task<IActionResult> ExportToExcel([FromBody] QueryViewModel query)
        {
            try
            {
                // 获取用户ID
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { error = "无法识别当前用户" });
                }
                
                // 验证用户权限
                bool canExport = await _userService.HasPermissionAsync(userId, SystemPermissions.ExportData);
                if (!canExport)
                {
                    return Forbid();
                }
                
                // 验证请求数据
                if (query == null || query.Tables == null || query.Tables.Count == 0)
                {
                    return BadRequest(new { error = "请求数据无效，请确保已选择表" });
                }
                
                Console.WriteLine($"开始导出Excel: 表数量:{query.Tables.Count}, 列数量:{query.Columns?.Count ?? 0}");
                
                // 确保Columns不为null
                if (query.Columns == null)
                {
                    query.Columns = new List<string>();
                }
                
                // 执行查询
                var dataTable = await _queryBuilderService.ExecuteQueryAsync(
                    query.Tables,
                    query.Columns,
                    query.JoinConditions ?? new List<string>(),
                    query.WhereConditions ?? new List<string>(),
                    query.OrderBy ?? new List<string>(),
                    query.Parameters ?? new Dictionary<string, object>()
                );
                
                Console.WriteLine($"查询执行完成，获取到{dataTable.Rows.Count}条数据，开始导出Excel");

                // 导出Excel
                // 确保文件名不为空
                string sheetName = !string.IsNullOrEmpty(query.QueryName) ? query.QueryName : "查询报表";
                
                byte[] fileContents = _excelExportService.ExportToExcel(
                    dataTable, 
                    sheetName, 
                    $"查询报表: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}"
                );
                
                Console.WriteLine($"Excel导出完成，文件大小: {fileContents.Length} 字节");

                // 生成文件名（查询名称+日期）
                string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                string fileName = $"{sheetName}_{dateStr}.xlsx";
                
                // 返回Excel文件
                return File(
                    fileContents, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出Excel时发生错误: {ex.Message}");
                Console.WriteLine($"错误堆栈: {ex.StackTrace}");
                return BadRequest(new { error = $"导出Excel失败: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "CanExportData")]
        public async Task<IActionResult> ExportToPDF([FromBody] QueryViewModel query)
        {
            try
            {
                // 获取用户ID
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { error = "无法识别当前用户" });
                }
                
                // 验证用户权限
                bool canExport = await _userService.HasPermissionAsync(userId, SystemPermissions.ExportData);
                if (!canExport)
                {
                    return Forbid();
                }
                
                // 验证请求数据
                if (query == null || query.Tables == null || query.Tables.Count == 0)
                {
                    return BadRequest(new { error = "请求数据无效，请确保已选择表" });
                }
                
                Console.WriteLine($"开始导出PDF: 表数量:{query.Tables.Count}, 列数量:{query.Columns?.Count ?? 0}");
                
                // 确保Columns不为null
                if (query.Columns == null)
                {
                    query.Columns = new List<string>();
                }
                
                // 执行查询
                var dataTable = await _queryBuilderService.ExecuteQueryAsync(
                    query.Tables,
                    query.Columns,
                    query.JoinConditions ?? new List<string>(),
                    query.WhereConditions ?? new List<string>(),
                    query.OrderBy ?? new List<string>(),
                    query.Parameters ?? new Dictionary<string, object>()
                );
                
                Console.WriteLine($"查询执行完成，获取到{dataTable.Rows.Count}条数据，开始导出PDF");

                // 导出PDF
                string reportTitle = !string.IsNullOrEmpty(query.QueryName) ? query.QueryName : "查询报表";
                
                var pdfOptions = new PdfExportOptions
                {
                    Title = reportTitle,
                    Author = "BZK查询系统",
                    IncludeTimestamp = true,
                    IncludePageNumbers = true,
                    MaxRowsPerPage = 50,
                    LandscapeOrientation = true  // 启用横向布局
                };
                
                byte[] fileContents = await _pdfExportService.ExportToPdfAsync(dataTable, pdfOptions);
                
                Console.WriteLine($"PDF导出完成，文件大小: {fileContents.Length} 字节");

                // 生成文件名（查询名称+日期）
                string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                string fileName = $"{reportTitle}_{dateStr}.pdf";
                
                // 返回PDF文件
                return File(
                    fileContents, 
                    "application/pdf",
                    fileName
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出PDF时发生错误: {ex.Message}");
                Console.WriteLine($"错误堆栈: {ex.StackTrace}");
                return BadRequest(new { error = $"导出PDF失败: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "CanSaveQueries")]
        public async Task<IActionResult> SaveQuery([FromBody] SaveQueryViewModel model)
        {
            try
            {
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                string userName = User.Identity.Name;
                
                Console.WriteLine($"SaveQuery: 用户已登录，userId: {userId}, 正在保存查询 '{model.Name}'");
                
                // 检查查询名称是否已存在
                bool nameExists = await _queryBuilderService.CheckQueryNameExistsAsync(userId, model.Name, model.Id);
                if (nameExists)
                {
                    return BadRequest(new { error = "查询名称已存在，请更改查询名称" });
                }
                
                var query = new SavedQuery
                {
                    Id = model.Id,
                    UserId = userId,
                    Name = model.Name,
                    Description = model.Description,
                    SqlQuery = model.SqlQuery,
                    TablesIncluded = JsonSerializer.Serialize(model.Tables),
                    ColumnsIncluded = JsonSerializer.Serialize(model.Columns),
                    FilterConditions = JsonSerializer.Serialize(model.WhereConditions),
                    SortOrder = JsonSerializer.Serialize(model.OrderBy),
                    JoinConditions = JsonSerializer.Serialize(model.JoinConditions),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = userName
                };

                await _queryBuilderService.SaveQueryAsync(query);
                
                // 保存查询成功
                var savedQuery = await _queryBuilderService.GetSavedQueryByIdAsync(query.Id);
                if (savedQuery == null)
                {
                    Console.WriteLine($"SaveQuery: 查询保存失败，请检查数据库连接: {query.Id} 查询");
                    return BadRequest(new { error = "查询保存失败，请检查数据库连接" });
                }
                
                Console.WriteLine($"SaveQuery: 查询保存成功，id: {query.Id}, joinConditions: {savedQuery.JoinConditions}");
                
                return Json(new { success = true, id = query.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存查询时发生错误: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SavedQueries()
        {
            string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            Console.WriteLine($"SavedQueries: 用户已登录，userId: {userId}, 正在获取所有查询");
            
            // 获取用户允许访问的表
            var tables = await _queryBuilderService.GetAllowedTablesForUserAsync(userId);
            
            // 获取用户保存的所有查询
            var savedQueries = await _queryBuilderService.GetSavedQueriesAsync(userId);
            
            Console.WriteLine($"SavedQueries: 获取到savedQueries.Count个查询");
            
            // 检查用户是否有权限保存查询、共享查询和导出数据
            bool canSaveQueries = await _userService.HasPermissionAsync(userId, SystemPermissions.SaveQueries);
            bool canShareQueries = await _userService.HasPermissionAsync(userId, SystemPermissions.ShareQueries);
            bool canExportData = await _userService.HasPermissionAsync(userId, SystemPermissions.ExportData);
            
            ViewBag.Tables = tables;
            ViewBag.SavedQueries = savedQueries;
            ViewBag.CanSaveQueries = canSaveQueries;
            ViewBag.CanShareQueries = canShareQueries;
            ViewBag.CanExportData = canExportData;
            ViewBag.ShowSavedQueries = true; // 显示所有查询
            
            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetSavedQuery(int id)
        {
            try 
            {
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { error = "无法识别当前用户" });
                }
                
                var queries = await _queryBuilderService.GetSavedQueriesAsync(userId);
                var query = queries.FirstOrDefault(q => q.Id == id);

                if (query == null)
                {
                    Console.WriteLine($"查询ID不存在，userId: {userId}, id: {id}");
                    return NotFound(new { error = $"查询ID不存在，userId: {userId}, id: {id}" });
                }

                try 
                {
                    var viewModel = new SaveQueryViewModel
                    {
                        Id = query.Id,
                        Name = query.Name,
                        Description = query.Description,
                        SqlQuery = query.SqlQuery,
                        Tables = JsonSerializer.Deserialize<List<string>>(query.TablesIncluded ?? "[]"),
                        Columns = JsonSerializer.Deserialize<List<string>>(query.ColumnsIncluded ?? "[]"),
                        WhereConditions = JsonSerializer.Deserialize<List<string>>(query.FilterConditions ?? "[]"),
                        OrderBy = JsonSerializer.Deserialize<List<string>>(query.SortOrder ?? "[]"),
                        JoinConditions = JsonSerializer.Deserialize<List<string>>(query.JoinConditions ?? "[]")
                    };

                    return Json(viewModel);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON反序列化错误: {ex.Message}");
                    return BadRequest(new { error = $"查询反序列化失败: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取查询时发生错误: {ex.Message}");
                return BadRequest(new { error = $"获取查询失败: {ex.Message}" });
            }
        }

        [HttpDelete]
        [Authorize(Policy = "CanSaveQueries")]
        public async Task<IActionResult> DeleteSavedQuery(int id)
        {
            try
            {
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                await _queryBuilderService.DeleteSavedQueryAsync(id, userId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Policy = "CanShareQueries")]
        public async Task<IActionResult> ShareQuery([FromBody] ShareQueryViewModel model)
        {
            try
            {
                // 检查请求数据
                if (model == null)
                {
                    Console.WriteLine("共享查询时发生错误: 请求数据为空");
                    return BadRequest(new { error = "请求数据为空" });
                }
                
                if (model.QueryId <= 0)
                {
                    Console.WriteLine($"共享查询时发生错误: 查询ID无效 {model.QueryId}");
                    return BadRequest(new { error = "查询ID无效" });
                }
                
                // 检查用户权限
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                string userName = User.Identity.Name;
                
                if (model.UserNames == null)
                {
                    Console.WriteLine("共享查询时发生错误: 用户列表为空");
                    return BadRequest(new { error = "用户列表为空" });
                }
                
                if (model.UserNames.Count == 0)
                {
                    Console.WriteLine($"用户 {userName} 共享查询 {model.QueryId} 失败，没有选择用户");
                }
                else
                {
                    Console.WriteLine($"用户 {userName} 共享查询 {model.QueryId} 成功，共享给 {string.Join(", ", model.UserNames)}");
                }
                
                // 检查查询是否存在
                var query = await _queryBuilderService.GetSavedQueryByIdAsync(model.QueryId);
                if (query == null)
                {
                    Console.WriteLine($"查询不存在，userId: {userId}, queryId: {model.QueryId}");
                    return NotFound(new { error = "查询不存在" });
                }
                
                // 检查用户权限
                if (query.CreatedBy != userName)
                {
                    Console.WriteLine($"用户 {userName} 没有权限共享查询 {model.QueryId}");
                    return BadRequest(new { error = "用户没有权限共享查询" });
                }
                
                // 共享查询
                await _queryBuilderService.ShareQueryAsync(model.QueryId, userId, model.UserNames);
                
                if (model.UserNames.Count == 0)
                {
                    Console.WriteLine($"查询 {model.QueryId} 共享成功，没有共享给其他用户");
                    return Json(new { success = true, message = "查询共享成功，没有共享给其他用户" });
                }
                else
                {
                    Console.WriteLine($"查询 {model.QueryId} 共享成功，共享给 {string.Join(", ", model.UserNames)}");
                    return Json(new { success = true, message = "查询共享成功，共享给其他用户" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"共享查询时发生错误: {ex.Message}");
                Console.WriteLine($"错误堆栈: {ex.StackTrace}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> RefreshSchema()
        {
            try
            {
                await _queryBuilderService.RefreshDatabaseSchemaAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Policy = "CanShareQueries")]
        public async Task<IActionResult> GetUsersForSharing()
        {
            try
            {
                // 获取当前用户ID
                string currentUserId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                // 获取所有用户
                var allUsers = await _userService.GetAllUsersAsync();
                
                // 获取可以共享查询的用户
                var usersForSharing = allUsers
                    .Where(u => u.Id != currentUserId && u.IsActive)
                    .Select(u => new { 
                        userId = u.Id,
                        userName = u.UserName,
                        displayName = u.DisplayName,
                        department = u.Department 
                    })
                    .ToList();
                
                return Json(usersForSharing);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取可以共享查询的用户时发生错误: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Policy = "CanShareQueries")]
        public async Task<IActionResult> GetQueryShareUsers(int queryId)
        {
            try
            {
                // 获取当前用户ID
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                // 检查查询是否存在
                var query = await _queryBuilderService.GetSavedQueryByIdAsync(queryId);
                if (query == null)
                {
                    return NotFound(new { error = "查询不存在" });
                }
                
                // 检查用户权限
                if (query.CreatedBy != User.Identity.Name && query.UserId != userId)
                {
                    return Forbid();
                }
                
                // 获取查询共享的用户
                var sharedUsers = await _queryBuilderService.GetQueryShareUsersAsync(queryId);
                
                // 构建结果对象
                var result = sharedUsers.Select(u => new { 
                    userId = u.UserId, 
                    userName = u.UserName 
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取查询共享的用户时发生错误: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class QueryViewModel
    {
        public List<string> Tables { get; set; }
        public List<string> Columns { get; set; }
        public List<string> JoinConditions { get; set; }
        public List<string> WhereConditions { get; set; }
        public List<string> OrderBy { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public string SqlQuery { get; set; }
        public string QueryName { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
    }

    public class SaveQueryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SqlQuery { get; set; }
        public List<string> Tables { get; set; }
        public List<string> Columns { get; set; }
        public List<string> WhereConditions { get; set; }
        public List<string> OrderBy { get; set; }
        public List<string> JoinConditions { get; set; }
    }

    public class ShareQueryViewModel
    {
        public int QueryId { get; set; }
        public List<string> UserNames { get; set; }
    }
} 