using BZKQuerySystem.DataAccess;
using BZKQuerySystem.Services;
using BZKQuerySystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace BZKQuerySystem.Web.Controllers
{
    [Authorize(Policy = "ManageTables")]
    public class TableController : Controller
    {
        private readonly BZKQueryDbContext _dbContext;
        private readonly QueryBuilderService _queryBuilderService;
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public TableController(
            BZKQueryDbContext dbContext,
            QueryBuilderService queryBuilderService,
            UserService userService,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _queryBuilderService = queryBuilderService;
            _userService = userService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var tables = await _queryBuilderService.GetAllTablesAsync();
            ViewBag.Tables = tables;
            return View();
        }

        public async Task<IActionResult> ManageTablePermissions(int tableId = 0, string tableName = "")
        {
            // ����ṩ�˱�����������ʹ�ñ���
            TableInfo tableInfo = null;
            if (!string.IsNullOrEmpty(tableName))
            {
                tableInfo = await _dbContext.TableInfos
                    .Include(t => t.Columns)
                    .FirstOrDefaultAsync(t => t.TableName == tableName);
            }
            else if (tableId > 0)
            {
                tableInfo = await _dbContext.TableInfos
                    .Include(t => t.Columns)
                    .FirstOrDefaultAsync(t => t.Id == tableId);
            }

            if (tableInfo == null && !string.IsNullOrEmpty(tableName))
            {
                // �������Ϣ�����ڵ��ṩ�˱�����������ʱ����Ϣ����
                tableInfo = new TableInfo
                {
                    TableName = tableName,
                    DisplayName = tableName,
                    Description = $"���ݱ�: {tableName}",
                    Columns = new List<ColumnInfo>() // ȷ��Columns��Ϊ��
                };
            }

            if (tableInfo == null)
            {
                return NotFound();
            }

            // ��ȡ�����û�
            var users = await _userService.GetAllUsersAsync();

            // ��ȡ�û��ı�Ȩ��
            var tablePermissions = await _dbContext.AllowedTables
                .Where(at => at.TableName == tableInfo.TableName)
                .ToListAsync();

            // ������ͼģ��
            var model = new TablePermissionViewModel
            {
                TableInfo = tableInfo,
                UserPermissions = new List<UserTablePermission>()
            };

            foreach (var user in users)
            {
                var permission = tablePermissions.FirstOrDefault(tp => tp.UserId == user.Id);
                model.UserPermissions.Add(new UserTablePermission
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    DisplayName = user.DisplayName ?? user.UserName,
                    TableName = tableInfo.TableName,
                    CanRead = permission?.CanRead ?? false,
                    CanExport = permission?.CanExport ?? false
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTablePermissions(TablePermissionViewModel model)
        {
            // ȷ��Columns��Ϊnull��������֤����
            if (model.TableInfo != null && model.TableInfo.Columns == null)
            {
                model.TableInfo.Columns = new List<ColumnInfo>();
                ModelState.Remove("TableInfo.Columns"); // �Ƴ����ܵĴ���״̬
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage));
                return BadRequest($"����֤ʧ��: {errors}");
            }

            try
            {
                // ֱ��ʹ��SQLɾ���������Ȩ�޼�¼
                using (var connection = new SqlConnection(_dbContext.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        @"DELETE FROM AllowedTables WHERE TableName = @TableName", connection))
                    {
                        command.Parameters.AddWithValue("@TableName", model.TableInfo.TableName);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // ����µ�Ȩ�޼�¼
                foreach (var userPerm in model.UserPermissions)
                {
                    if (userPerm.CanRead) // ֻҪ�в鿴Ȩ�޾ͱ����¼
                    {
                        try
                        {
                            // ���ȳ���ʹ��SQLֱ�Ӳ����¼��ȷ��ͬʱ����CanWrite��CanExport
                            using (var connection = new SqlConnection(_dbContext.Database.GetConnectionString()))
                            {
                                await connection.OpenAsync();
                                using (var command = new SqlCommand(
                                    @"INSERT INTO AllowedTables (UserId, TableName, CanRead, CanExport, CanWrite) 
                                      VALUES (@UserId, @TableName, @CanRead, @CanExport, @CanExport)", connection))
                                {
                                    command.Parameters.AddWithValue("@UserId", userPerm.UserId);
                                    command.Parameters.AddWithValue("@TableName", model.TableInfo.TableName);
                                    command.Parameters.AddWithValue("@CanRead", userPerm.CanRead);
                                    command.Parameters.AddWithValue("@CanExport", userPerm.CanExport);
                                    await command.ExecuteNonQueryAsync();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // ���ֱ��SQL����ʧ�ܣ���¼���󲢼���ʹ�ó��淽��
                            Console.WriteLine($"ʹ��SQLֱ�Ӳ���Ȩ�޼�¼ʧ��: {ex.Message}");
                            
                            // ����ʹ�÷���ΪCanWrite���Ը�ֵ
                            var entity = new AllowedTable
                            {
                                UserId = userPerm.UserId,
                                TableName = model.TableInfo.TableName,
                                CanRead = userPerm.CanRead,
                                CanExport = userPerm.CanExport
                            };
                            
                            // ʹ�÷�������CanWrite����
                            var canWriteProperty = entity.GetType().GetProperty("CanWrite");
                            if (canWriteProperty != null)
                            {
                                canWriteProperty.SetValue(entity, userPerm.CanExport);
                            }
                            
                            _dbContext.AllowedTables.Add(entity);
                            try
                            {
                                await _dbContext.SaveChangesAsync();
                            }
                            catch (Exception saveEx)
                            {
                                Console.WriteLine($"����ʵ��ʱ��������: {saveEx.Message}");
                                // ���ﲻ�׳��쳣�������������������¼
                            }
                        }
                    }
                }

                // ����Ҫ����SaveChangesAsync����Ϊ����ֱ��ʹ��SQLִ�в�����

                TempData["SuccessMessage"] = $"�� '{model.TableInfo.TableName}' ��Ȩ���ѳɹ�����";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // ��ȡ�������쳣��Ϣ�������ڲ��쳣
                string fullErrorMessage = ex.Message;
                var innerException = ex.InnerException;
                while (innerException != null)
                {
                    fullErrorMessage += $" -> {innerException.Message}";
                    innerException = innerException.InnerException;
                }
                
                ModelState.AddModelError("", $"����Ȩ��ʧ��: {fullErrorMessage}");
                
                // ���»�ȡ�����û������¹�����ͼģ��
                var users = await _userService.GetAllUsersAsync();
                var tableInfo = await _dbContext.TableInfos
                    .Include(t => t.Columns)
                    .FirstOrDefaultAsync(t => t.TableName == model.TableInfo.TableName);
                
                // ����Ҳ�������Ϣ��Ϊ�գ�����һ�����п�Columns���ϵ��±���Ϣ
                if (tableInfo == null)
                {
                    if (model.TableInfo != null && model.TableInfo.Columns == null)
                    {
                        model.TableInfo.Columns = new List<ColumnInfo>();
                    }
                }
                else
                {
                    model.TableInfo = tableInfo;
                }
                
                return View("ManageTablePermissions", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> RefreshTableSchema()
        {
            try
            {
                await _queryBuilderService.RefreshDatabaseSchemaAsync();
                TempData["SuccessMessage"] = "���ݿ��ṹ�ѳɹ�ˢ��";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"ˢ�±�ṹʧ��: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> RefreshDatabase()
        {
            await _queryBuilderService.RefreshDatabaseSchemaAsync();
            
            TempData["SuccessMessage"] = "���ݿ�ܹ�ˢ�³ɹ�";
            return RedirectToAction("Index");
        }
        
        // ��ͼ����ҳ��
        [HttpGet]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> Views()
        {
            var views = await _dbContext.TableInfos
                .Where(t => t.IsView)
                .OrderBy(t => t.TableName)
                .ToListAsync();
            
            return View(views);
        }
        
        // ������ͼҳ��
        [HttpGet]
        [Authorize(Policy = "SystemAdmin")]
        public IActionResult CreateView()
        {
            return View(new CreateViewViewModel());
        }
        
        // ������ͼ�ύ
        [HttpPost]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> CreateView(CreateViewViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            
            try
            {
                // ִ�д�����ͼ��SQL
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(model.SqlDefinition, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                
                // ˢ�����ݿ�ܹ�
                await _queryBuilderService.RefreshDatabaseSchemaAsync();
                
                TempData["SuccessMessage"] = "��ͼ�����ɹ�";
                return RedirectToAction("Views");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"������ͼʧ��: {ex.Message}");
                return View(model);
            }
        }
        
        // ɾ����ͼ
        [HttpPost]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> DeleteView(string viewName)
        {
            try
            {
                // ִ��ɾ����ͼ��SQL
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    string sql = $"DROP VIEW IF EXISTS [{viewName}]";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                
                // ˢ�����ݿ�ܹ�
                await _queryBuilderService.RefreshDatabaseSchemaAsync();
                
                return Json(new { success = true, message = "��ͼɾ���ɹ�" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ɾ����ͼʧ��: {ex.Message}" });
            }
        }
        
        // ��ȡ��ͼSQL����
        [HttpGet]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> GetViewDefinition(string viewName)
        {
            try
            {
                string definition = "";
                
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"
                        SELECT m.definition
                        FROM sys.views v
                        JOIN sys.objects o ON v.object_id = o.object_id
                        JOIN sys.schemas s ON v.schema_id = s.schema_id
                        JOIN sys.sql_modules m ON v.object_id = m.object_id
                        WHERE s.name = 'dbo' AND v.name = @viewName";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@viewName", viewName);
                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            definition = result.ToString();
                        }
                    }
                }
                
                return Json(new { success = true, definition });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"��ȡ��ͼ����ʧ��: {ex.Message}" });
            }
        }
        
        // �༭��ͼҳ��
        [HttpGet]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> EditView(string viewName)
        {
            try
            {
                string definition = "";
                
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"
                        SELECT m.definition
                        FROM sys.views v
                        JOIN sys.objects o ON v.object_id = o.object_id
                        JOIN sys.schemas s ON v.schema_id = s.schema_id
                        JOIN sys.sql_modules m ON v.object_id = m.object_id
                        WHERE s.name = 'dbo' AND v.name = @viewName";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@viewName", viewName);
                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            definition = result.ToString();
                        }
                    }
                }
                
                var model = new EditViewViewModel
                {
                    ViewName = viewName,
                    SqlDefinition = definition
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"��ȡ��ͼ����ʧ��: {ex.Message}";
                return RedirectToAction("Views");
            }
        }
        
        // ������ͼ����
        [HttpPost]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> EditView(EditViewViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            
            try
            {
                // ��ɾ��ԭ��ͼ���ٴ�������ͼ
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    
                    // ɾ��ԭ��ͼ
                    string dropSql = $"DROP VIEW IF EXISTS [{model.ViewName}]";
                    using (var command = new SqlCommand(dropSql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    // ��������ͼ
                    using (var command = new SqlCommand(model.SqlDefinition, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                
                // ˢ�����ݿ�ܹ�
                await _queryBuilderService.RefreshDatabaseSchemaAsync();
                
                TempData["SuccessMessage"] = "��ͼ���³ɹ�";
                return RedirectToAction("Views");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"������ͼʧ��: {ex.Message}");
                return View(model);
            }
        }
    }
    
    // ������ͼ��ͼģ��
    public class CreateViewViewModel
    {
        [Required(ErrorMessage = "SQL���岻��Ϊ��")]
        [Display(Name = "SQL����")]
        public string SqlDefinition { get; set; }
    }
    
    // �༭��ͼ��ͼģ��
    public class EditViewViewModel
    {
        [Required(ErrorMessage = "��ͼ���Ʋ���Ϊ��")]
        [Display(Name = "��ͼ����")]
        public string ViewName { get; set; }
        
        [Required(ErrorMessage = "SQL���岻��Ϊ��")]
        [Display(Name = "SQL����")]
        public string SqlDefinition { get; set; }
    }
} 