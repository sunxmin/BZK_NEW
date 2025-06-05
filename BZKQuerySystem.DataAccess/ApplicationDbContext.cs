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

            // �����û�ʵ��
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserName).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // ���ý�ɫʵ��
            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // �����û���ɫʵ��
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

            // ���ý�ɫ����ʵ��
            modelBuilder.Entity<RoleClaim>(entity =>
            {
                entity.ToTable("RoleClaims");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Role)
                    .WithMany(e => e.RoleClaims)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ���Ĭ�Ϲ���Ա��ɫ���û�
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // ��ӹ���Ա��ɫ
            var adminRoleId = Guid.NewGuid().ToString();
            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = adminRoleId,
                    Name = "Administrator",
                    NormalizedName = "ADMINISTRATOR",
                    Description = "ϵͳ����Ա��ӵ������Ȩ��",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                }
            );

            // ���ϵͳ����ԱȨ��
            modelBuilder.Entity<RoleClaim>().HasData(
                new RoleClaim
                {
                    Id = 1,
                    RoleId = adminRoleId,
                    ClaimType = "Permission",
                    ClaimValue = SystemPermissions.SystemAdmin
                }
            );

            // ���admin�û�
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
                    // ʹ�ü������ϣ��������"123"
                    // ����ʹ�ù̶���ֵ�ֶ�����Ĺ�ϣ��ȷ����UserService����֤��������
                    PasswordHash = "ARYEPnXlZJlUm5bCHsxaBOJ4b3eQQFI22KCPCXR5vO8UnlIQ", 
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    PhoneNumber = "13800138000",
                    DisplayName = "ϵͳ����Ա",
                    Department = "IT����",
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    LockoutEnabled = false
                }
            );

            // ��admin�û���ӵ�����Ա��ɫ
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