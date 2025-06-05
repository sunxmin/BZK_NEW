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

            // 如果到这里，表示出错了，重新加载角色列表
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

                    // 如果提供了新密码，则重置密码
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

            // 如果到这里，表示出错了，重新加载角色列表
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
                // 设置临时数据以在Index页面显示错误
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // 角色管理
        public async Task<IActionResult> Roles()
        {
            try
            {
                // 加载角色列表
                var roles = await _userService.GetAllRolesAsync();
                return View(roles);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"加载角色列表失败: {ex.Message}";
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
                // 检查表单中是否包含权限数据
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

                        // 添加一个调试日志
                        using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                // 手动保存角色和权限
                                
                                // 1. 设置角色属性
                                role.Id = model.Id; // 使用模型中的Id
                                role.NormalizedName = role.Name.ToUpper();
                                role.ConcurrencyStamp = Guid.NewGuid().ToString();
                                
                                // 2. 保存角色到数据库
                                _dbContext.Roles.Add(role);
                                int roleResult = await _dbContext.SaveChangesAsync();
                                
                                // 3. 添加权限
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
                                
                                // 4. 提交事务
                                await transaction.CommitAsync();
                                
                                // 添加成功消息
                                TempData["SuccessMessage"] = $"角色 '{model.Name}' 已成功创建";
                                
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
                        ModelState.AddModelError("", $"创建角色失败: {ex.Message}");
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

                // 如果到这里，表示出错了，重新加载权限列表
                ViewBag.Permissions = GetAllPermissions();
                return View(model);
            }
            catch (Exception globalEx)
            {
                ModelState.AddModelError("", $"创建角色时发生未预期错误: {globalEx.Message}");
                
                ViewBag.Permissions = GetAllPermissions();
                return View(model);
            }
        }

        // 添加调试端点
        [HttpGet("/user/debug-create-role")]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> DebugCreateRole()
        {
            try
            {
                // 创建一个测试角色
                var roleName = $"测试角色_{DateTime.Now:yyyyMMddHHmmss}";
                var role = new ApplicationRole
                {
                    Name = roleName,
                    Description = "调试创建的测试角色"
                };

                var permissions = new List<string> { SystemPermissions.ViewUsers };

                using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var result = await _userService.CreateRoleAsync(role, permissions);
                        await transaction.CommitAsync();
                        return Content($"<h2>测试角色创建成功</h2><p>ID: {result.Id}</p><p>名称: {result.Name}</p>", "text/html");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return Content($"<h2>测试角色创建失败</h2><p>错误: {ex.Message}</p><pre>{ex.StackTrace}</pre>", "text/html");
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"<h2>调试过程发生错误</h2><p>{ex.Message}</p><pre>{ex.StackTrace}</pre>", "text/html");
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
            // 检查表单中是否包含权限数据
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
                            // 手动更新角色
                            
                            // 1. 获取现有角色
                            var existingRole = await _dbContext.Roles.FindAsync(model.Id);
                            if (existingRole == null)
                            {
                                throw new Exception($"找不到ID为 {model.Id} 的角色");
                            }
                            
                            // 2. 更新角色属性
                            existingRole.Description = model.Description;
                            if (existingRole.Name != model.Name)
                            {
                                // 检查名称是否已存在
                                bool nameExists = await _dbContext.Roles
                                    .AnyAsync(r => r.Name == model.Name && r.Id != model.Id);
                                if (nameExists)
                                {
                                    throw new Exception($"角色名 '{model.Name}' 已存在");
                                }
                                
                                existingRole.Name = model.Name;
                                existingRole.NormalizedName = model.Name.ToUpper();
                            }
                            
                            // 3. 保存角色更新
                            int roleResult = await _dbContext.SaveChangesAsync();
                            
                            // 4. 更新权限
                            // 先删除现有权限
                            var existingClaims = await _dbContext.RoleClaims
                                .Where(rc => rc.RoleId == model.Id)
                                .ToListAsync();
                            
                            if (existingClaims.Any())
                            {
                                _dbContext.RoleClaims.RemoveRange(existingClaims);
                                int removeResult = await _dbContext.SaveChangesAsync();
                            }
                            
                            // 添加新权限
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
                            
                            // 5. 提交事务
                            await transaction.CommitAsync();
                            
                            // 添加成功消息
                            TempData["SuccessMessage"] = $"角色 '{model.Name}' 已成功更新";
                            
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
                    ModelState.AddModelError("", $"更新角色失败: {ex.Message}");
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

            // 如果到这里，表示出错了，重新加载权限列表
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
                // 设置临时数据以在Roles页面显示错误
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Roles));
            }
        }

        private List<PermissionViewModel> GetAllPermissions()
        {
            // 获取所有预定义的权限
            var permissionFields = typeof(SystemPermissions).GetFields()
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            var permissions = new List<PermissionViewModel>();
            foreach (var field in permissionFields)
            {
                var permissionName = (string)field.GetValue(null);
                var displayName = field.Name; // 使用字段名作为显示名称

                // 为每个权限定义中文名称和说明
                string chineseName = displayName;
                string description = "";

                switch (permissionName)
                {
                    case SystemPermissions.ManageUsers:
                        chineseName = "用户管理";
                        description = "创建、编辑和删除系统用户";
                        break;
                    case SystemPermissions.ViewUsers:
                        chineseName = "查看用户";
                        description = "查看系统中的用户列表";
                        break;
                    case SystemPermissions.ManageTables:
                        chineseName = "管理数据表";
                        description = "管理系统中可查询的数据表";
                        break;
                    case SystemPermissions.ViewAllTables:
                        chineseName = "查看所有表";
                        description = "查看系统中所有的数据表";
                        break;
                    case SystemPermissions.SaveQueries:
                        chineseName = "保存查询";
                        description = "保存自定义查询条件";
                        break;
                    case SystemPermissions.ShareQueries:
                        chineseName = "分享查询";
                        description = "将查询分享给其他用户";
                        break;
                    case SystemPermissions.RunComplexQueries:
                        chineseName = "复杂查询";
                        description = "执行复杂的多表查询";
                        break;
                    case SystemPermissions.ExportData:
                        chineseName = "导出数据";
                        description = "将查询结果导出为Excel";
                        break;
                    case SystemPermissions.SystemAdmin:
                        chineseName = "系统管理员";
                        description = "拥有系统的所有权限";
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

        // 获取所有用户（用于分享功能）
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                
                // 只返回需要的用户信息，不包含敏感数据
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
        [Required(ErrorMessage = "用户名不能为空")]
        [Display(Name = "用户名")]
        public string UserName { get; set; }
        
        [Required(ErrorMessage = "密码不能为空")]
        [Display(Name = "密码")]
        public string Password { get; set; }
        
        [Display(Name = "电子邮件")]
        [EmailAddress(ErrorMessage = "请输入有效的电子邮件地址")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "显示名称不能为空")]
        [Display(Name = "显示名称")]
        public string DisplayName { get; set; }
        
        [Display(Name = "所属部门")]
        public string Department { get; set; }
        
        [Display(Name = "联系电话")]
        [Phone(ErrorMessage = "请输入有效的电话号码")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "所属角色")]
        public List<string> RoleIds { get; set; } = new List<string>();
    }

    public class UserEditViewModel
    {
        public string Id { get; set; }
        
        [Required(ErrorMessage = "用户名不能为空")]
        [Display(Name = "用户名")]
        public string UserName { get; set; }
        
        [Display(Name = "电子邮件")]
        [EmailAddress(ErrorMessage = "请输入有效的电子邮件地址")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "显示名称不能为空")]
        [Display(Name = "显示名称")]
        public string DisplayName { get; set; }
        
        [Display(Name = "所属部门")]
        public string Department { get; set; }
        
        [Display(Name = "联系电话")]
        [Phone(ErrorMessage = "请输入有效的电话号码")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "是否启用")]
        public bool IsActive { get; set; }
        
        [Display(Name = "新密码")]
        public string NewPassword { get; set; } // 可选，如果提供则重置密码
        
        [Display(Name = "所属角色")]
        public List<string> RoleIds { get; set; } = new List<string>();
    }

    public class RoleViewModel
    {
        [Required(ErrorMessage = "Id字段是必需的")]
        public string Id { get; set; }
        
        [Required(ErrorMessage = "角色名称不能为空")]
        [Display(Name = "角色名称")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "角色描述不能为空")]
        [Display(Name = "角色描述")]
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