using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BZKQuerySystem.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<ApplicationRole> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RoleClaim> RoleClaims { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置用户实体
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserName).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // 配置角色实体
            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // 配置用户角色实体
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasOne(e => e.User)
                    .WithMany(e => e.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                    .WithMany(e => e.UserRoles)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置角色声明实体
            modelBuilder.Entity<RoleClaim>(entity =>
            {
                entity.ToTable("RoleClaims");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Role)
                    .WithMany(e => e.RoleClaims)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 添加默认管理员角色和用户
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // 添加管理员角色
            var adminRoleId = Guid.NewGuid().ToString();
            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = adminRoleId,
                    Name = "Administrator",
                    NormalizedName = "ADMINISTRATOR",
                    Description = "系统管理员，拥有所有权限",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                }
            );

            // 添加系统管理员权限
            modelBuilder.Entity<RoleClaim>().HasData(
                new RoleClaim
                {
                    Id = 1,
                    RoleId = adminRoleId,
                    ClaimType = "Permission",
                    ClaimValue = SystemPermissions.SystemAdmin
                }
            );

            // 添加admin用户
            var adminUserId = Guid.NewGuid().ToString();
            modelBuilder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = adminUserId,
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@example.com",
                    NormalizedEmail = "ADMIN@EXAMPLE.COM",
                    EmailConfirmed = true,
                    // 使用简单密码哈希，密码是"123"
                    // 这是使用固定盐值手动计算的哈希，确保与UserService的验证方法兼容
                    PasswordHash = "ARYEPnXlZJlUm5bCHsxaBOJ4b3eQQFI22KCPCXR5vO8UnlIQ", 
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    PhoneNumber = "13800138000",
                    DisplayName = "系统管理员",
                    Department = "IT部门",
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    LockoutEnabled = false
                }
            );

            // 将admin用户添加到管理员角色
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole
                {
                    UserId = adminUserId,
                    RoleId = adminRoleId
                }
            );
        }
    }
} 