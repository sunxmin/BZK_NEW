using BZKQuerySystem.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;

namespace BZKQuerySystem.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "SystemAdmin")]
    public class DiagnosticsController : Controller
    {
        private readonly BZKQueryDbContext _dbContext;
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAntiforgery _antiforgery;

        public DiagnosticsController(BZKQueryDbContext dbContext, ILogger<DiagnosticsController> logger, IConfiguration configuration, IAntiforgery antiforgery)
        {
            _dbContext = dbContext;
            _logger = logger;
            _configuration = configuration;
            _antiforgery = antiforgery;
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                _logger.LogInformation("正在获取所有角色信息");
                
                var roles = await _dbContext.Roles
                    .Include(r => r.RoleClaims)
                    .Include(r => r.UserRoles)
                    .ToListAsync();
                
                _logger.LogInformation($"从数据库获取到 {roles.Count} 个角色");
                
                var sb = new StringBuilder();
                sb.AppendLine($"共找到 {roles.Count} 个角色：");
                
                foreach (var role in roles)
                {
                    sb.AppendLine($"角色ID: {role.Id}");
                    sb.AppendLine($"角色名称: {role.Name}");
                    sb.AppendLine($"角色描述: {role.Description}");
                    sb.AppendLine($"标准化名称: {role.NormalizedName}");
                    sb.AppendLine($"并发标记: {role.ConcurrencyStamp}");
                    sb.AppendLine($"用户数: {role.UserRoles?.Count ?? 0}");
                    sb.AppendLine($"权限数: {role.RoleClaims?.Count ?? 0}");
                    
                    if (role.RoleClaims != null && role.RoleClaims.Any())
                    {
                        sb.AppendLine("权限列表:");
                        foreach (var claim in role.RoleClaims)
                        {
                            sb.AppendLine($"  - {claim.ClaimType}: {claim.ClaimValue}");
                        }
                    }
                    
                    sb.AppendLine("------------------------------");
                }
                
                return Ok(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色列表时发生错误");
                return StatusCode(500, $"错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        [HttpGet("db-connection")]
        public IActionResult TestDbConnection()
        {
            try
            {
                _logger.LogInformation("正在测试数据库连接");
                
                bool canConnect = _dbContext.Database.CanConnect();
                string provider = _dbContext.Database.ProviderName;
                string connectionString = _dbContext.Database.GetConnectionString();
                
                // 安全处理：隐藏连接字符串中的密码
                if (!string.IsNullOrEmpty(connectionString))
                {
                    connectionString = connectionString.Replace(connectionString.Split(';')
                        .FirstOrDefault(s => s.StartsWith("Password=", StringComparison.OrdinalIgnoreCase)) ?? "", "Password=******");
                }
                
                return Ok(new 
                {
                    CanConnect = canConnect,
                    Provider = provider,
                    ConnectionString = connectionString,
                    CurrentTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试数据库连接时发生错误");
                return StatusCode(500, $"错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        [HttpGet("tempdata-test")]
        public IActionResult TempDataTest()
        {
            TempData["TestMessage"] = $"测试消息 - {DateTime.Now}";
            return Ok("TempData已设置，请返回角色管理页面查看");
        }

        [HttpGet("/diagnostics/db")]
        public async Task<IActionResult> CheckDatabase()
        {
            var result = new StringBuilder();
            result.AppendLine("<h3>数据库诊断</h3>");

            // 检查连接字符串
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            result.AppendLine($"<p>连接字符串: {connectionString}</p>");

            try
            {
                // 测试数据库连接
                var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                result.AppendLine("<p style='color:green'>数据库连接成功!</p>");

                // 检查角色表
                result.AppendLine("<h4>角色表信息:</h4>");
                var roles = await _dbContext.Roles.ToListAsync();
                result.AppendLine($"<p>角色表记录数: {roles.Count}</p>");
                
                result.AppendLine("<table border='1' cellpadding='5'>");
                result.AppendLine("<tr><th>ID</th><th>名称</th><th>描述</th></tr>");
                foreach (var role in roles)
                {
                    result.AppendLine($"<tr><td>{role.Id}</td><td>{role.Name}</td><td>{role.Description}</td></tr>");
                }
                result.AppendLine("</table>");

                // 检查角色权限表
                result.AppendLine("<h4>角色权限表信息:</h4>");
                var roleClaims = await _dbContext.RoleClaims.ToListAsync();
                result.AppendLine($"<p>角色权限表记录数: {roleClaims.Count}</p>");
                
                result.AppendLine("<table border='1' cellpadding='5'>");
                result.AppendLine("<tr><th>ID</th><th>角色ID</th><th>权限名称</th></tr>");
                foreach (var claim in roleClaims)
                {
                    result.AppendLine($"<tr><td>{claim.Id}</td><td>{claim.RoleId}</td><td>{claim.ClaimValue}</td></tr>");
                }
                result.AppendLine("</table>");

                connection.Close();
            }
            catch (Exception ex)
            {
                result.AppendLine($"<p style='color:red'>数据库连接错误: {ex.Message}</p>");
                result.AppendLine($"<pre>{ex.StackTrace}</pre>");
            }

            return Content(result.ToString(), "text/html", Encoding.UTF8);
        }

        [HttpGet("/diagnostics/create-role-test")]
        public async Task<IActionResult> TestCreateRole()
        {
            var result = new StringBuilder();
            result.AppendLine("<h3>角色创建测试</h3>");

            try
            {
                // 1. 检查数据库连接
                result.AppendLine("<h4>步骤1: 检查数据库连接</h4>");
                var connection = _dbContext.Database.GetDbConnection();
                var connectionState = connection.State.ToString();
                result.AppendLine($"<p>连接状态: {connectionState}</p>");
                
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                    result.AppendLine("<p style='color:green'>数据库连接已打开</p>");
                }

                // 2. 创建测试角色
                result.AppendLine("<h4>步骤2: 尝试创建测试角色</h4>");
                var roleName = $"测试角色_{DateTime.Now:yyyyMMddHHmmss}";
                result.AppendLine($"<p>角色名称: {roleName}</p>");
                
                var role = new ApplicationRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpper(),
                    Description = "测试角色描述",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                
                // 直接使用DbContext创建角色
                result.AppendLine("<h5>2.1 直接使用DbContext创建角色</h5>");
                _dbContext.Roles.Add(role);
                int saveResult = await _dbContext.SaveChangesAsync();
                result.AppendLine($"<p>保存结果: {saveResult} 条记录受影响</p>");
                
                if (saveResult > 0)
                {
                    result.AppendLine($"<p style='color:green'>角色 '{roleName}' 创建成功, ID: {role.Id}</p>");
                    
                    // 3. 添加角色权限
                    result.AppendLine("<h4>步骤3: 添加角色权限</h4>");
                    var permissions = new[] { SystemPermissions.ViewUsers, SystemPermissions.ViewAllTables };
                    
                    foreach (var permission in permissions)
                    {
                        var roleClaim = new RoleClaim
                        {
                            Id = 0, // 自增ID
                            RoleId = role.Id,
                            ClaimType = "Permission",
                            ClaimValue = permission
                        };
                        
                        _dbContext.RoleClaims.Add(roleClaim);
                    }
                    
                    int permissionSaveResult = await _dbContext.SaveChangesAsync();
                    result.AppendLine($"<p>权限保存结果: {permissionSaveResult} 条记录受影响</p>");
                    
                    if (permissionSaveResult > 0)
                    {
                        result.AppendLine($"<p style='color:green'>角色权限添加成功</p>");
                    }
                    else
                    {
                        result.AppendLine($"<p style='color:red'>角色权限添加失败</p>");
                    }
                    
                    // 4. 验证角色和权限是否已创建
                    result.AppendLine("<h4>步骤4: 验证角色和权限</h4>");
                    
                    var createdRole = await _dbContext.Roles.FindAsync(role.Id);
                    if (createdRole != null)
                    {
                        result.AppendLine($"<p style='color:green'>角色验证成功: {createdRole.Name}</p>");
                    }
                    else
                    {
                        result.AppendLine($"<p style='color:red'>角色验证失败: 找不到角色</p>");
                    }
                    
                    var createdClaims = await _dbContext.RoleClaims
                        .Where(rc => rc.RoleId == role.Id)
                        .ToListAsync();
                    
                    result.AppendLine($"<p>找到 {createdClaims.Count} 个角色权限</p>");
                    if (createdClaims.Any())
                    {
                        result.AppendLine("<ul>");
                        foreach (var claim in createdClaims)
                        {
                            result.AppendLine($"<li>{claim.ClaimValue}</li>");
                        }
                        result.AppendLine("</ul>");
                    }
                }
                else
                {
                    result.AppendLine($"<p style='color:red'>角色创建失败</p>");
                }
            }
            catch (Exception ex)
            {
                result.AppendLine($"<h4 style='color:red'>发生错误</h4>");
                result.AppendLine($"<p>错误消息: {ex.Message}</p>");
                result.AppendLine($"<p>错误详情: {ex.ToString()}</p>");
                
                if (ex.InnerException != null)
                {
                    result.AppendLine($"<p>内部错误: {ex.InnerException.Message}</p>");
                }
            }

            return Content(result.ToString(), "text/html", Encoding.UTF8);
        }

        [HttpGet("/diagnostics/form-test")]
        public IActionResult FormTest()
        {
            return View();
        }

        [HttpPost("/diagnostics/form-submit")]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult FormSubmit(IFormCollection form)
        {
            var result = new StringBuilder();
            result.AppendLine("<h1>表单提交结果</h1>");
            
            result.AppendLine("<h2>表单数据</h2>");
            result.AppendLine("<table border='1' cellpadding='5'>");
            result.AppendLine("<tr><th>键</th><th>值</th></tr>");
            
            foreach (var key in form.Keys)
            {
                if (key == "permissions")
                {
                    var values = form[key];
                    result.AppendLine($"<tr><td>{key}</td><td>数组：{values.Count} 项<ul>");
                    foreach (var val in values)
                    {
                        result.AppendLine($"<li>{val}</li>");
                    }
                    result.AppendLine("</ul></td></tr>");
                }
                else
                {
                    result.AppendLine($"<tr><td>{key}</td><td>{form[key]}</td></tr>");
                }
            }
            result.AppendLine("</table>");
            
            result.AppendLine("<h2>原始表单数据</h2>");
            result.AppendLine("<pre>");
            foreach (var key in form.Keys)
            {
                var values = form[key];
                result.AppendLine($"{key} = {string.Join(", ", values)}");
            }
            result.AppendLine("</pre>");
            
            result.AppendLine("<p><a href='/diagnostics/form-test'>返回测试表单</a></p>");
            
            return Content(result.ToString(), "text/html", Encoding.UTF8);
        }
    }
} 