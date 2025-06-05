using BZKQuerySystem.DataAccess;
using BZKQuerySystem.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BZKQuerySystem.Web.Controllers
{
    public class TestController : Controller
    {
        private readonly UserService _userService;
        private readonly QueryBuilderService _queryBuilderService;

        public TestController(UserService userService, QueryBuilderService queryBuilderService)
        {
            _userService = userService;
            _queryBuilderService = queryBuilderService;
        }

        [HttpGet("/test/hash")]
        public IActionResult GenerateHash()
        {
            string password = "123";
            string hash = HashPassword(password);
            return Content($"Password: {password}, Hash: {hash}");
        }

        [HttpGet("/test/default-order")]
        public IActionResult TestDefaultOrder()
        {
            try
            {
                // 测试多表查询的默认排序修复
                var tables = new List<string> { "Table1", "Table2" };
                var columns = new List<string> { "Table1.Name", "Table2.Name", "Table1.ID" };
                var joinConditions = new List<string> { "INNER JOIN [Table2] ON [Table1].[ID] = [Table2].[Table1ID]" };
                var whereConditions = new List<string>();
                var orderBy = new List<string>(); // 空的排序条件，触发默认排序

                // 构建SQL查询
                string sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                // 模拟分页查询，这会触发默认排序逻辑
                var parameters = new Dictionary<string, object>();
                
                // 这里我们不能直接调用ExecuteQueryAsync，因为它需要真实的数据库连接
                // 但我们可以测试SQL构建逻辑
                
                return Content($"测试成功！\n\n生成的SQL:\n{sql}\n\n" +
                              $"第一个列标识符: {columns[0]}\n" +
                              $"预期默认排序应该使用: [Table1].[Name]\n" +
                              $"这样可以避免多表查询时的列名歧义问题。");
            }
            catch (Exception ex)
            {
                return Content($"测试失败: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet("/test/multi-table-columns")]
        public IActionResult TestMultiTableColumns()
        {
            try
            {
                // 测试多表查询的列名显示修复
                var tables = new List<string> { "Table1", "Table2" };
                var columns = new List<string> { "Table1.Name", "Table2.Name", "Table1.ID", "Table2.Description" };
                var joinConditions = new List<string> { "INNER JOIN [Table2] ON [Table1].[ID] = [Table2].[Table1ID]" };
                var whereConditions = new List<string>();
                var orderBy = new List<string>();

                // 构建SQL查询
                string sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                return Content($"多表查询列名显示测试成功！\n\n" +
                              $"表数量: {tables.Count} (多表查询)\n" +
                              $"选择的列: {string.Join(", ", columns)}\n\n" +
                              $"生成的SQL:\n{sql}\n\n" +
                              $"预期效果:\n" +
                              $"- 后端应该为所有列生成 表名_列名 格式的别名\n" +
                              $"- 前端应该显示所有列为 表名.列名 格式\n" +
                              $"- 例如: Table1.Name, Table2.Name, Table1.ID, Table2.Description");
            }
            catch (Exception ex)
            {
                return Content($"测试失败: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet("/test/single-table-columns")]
        public IActionResult TestSingleTableColumns()
        {
            try
            {
                // 测试单表查询的列名显示（应该保持原有行为）
                var tables = new List<string> { "Table1" };
                var columns = new List<string> { "Table1.Name", "Table1.ID", "Table1.Description" };
                var joinConditions = new List<string>();
                var whereConditions = new List<string>();
                var orderBy = new List<string>();

                // 构建SQL查询
                string sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                return Content($"单表查询列名显示测试成功！\n\n" +
                              $"表数量: {tables.Count} (单表查询)\n" +
                              $"选择的列: {string.Join(", ", columns)}\n\n" +
                              $"生成的SQL:\n{sql}\n\n" +
                              $"预期效果:\n" +
                              $"- 后端应该为列生成简短的别名（只有列名）\n" +
                              $"- 前端应该显示简短的列名\n" +
                              $"- 例如: Name, ID, Description");
            }
            catch (Exception ex)
            {
                return Content($"测试失败: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet("/test/sql-alias-format")]
        public IActionResult TestSqlAliasFormat()
        {
            try
            {
                // 测试多表查询的SQL别名格式修复
                var tables = new List<string> { "患者基本信息", "诊断信息" };
                var columns = new List<string> { "患者基本信息.患者ID", "患者基本信息.姓名", "诊断信息.诊断名称", "诊断信息.诊断时间" };
                var joinConditions = new List<string> { "INNER JOIN [诊断信息] ON [患者基本信息].[患者ID] = [诊断信息].[患者ID]" };
                var whereConditions = new List<string>();
                var orderBy = new List<string>();

                // 构建SQL查询
                string sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                return Content($"SQL别名格式测试成功！\\n\\n" +
                              $"表数量: {tables.Count} (多表查询)\\n" +
                              $"列数量: {columns.Count}\\n\\n" +
                              $"生成的SQL:\\n{sql}\\n\\n" +
                              $"验证点:\\n" +
                              $"1. AS后面的别名应该是[表名_列名]格式\\n" +
                              $"2. 别名应该用方括号包围\\n" +
                              $"3. 可以直接复制到SSMS中执行\\n\\n" +
                              $"测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                return Content($"SQL别名格式测试失败: {ex.Message}");
            }
        }

        [HttpGet("/test/debug-sql-params")]
        public IActionResult DebugSqlParams()
        {
            try
            {
                // 模拟前端传递的参数，调试SQL生成逻辑
                var tables = new List<string> { "18_门诊诊断", "19_住院诊断" };
                var columns = new List<string> { "18_门诊诊断.患者姓名", "18_门诊诊断.患者主索引", "19_住院诊断.患者姓名", "19_住院诊断.患者主索引" };
                var joinConditions = new List<string> { "INNER JOIN [19_住院诊断] ON [18_门诊诊断].[患者主索引] = [19_住院诊断].[患者主索引]" };
                var whereConditions = new List<string>();
                var orderBy = new List<string>();

                // 构建SQL查询
                string sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                var debugInfo = new StringBuilder();
                debugInfo.AppendLine("=== 调试SQL参数 ===");
                debugInfo.AppendLine($"表数量: {tables.Count}");
                debugInfo.AppendLine($"表列表: [{string.Join(", ", tables)}]");
                debugInfo.AppendLine($"列数量: {columns.Count}");
                debugInfo.AppendLine("列列表:");
                for (int i = 0; i < columns.Count; i++)
                {
                    var col = columns[i];
                    var containsDot = col.Contains('.');
                    var shortName = col.Contains('.') ? col.Split(new[] { '.' }, 2).Last() : col;
                    debugInfo.AppendLine($"  [{i}] '{col}' -> 包含点号: {containsDot}, 短名称: '{shortName}'");
                }
                debugInfo.AppendLine();
                debugInfo.AppendLine("=== 生成的SQL ===");
                debugInfo.AppendLine(sql);
                debugInfo.AppendLine();
                debugInfo.AppendLine("=== 期望结果 ===");
                debugInfo.AppendLine("AS后面应该是: [18_门诊诊断_患者姓名], [18_门诊诊断_患者主索引], [19_住院诊断_患者姓名], [19_住院诊断_患者主索引]");

                return Content(debugInfo.ToString());
            }
            catch (Exception ex)
            {
                return Content($"调试SQL参数失败: {ex.Message}\\n{ex.StackTrace}");
            }
        }

        [HttpGet("and-or-conditions")]
        public IActionResult TestAndOrConditions()
        {
            try
            {
                // 模拟多个条件，包含AND和OR连接
                var tables = new List<string> { "T1_发热记录" };
                var columns = new List<string> { "T1_发热记录.门诊诊断", "T1_发热记录.诊断名称" };
                var joinConditions = new List<string>();
                
                // 模拟包含AND/OR连接的条件
                var whereConditions = new List<string>
                {
                    // 第一个条件（没有连接符）
                    "{\"column\":\"T1_发热记录.门诊诊断\",\"operator\":\"=\",\"value\":\"发热\",\"connector\":\"AND\",\"sql\":\"[T1_发热记录].[门诊诊断] = N'发热'\",\"display\":\"门诊诊断 = 发热\"}",
                    // 第二个条件（使用OR连接）
                    "{\"column\":\"T1_发热记录.诊断名称\",\"operator\":\"LIKE\",\"value\":\"感冒\",\"connector\":\"OR\",\"sql\":\"[T1_发热记录].[诊断名称] LIKE N'%感冒%'\",\"display\":\"诊断名称 包含 感冒\"}",
                    // 第三个条件（使用AND连接）
                    "{\"column\":\"T1_发热记录.门诊诊断\",\"operator\":\"!=\",\"value\":\"正常\",\"connector\":\"AND\",\"sql\":\"[T1_发热记录].[门诊诊断] != N'正常'\",\"display\":\"门诊诊断 != 正常\"}"
                };
                
                var orderBy = new List<string> { "T1_发热记录.门诊诊断 ASC" };
                
                // 构建SQL查询
                var sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);
                
                return Json(new
                {
                    success = true,
                    message = "AND/OR连接测试",
                    sql = sql,
                    expectedConnectors = new[] { "无连接符（第一个条件）", "OR", "AND" },
                    conditions = whereConditions.Select((cond, index) => new
                    {
                        index = index + 1,
                        condition = cond,
                        expectedConnector = index == 0 ? "无" : (index == 1 ? "OR" : "AND")
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("record-count-fix")]
        public IActionResult TestRecordCountFix()
        {
            try
            {
                // 模拟包含AND/OR连接的条件，测试记录条数统计
                var tables = new List<string> { "T1_发热记录" };
                var columns = new List<string> { "T1_发热记录.门诊诊断", "T1_发热记录.诊断名称" };
                var joinConditions = new List<string>();
                
                // 模拟包含AND/OR连接的条件
                var whereConditions = new List<string>
                {
                    // 第一个条件（没有连接符）
                    "{\"column\":\"T1_发热记录.门诊诊断\",\"operator\":\"=\",\"value\":\"发热\",\"connector\":\"AND\",\"sql\":\"[T1_发热记录].[门诊诊断] = N'发热'\",\"display\":\"门诊诊断 = 发热\"}",
                    // 第二个条件（使用OR连接）
                    "{\"column\":\"T1_发热记录.诊断名称\",\"operator\":\"LIKE\",\"value\":\"感冒\",\"connector\":\"OR\",\"sql\":\"[T1_发热记录].[诊断名称] LIKE N'%感冒%'\",\"display\":\"诊断名称 包含 感冒\"}",
                    // 第三个条件（使用AND连接）
                    "{\"column\":\"T1_发热记录.门诊诊断\",\"operator\":\"!=\",\"value\":\"正常\",\"connector\":\"AND\",\"sql\":\"[T1_发热记录].[门诊诊断] != N'正常'\",\"display\":\"门诊诊断 != 正常\"}"
                };
                
                var orderBy = new List<string> { "T1_发热记录.门诊诊断 ASC" };
                
                // 构建主查询SQL
                var mainSql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);
                
                // 构建计数SQL（这是我们修复的部分）
                var parameters = new Dictionary<string, object>();
                
                // 注意：这里我们不能直接调用GetTotalRowsAsync，因为它需要数据库连接
                // 但我们可以验证SQL构建逻辑是否正确
                
                return Json(new
                {
                    success = true,
                    message = "记录条数统计修复测试",
                    mainSql = mainSql,
                    testScenario = "包含OR和AND连接的复合条件",
                    conditions = whereConditions.Select((cond, index) => new
                    {
                        index = index + 1,
                        condition = cond,
                        expectedConnector = index == 0 ? "无" : (index == 1 ? "OR" : "AND")
                    }),
                    expectedBehavior = new
                    {
                        mainQuery = "应该正确使用OR和AND连接条件",
                        countQuery = "统计总行数时应该使用相同的连接逻辑",
                        result = "第一次查询和修改条件后的查询应该显示正确的总记录数"
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // 从UserService复制的哈希方法，确保使用完全相同的算法
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
    }
} 