using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BZKQuerySystem.DataAccess
{
    // 系统用户
    public class ApplicationUser
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string NormalizedUserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string SecurityStamp { get; set; } = string.Empty;
        public string ConcurrencyStamp { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; }
        
        // 导航属性
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    // 系统角色
    public class ApplicationRole
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string ConcurrencyStamp { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // 导航属性
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RoleClaim> RoleClaims { get; set; } = new List<RoleClaim>();
    }

    // 用户-角色关联
    public class UserRole
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        
        // 导航属性
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ApplicationRole Role { get; set; } = null!;
    }

    // 角色权限
    public class RoleClaim
    {
        public int Id { get; set; }
        public string RoleId { get; set; } = string.Empty;
        public string ClaimType { get; set; } = string.Empty;
        public string ClaimValue { get; set; } = string.Empty;
        
        // 导航属性
        public virtual ApplicationRole Role { get; set; } = null!;
    }

    // 系统权限预定义
    public static class SystemPermissions
    {
        // 用户管理权限
        public const string ManageUsers = "ManageUsers"; // 用户管理
        public const string ViewUsers = "ViewUsers"; // 查看用户列表
        
        // 数据权限
        public const string ManageTables = "ManageTables"; // 数据管理权限
        public const string ViewAllTables = "ViewAllTables"; // 查看表列表
        
        // 查询权限
        public const string SaveQueries = "SaveQueries"; // 保存查询
        public const string ShareQueries = "ShareQueries"; // 分享查询
        public const string RunComplexQueries = "RunComplexQueries"; // 运行复杂查询
        public const string ExportData = "ExportData"; // 导出数据
        
        // 系统管理权限
        public const string SystemAdmin = "SystemAdmin"; // 系统管理员最高权限
    }
} 