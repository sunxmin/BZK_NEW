using BZKQuerySystem.DataAccess;
using BZKQuerySystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BZKQuerySystem.Web.Controllers
{
    public class DebugController : Controller
    {
        private readonly BZKQueryDbContext _dbContext;
        private readonly DataSeederService _dataSeederService;

        public DebugController(BZKQueryDbContext dbContext, DataSeederService dataSeederService)
        {
            _dbContext = dbContext;
            _dataSeederService = dataSeederService;
        }

        /// <summary>
        /// ������ݳ�ʼ��״̬
        /// </summary>
        public async Task<IActionResult> CheckDataStatus()
        {
            var testUserId = "test-admin-001";
            
            var result = new
            {
                TestUser = await _dbContext.Users.FindAsync(testUserId),
                UserCount = await _dbContext.Users.CountAsync(),
                AllowedTablesCount = await _dbContext.AllowedTables
                    .Where(at => at.UserId == testUserId)
                    .CountAsync(),
                AllowedTables = await _dbContext.AllowedTables
                    .Where(at => at.UserId == testUserId)
                    .Select(at => new { at.TableName, at.CanRead, at.CanExport })
                    .ToListAsync(),
                SavedQueriesCount = await _dbContext.SavedQueries
                    .Where(q => q.UserId == testUserId)
                    .CountAsync(),
                SavedQueries = await _dbContext.SavedQueries
                    .Where(q => q.UserId == testUserId)
                    .Select(q => new { q.Name, q.Description, q.CreatedBy, q.IsShared })
                    .ToListAsync()
            };

            return Json(result);
        }

        /// <summary>
        /// ǿ�����³�ʼ����������
        /// </summary>
        public async Task<IActionResult> ReinitializeData()
        {
            try
            {
                // �������в�������
                await _dataSeederService.CleanupTestDataAsync();
                
                // ���³�ʼ��
                await _dataSeederService.InitializeTestDataAsync();
                
                return Json(new { success = true, message = "�������³�ʼ���ɹ�" });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// ��ȡ�������ݿ��
        /// </summary>
        public async Task<IActionResult> GetAllTables()
        {
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(
                    "Server=localhost;Database=ZBKQuerySystem;User Id=sa;Password=123;TrustServerCertificate=True;");
                await connection.OpenAsync();

                const string sql = @"
                    SELECT TABLE_NAME, TABLE_TYPE
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = 'dbo'
                    ORDER BY TABLE_NAME";

                using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                var tables = new List<object>();
                while (await reader.ReadAsync())
                {
                    tables.Add(new
                    {
                        TableName = reader.GetString(0),
                        TableType = reader.GetString(1)
                    });
                }

                return Json(tables);
            }
            catch (System.Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
} 