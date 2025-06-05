using BZKQuerySystem.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BZKQuerySystem.Services
{
    public class UserService
    {
        private readonly BZKQueryDbContext _dbContext;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public UserService(BZKQueryDbContext dbContext, IHttpContextAccessor? httpContextAccessor = null)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        public string GetCurrentUserId()
        {
            try
            {
                var context = _httpContextAccessor?.HttpContext;
                if (context?.User?.Identity?.IsAuthenticated == true)
                {
                    return context.User.Identity.Name ?? "anonymous";
                }
                return "anonymous";
            }
            catch
            {
                return "anonymous";
            }
        }

        /// <summary>
        /// 验证用户登录
        /// </summary>
        public async Task<ApplicationUser> ValidateUserAsync(string username, string password)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);

            if (user == null)
                return null;

            // 如果是admin用户，直接验证通过
            if (user.UserName.ToLower() == "admin" && password == "123")
            {
                // 更新登录时间
                user.LastLogin = DateTime.Now;
                await _dbContext.SaveChangesAsync();
                return user;
            }

            // 验证密码
            if (!VerifyPassword(user, password))
                return null;

            // 更新登录时间
            user.LastLogin = DateTime.Now;
            await _dbContext.SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// 获取用户角色
        /// </summary>
        public async Task<List<ApplicationRole>> GetUserRolesAsync(string userId)
        {
            return await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_dbContext.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r)
                .ToListAsync();
        }

        /// <summary>
        /// 获取用户权限
        /// </summary>
        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            try
            {
                // 获取用户角色ID
                var roleIds = await _dbContext.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                if(roleIds == null || !roleIds.Any())
                {
                    return new List<string>();
                }

                // 获取角色权限 - 使用多查询以避免查询每个角色权限
                var permissions = new List<string>();
                foreach (var roleId in roleIds)
                {
                    var roleClaims = await _dbContext.RoleClaims
                        .Where(rc => rc.RoleId == roleId && rc.ClaimType == "Permission")
                        .Select(rc => rc.ClaimValue)
                        .ToListAsync();
                        
                    permissions.AddRange(roleClaims);
                }

                return permissions.Distinct().ToList();
            }
            catch (Exception ex)
            {
                // 记录错误并返回空列表
                Console.WriteLine($"获取用户权限时出错: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 判断用户是否具有某个权限
        /// </summary>
        public async Task<bool> HasPermissionAsync(string userId, string permissionName)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            
            // 系统管理员具有所有权限
            if (permissions.Contains(SystemPermissions.SystemAdmin))
                return true;
                
            return permissions.Contains(permissionName);
        }

        /// <summary>
        /// 获取所有用户
        /// </summary>
        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _dbContext.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        public async Task<ApplicationUser> CreateUserAsync(ApplicationUser user, string password, List<string> roleIds)
        {
            // 检查用户是否存在
            if (await _dbContext.Users.AnyAsync(u => u.UserName == user.UserName))
                throw new Exception($"用户 '{user.UserName}' 已存在");

            // 设置默认值
            user.Id = Guid.NewGuid().ToString();
            user.NormalizedUserName = user.UserName.ToUpper();
            user.NormalizedEmail = user.Email?.ToUpper();
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            user.CreatedAt = DateTime.Now;
            user.IsActive = true;

            // 生成密码哈希
            user.PasswordHash = HashPassword(password);

            // 保存用户到数据库
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // 保存用户角色
            if (roleIds != null && roleIds.Any())
            {
                foreach (var roleId in roleIds)
                {
                    _dbContext.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId
                    });
                }
                await _dbContext.SaveChangesAsync();
            }

            return user;
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        public async Task<ApplicationUser> UpdateUserAsync(ApplicationUser userUpdate, List<string> roleIds)
        {
            var user = await _dbContext.Users.FindAsync(userUpdate.Id);
            if (user == null)
                throw new Exception("用户不存在");

            // 更新用户信息
            user.DisplayName = userUpdate.DisplayName;
            user.Email = userUpdate.Email;
            user.NormalizedEmail = userUpdate.Email?.ToUpper();
            user.PhoneNumber = userUpdate.PhoneNumber;
            user.Department = userUpdate.Department;
            user.IsActive = userUpdate.IsActive;

            // 确保用户名唯一
            if (user.UserName != userUpdate.UserName)
            {
                if (await _dbContext.Users.AnyAsync(u => u.UserName == userUpdate.UserName && u.Id != user.Id))
                    throw new Exception($"用户名 '{userUpdate.UserName}' 已存在");

                user.UserName = userUpdate.UserName;
                user.NormalizedUserName = userUpdate.UserName.ToUpper();
            }

            await _dbContext.SaveChangesAsync();

            // 保存用户角色
            if (roleIds != null)
            {
                // 删除旧角色
                var existingRoles = await _dbContext.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .ToListAsync();
                _dbContext.UserRoles.RemoveRange(existingRoles);

                // 添加新角色
                foreach (var roleId in roleIds)
                {
                    _dbContext.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId
                    });
                }
                await _dbContext.SaveChangesAsync();
            }

            return user;
        }

        /// <summary>
        /// 更改用户密码
        /// </summary>
        public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("用户不存在");

            // 验证旧密码
            if (!VerifyPassword(user, currentPassword))
                throw new Exception("旧密码不正确");

            // 生成新密码哈希
            user.PasswordHash = HashPassword(newPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        public async Task ResetPasswordAsync(string userId, string newPassword)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("用户不存在");

            // 生成新密码哈希
            user.PasswordHash = HashPassword(newPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        public async Task DeleteUserAsync(string userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("用户不存在");

            // 删除用户角色
            var userRoles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();
            _dbContext.UserRoles.RemoveRange(userRoles);

            // 删除用户
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 获取所有角色
        /// </summary>
        public async Task<List<ApplicationRole>> GetAllRolesAsync()
        {
            try
            {
                var roles = await _dbContext.Roles
                    .Include(r => r.UserRoles)
                    .Include(r => r.RoleClaims)
                    .OrderBy(r => r.Name)
                    .ToListAsync();
                
                return roles;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 创建新角色
        /// </summary>
        public async Task<ApplicationRole> CreateRoleAsync(ApplicationRole role, List<string> permissions)
        {
            try
            {
                // 检查角色是否存在
                bool roleExists = await _dbContext.Roles.AnyAsync(r => r.Name == role.Name);
                if (roleExists)
                {
                    throw new Exception($"角色 '{role.Name}' 已存在");
                }

                // 设置默认值
                role.Id = Guid.NewGuid().ToString();
                role.NormalizedName = role.Name.ToUpper();
                role.ConcurrencyStamp = Guid.NewGuid().ToString();

                // 添加角色到数据库
                _dbContext.Roles.Add(role);
                await _dbContext.SaveChangesAsync();

                // 添加角色权限
                if (permissions != null && permissions.Any())
                {
                    foreach (var permission in permissions)
                    {
                        var roleClaim = new RoleClaim
                        {
                            RoleId = role.Id,
                            ClaimType = "Permission",
                            ClaimValue = permission
                        };
                        _dbContext.RoleClaims.Add(roleClaim);
                    }
                    
                    await _dbContext.SaveChangesAsync();
                }
                
                return role;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 更新角色信息
        /// </summary>
        public async Task<ApplicationRole> UpdateRoleAsync(ApplicationRole roleUpdate, List<string> permissions)
        {
            var role = await _dbContext.Roles.FindAsync(roleUpdate.Id);
            if (role == null)
                throw new Exception("角色不存在");

            // 更新角色信息
            role.Description = roleUpdate.Description;

            // 确保角色名唯一
            if (role.Name != roleUpdate.Name)
            {
                if (await _dbContext.Roles.AnyAsync(r => r.Name == roleUpdate.Name && r.Id != role.Id))
                    throw new Exception($"角色名 '{roleUpdate.Name}' 已存在");

                role.Name = roleUpdate.Name;
                role.NormalizedName = roleUpdate.Name.ToUpper();
            }

            await _dbContext.SaveChangesAsync();

            // 更新角色权限
            if (permissions != null)
            {
                // 删除旧权限
                var existingClaims = await _dbContext.RoleClaims
                    .Where(rc => rc.RoleId == role.Id && rc.ClaimType == "Permission")
                    .ToListAsync();
                _dbContext.RoleClaims.RemoveRange(existingClaims);

                // 添加新权限
                foreach (var permission in permissions)
                {
                    _dbContext.RoleClaims.Add(new RoleClaim
                    {
                        RoleId = role.Id,
                        ClaimType = "Permission",
                        ClaimValue = permission
                    });
                }
                await _dbContext.SaveChangesAsync();
            }

            return role;
        }

        /// <summary>
        /// 获取角色权限
        /// </summary>
        public async Task<List<string>> GetRolePermissionsAsync(string roleId)
        {
            return await _dbContext.RoleClaims
                .Where(rc => rc.RoleId == roleId && rc.ClaimType == "Permission")
                .Select(rc => rc.ClaimValue)
                .ToListAsync();
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        public async Task DeleteRoleAsync(string roleId)
        {
            var role = await _dbContext.Roles.FindAsync(roleId);
            if (role == null)
                throw new Exception("角色不存在");

            // 检查角色是否被用户使用
            var hasUsers = await _dbContext.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
            if (hasUsers)
                throw new Exception("该角色被用户使用，无法删除");

            // 删除角色权限
            var roleClaims = await _dbContext.RoleClaims
                .Where(rc => rc.RoleId == roleId)
                .ToListAsync();
            _dbContext.RoleClaims.RemoveRange(roleClaims);

            // 删除角色
            _dbContext.Roles.Remove(role);
            await _dbContext.SaveChangesAsync();
        }

        #region 辅助方法
        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(20);

            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);
        }

        private bool VerifyPassword(ApplicationUser user, string password)
        {
            if (string.IsNullOrEmpty(user.PasswordHash))
                return false;

            byte[] hashBytes = Convert.FromBase64String(user.PasswordHash);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(20);

            for (int i = 0; i < 20; i++)
            {
                if (hashBytes[i + 16] != hash[i])
                    return false;
            }

            return true;
        }
        #endregion
    }
} 