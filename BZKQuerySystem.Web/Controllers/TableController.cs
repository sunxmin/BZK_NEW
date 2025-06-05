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
            // 如果提供了表名，则优先使用表名
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
                // 如果表信息不存在但提供了表名，创建临时表信息对象
                tableInfo = new TableInfo
                {
                    TableName = tableName,
                    DisplayName = tableName,
                    Description = $"数据表: {tableName}",
                    Columns = new List<ColumnInfo>() // 确保Columns不为空
                };
            }

            if (tableInfo == null)
            {
                return NotFound();
            }

            // 获取所有用户
            var users = await _userService.GetAllUsersAsync();

            // 获取用户的表权限
            var tablePermissions = await _dbContext.AllowedTables
                .Where(at => at.TableName == tableInfo.TableName)
                .ToListAsync();

            // 创建视图模型
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
            // 确保Columns不为null，避免验证错误
            if (model.TableInfo != null && model.TableInfo.Columns == null)
            {
                model.TableInfo.Columns = new List<ColumnInfo>();
                ModelState.Remove("TableInfo.Columns"); // 移除可能的错误状态
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage));
                return BadRequest($"表单验证失败: {errors}");
            }

            try
            {
                // 直接使用SQL删除表的所有权限记录
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

                // 添加新的权限记录
                foreach (var userPerm in model.UserPermissions)
                {
                    if (userPerm.CanRead) // 只要有查看权限就保存记录
                    {
                        try
                        {
                            // 首先尝试使用SQL直接插入记录，确保同时设置CanWrite和CanExport
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
                            // 如果直接SQL插入失败，记录错误并继续使用常规方法
                            Console.WriteLine($"使用SQL直接插入权限记录失败: {ex.Message}");
                            
                            // 尝试使用反射为CanWrite属性赋值
                            var entity = new AllowedTable
                            {
                                UserId = userPerm.UserId,
                                TableName = model.TableInfo.TableName,
                                CanRead = userPerm.CanRead,
                                CanExport = userPerm.CanExport
                            };
                            
                            // 使用反射设置CanWrite属性
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
                                Console.WriteLine($"保存实体时发生错误: {saveEx.Message}");
                                // 这里不抛出异常，让其继续处理其他记录
                            }
                        }
                    }
                }

                // 不需要调用SaveChangesAsync，因为我们直接使用SQL执行操作了

                TempData["SuccessMessage"] = $"表 '{model.TableInfo.TableName}' 的权限已成功更新";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // 获取完整的异常信息，包括内部异常
                string fullErrorMessage = ex.Message;
                var innerException = ex.InnerException;
                while (innerException != null)
                {
                    fullErrorMessage += $" -> {innerException.Message}";
                    innerException = innerException.InnerException;
                }
                
                ModelState.AddModelError("", $"保存权限失败: {fullErrorMessage}");
                
                // 重新获取所有用户并重新构建视图模型
                var users = await _userService.GetAllUsersAsync();
                var tableInfo = await _dbContext.TableInfos
                    .Include(t => t.Columns)
                    .FirstOrDefaultAsync(t => t.TableName == model.TableInfo.TableName);
                
                // 如果找不到表信息或为空，创建一个带有空Columns集合的新表信息
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
                TempData["SuccessMessage"] = "数据库表结构已成功刷新";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"刷新表结构失败: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> RefreshDatabase()
        {
            await _queryBuilderService.RefreshDatabaseSchemaAsync();
            
            TempData["SuccessMessage"] = "数据库架构刷新成功";
            return RedirectToAction("Index");
        }
        
        // 视图管理页面
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
        
        // 创建视图页面
        [HttpGet]
        [Authorize(Policy = "SystemAdmin")]
        public IActionResult CreateView()
        {
            return View(new CreateViewViewModel());
        }
        
        // 创建视图提交
        [HttpPost]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> CreateView(CreateViewViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            
            try
            {
                // 执行创建视图的SQL
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(model.SqlDefinition, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                
                // 刷新数据库架构
                await _queryBuilderService.RefreshDatabaseSchemaAsync();
                
                TempData["SuccessMessage"] = "视图创建成功";
                return RedirectToAction("Views");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"创建视图失败: {ex.Message}");
                return View(model);
            }
        }
        
        // 删除视图
        [HttpPost]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> DeleteView(string viewName)
        {
            try
            {
                // 执行删除视图的SQL
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    string sql = $"DROP VIEW IF EXISTS [{viewName}]";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                
                // 刷新数据库架构
                await _queryBuilderService.RefreshDatabaseSchemaAsync();
                
                return Json(new { success = true, message = "视图删除成功" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"删除视图失败: {ex.Message}" });
            }
        }
        
        // 获取视图SQL定义
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
                return Json(new { success = false, message = $"获取视图定义失败: {ex.Message}" });
            }
        }
        
        // 编辑视图页面
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
                TempData["ErrorMessage"] = $"获取视图定义失败: {ex.Message}";
                return RedirectToAction("Views");
            }
        }
        
        // 更新视图定义
        [HttpPost]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> EditView(EditViewViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            
            try
            {
                // 先删除原视图，再创建新视图
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    
                    // 删除原视图
                    string dropSql = $"DROP VIEW IF EXISTS [{model.ViewName}]";
                    using (var command = new SqlCommand(dropSql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    // 创建新视图
                    using (var command = new SqlCommand(model.SqlDefinition, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
                
                // 刷新数据库架构
                await _queryBuilderService.RefreshDatabaseSchemaAsync();
                
                TempData["SuccessMessage"] = "视图更新成功";
                return RedirectToAction("Views");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"更新视图失败: {ex.Message}");
                return View(model);
            }
        }
    }
    
    // 创建视图视图模型
    public class CreateViewViewModel
    {
        [Required(ErrorMessage = "SQL定义不能为空")]
        [Display(Name = "SQL定义")]
        public string SqlDefinition { get; set; }
    }
    
    // 编辑视图视图模型
    public class EditViewViewModel
    {
        [Required(ErrorMessage = "视图名称不能为空")]
        [Display(Name = "视图名称")]
        public string ViewName { get; set; }
        
        [Required(ErrorMessage = "SQL定义不能为空")]
        [Display(Name = "SQL定义")]
        public string SqlDefinition { get; set; }
    }
} 