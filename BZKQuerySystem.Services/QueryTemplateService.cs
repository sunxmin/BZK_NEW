using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BZKQuerySystem.Services
{
    public interface IQueryTemplateService
    {
        Task<List<QueryTemplate>> GetUserTemplatesAsync(string userId);
        Task<List<QueryTemplate>> GetPublicTemplatesAsync();
        Task<QueryTemplate?> GetTemplateByIdAsync(int templateId);
        Task<int> CreateTemplateAsync(QueryTemplate template);
        Task<bool> UpdateTemplateAsync(QueryTemplate template);
        Task<bool> DeleteTemplateAsync(int templateId, string userId);
        Task<bool> ShareTemplateAsync(int templateId, string userId, bool isPublic);
        Task<List<QueryTemplate>> SearchTemplatesAsync(string keyword, string? userId = null);
        Task<QueryTemplate?> ApplyTemplateAsync(int templateId, Dictionary<string, object>? parameters = null);
        Task<List<QueryTemplateCategory>> GetCategoriesAsync();
        Task<bool> SetTemplateFavoriteAsync(int templateId, string userId, bool isFavorite);
    }

    public class QueryTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public bool IsPublic { get; set; } = false;
        public bool IsFavorite { get; set; } = false;
        public int UsageCount { get; set; } = 0;
        public string CategoryName { get; set; } = "Ĭ��";
        public string Tags { get; set; } = "";
        
        // ��ѯ���ã�JSON��ʽ�洢��
        public string QueryConfigJson { get; set; } = "";
        
        // �������壨JSON��ʽ�洢��
        public string ParametersJson { get; set; } = "";
        
        // �����л��Ĳ�ѯ����
        public QueryConfiguration? QueryConfig { get; set; }
        
        // �����л��Ĳ�������
        public List<TemplateParameter>? Parameters { get; set; }
    }

    public class QueryConfiguration
    {
        public List<string> SelectedTables { get; set; } = new();
        public List<FieldSelection> SelectedFields { get; set; } = new();
        public List<QueryCondition> Conditions { get; set; } = new();
        public List<JoinConfiguration> Joins { get; set; } = new();
        public string? OrderBy { get; set; }
        public string? GroupBy { get; set; }
        public int? Limit { get; set; }
    }

    public class FieldSelection
    {
        public string TableName { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string? Alias { get; set; }
        public string? AggregateFunction { get; set; }
    }

    public class QueryCondition
    {
        public string TableName { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string Operator { get; set; } = "";
        public string? Value { get; set; }
        public string? ParameterName { get; set; }
        public string LogicalOperator { get; set; } = "AND";
    }

    public class JoinConfiguration
    {
        public string LeftTable { get; set; } = "";
        public string RightTable { get; set; } = "";
        public string LeftField { get; set; } = "";
        public string RightField { get; set; } = "";
        public string JoinType { get; set; } = "INNER";
    }

    public class TemplateParameter
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string DataType { get; set; } = "string";
        public string? DefaultValue { get; set; }
        public bool IsRequired { get; set; } = false;
        public string? Description { get; set; }
        public List<string>? AllowedValues { get; set; }
    }

    public class QueryTemplateCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int TemplateCount { get; set; }
    }

    public class QueryTemplateService : IQueryTemplateService
    {
        private readonly string _connectionString;
        private readonly ILogger<QueryTemplateService> _logger;

        public QueryTemplateService(string connectionString, ILogger<QueryTemplateService> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
            EnsureTablesExist();
        }

        private void EnsureTablesExist()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // ������ѯģ���
                var createTemplatesTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='QueryTemplates' AND xtype='U')
                CREATE TABLE QueryTemplates (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(255) NOT NULL,
                    Description NVARCHAR(1000),
                    CreatedBy NVARCHAR(255) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                    IsPublic BIT NOT NULL DEFAULT 0,
                    UsageCount INT NOT NULL DEFAULT 0,
                    CategoryName NVARCHAR(100) NOT NULL DEFAULT 'Ĭ��',
                    Tags NVARCHAR(500),
                    QueryConfigJson NVARCHAR(MAX) NOT NULL,
                    ParametersJson NVARCHAR(MAX),
                    INDEX IX_QueryTemplates_CreatedBy (CreatedBy),
                    INDEX IX_QueryTemplates_IsPublic (IsPublic),
                    INDEX IX_QueryTemplates_CategoryName (CategoryName)
                )";

                using var cmd1 = new SqlCommand(createTemplatesTableSql, connection);
                cmd1.ExecuteNonQuery();

                // �����û��ղر�
                var createFavoritesTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserTemplateFavorites' AND xtype='U')
                CREATE TABLE UserTemplateFavorites (
                    UserId NVARCHAR(255) NOT NULL,
                    TemplateId INT NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                    PRIMARY KEY (UserId, TemplateId),
                    FOREIGN KEY (TemplateId) REFERENCES QueryTemplates(Id) ON DELETE CASCADE
                )";

                using var cmd2 = new SqlCommand(createFavoritesTableSql, connection);
                cmd2.ExecuteNonQuery();

                _logger.LogInformation("��ѯģ����ʼ���ɹ�");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ѯģ���ʧ��");
            }
        }

        public async Task<List<QueryTemplate>> GetUserTemplatesAsync(string userId)
        {
            var templates = new List<QueryTemplate>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                SELECT t.*, 
                       CASE WHEN f.UserId IS NOT NULL THEN 1 ELSE 0 END as IsFavorite
                FROM QueryTemplates t
                LEFT JOIN UserTemplateFavorites f ON t.Id = f.TemplateId AND f.UserId = @UserId
                WHERE t.CreatedBy = @UserId OR t.IsPublic = 1
                ORDER BY t.UpdatedAt DESC";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var template = MapTemplate(reader);
                    templates.Add(template);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ�û�ģ��ʧ��: {UserId}", userId);
            }

            return templates;
        }

        public async Task<List<QueryTemplate>> GetPublicTemplatesAsync()
        {
            var templates = new List<QueryTemplate>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                SELECT * FROM QueryTemplates 
                WHERE IsPublic = 1 
                ORDER BY UsageCount DESC, UpdatedAt DESC";

                using var cmd = new SqlCommand(sql, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var template = MapTemplate(reader);
                    templates.Add(template);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ����ģ��ʧ��");
            }

            return templates;
        }

        public async Task<QueryTemplate?> GetTemplateByIdAsync(int templateId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT * FROM QueryTemplates WHERE Id = @Id";
                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Id", templateId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapTemplate(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡģ��ʧ��: {TemplateId}", templateId);
            }

            return null;
        }

        public async Task<int> CreateTemplateAsync(QueryTemplate template)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                INSERT INTO QueryTemplates 
                (Name, Description, CreatedBy, IsPublic, CategoryName, Tags, QueryConfigJson, ParametersJson)
                VALUES 
                (@Name, @Description, @CreatedBy, @IsPublic, @CategoryName, @Tags, @QueryConfigJson, @ParametersJson);
                SELECT SCOPE_IDENTITY();";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Name", template.Name);
                cmd.Parameters.AddWithValue("@Description", template.Description ?? "");
                cmd.Parameters.AddWithValue("@CreatedBy", template.CreatedBy);
                cmd.Parameters.AddWithValue("@IsPublic", template.IsPublic);
                cmd.Parameters.AddWithValue("@CategoryName", template.CategoryName);
                cmd.Parameters.AddWithValue("@Tags", template.Tags ?? "");
                cmd.Parameters.AddWithValue("@QueryConfigJson", template.QueryConfigJson);
                cmd.Parameters.AddWithValue("@ParametersJson", template.ParametersJson ?? "");

                var result = await cmd.ExecuteScalarAsync();
                var newId = Convert.ToInt32(result);

                _logger.LogInformation("������ѯģ��ɹ�: {TemplateId}, ����: {Name}", newId, template.Name);
                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ѯģ��ʧ��: {Name}", template.Name);
                return -1;
            }
        }

        public async Task<bool> UpdateTemplateAsync(QueryTemplate template)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                UPDATE QueryTemplates 
                SET Name = @Name, 
                    Description = @Description, 
                    IsPublic = @IsPublic, 
                    CategoryName = @CategoryName, 
                    Tags = @Tags, 
                    QueryConfigJson = @QueryConfigJson, 
                    ParametersJson = @ParametersJson,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id AND CreatedBy = @CreatedBy";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Id", template.Id);
                cmd.Parameters.AddWithValue("@Name", template.Name);
                cmd.Parameters.AddWithValue("@Description", template.Description ?? "");
                cmd.Parameters.AddWithValue("@IsPublic", template.IsPublic);
                cmd.Parameters.AddWithValue("@CategoryName", template.CategoryName);
                cmd.Parameters.AddWithValue("@Tags", template.Tags ?? "");
                cmd.Parameters.AddWithValue("@QueryConfigJson", template.QueryConfigJson);
                cmd.Parameters.AddWithValue("@ParametersJson", template.ParametersJson ?? "");
                cmd.Parameters.AddWithValue("@CreatedBy", template.CreatedBy);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("���²�ѯģ��: {TemplateId}, Ӱ������: {RowsAffected}", template.Id, rowsAffected);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���²�ѯģ��ʧ��: {TemplateId}", template.Id);
                return false;
            }
        }

        public async Task<bool> DeleteTemplateAsync(int templateId, string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "DELETE FROM QueryTemplates WHERE Id = @Id AND CreatedBy = @UserId";
                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Id", templateId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("ɾ����ѯģ��: {TemplateId}, Ӱ������: {RowsAffected}", templateId, rowsAffected);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ɾ����ѯģ��ʧ��: {TemplateId}", templateId);
                return false;
            }
        }

        public async Task<bool> ShareTemplateAsync(int templateId, string userId, bool isPublic)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                UPDATE QueryTemplates 
                SET IsPublic = @IsPublic, UpdatedAt = GETDATE()
                WHERE Id = @Id AND CreatedBy = @UserId";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Id", templateId);
                cmd.Parameters.AddWithValue("@IsPublic", isPublic);
                cmd.Parameters.AddWithValue("@UserId", userId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("����ģ�干��״̬: {TemplateId}, ����: {IsPublic}", templateId, isPublic);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "����ģ�干��״̬ʧ��: {TemplateId}", templateId);
                return false;
            }
        }

        public async Task<List<QueryTemplate>> SearchTemplatesAsync(string keyword, string? userId = null)
        {
            var templates = new List<QueryTemplate>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                SELECT t.*, 
                       CASE WHEN f.UserId IS NOT NULL THEN 1 ELSE 0 END as IsFavorite
                FROM QueryTemplates t
                LEFT JOIN UserTemplateFavorites f ON t.Id = f.TemplateId AND f.UserId = @UserId
                WHERE (t.IsPublic = 1 OR t.CreatedBy = @UserId)
                  AND (t.Name LIKE @Keyword OR t.Description LIKE @Keyword OR t.Tags LIKE @Keyword)
                ORDER BY t.UsageCount DESC, t.UpdatedAt DESC";

                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Keyword", $"%{keyword}%");
                cmd.Parameters.AddWithValue("@UserId", userId ?? "");

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var template = MapTemplate(reader);
                    templates.Add(template);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ѯģ��ʧ��: {Keyword}", keyword);
            }

            return templates;
        }

        public async Task<QueryTemplate?> ApplyTemplateAsync(int templateId, Dictionary<string, object>? parameters = null)
        {
            try
            {
                var template = await GetTemplateByIdAsync(templateId);
                if (template == null) return null;

                // ����ʹ�ô���
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "UPDATE QueryTemplates SET UsageCount = UsageCount + 1 WHERE Id = @Id";
                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Id", templateId);
                await cmd.ExecuteNonQueryAsync();

                template.UsageCount++;

                // ����ṩ�˲�����Ӧ�ò���ֵ
                if (parameters != null && !string.IsNullOrEmpty(template.ParametersJson))
                {
                    template.Parameters = JsonSerializer.Deserialize<List<TemplateParameter>>(template.ParametersJson);
                    // ������Դ�������滻�߼�
                }

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ӧ�ò�ѯģ��ʧ��: {TemplateId}", templateId);
                return null;
            }
        }

        public async Task<List<QueryTemplateCategory>> GetCategoriesAsync()
        {
            var categories = new List<QueryTemplateCategory>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                SELECT CategoryName, COUNT(*) as TemplateCount
                FROM QueryTemplates 
                GROUP BY CategoryName
                ORDER BY TemplateCount DESC, CategoryName";

                using var cmd = new SqlCommand(sql, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    categories.Add(new QueryTemplateCategory
                    {
                        Name = reader.GetString(reader.GetOrdinal("CategoryName")),
                        TemplateCount = reader.GetInt32(reader.GetOrdinal("TemplateCount"))
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡģ�����ʧ��");
            }

            return categories;
        }

        public async Task<bool> SetTemplateFavoriteAsync(int templateId, string userId, bool isFavorite)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                if (isFavorite)
                {
                    var sql = @"
                    IF NOT EXISTS (SELECT 1 FROM UserTemplateFavorites WHERE UserId = @UserId AND TemplateId = @TemplateId)
                    INSERT INTO UserTemplateFavorites (UserId, TemplateId) VALUES (@UserId, @TemplateId)";
                    
                    using var cmd = new SqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@TemplateId", templateId);
                    await cmd.ExecuteNonQueryAsync();
                }
                else
                {
                    var sql = "DELETE FROM UserTemplateFavorites WHERE UserId = @UserId AND TemplateId = @TemplateId";
                    using var cmd = new SqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@TemplateId", templateId);
                    await cmd.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("����ģ���ղ�״̬: {TemplateId}, �û�: {UserId}, �ղ�: {IsFavorite}", 
                    templateId, userId, isFavorite);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "����ģ���ղ�״̬ʧ��: {TemplateId}", templateId);
                return false;
            }
        }

        private QueryTemplate MapTemplate(SqlDataReader reader)
        {
            var template = new QueryTemplate
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                UsageCount = reader.GetInt32(reader.GetOrdinal("UsageCount")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                Tags = reader.IsDBNull(reader.GetOrdinal("Tags")) ? "" : reader.GetString(reader.GetOrdinal("Tags")),
                QueryConfigJson = reader.GetString(reader.GetOrdinal("QueryConfigJson")),
                ParametersJson = reader.IsDBNull(reader.GetOrdinal("ParametersJson")) ? "" : reader.GetString(reader.GetOrdinal("ParametersJson"))
            };

            // ����Ƿ����ղ��ֶ� - ʹ�ð�ȫ���ֶμ�鷽ʽ
            var hasIsFavoriteColumn = false;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals("IsFavorite", StringComparison.OrdinalIgnoreCase))
                {
                    hasIsFavoriteColumn = true;
                    break;
                }
            }

            if (hasIsFavoriteColumn && !reader.IsDBNull(reader.GetOrdinal("IsFavorite")))
            {
                template.IsFavorite = reader.GetBoolean(reader.GetOrdinal("IsFavorite"));
            }
            else
            {
                template.IsFavorite = false;
            }

            // �����л�JSON����
            try
            {
                if (!string.IsNullOrEmpty(template.QueryConfigJson))
                {
                    template.QueryConfig = JsonSerializer.Deserialize<QueryConfiguration>(template.QueryConfigJson);
                }

                if (!string.IsNullOrEmpty(template.ParametersJson))
                {
                    template.Parameters = JsonSerializer.Deserialize<List<TemplateParameter>>(template.ParametersJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�����л�ģ������ʧ��: {TemplateId}", template.Id);
            }

            return template;
        }
    }
} 