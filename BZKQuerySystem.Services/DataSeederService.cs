using BZKQuerySystem.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BZKQuerySystem.Services
{
    public class DataSeederService
    {
        private readonly BZKQueryDbContext _dbContext;
        private readonly string _connectionString;
        private readonly ILogger<DataSeederService> _logger;

        public DataSeederService(BZKQueryDbContext dbContext, string connectionString, ILogger<DataSeederService> logger)
        {
            _dbContext = dbContext;
            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// 检查数据库连接状态
        /// </summary>
        public async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                _logger.LogInformation("开始检查数据库连接...");
                
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // 测试基本查询
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync();
                
                _logger.LogInformation("数据库连接检查成功");
                return true;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Server连接失败: {ErrorMessage}", sqlEx.Message);
                
                // 提供详细的错误诊断
                switch (sqlEx.Number)
                {
                    case 2: // Network error
                    case 53: // Network path not found
                        _logger.LogError("网络连接错误 - SQL Server服务可能未启动");
                        _logger.LogError("解决方案：");
                        _logger.LogError("1. 以管理员身份打开命令提示符");
                        _logger.LogError("2. 运行命令：net start MSSQLSERVER");
                        _logger.LogError("3. 或者在 services.msc 中找到 'SQL Server (MSSQLSERVER)' 并启动");
                        break;
                        
                    case 18456: // Login failed
                        _logger.LogError("登录验证失败 - sa用户可能被禁用或密码不正确");
                        _logger.LogError("解决方案：");
                        _logger.LogError("1. 打开SQL Server Management Studio");
                        _logger.LogError("2. 使用Windows身份验证连接");
                        _logger.LogError("3. 启用sa用户并设置密码为：123");
                        _logger.LogError("4. 启用混合身份验证模式");
                        break;
                        
                    case -2: // Timeout
                        _logger.LogError("连接超时 - SQL Server响应慢或不可达");
                        _logger.LogError("解决方案：");
                        _logger.LogError("1. 检查SQL Server是否正在运行");
                        _logger.LogError("2. 检查防火墙设置");
                        _logger.LogError("3. 验证TCP/IP协议已启用");
                        break;
                        
                    default:
                        _logger.LogError("SQL Server错误代码: {ErrorNumber}", sqlEx.Number);
                        _logger.LogError("通用解决方案：");
                        _logger.LogError("1. 确保SQL Server服务正在运行");
                        _logger.LogError("2. 检查连接字符串配置");
                        _logger.LogError("3. 验证用户名和密码");
                        break;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接检查时发生未知错误: {ErrorMessage}", ex.Message);
                _logger.LogError("建议检查：");
                _logger.LogError("1. SQL Server是否已正确安装");
                _logger.LogError("2. .NET数据提供程序是否可用");
                _logger.LogError("3. 系统网络配置");
                
                return false;
            }
        }

        /// <summary>
        /// 创建数据库如果不存在
        /// </summary>
        public async Task<bool> CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                _logger.LogInformation("检查并创建数据库...");
                
                // 解析连接字符串以获取数据库名称
                var builder = new SqlConnectionStringBuilder(_connectionString);
                var databaseName = builder.InitialCatalog;
                var masterConnectionString = _connectionString.Replace($"Database={databaseName}", "Database=master");
                
                using var connection = new SqlConnection(masterConnectionString);
                await connection.OpenAsync();
                
                // 检查数据库是否存在
                using var checkCommand = new SqlCommand(
                    $"SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}'", connection);
                var exists = (int)await checkCommand.ExecuteScalarAsync() > 0;
                
                if (!exists)
                {
                    _logger.LogInformation("数据库 {DatabaseName} 不存在，开始创建...", databaseName);
                    
                    using var createCommand = new SqlCommand($"CREATE DATABASE [{databaseName}]", connection);
                    await createCommand.ExecuteNonQueryAsync();
                    
                    _logger.LogInformation("数据库 {DatabaseName} 创建成功", databaseName);
                }
                else
                {
                    _logger.LogInformation("数据库 {DatabaseName} 已存在", databaseName);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建数据库失败: {ErrorMessage}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 初始化测试数据，包括数据库检查
        /// </summary>
        public async Task InitializeTestDataAsync()
        {
            try
            {
                _logger.LogInformation("开始初始化测试数据...");

                // 1. 检查数据库连接
                if (!await CheckDatabaseConnectionAsync())
                {
                    _logger.LogError("数据库连接失败，无法初始化测试数据");
                    throw new Exception("数据库连接失败，请检查SQL Server是否正在运行以及连接字符串是否正确");
                }

                // 2. 确保数据库存在
                await CreateDatabaseIfNotExistsAsync();

                // 3. 确保测试用户存在
                await EnsureTestUserExistsAsync();

                // 4. 初始化表权限
                await InitializeTablePermissionsAsync();

                // 5. 创建示例查询
                await CreateSampleQueriesAsync();

                _logger.LogInformation("测试数据初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试数据初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 确保测试用户存在
        /// </summary>
        private async Task EnsureTestUserExistsAsync()
        {
            try
            {
                var testUserId = "test-admin-001";
                var testUserName = "admin";
                
                // 检查是否存在具有相同ID的用户或用户名
                var existingUserById = await _dbContext.Users.FindAsync(testUserId);
                var existingUserByName = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.UserName == testUserName);

                if (existingUserById != null || existingUserByName != null)
                {
                    _logger.LogInformation("用户已存在: ID={UserId}, UserName={UserName}", testUserId, testUserName);
                    return;
                }

                var testUser = new ApplicationUser
                {
                    Id = testUserId,
                    UserName = testUserName,
                    NormalizedUserName = "ADMIN",
                    DisplayName = "系统管理员",
                    Email = "admin@test.com",
                    NormalizedEmail = "ADMIN@TEST.COM",
                    EmailConfirmed = true,
                    Department = "系统管理部",
                    PhoneNumber = "18888888888",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    PasswordHash = "test-hash",
                    LastLogin = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    LockoutEnabled = false,
                    AccessFailedCount = 0,
                    TwoFactorEnabled = false,
                    PhoneNumberConfirmed = false
                };

                _dbContext.Users.Add(testUser);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("用户创建成功: {UserId}", testUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败");
                throw;
            }
        }

        /// <summary>
        /// 初始化表权限
        /// </summary>
        private async Task InitializeTablePermissionsAsync()
        {
            try
            {
                var testUserId = "test-admin-001";

                // 获取数据库中的所有表
                var tableNames = await GetAllUserTablesAsync();
                
                _logger.LogInformation("找到 {Count} 个用户表", tableNames.Count);

                // 获取用户已有的权限表
                var existingPermissions = await _dbContext.AllowedTables
                    .Where(at => at.UserId == testUserId)
                    .Select(at => at.TableName)
                    .ToListAsync();

                // 为该用户添加所有表的读取权限
                foreach (var tableName in tableNames)
                {
                    if (!existingPermissions.Contains(tableName))
                    {
                        var allowedTable = new AllowedTable
                        {
                            UserId = testUserId,
                            TableName = tableName,
                            CanRead = true,
                            CanExport = false
                        };

                        _dbContext.AllowedTables.Add(allowedTable);
                    }
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("表权限初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化表权限失败");
                throw;
            }
        }

        /// <summary>
        /// 创建示例查询
        /// </summary>
        private async Task CreateSampleQueriesAsync()
        {
            try
            {
                var testUserId = "test-admin-001";

                // 检查是否存在示例查询
                var existingQueries = await _dbContext.SavedQueries
                    .Where(q => q.UserId == testUserId)
                    .CountAsync();

                if (existingQueries == 0)
                {
                    // 获取第一个用户表以创建示例查询
                    var firstTable = await GetFirstUserTableAsync();
                    
                    if (!string.IsNullOrEmpty(firstTable))
                    {
                        var sampleQueries = new List<SavedQuery>
                        {
                            new SavedQuery
                            {
                                Name = "示例查询 - 查看表记录",
                                Description = "创建一个示例查询，用于查看表中的记录",
                                SqlQuery = $"SELECT * FROM [{firstTable}]",
                                TablesIncluded = firstTable,
                                ColumnsIncluded = "*",
                                FilterConditions = "",
                                SortOrder = "",
                                JoinConditions = "",
                                UserId = testUserId,
                                CreatedBy = "admin",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                IsShared = false
                            },
                            new SavedQuery
                            {
                                Name = "示例查询 - 前10条记录",
                                Description = "创建一个示例查询，用于查看表中的前10条记录",
                                SqlQuery = $"SELECT TOP 10 * FROM [{firstTable}]",
                                TablesIncluded = firstTable,
                                ColumnsIncluded = "*",
                                FilterConditions = "",
                                SortOrder = "",
                                JoinConditions = "",
                                UserId = testUserId,
                                CreatedBy = "admin",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                IsShared = true
                            }
                        };

                        _dbContext.SavedQueries.AddRange(sampleQueries);
                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("示例查询创建完成");
                    }
                    else
                    {
                        _logger.LogWarning("没有找到用户表以创建示例查询");
                    }
                }
                else
                {
                    _logger.LogInformation("示例查询已存在，无需创建");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建示例查询失败");
                throw;
            }
        }

        /// <summary>
        /// 获取数据库中的所有用户表
        /// </summary>
        private async Task<List<string>> GetAllUserTablesAsync()
        {
            var systemTables = new List<string>
            {
                "__EFMigrationsHistory", "AllowedTables", "ColumnInfos", "DatabaseConnections",
                "QueryShares", "RoleClaims", "Roles", "SavedQueries", "TableInfos", "UserRoles", "Users", "AuditLogs"
            };

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_TYPE = 'BASE TABLE' 
                    AND TABLE_SCHEMA = 'dbo'
                    ORDER BY TABLE_NAME";

                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                var tables = new List<string>();
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    if (!systemTables.Contains(tableName))
                    {
                        tables.Add(tableName);
                    }
                }

                return tables;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户表列表失败");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取第一个用户表
        /// </summary>
        private async Task<string?> GetFirstUserTableAsync()
        {
            var tables = await GetAllUserTablesAsync();
            return tables.FirstOrDefault();
        }

        /// <summary>
        /// 清理测试数据，包括删除用户查询和权限
        /// </summary>
        public async Task CleanupTestDataAsync()
        {
            try
            {
                var testUserId = "test-admin-001";

                // 删除用户查询
                var testQueries = await _dbContext.SavedQueries
                    .Where(q => q.UserId == testUserId)
                    .ToListAsync();
                _dbContext.SavedQueries.RemoveRange(testQueries);

                // 删除用户权限
                var testPermissions = await _dbContext.AllowedTables
                    .Where(at => at.UserId == testUserId)
                    .ToListAsync();
                _dbContext.AllowedTables.RemoveRange(testPermissions);

                // 删除用户
                var testUser = await _dbContext.Users.FindAsync(testUserId);
                if (testUser != null)
                {
                    _dbContext.Users.Remove(testUser);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("测试数据清理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试数据清理失败");
                throw;
            }
        }
    }
} 