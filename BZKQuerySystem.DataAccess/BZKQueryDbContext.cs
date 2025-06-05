using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace BZKQuerySystem.DataAccess
{
    public class BZKQueryDbContext : DbContext
    {
        public BZKQueryDbContext(DbContextOptions<BZKQueryDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // 默认连接字符串，仅用于开发测试或工具使用
                optionsBuilder.UseSqlServer("Server=localhost;Database=ZBKQuerySystem;User Id=sa;Password=123;TrustServerCertificate=True;");
            }
        }

        // 动态表和数据库架构信息，用于查询构建器
        public DbSet<TableInfo> TableInfos { get; set; }
        public DbSet<ColumnInfo> ColumnInfos { get; set; }
        public DbSet<SavedQuery> SavedQueries { get; set; }
        public DbSet<AllowedTable> AllowedTables { get; set; }
        public DbSet<QueryShare> QueryShares { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<ApplicationRole> Roles { get; set; }
        public DbSet<RoleClaim> RoleClaims { get; set; }
        
        // 审计日志
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置实体关系和约束
            modelBuilder.Entity<TableInfo>()
                .HasMany(t => t.Columns)
                .WithOne(c => c.Table)
                .HasForeignKey(c => c.TableId);

            modelBuilder.Entity<SavedQuery>()
                .HasIndex(q => q.Name)
                .IsUnique();

            modelBuilder.Entity<AllowedTable>()
                .HasIndex(t => new { t.UserId, t.TableName })
                .IsUnique();

            // 配置SavedQuery与QueryShare的关系
            modelBuilder.Entity<QueryShare>()
                .HasOne(qs => qs.Query)
                .WithMany(q => q.SharedUsers)
                .HasForeignKey(qs => qs.QueryId)
                .OnDelete(DeleteBehavior.Cascade); // 删除查询时级联删除分享记录

            modelBuilder.Entity<QueryShare>()
                .HasIndex(qs => new { qs.QueryId, qs.UserId })
                .IsUnique(); // 确保同一查询不能重复分享给同一用户

            // 配置UserRole复合键
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });
                
            // 配置UserRole的关系
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);
                
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);
                
            // 配置RoleClaim的关系
            modelBuilder.Entity<RoleClaim>()
                .HasOne(rc => rc.Role)
                .WithMany(r => r.RoleClaims)
                .HasForeignKey(rc => rc.RoleId);
                
            // 配置AuditLog实体
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.EventType).IsRequired();
                
                // 添加索引
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => new { e.UserId, e.Timestamp });
            });
        }
    }

    // 显示数据库中的表
    public class TableInfo
    {
        public int Id { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsView { get; set; } = false; // 新增标识是否为视图（保留）
        public virtual ICollection<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
    }

    // 显示数据库中的列
    public class ColumnInfo
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPrimaryKey { get; set; }
        public bool IsNullable { get; set; }
        public virtual TableInfo Table { get; set; } = null!;
    }

    // 保存查询图
    public class SavedQuery
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SqlQuery { get; set; } = string.Empty;
        public string TablesIncluded { get; set; } = string.Empty;
        public string ColumnsIncluded { get; set; } = string.Empty;
        public string FilterConditions { get; set; } = string.Empty;
        public string SortOrder { get; set; } = string.Empty;
        public string JoinConditions { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // 属
        public string CreatedBy { get; set; } = string.Empty; // 查询用户
        public bool IsShared { get; set; } = false; // 是否共享
        
        // 查询分享
        public virtual ICollection<QueryShare> SharedUsers { get; set; } = new List<QueryShare>();
    }

    // 用户允许的表
    public class AllowedTable
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public bool CanRead { get; set; }
        public bool CanExport { get; set; }
    }

    // 查询分享
    public class QueryShare
    {
        public int Id { get; set; }
        public int QueryId { get; set; }
        public string UserId { get; set; } = string.Empty; // 用户ID
        public DateTime SharedAt { get; set; }
        public string SharedBy { get; set; } = string.Empty; // 执行ID
        
        // 查询
        public virtual SavedQuery Query { get; set; } = null!;
    }

    /// <summary>
    /// 审计日志实体
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;
        
        public string Details { get; set; } = string.Empty;
        
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
        
        public AuditEventType EventType { get; set; }
    }

    /// <summary>
    /// 审计日志类型
    /// </summary>
    public enum AuditEventType
    {
        UserAction = 1,
        QueryExecution = 2,
        DataExport = 3,
        SecurityEvent = 4,
        SystemEvent = 5
    }

    /// <summary>
    /// 审计日志查询模型
    /// </summary>
    public class AuditLogQuery
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// 统计信息
    /// </summary>
    public class AuditStatistics
    {
        public int TotalLogs { get; set; }
        public int QueryExecutions { get; set; }
        public int DataExports { get; set; }
        public int SecurityEvents { get; set; }
        public DateTime OldestLog { get; set; }
        public DateTime NewestLog { get; set; }
        public List<TopUserActivity> TopUsers { get; set; } = new();
    }

    /// <summary>
    /// 用户活动
    /// </summary>
    public class TopUserActivity
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int ActivityCount { get; set; }
    }
} 