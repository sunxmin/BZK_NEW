using BZKQuerySystem.DataAccess;
using BZKQuerySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace BZKQuerySystem.Web.Controllers
{
    [Authorize(Policy = "ManageUsers")]
    public class UserController : Controller
    {
        private readonly UserService _userService;
        private readonly BZKQueryDbContext _dbContext;

        public UserController(UserService userService, BZKQueryDbContext dbContext)
        {
            _userService = userService;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        public async Task<IActionResult> Create()
        {
            var roles = await _userService.GetAllRolesAsync();
            ViewBag.Roles = roles;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = new ApplicationUser
                    {
                        UserName = model.UserName,
                        Email = model.Email,
                        DisplayName = model.DisplayName,
                        Department = model.Department,
                        EmailConfirmed = true,
                        PhoneNumber = model.PhoneNumber,
                        PhoneNumberConfirmed = true,
                        TwoFactorEnabled = false,
                        LockoutEnabled = false,
                        AccessFailedCount = 0
                    };

                    await _userService.CreateUserAsync(user, model.Password, model.RoleIds);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // ����������ʾ�����ˣ����¼��ؽ�ɫ�б�
            var roles = await _userService.GetAllRolesAsync();
            ViewBag.Roles = roles;
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var users = await _userService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userService.GetAllRolesAsync();
            ViewBag.Roles = roles;

            var userRoles = await _userService.GetUserRolesAsync(id);
            var model = new UserEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Department = user.Department,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                RoleIds = userRoles.Select(r => r.Id).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = new ApplicationUser
                    {
                        Id = model.Id,
                        UserName = model.UserName,
                        Email = model.Email,
                        DisplayName = model.DisplayName,
                        Department = model.Department,
                        PhoneNumber = model.PhoneNumber,
                        IsActive = model.IsActive
                    };

                    await _userService.UpdateUserAsync(user, model.RoleIds);

                    // ����ṩ�������룬����������
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        await _userService.ResetPasswordAsync(model.Id, model.NewPassword);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // ����������ʾ�����ˣ����¼��ؽ�ɫ�б�
            var roles = await _userService.GetAllRolesAsync();
            ViewBag.Roles = roles;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // ������ʱ��������Indexҳ����ʾ����
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // ��ɫ����
        public async Task<IActionResult> Roles()
        {
            try
            {
                // ���ؽ�ɫ�б�
                var roles = await _userService.GetAllRolesAsync();
                return View(roles);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"���ؽ�ɫ�б�ʧ��: {ex.Message}";
                return View(new List<ApplicationRole>());
            }
        }

        public IActionResult CreateRole()
        {
            ViewBag.Permissions = GetAllPermissions();
            return View(new RoleViewModel { Id = Guid.NewGuid().ToString() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> CreateRole(RoleViewModel model, IFormCollection form)
        {
            try
            {
                // �������Ƿ����Ȩ������
                var permissionValues = form["permissions"];
                
                if (permissionValues.Count > 0)
                {
                    model.Permissions = permissionValues.ToList();
                }
                
                if (ModelState.IsValid)
                {
                    try
                    {
                        var role = new ApplicationRole
                        {
                            Name = model.Name,
                            Description = model.Description
                        };

                        // ���һ��������־
                        using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                // �ֶ������ɫ��Ȩ��
                                
                                // 1. ���ý�ɫ����
                                role.Id = model.Id; // ʹ��ģ���е�Id
                                role.NormalizedName = role.Name.ToUpper();
                                role.ConcurrencyStamp = Guid.NewGuid().ToString();
                                
                                // 2. �����ɫ�����ݿ�
                                _dbContext.Roles.Add(role);
                                int roleResult = await _dbContext.SaveChangesAsync();
                                
                                // 3. ���Ȩ��
                                if (model.Permissions != null && model.Permissions.Any())
                                {
                                    
                                    foreach (var permission in model.Permissions)
                                    {
                                        var roleClaim = new RoleClaim
                                        {
                                            RoleId = role.Id,
                                            ClaimType = "Permission",
                                            ClaimValue = permission
                                        };
                                        _dbContext.RoleClaims.Add(roleClaim);
                                    }
                                    
                                    int permissionResult = await _dbContext.SaveChangesAsync();
                                }
                                
                                // 4. �ύ����
                                await transaction.CommitAsync();
                                
                                // ��ӳɹ���Ϣ
                                TempData["SuccessMessage"] = $"��ɫ '{model.Name}' �ѳɹ�����";
                                
                                return RedirectToAction(nameof(Roles));
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"������ɫʧ��: {ex.Message}");
                    }
                }
                else
                {
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            ModelState.AddModelError("", $"- {state.Key}: {error.ErrorMessage}");
                        }
                    }
                }

                // ����������ʾ�����ˣ����¼���Ȩ���б�
                ViewBag.Permissions = GetAllPermissions();
                return View(model);
            }
            catch (Exception globalEx)
            {
                ModelState.AddModelError("", $"������ɫʱ����δԤ�ڴ���: {globalEx.Message}");
                
                ViewBag.Permissions = GetAllPermissions();
                return View(model);
            }
        }

        // ��ӵ��Զ˵�
        [HttpGet("/user/debug-create-role")]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> DebugCreateRole()
        {
            try
            {
                // ����һ�����Խ�ɫ
                var roleName = $"���Խ�ɫ_{DateTime.Now:yyyyMMddHHmmss}";
                var role = new ApplicationRole
                {
                    Name = roleName,
                    Description = "���Դ����Ĳ��Խ�ɫ"
                };

                var permissions = new List<string> { SystemPermissions.ViewUsers };

                using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var result = await _userService.CreateRoleAsync(role, permissions);
                        await transaction.CommitAsync();
                        return Content($"<h2>���Խ�ɫ�����ɹ�</h2><p>ID: {result.Id}</p><p>����: {result.Name}</p>", "text/html");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return Content($"<h2>���Խ�ɫ����ʧ��</h2><p>����: {ex.Message}</p><pre>{ex.StackTrace}</pre>", "text/html");
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"<h2>���Թ��̷�������</h2><p>{ex.Message}</p><pre>{ex.StackTrace}</pre>", "text/html");
            }
        }

        public async Task<IActionResult> EditRole(string id)
        {
            var roles = await _userService.GetAllRolesAsync();
            var role = roles.FirstOrDefault(r => r.Id == id);
            if (role == null)
            {
                return NotFound();
            }

            var permissions = await _userService.GetRolePermissionsAsync(id);
            ViewBag.Permissions = GetAllPermissions();

            var model = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = permissions
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> EditRole(RoleViewModel model, IFormCollection form)
        {
            // �������Ƿ����Ȩ������
            var permissionValues = form["permissions"];
            
            if (permissionValues.Count > 0)
            {
                model.Permissions = permissionValues.ToList();
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    var role = new ApplicationRole
                    {
                        Id = model.Id,
                        Name = model.Name,
                        Description = model.Description
                    };

                    using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            // �ֶ����½�ɫ
                            
                            // 1. ��ȡ���н�ɫ
                            var existingRole = await _dbContext.Roles.FindAsync(model.Id);
                            if (existingRole == null)
                            {
                                throw new Exception($"�Ҳ���IDΪ {model.Id} �Ľ�ɫ");
                            }
                            
                            // 2. ���½�ɫ����
                            existingRole.Description = model.Description;
                            if (existingRole.Name != model.Name)
                            {
                                // ��������Ƿ��Ѵ���
                                bool nameExists = await _dbContext.Roles
                                    .AnyAsync(r => r.Name == model.Name && r.Id != model.Id);
                                if (nameExists)
                                {
                                    throw new Exception($"��ɫ�� '{model.Name}' �Ѵ���");
                                }
                                
                                existingRole.Name = model.Name;
                                existingRole.NormalizedName = model.Name.ToUpper();
                            }
                            
                            // 3. �����ɫ����
                            int roleResult = await _dbContext.SaveChangesAsync();
                            
                            // 4. ����Ȩ��
                            // ��ɾ������Ȩ��
                            var existingClaims = await _dbContext.RoleClaims
                                .Where(rc => rc.RoleId == model.Id)
                                .ToListAsync();
                            
                            if (existingClaims.Any())
                            {
                                _dbContext.RoleClaims.RemoveRange(existingClaims);
                                int removeResult = await _dbContext.SaveChangesAsync();
                            }
                            
                            // �����Ȩ��
                            if (model.Permissions != null && model.Permissions.Any())
                            {
                                foreach (var permission in model.Permissions)
                                {
                                    var roleClaim = new RoleClaim
                                    {
                                        RoleId = model.Id,
                                        ClaimType = "Permission",
                                        ClaimValue = permission
                                    };
                                    _dbContext.RoleClaims.Add(roleClaim);
                                }
                                
                                int permissionResult = await _dbContext.SaveChangesAsync();
                            }
                            
                            // 5. �ύ����
                            await transaction.CommitAsync();
                            
                            // ��ӳɹ���Ϣ
                            TempData["SuccessMessage"] = $"��ɫ '{model.Name}' �ѳɹ�����";
                            
                            return RedirectToAction(nameof(Roles));
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"���½�ɫʧ��: {ex.Message}");
                }
            }
            else
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        ModelState.AddModelError("", $"- {state.Key}: {error.ErrorMessage}");
                    }
                }
            }

            // ����������ʾ�����ˣ����¼���Ȩ���б�
            ViewBag.Permissions = GetAllPermissions();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string id)
        {
            try
            {
                await _userService.DeleteRoleAsync(id);
                return RedirectToAction(nameof(Roles));
            }
            catch (Exception ex)
            {
                // ������ʱ��������Rolesҳ����ʾ����
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Roles));
            }
        }

        private List<PermissionViewModel> GetAllPermissions()
        {
            // ��ȡ����Ԥ�����Ȩ��
            var permissionFields = typeof(SystemPermissions).GetFields()
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            var permissions = new List<PermissionViewModel>();
            foreach (var field in permissionFields)
            {
                var permissionName = (string)field.GetValue(null);
                var displayName = field.Name; // ʹ���ֶ�����Ϊ��ʾ����

                // Ϊÿ��Ȩ�޶����������ƺ�˵��
                string chineseName = displayName;
                string description = "";

                switch (permissionName)
                {
                    case SystemPermissions.ManageUsers:
                        chineseName = "�û�����";
                        description = "�������༭��ɾ��ϵͳ�û�";
                        break;
                    case SystemPermissions.ViewUsers:
                        chineseName = "�鿴�û�";
                        description = "�鿴ϵͳ�е��û��б�";
                        break;
                    case SystemPermissions.ManageTables:
                        chineseName = "�������ݱ�";
                        description = "����ϵͳ�пɲ�ѯ�����ݱ�";
                        break;
                    case SystemPermissions.ViewAllTables:
                        chineseName = "�鿴���б�";
                        description = "�鿴ϵͳ�����е����ݱ�";
                        break;
                    case SystemPermissions.SaveQueries:
                        chineseName = "�����ѯ";
                        description = "�����Զ����ѯ����";
                        break;
                    case SystemPermissions.ShareQueries:
                        chineseName = "�����ѯ";
                        description = "����ѯ����������û�";
                        break;
                    case SystemPermissions.RunComplexQueries:
                        chineseName = "���Ӳ�ѯ";
                        description = "ִ�и��ӵĶ���ѯ";
                        break;
                    case SystemPermissions.ExportData:
                        chineseName = "��������";
                        description = "����ѯ�������ΪExcel";
                        break;
                    case SystemPermissions.SystemAdmin:
                        chineseName = "ϵͳ����Ա";
                        description = "ӵ��ϵͳ������Ȩ��";
                        break;
                }

                permissions.Add(new PermissionViewModel
                {
                    Name = permissionName,
                    DisplayName = chineseName,
                    Description = description
                });
            }

            return permissions;
        }

        // ��ȡ�����û������ڷ����ܣ�
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                
                // ֻ������Ҫ���û���Ϣ����������������
                var userList = users.Select(u => new 
                {
                    userId = u.Id,
                    userName = u.UserName,
                    displayName = u.DisplayName,
                    department = u.Department
                }).ToList();
                
                return Json(userList);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class UserViewModel
    {
        [Required(ErrorMessage = "�û�������Ϊ��")]
        [Display(Name = "�û���")]
        public string UserName { get; set; }
        
        [Required(ErrorMessage = "���벻��Ϊ��")]
        [Display(Name = "����")]
        public string Password { get; set; }
        
        [Display(Name = "�����ʼ�")]
        [EmailAddress(ErrorMessage = "��������Ч�ĵ����ʼ���ַ")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "��ʾ���Ʋ���Ϊ��")]
        [Display(Name = "��ʾ����")]
        public string DisplayName { get; set; }
        
        [Display(Name = "��������")]
        public string Department { get; set; }
        
        [Display(Name = "��ϵ�绰")]
        [Phone(ErrorMessage = "��������Ч�ĵ绰����")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "������ɫ")]
        public List<string> RoleIds { get; set; } = new List<string>();
    }

    public class UserEditViewModel
    {
        public string Id { get; set; }
        
        [Required(ErrorMessage = "�û�������Ϊ��")]
        [Display(Name = "�û���")]
        public string UserName { get; set; }
        
        [Display(Name = "�����ʼ�")]
        [EmailAddress(ErrorMessage = "��������Ч�ĵ����ʼ���ַ")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "��ʾ���Ʋ���Ϊ��")]
        [Display(Name = "��ʾ����")]
        public string DisplayName { get; set; }
        
        [Display(Name = "��������")]
        public string Department { get; set; }
        
        [Display(Name = "��ϵ�绰")]
        [Phone(ErrorMessage = "��������Ч�ĵ绰����")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "�Ƿ�����")]
        public bool IsActive { get; set; }
        
        [Display(Name = "������")]
        public string NewPassword { get; set; } // ��ѡ������ṩ����������
        
        [Display(Name = "������ɫ")]
        public List<string> RoleIds { get; set; } = new List<string>();
    }

    public class RoleViewModel
    {
        [Required(ErrorMessage = "Id�ֶ��Ǳ����")]
        public string Id { get; set; }
        
        [Required(ErrorMessage = "��ɫ���Ʋ���Ϊ��")]
        [Display(Name = "��ɫ����")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "��ɫ��������Ϊ��")]
        [Display(Name = "��ɫ����")]
        public string Description { get; set; }
        
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class PermissionViewModel
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }
} 