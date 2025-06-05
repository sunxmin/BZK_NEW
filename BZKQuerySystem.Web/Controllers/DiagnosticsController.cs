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
                _logger.LogInformation("���ڻ�ȡ���н�ɫ��Ϣ");
                
                var roles = await _dbContext.Roles
                    .Include(r => r.RoleClaims)
                    .Include(r => r.UserRoles)
                    .ToListAsync();
                
                _logger.LogInformation($"�����ݿ��ȡ�� {roles.Count} ����ɫ");
                
                var sb = new StringBuilder();
                sb.AppendLine($"���ҵ� {roles.Count} ����ɫ��");
                
                foreach (var role in roles)
                {
                    sb.AppendLine($"��ɫID: {role.Id}");
                    sb.AppendLine($"��ɫ����: {role.Name}");
                    sb.AppendLine($"��ɫ����: {role.Description}");
                    sb.AppendLine($"��׼������: {role.NormalizedName}");
                    sb.AppendLine($"�������: {role.ConcurrencyStamp}");
                    sb.AppendLine($"�û���: {role.UserRoles?.Count ?? 0}");
                    sb.AppendLine($"Ȩ����: {role.RoleClaims?.Count ?? 0}");
                    
                    if (role.RoleClaims != null && role.RoleClaims.Any())
                    {
                        sb.AppendLine("Ȩ���б�:");
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
                _logger.LogError(ex, "��ȡ��ɫ�б�ʱ��������");
                return StatusCode(500, $"����: {ex.Message}\n��ջ����: {ex.StackTrace}");
            }
        }
        
        [HttpGet("db-connection")]
        public IActionResult TestDbConnection()
        {
            try
            {
                _logger.LogInformation("���ڲ������ݿ�����");
                
                bool canConnect = _dbContext.Database.CanConnect();
                string provider = _dbContext.Database.ProviderName;
                string connectionString = _dbContext.Database.GetConnectionString();
                
                // ��ȫ�������������ַ����е�����
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
                _logger.LogError(ex, "�������ݿ�����ʱ��������");
                return StatusCode(500, $"����: {ex.Message}\n��ջ����: {ex.StackTrace}");
            }
        }
        
        [HttpGet("tempdata-test")]
        public IActionResult TempDataTest()
        {
            TempData["TestMessage"] = $"������Ϣ - {DateTime.Now}";
            return Ok("TempData�����ã��뷵�ؽ�ɫ����ҳ��鿴");
        }

        [HttpGet("/diagnostics/db")]
        public async Task<IActionResult> CheckDatabase()
        {
            var result = new StringBuilder();
            result.AppendLine("<h3>���ݿ����</h3>");

            // ��������ַ���
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            result.AppendLine($"<p>�����ַ���: {connectionString}</p>");

            try
            {
                // �������ݿ�����
                var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                result.AppendLine("<p style='color:green'>���ݿ����ӳɹ�!</p>");

                // ����ɫ��
                result.AppendLine("<h4>��ɫ����Ϣ:</h4>");
                var roles = await _dbContext.Roles.ToListAsync();
                result.AppendLine($"<p>��ɫ���¼��: {roles.Count}</p>");
                
                result.AppendLine("<table border='1' cellpadding='5'>");
                result.AppendLine("<tr><th>ID</th><th>����</th><th>����</th></tr>");
                foreach (var role in roles)
                {
                    result.AppendLine($"<tr><td>{role.Id}</td><td>{role.Name}</td><td>{role.Description}</td></tr>");
                }
                result.AppendLine("</table>");

                // ����ɫȨ�ޱ�
                result.AppendLine("<h4>��ɫȨ�ޱ���Ϣ:</h4>");
                var roleClaims = await _dbContext.RoleClaims.ToListAsync();
                result.AppendLine($"<p>��ɫȨ�ޱ��¼��: {roleClaims.Count}</p>");
                
                result.AppendLine("<table border='1' cellpadding='5'>");
                result.AppendLine("<tr><th>ID</th><th>��ɫID</th><th>Ȩ������</th></tr>");
                foreach (var claim in roleClaims)
                {
                    result.AppendLine($"<tr><td>{claim.Id}</td><td>{claim.RoleId}</td><td>{claim.ClaimValue}</td></tr>");
                }
                result.AppendLine("</table>");

                connection.Close();
            }
            catch (Exception ex)
            {
                result.AppendLine($"<p style='color:red'>���ݿ����Ӵ���: {ex.Message}</p>");
                result.AppendLine($"<pre>{ex.StackTrace}</pre>");
            }

            return Content(result.ToString(), "text/html", Encoding.UTF8);
        }

        [HttpGet("/diagnostics/create-role-test")]
        public async Task<IActionResult> TestCreateRole()
        {
            var result = new StringBuilder();
            result.AppendLine("<h3>��ɫ��������</h3>");

            try
            {
                // 1. ������ݿ�����
                result.AppendLine("<h4>����1: ������ݿ�����</h4>");
                var connection = _dbContext.Database.GetDbConnection();
                var connectionState = connection.State.ToString();
                result.AppendLine($"<p>����״̬: {connectionState}</p>");
                
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                    result.AppendLine("<p style='color:green'>���ݿ������Ѵ�</p>");
                }

                // 2. �������Խ�ɫ
                result.AppendLine("<h4>����2: ���Դ������Խ�ɫ</h4>");
                var roleName = $"���Խ�ɫ_{DateTime.Now:yyyyMMddHHmmss}";
                result.AppendLine($"<p>��ɫ����: {roleName}</p>");
                
                var role = new ApplicationRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpper(),
                    Description = "���Խ�ɫ����",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                
                // ֱ��ʹ��DbContext������ɫ
                result.AppendLine("<h5>2.1 ֱ��ʹ��DbContext������ɫ</h5>");
                _dbContext.Roles.Add(role);
                int saveResult = await _dbContext.SaveChangesAsync();
                result.AppendLine($"<p>������: {saveResult} ����¼��Ӱ��</p>");
                
                if (saveResult > 0)
                {
                    result.AppendLine($"<p style='color:green'>��ɫ '{roleName}' �����ɹ�, ID: {role.Id}</p>");
                    
                    // 3. ��ӽ�ɫȨ��
                    result.AppendLine("<h4>����3: ��ӽ�ɫȨ��</h4>");
                    var permissions = new[] { SystemPermissions.ViewUsers, SystemPermissions.ViewAllTables };
                    
                    foreach (var permission in permissions)
                    {
                        var roleClaim = new RoleClaim
                        {
                            Id = 0, // ����ID
                            RoleId = role.Id,
                            ClaimType = "Permission",
                            ClaimValue = permission
                        };
                        
                        _dbContext.RoleClaims.Add(roleClaim);
                    }
                    
                    int permissionSaveResult = await _dbContext.SaveChangesAsync();
                    result.AppendLine($"<p>Ȩ�ޱ�����: {permissionSaveResult} ����¼��Ӱ��</p>");
                    
                    if (permissionSaveResult > 0)
                    {
                        result.AppendLine($"<p style='color:green'>��ɫȨ����ӳɹ�</p>");
                    }
                    else
                    {
                        result.AppendLine($"<p style='color:red'>��ɫȨ�����ʧ��</p>");
                    }
                    
                    // 4. ��֤��ɫ��Ȩ���Ƿ��Ѵ���
                    result.AppendLine("<h4>����4: ��֤��ɫ��Ȩ��</h4>");
                    
                    var createdRole = await _dbContext.Roles.FindAsync(role.Id);
                    if (createdRole != null)
                    {
                        result.AppendLine($"<p style='color:green'>��ɫ��֤�ɹ�: {createdRole.Name}</p>");
                    }
                    else
                    {
                        result.AppendLine($"<p style='color:red'>��ɫ��֤ʧ��: �Ҳ�����ɫ</p>");
                    }
                    
                    var createdClaims = await _dbContext.RoleClaims
                        .Where(rc => rc.RoleId == role.Id)
                        .ToListAsync();
                    
                    result.AppendLine($"<p>�ҵ� {createdClaims.Count} ����ɫȨ��</p>");
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
                    result.AppendLine($"<p style='color:red'>��ɫ����ʧ��</p>");
                }
            }
            catch (Exception ex)
            {
                result.AppendLine($"<h4 style='color:red'>��������</h4>");
                result.AppendLine($"<p>������Ϣ: {ex.Message}</p>");
                result.AppendLine($"<p>��������: {ex.ToString()}</p>");
                
                if (ex.InnerException != null)
                {
                    result.AppendLine($"<p>�ڲ�����: {ex.InnerException.Message}</p>");
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
            result.AppendLine("<h1>���ύ���</h1>");
            
            result.AppendLine("<h2>������</h2>");
            result.AppendLine("<table border='1' cellpadding='5'>");
            result.AppendLine("<tr><th>��</th><th>ֵ</th></tr>");
            
            foreach (var key in form.Keys)
            {
                if (key == "permissions")
                {
                    var values = form[key];
                    result.AppendLine($"<tr><td>{key}</td><td>���飺{values.Count} ��<ul>");
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
            
            result.AppendLine("<h2>ԭʼ������</h2>");
            result.AppendLine("<pre>");
            foreach (var key in form.Keys)
            {
                var values = form[key];
                result.AppendLine($"{key} = {string.Join(", ", values)}");
            }
            result.AppendLine("</pre>");
            
            result.AppendLine("<p><a href='/diagnostics/form-test'>���ز��Ա�</a></p>");
            
            return Content(result.ToString(), "text/html", Encoding.UTF8);
        }
    }
} 