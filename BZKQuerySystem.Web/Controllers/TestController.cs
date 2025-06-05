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
                // ���Զ���ѯ��Ĭ�������޸�
                var tables = new List<string> { "Table1", "Table2" };
                var columns = new List<string> { "Table1.Name", "Table2.Name", "Table1.ID" };
                var joinConditions = new List<string> { "INNER JOIN [Table2] ON [Table1].[ID] = [Table2].[Table1ID]" };
                var whereConditions = new List<string>();
                var orderBy = new List<string>(); // �յ���������������Ĭ������

                // ����SQL��ѯ
                string sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                // ģ���ҳ��ѯ����ᴥ��Ĭ�������߼�
                var parameters = new Dictionary<string, object>();
                
                // �������ǲ���ֱ�ӵ���ExecuteQueryAsync����Ϊ����Ҫ��ʵ�����ݿ�����
                // �����ǿ��Բ���SQL�����߼�
                
                return Content($"���Գɹ���\n\n���ɵ�SQL:\n{sql}\n\n" +
                              $"��һ���б�ʶ��: {columns[0]}\n" +
                              $"Ԥ��Ĭ������Ӧ��ʹ��: [Table1].[Name]\n" +
                              $"�������Ա������ѯʱ�������������⡣");
            }
            catch (Exception ex)
            {
                return Content($"����ʧ��: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet("/test/multi-table-columns")]
        public IActionResult TestMultiTableColumns()
        {
            try
            {
                // ���Զ���ѯ��������ʾ�޸�
                var tables = new List<string> { "Table1", "Table2" };
                var columns = new List<string> { "Table1.Name", "Table2.Name", "Table1.ID", "Table2.Description" };
                var joinConditions = new List<string> { "INNER JOIN [Table2] ON [Table1].[ID] = [Table2].[Table1ID]" };
                var whereConditions = new List<string>();
                var orderBy = new List<string>();

                // ����SQL��ѯ
                string sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                return Content($"����ѯ������ʾ���Գɹ���\n\n" +
                              $"������: {tables.Count} (����ѯ)\n" +
                              $"ѡ�����: {string.Join(", ", columns)}\n\n" +
                              $"���ɵ�SQL:\n{sql}\n\n" +
                              $"Ԥ��Ч��:\n" +
                              $"- ���Ӧ��Ϊ���������� ����_���� ��ʽ�ı���\n" +
                              $"- ǰ��Ӧ����ʾ������Ϊ ����.���� ��ʽ\n" +
                              $"- ����: Table1.Name, Table2.Name, Table1.ID, Table2.Description");
            }
            catch (Exception ex)
            {
                return Content($"����ʧ��: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet("/test/single-table-columns")]
        public IActionResult TestSingleTableColumns()
        {
            try
            {
                // ���Ե����ѯ��������ʾ��Ӧ�ñ���ԭ����Ϊ��
                var tables = new List<string> { "Table1" };
                var columns = new List<string> { "Table1.Name", "Table1.ID", "Table1.Description" };
                var joinConditions = new List<string>();
                var whereConditions = new List<string>();
                var orderBy = new List<string>();

                // ����SQL��ѯ
                string sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                return Content($"�����ѯ������ʾ���Գɹ���\n\n" +
                              $"������: {tables.Count} (�����ѯ)\n" +
                              $"ѡ�����: {string.Join(", ", columns)}\n\n" +
                              $"���ɵ�SQL:\n{sql}\n\n" +
                              $"Ԥ��Ч��:\n" +
                              $"- ���Ӧ��Ϊ�����ɼ�̵ı�����ֻ��������\n" +
                              $"- ǰ��Ӧ����ʾ��̵�����\n" +
                              $"- ����: Name, ID, Description");
            }
            catch (Exception ex)
            {
                return Content($"����ʧ��: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet("/test/sql-alias-format")]
        public IActionResult TestSqlAliasFormat()
        {
            try
            {
                // ���Զ���ѯ��SQL������ʽ�޸�
                var tables = new List<string> { "���߻�����Ϣ", "�����Ϣ" };
                var columns = new List<string> { "���߻�����Ϣ.����ID", "���߻�����Ϣ.����", "�����Ϣ.�������", "�����Ϣ.���ʱ��" };
                var joinConditions = new List<string> { "INNER JOIN [�����Ϣ] ON [���߻�����Ϣ].[����ID] = [�����Ϣ].[����ID]" };
                var whereConditions = new List<string>();
                var orderBy = new List<string>();

                // ����SQL��ѯ
                string sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                return Content($"SQL������ʽ���Գɹ���\\n\\n" +
                              $"������: {tables.Count} (����ѯ)\\n" +
                              $"������: {columns.Count}\\n\\n" +
                              $"���ɵ�SQL:\\n{sql}\\n\\n" +
                              $"��֤��:\\n" +
                              $"1. AS����ı���Ӧ����[����_����]��ʽ\\n" +
                              $"2. ����Ӧ���÷����Ű�Χ\\n" +
                              $"3. ����ֱ�Ӹ��Ƶ�SSMS��ִ��\\n\\n" +
                              $"����ʱ��: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                return Content($"SQL������ʽ����ʧ��: {ex.Message}");
            }
        }

        [HttpGet("/test/debug-sql-params")]
        public IActionResult DebugSqlParams()
        {
            try
            {
                // ģ��ǰ�˴��ݵĲ���������SQL�����߼�
                var tables = new List<string> { "18_�������", "19_סԺ���" };
                var columns = new List<string> { "18_�������.��������", "18_�������.����������", "19_סԺ���.��������", "19_סԺ���.����������" };
                var joinConditions = new List<string> { "INNER JOIN [19_סԺ���] ON [18_�������].[����������] = [19_סԺ���].[����������]" };
                var whereConditions = new List<string>();
                var orderBy = new List<string>();

                // ����SQL��ѯ
                string sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);

                var debugInfo = new StringBuilder();
                debugInfo.AppendLine("=== ����SQL���� ===");
                debugInfo.AppendLine($"������: {tables.Count}");
                debugInfo.AppendLine($"���б�: [{string.Join(", ", tables)}]");
                debugInfo.AppendLine($"������: {columns.Count}");
                debugInfo.AppendLine("���б�:");
                for (int i = 0; i < columns.Count; i++)
                {
                    var col = columns[i];
                    var containsDot = col.Contains('.');
                    var shortName = col.Contains('.') ? col.Split(new[] { '.' }, 2).Last() : col;
                    debugInfo.AppendLine($"  [{i}] '{col}' -> �������: {containsDot}, ������: '{shortName}'");
                }
                debugInfo.AppendLine();
                debugInfo.AppendLine("=== ���ɵ�SQL ===");
                debugInfo.AppendLine(sql);
                debugInfo.AppendLine();
                debugInfo.AppendLine("=== ������� ===");
                debugInfo.AppendLine("AS����Ӧ����: [18_�������_��������], [18_�������_����������], [19_סԺ���_��������], [19_סԺ���_����������]");

                return Content(debugInfo.ToString());
            }
            catch (Exception ex)
            {
                return Content($"����SQL����ʧ��: {ex.Message}\\n{ex.StackTrace}");
            }
        }

        [HttpGet("and-or-conditions")]
        public IActionResult TestAndOrConditions()
        {
            try
            {
                // ģ��������������AND��OR����
                var tables = new List<string> { "T1_���ȼ�¼" };
                var columns = new List<string> { "T1_���ȼ�¼.�������", "T1_���ȼ�¼.�������" };
                var joinConditions = new List<string>();
                
                // ģ�����AND/OR���ӵ�����
                var whereConditions = new List<string>
                {
                    // ��һ��������û�����ӷ���
                    "{\"column\":\"T1_���ȼ�¼.�������\",\"operator\":\"=\",\"value\":\"����\",\"connector\":\"AND\",\"sql\":\"[T1_���ȼ�¼].[�������] = N'����'\",\"display\":\"������� = ����\"}",
                    // �ڶ���������ʹ��OR���ӣ�
                    "{\"column\":\"T1_���ȼ�¼.�������\",\"operator\":\"LIKE\",\"value\":\"��ð\",\"connector\":\"OR\",\"sql\":\"[T1_���ȼ�¼].[�������] LIKE N'%��ð%'\",\"display\":\"������� ���� ��ð\"}",
                    // ������������ʹ��AND���ӣ�
                    "{\"column\":\"T1_���ȼ�¼.�������\",\"operator\":\"!=\",\"value\":\"����\",\"connector\":\"AND\",\"sql\":\"[T1_���ȼ�¼].[�������] != N'����'\",\"display\":\"������� != ����\"}"
                };
                
                var orderBy = new List<string> { "T1_���ȼ�¼.������� ASC" };
                
                // ����SQL��ѯ
                var sql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);
                
                return Json(new
                {
                    success = true,
                    message = "AND/OR���Ӳ���",
                    sql = sql,
                    expectedConnectors = new[] { "�����ӷ�����һ��������", "OR", "AND" },
                    conditions = whereConditions.Select((cond, index) => new
                    {
                        index = index + 1,
                        condition = cond,
                        expectedConnector = index == 0 ? "��" : (index == 1 ? "OR" : "AND")
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
                // ģ�����AND/OR���ӵ����������Լ�¼����ͳ��
                var tables = new List<string> { "T1_���ȼ�¼" };
                var columns = new List<string> { "T1_���ȼ�¼.�������", "T1_���ȼ�¼.�������" };
                var joinConditions = new List<string>();
                
                // ģ�����AND/OR���ӵ�����
                var whereConditions = new List<string>
                {
                    // ��һ��������û�����ӷ���
                    "{\"column\":\"T1_���ȼ�¼.�������\",\"operator\":\"=\",\"value\":\"����\",\"connector\":\"AND\",\"sql\":\"[T1_���ȼ�¼].[�������] = N'����'\",\"display\":\"������� = ����\"}",
                    // �ڶ���������ʹ��OR���ӣ�
                    "{\"column\":\"T1_���ȼ�¼.�������\",\"operator\":\"LIKE\",\"value\":\"��ð\",\"connector\":\"OR\",\"sql\":\"[T1_���ȼ�¼].[�������] LIKE N'%��ð%'\",\"display\":\"������� ���� ��ð\"}",
                    // ������������ʹ��AND���ӣ�
                    "{\"column\":\"T1_���ȼ�¼.�������\",\"operator\":\"!=\",\"value\":\"����\",\"connector\":\"AND\",\"sql\":\"[T1_���ȼ�¼].[�������] != N'����'\",\"display\":\"������� != ����\"}"
                };
                
                var orderBy = new List<string> { "T1_���ȼ�¼.������� ASC" };
                
                // ��������ѯSQL
                var mainSql = _queryBuilderService.BuildSqlQuery(tables, columns, joinConditions, whereConditions, orderBy);
                
                // ��������SQL�����������޸��Ĳ��֣�
                var parameters = new Dictionary<string, object>();
                
                // ע�⣺�������ǲ���ֱ�ӵ���GetTotalRowsAsync����Ϊ����Ҫ���ݿ�����
                // �����ǿ�����֤SQL�����߼��Ƿ���ȷ
                
                return Json(new
                {
                    success = true,
                    message = "��¼����ͳ���޸�����",
                    mainSql = mainSql,
                    testScenario = "����OR��AND���ӵĸ�������",
                    conditions = whereConditions.Select((cond, index) => new
                    {
                        index = index + 1,
                        condition = cond,
                        expectedConnector = index == 0 ? "��" : (index == 1 ? "OR" : "AND")
                    }),
                    expectedBehavior = new
                    {
                        mainQuery = "Ӧ����ȷʹ��OR��AND��������",
                        countQuery = "ͳ��������ʱӦ��ʹ����ͬ�������߼�",
                        result = "��һ�β�ѯ���޸�������Ĳ�ѯӦ����ʾ��ȷ���ܼ�¼��"
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

        // ��UserService���ƵĹ�ϣ������ȷ��ʹ����ȫ��ͬ���㷨
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