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
            
            Console.WriteLine($"Index: �û��ѵ�¼��userId: {userId}");
            
            // ��ȡ�û�������ʵı�
            var tables = await _queryBuilderService.GetAllowedTablesForUserAsync(userId);
            Console.WriteLine($"Index: ��ȡ��tables.Count����");
            
            // ��ȡ�û���������в�ѯ
            var savedQueries = await _queryBuilderService.GetSavedQueriesAsync(userId);
            
            // ��ȡ�����ѯ���û�
            Dictionary<int, List<string>> sharedQueryUsers = new Dictionary<int, List<string>>();
            foreach (var query in savedQueries)
            {
                // ��ȡ�����ѯ���û�
                if (query.CreatedBy == User.Identity.Name)
                {
                    var sharedUsers = await _queryBuilderService.GetQueryShareUsersAsync(query.Id);
                    if (sharedUsers.Any())
                    {
                        sharedQueryUsers[query.Id] = sharedUsers.Select(u => u.UserName).ToList();
                    }
                }
            }
            
            // ����û���������в�ѯ
            Console.WriteLine($"Index: ��ȡ��savedQueries.Count����ѯ");
            foreach (var query in savedQueries)
            {
                Console.WriteLine($"��ѯID: {query.Id}, ��ѯ����: {query.Name}, ������: {query.CreatedBy}");
            }
            
            // ����û��Ƿ���Ȩ�ޱ����ѯ�������ѯ�͵�������
            bool canSaveQueries = await _userService.HasPermissionAsync(userId, SystemPermissions.SaveQueries);
            bool canShareQueries = await _userService.HasPermissionAsync(userId, SystemPermissions.ShareQueries);
            bool canExportData = await _userService.HasPermissionAsync(userId, SystemPermissions.ExportData);
            
            Console.WriteLine($"�û�Ȩ��: CanSaveQueries={canSaveQueries}, CanShareQueries={canShareQueries}, CanExportData={canExportData}");
            
            ViewBag.Tables = tables;
            ViewBag.SavedQueries = savedQueries;
            ViewBag.CanSaveQueries = canSaveQueries;
            ViewBag.CanShareQueries = canShareQueries;
            ViewBag.CanExportData = canExportData;
            ViewBag.SharedQueryUsers = sharedQueryUsers;
            
            Console.WriteLine("Index: �û���¼�ɹ���������ͼ");
            
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
                
                // ��Columns����
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
                return BadRequest(new { error = $"��ȡColumnsʱ��������: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteQuery([FromBody] QueryViewModel query)
        {
            try
            {
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                // ����û��Ƿ���Ȩ�޵�������
                bool canExport = await _userService.HasPermissionAsync(userId, SystemPermissions.ExportData);
                ViewBag.CanExport = canExport;

                // ȷ��SQL��ѯ����ԷֺŽ�β
                if (!string.IsNullOrEmpty(query.SqlQuery) && !query.SqlQuery.TrimEnd().EndsWith(";"))
                {
                    query.SqlQuery = query.SqlQuery.TrimEnd() + ";";
                }

                // ��ȡ��ѯ������
                int totalRows = await _queryBuilderService.GetTotalRowsAsync(
                    query.Tables,
                    query.Columns,
                    query.JoinConditions ?? new List<string>(),
                    query.WhereConditions ?? new List<string>(),
                    query.Parameters ?? new Dictionary<string, object>()
                );

                // ִ�в�ѯ
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

                // ��ȡ��ѯ���
                int pageSize = query.PageSize ?? 0;
                int totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalRows / pageSize) : 1;
                
                // ��ȡ������SQL��ѯ���
                string sqlQuery = _queryBuilderService.BuildSqlQuery(
                    query.Tables,
                    query.Columns,
                    query.JoinConditions ?? new List<string>(),
                    query.WhereConditions ?? new List<string>(),
                    query.OrderBy ?? new List<string>()
                );

                // �����������
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
                // ��ȡ�û�ID
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { error = "�޷�ʶ��ǰ�û�" });
                }
                
                // ��֤�û�Ȩ��
                bool canExport = await _userService.HasPermissionAsync(userId, SystemPermissions.ExportData);
                if (!canExport)
                {
                    return Forbid();
                }
                
                // ��֤��������
                if (query == null || query.Tables == null || query.Tables.Count == 0)
                {
                    return BadRequest(new { error = "����������Ч����ȷ����ѡ���" });
                }
                
                Console.WriteLine($"��ʼ����Excel: ������:{query.Tables.Count}, ������:{query.Columns?.Count ?? 0}");
                
                // ȷ��Columns��Ϊnull
                if (query.Columns == null)
                {
                    query.Columns = new List<string>();
                }
                
                // ִ�в�ѯ
                var dataTable = await _queryBuilderService.ExecuteQueryAsync(
                    query.Tables,
                    query.Columns,
                    query.JoinConditions ?? new List<string>(),
                    query.WhereConditions ?? new List<string>(),
                    query.OrderBy ?? new List<string>(),
                    query.Parameters ?? new Dictionary<string, object>()
                );
                
                Console.WriteLine($"��ѯִ����ɣ���ȡ��{dataTable.Rows.Count}�����ݣ���ʼ����Excel");

                // ����Excel
                // ȷ���ļ�����Ϊ��
                string sheetName = !string.IsNullOrEmpty(query.QueryName) ? query.QueryName : "��ѯ����";
                
                byte[] fileContents = _excelExportService.ExportToExcel(
                    dataTable, 
                    sheetName, 
                    $"��ѯ����: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}"
                );
                
                Console.WriteLine($"Excel������ɣ��ļ���С: {fileContents.Length} �ֽ�");

                // �����ļ�������ѯ����+���ڣ�
                string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                string fileName = $"{sheetName}_{dateStr}.xlsx";
                
                // ����Excel�ļ�
                return File(
                    fileContents, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"����Excelʱ��������: {ex.Message}");
                Console.WriteLine($"�����ջ: {ex.StackTrace}");
                return BadRequest(new { error = $"����Excelʧ��: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "CanExportData")]
        public async Task<IActionResult> ExportToPDF([FromBody] QueryViewModel query)
        {
            try
            {
                // ��ȡ�û�ID
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { error = "�޷�ʶ��ǰ�û�" });
                }
                
                // ��֤�û�Ȩ��
                bool canExport = await _userService.HasPermissionAsync(userId, SystemPermissions.ExportData);
                if (!canExport)
                {
                    return Forbid();
                }
                
                // ��֤��������
                if (query == null || query.Tables == null || query.Tables.Count == 0)
                {
                    return BadRequest(new { error = "����������Ч����ȷ����ѡ���" });
                }
                
                Console.WriteLine($"��ʼ����PDF: ������:{query.Tables.Count}, ������:{query.Columns?.Count ?? 0}");
                
                // ȷ��Columns��Ϊnull
                if (query.Columns == null)
                {
                    query.Columns = new List<string>();
                }
                
                // ִ�в�ѯ
                var dataTable = await _queryBuilderService.ExecuteQueryAsync(
                    query.Tables,
                    query.Columns,
                    query.JoinConditions ?? new List<string>(),
                    query.WhereConditions ?? new List<string>(),
                    query.OrderBy ?? new List<string>(),
                    query.Parameters ?? new Dictionary<string, object>()
                );
                
                Console.WriteLine($"��ѯִ����ɣ���ȡ��{dataTable.Rows.Count}�����ݣ���ʼ����PDF");

                // ����PDF
                string reportTitle = !string.IsNullOrEmpty(query.QueryName) ? query.QueryName : "��ѯ����";
                
                var pdfOptions = new PdfExportOptions
                {
                    Title = reportTitle,
                    Author = "BZK��ѯϵͳ",
                    IncludeTimestamp = true,
                    IncludePageNumbers = true,
                    MaxRowsPerPage = 50,
                    LandscapeOrientation = true  // ���ú��򲼾�
                };
                
                byte[] fileContents = await _pdfExportService.ExportToPdfAsync(dataTable, pdfOptions);
                
                Console.WriteLine($"PDF������ɣ��ļ���С: {fileContents.Length} �ֽ�");

                // �����ļ�������ѯ����+���ڣ�
                string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                string fileName = $"{reportTitle}_{dateStr}.pdf";
                
                // ����PDF�ļ�
                return File(
                    fileContents, 
                    "application/pdf",
                    fileName
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"����PDFʱ��������: {ex.Message}");
                Console.WriteLine($"�����ջ: {ex.StackTrace}");
                return BadRequest(new { error = $"����PDFʧ��: {ex.Message}" });
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
                
                Console.WriteLine($"SaveQuery: �û��ѵ�¼��userId: {userId}, ���ڱ����ѯ '{model.Name}'");
                
                // ����ѯ�����Ƿ��Ѵ���
                bool nameExists = await _queryBuilderService.CheckQueryNameExistsAsync(userId, model.Name, model.Id);
                if (nameExists)
                {
                    return BadRequest(new { error = "��ѯ�����Ѵ��ڣ�����Ĳ�ѯ����" });
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
                
                // �����ѯ�ɹ�
                var savedQuery = await _queryBuilderService.GetSavedQueryByIdAsync(query.Id);
                if (savedQuery == null)
                {
                    Console.WriteLine($"SaveQuery: ��ѯ����ʧ�ܣ��������ݿ�����: {query.Id} ��ѯ");
                    return BadRequest(new { error = "��ѯ����ʧ�ܣ��������ݿ�����" });
                }
                
                Console.WriteLine($"SaveQuery: ��ѯ����ɹ���id: {query.Id}, joinConditions: {savedQuery.JoinConditions}");
                
                return Json(new { success = true, id = query.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"�����ѯʱ��������: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SavedQueries()
        {
            string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            Console.WriteLine($"SavedQueries: �û��ѵ�¼��userId: {userId}, ���ڻ�ȡ���в�ѯ");
            
            // ��ȡ�û�������ʵı�
            var tables = await _queryBuilderService.GetAllowedTablesForUserAsync(userId);
            
            // ��ȡ�û���������в�ѯ
            var savedQueries = await _queryBuilderService.GetSavedQueriesAsync(userId);
            
            Console.WriteLine($"SavedQueries: ��ȡ��savedQueries.Count����ѯ");
            
            // ����û��Ƿ���Ȩ�ޱ����ѯ�������ѯ�͵�������
            bool canSaveQueries = await _userService.HasPermissionAsync(userId, SystemPermissions.SaveQueries);
            bool canShareQueries = await _userService.HasPermissionAsync(userId, SystemPermissions.ShareQueries);
            bool canExportData = await _userService.HasPermissionAsync(userId, SystemPermissions.ExportData);
            
            ViewBag.Tables = tables;
            ViewBag.SavedQueries = savedQueries;
            ViewBag.CanSaveQueries = canSaveQueries;
            ViewBag.CanShareQueries = canShareQueries;
            ViewBag.CanExportData = canExportData;
            ViewBag.ShowSavedQueries = true; // ��ʾ���в�ѯ
            
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
                    return BadRequest(new { error = "�޷�ʶ��ǰ�û�" });
                }
                
                var queries = await _queryBuilderService.GetSavedQueriesAsync(userId);
                var query = queries.FirstOrDefault(q => q.Id == id);

                if (query == null)
                {
                    Console.WriteLine($"��ѯID�����ڣ�userId: {userId}, id: {id}");
                    return NotFound(new { error = $"��ѯID�����ڣ�userId: {userId}, id: {id}" });
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
                    Console.WriteLine($"JSON�����л�����: {ex.Message}");
                    return BadRequest(new { error = $"��ѯ�����л�ʧ��: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"��ȡ��ѯʱ��������: {ex.Message}");
                return BadRequest(new { error = $"��ȡ��ѯʧ��: {ex.Message}" });
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
                // �����������
                if (model == null)
                {
                    Console.WriteLine("�����ѯʱ��������: ��������Ϊ��");
                    return BadRequest(new { error = "��������Ϊ��" });
                }
                
                if (model.QueryId <= 0)
                {
                    Console.WriteLine($"�����ѯʱ��������: ��ѯID��Ч {model.QueryId}");
                    return BadRequest(new { error = "��ѯID��Ч" });
                }
                
                // ����û�Ȩ��
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                string userName = User.Identity.Name;
                
                if (model.UserNames == null)
                {
                    Console.WriteLine("�����ѯʱ��������: �û��б�Ϊ��");
                    return BadRequest(new { error = "�û��б�Ϊ��" });
                }
                
                if (model.UserNames.Count == 0)
                {
                    Console.WriteLine($"�û� {userName} �����ѯ {model.QueryId} ʧ�ܣ�û��ѡ���û�");
                }
                else
                {
                    Console.WriteLine($"�û� {userName} �����ѯ {model.QueryId} �ɹ�������� {string.Join(", ", model.UserNames)}");
                }
                
                // ����ѯ�Ƿ����
                var query = await _queryBuilderService.GetSavedQueryByIdAsync(model.QueryId);
                if (query == null)
                {
                    Console.WriteLine($"��ѯ�����ڣ�userId: {userId}, queryId: {model.QueryId}");
                    return NotFound(new { error = "��ѯ������" });
                }
                
                // ����û�Ȩ��
                if (query.CreatedBy != userName)
                {
                    Console.WriteLine($"�û� {userName} û��Ȩ�޹����ѯ {model.QueryId}");
                    return BadRequest(new { error = "�û�û��Ȩ�޹����ѯ" });
                }
                
                // �����ѯ
                await _queryBuilderService.ShareQueryAsync(model.QueryId, userId, model.UserNames);
                
                if (model.UserNames.Count == 0)
                {
                    Console.WriteLine($"��ѯ {model.QueryId} ����ɹ���û�й���������û�");
                    return Json(new { success = true, message = "��ѯ����ɹ���û�й���������û�" });
                }
                else
                {
                    Console.WriteLine($"��ѯ {model.QueryId} ����ɹ�������� {string.Join(", ", model.UserNames)}");
                    return Json(new { success = true, message = "��ѯ����ɹ�������������û�" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"�����ѯʱ��������: {ex.Message}");
                Console.WriteLine($"�����ջ: {ex.StackTrace}");
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
                // ��ȡ��ǰ�û�ID
                string currentUserId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                // ��ȡ�����û�
                var allUsers = await _userService.GetAllUsersAsync();
                
                // ��ȡ���Թ����ѯ���û�
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
                Console.WriteLine($"��ȡ���Թ����ѯ���û�ʱ��������: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Policy = "CanShareQueries")]
        public async Task<IActionResult> GetQueryShareUsers(int queryId)
        {
            try
            {
                // ��ȡ��ǰ�û�ID
                string userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                // ����ѯ�Ƿ����
                var query = await _queryBuilderService.GetSavedQueryByIdAsync(queryId);
                if (query == null)
                {
                    return NotFound(new { error = "��ѯ������" });
                }
                
                // ����û�Ȩ��
                if (query.CreatedBy != User.Identity.Name && query.UserId != userId)
                {
                    return Forbid();
                }
                
                // ��ȡ��ѯ������û�
                var sharedUsers = await _queryBuilderService.GetQueryShareUsersAsync(queryId);
                
                // �����������
                var result = sharedUsers.Select(u => new { 
                    userId = u.UserId, 
                    userName = u.UserName 
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"��ȡ��ѯ������û�ʱ��������: {ex.Message}");
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