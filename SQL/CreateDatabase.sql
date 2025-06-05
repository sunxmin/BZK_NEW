-- 创建 ICD_JZCW 数据库
IF NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'ICD_JZCW')
BEGIN
    CREATE DATABASE [ICD_JZCW]
END
GO

USE [ICD_JZCW]
GO

-- 创建用户表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users](
        [Id] [nvarchar](450) NOT NULL,
        [UserName] [nvarchar](256) NOT NULL,
        [NormalizedUserName] [nvarchar](256) NOT NULL,
        [Email] [nvarchar](256) NULL,
        [NormalizedEmail] [nvarchar](256) NULL,
        [EmailConfirmed] [bit] NOT NULL,
        [PasswordHash] [nvarchar](max) NULL,
        [SecurityStamp] [nvarchar](max) NULL,
        [ConcurrencyStamp] [nvarchar](max) NULL,
        [PhoneNumber] [nvarchar](max) NULL,
        [PhoneNumberConfirmed] [bit] NOT NULL,
        [TwoFactorEnabled] [bit] NOT NULL,
        [LockoutEnd] [datetimeoffset](7) NULL,
        [LockoutEnabled] [bit] NOT NULL,
        [AccessFailedCount] [int] NOT NULL,
        [DisplayName] [nvarchar](256) NULL,
        [Department] [nvarchar](256) NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [LastLogin] [datetime2](7) NOT NULL,
        [IsActive] [bit] NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        )
    )
END
GO

-- 创建角色表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Roles](
        [Id] [nvarchar](450) NOT NULL,
        [Name] [nvarchar](256) NOT NULL,
        [NormalizedName] [nvarchar](256) NOT NULL,
        [ConcurrencyStamp] [nvarchar](max) NULL,
        [Description] [nvarchar](256) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        )
    )
END
GO

-- 创建用户角色关联表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRoles](
        [UserId] [nvarchar](450) NOT NULL,
        [RoleId] [nvarchar](450) NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED 
        (
            [UserId] ASC,
            [RoleId] ASC
        )
    )
END
GO

-- 创建角色权限表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleClaims]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RoleClaims](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [RoleId] [nvarchar](450) NOT NULL,
        [ClaimType] [nvarchar](max) NULL,
        [ClaimValue] [nvarchar](max) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        )
    )
END
GO

-- 创建表信息表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TableInfos]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TableInfos](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [TableName] [nvarchar](256) NOT NULL,
        [DisplayName] [nvarchar](256) NOT NULL,
        [Description] [nvarchar](512) NULL,
        CONSTRAINT [PK_TableInfos] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        )
    )
END
GO

-- 创建列信息表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ColumnInfos]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ColumnInfos](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [TableId] [int] NOT NULL,
        [ColumnName] [nvarchar](256) NOT NULL,
        [DisplayName] [nvarchar](256) NOT NULL,
        [DataType] [nvarchar](50) NOT NULL,
        [Description] [nvarchar](512) NULL,
        [IsPrimaryKey] [bit] NOT NULL,
        [IsNullable] [bit] NOT NULL,
        CONSTRAINT [PK_ColumnInfos] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        )
    )
END
GO

-- 创建保存的查询表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SavedQueries]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SavedQueries](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [nvarchar](450) NOT NULL,
        [Name] [nvarchar](256) NOT NULL,
        [Description] [nvarchar](512) NULL,
        [SqlQuery] [nvarchar](max) NOT NULL,
        [TablesIncluded] [nvarchar](max) NULL,
        [ColumnsIncluded] [nvarchar](max) NULL,
        [FilterConditions] [nvarchar](max) NULL,
        [SortOrder] [nvarchar](max) NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [UpdatedAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_SavedQueries] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        )
    )
END
GO

-- 创建表权限表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AllowedTables]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AllowedTables](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [nvarchar](450) NOT NULL,
        [TableName] [nvarchar](256) NOT NULL,
        [CanRead] [bit] NOT NULL,
        [CanExport] [bit] NOT NULL,
        CONSTRAINT [PK_AllowedTables] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        )
    )
END
GO

-- 添加外键约束
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Users]'))
BEGIN
    ALTER TABLE [dbo].[UserRoles] WITH CHECK ADD CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY([UserId])
    REFERENCES [dbo].[Users] ([Id])
    ON DELETE CASCADE
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_UserRoles_Roles]'))
BEGIN
    ALTER TABLE [dbo].[UserRoles] WITH CHECK ADD CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY([RoleId])
    REFERENCES [dbo].[Roles] ([Id])
    ON DELETE CASCADE
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RoleClaims_Roles]'))
BEGIN
    ALTER TABLE [dbo].[RoleClaims] WITH CHECK ADD CONSTRAINT [FK_RoleClaims_Roles] FOREIGN KEY([RoleId])
    REFERENCES [dbo].[Roles] ([Id])
    ON DELETE CASCADE
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ColumnInfos_TableInfos]'))
BEGIN
    ALTER TABLE [dbo].[ColumnInfos] WITH CHECK ADD CONSTRAINT [FK_ColumnInfos_TableInfos] FOREIGN KEY([TableId])
    REFERENCES [dbo].[TableInfos] ([Id])
    ON DELETE CASCADE
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SavedQueries_Users]'))
BEGIN
    ALTER TABLE [dbo].[SavedQueries] WITH CHECK ADD CONSTRAINT [FK_SavedQueries_Users] FOREIGN KEY([UserId])
    REFERENCES [dbo].[Users] ([Id])
    ON DELETE CASCADE
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_AllowedTables_Users]'))
BEGIN
    ALTER TABLE [dbo].[AllowedTables] WITH CHECK ADD CONSTRAINT [FK_AllowedTables_Users] FOREIGN KEY([UserId])
    REFERENCES [dbo].[Users] ([Id])
    ON DELETE CASCADE
END
GO

-- 添加唯一索引
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_UserName' AND object_id = OBJECT_ID(N'[dbo].[Users]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_UserName] ON [dbo].[Users]
    (
        [UserName] ASC
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID(N'[dbo].[Users]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]
    (
        [Email] ASC
    )
    WHERE [Email] IS NOT NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Roles_Name' AND object_id = OBJECT_ID(N'[dbo].[Roles]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Roles_Name] ON [dbo].[Roles]
    (
        [Name] ASC
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SavedQueries_Name' AND object_id = OBJECT_ID(N'[dbo].[SavedQueries]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_SavedQueries_Name] ON [dbo].[SavedQueries]
    (
        [Name] ASC
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AllowedTables_UserId_TableName' AND object_id = OBJECT_ID(N'[dbo].[AllowedTables]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_AllowedTables_UserId_TableName] ON [dbo].[AllowedTables]
    (
        [UserId] ASC,
        [TableName] ASC
    )
END
GO

-- 插入默认管理员角色和用户
DECLARE @AdminRoleId NVARCHAR(450) = NEWID()
DECLARE @AdminUserId NVARCHAR(450) = NEWID()

-- 插入管理员角色
IF NOT EXISTS (SELECT * FROM [dbo].[Roles] WHERE [Name] = 'Administrator')
BEGIN
    INSERT INTO [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [Description])
    VALUES (@AdminRoleId, 'Administrator', 'ADMINISTRATOR', NEWID(), '系统管理员，拥有所有权限')
END
ELSE
BEGIN
    SELECT @AdminRoleId = [Id] FROM [dbo].[Roles] WHERE [Name] = 'Administrator'
END

-- 插入系统管理员权限
IF NOT EXISTS (SELECT * FROM [dbo].[RoleClaims] WHERE [RoleId] = @AdminRoleId AND [ClaimValue] = 'SystemAdmin')
BEGIN
    INSERT INTO [dbo].[RoleClaims] ([RoleId], [ClaimType], [ClaimValue])
    VALUES (@AdminRoleId, 'Permission', 'SystemAdmin')
END

-- 插入Admin用户
IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [UserName] = 'admin')
BEGIN
    -- 密码：123
    -- 注意：实际应用中应该使用更强的密码哈希方法
    INSERT INTO [dbo].[Users] (
        [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
        [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
        [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled],
        [LockoutEnd], [LockoutEnabled], [AccessFailedCount],
        [DisplayName], [Department], [CreatedAt], [LastLogin], [IsActive]
    )
    VALUES (
        @AdminUserId, 'admin', 'ADMIN', 'admin@example.com', 'ADMIN@EXAMPLE.COM',
        1, 'AQAAAAIAAYagAAAAEFHPFjXEZf8z7xYqoNu/cNKPdMqOVu+evFjTyvoXeILQNp0QTwYH3GRVjg7lPPdPmg==', NEWID(), NEWID(),
        NULL, 0, 0,
        NULL, 0, 0,
        '系统管理员', 'IT部门', GETDATE(), GETDATE(), 1
    )
END
ELSE
BEGIN
    SELECT @AdminUserId = [Id] FROM [dbo].[Users] WHERE [UserName] = 'admin'
END

-- 将Admin用户添加到管理员角色
IF NOT EXISTS (SELECT * FROM [dbo].[UserRoles] WHERE [UserId] = @AdminUserId AND [RoleId] = @AdminRoleId)
BEGIN
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
    VALUES (@AdminUserId, @AdminRoleId)
END

-- 创建示例数据表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Patients](
        [PatientId] [int] IDENTITY(1,1) NOT NULL,
        [PatientNo] [nvarchar](50) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Gender] [nvarchar](10) NOT NULL,
        [Age] [int] NULL,
        [BirthDate] [date] NULL,
        [Phone] [nvarchar](20) NULL,
        [Address] [nvarchar](200) NULL,
        [AdmissionDate] [datetime2](7) NULL,
        [DischargeDate] [datetime2](7) NULL,
        [Department] [nvarchar](50) NULL,
        [Diagnosis] [nvarchar](500) NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [UpdatedAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_Patients] PRIMARY KEY CLUSTERED 
        (
            [PatientId] ASC
        )
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Diagnoses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Diagnoses](
        [DiagnosisId] [int] IDENTITY(1,1) NOT NULL,
        [ICD10Code] [nvarchar](20) NOT NULL,
        [ChineseName] [nvarchar](200) NOT NULL,
        [EnglishName] [nvarchar](200) NULL,
        [Category] [nvarchar](100) NULL,
        [Description] [nvarchar](500) NULL,
        CONSTRAINT [PK_Diagnoses] PRIMARY KEY CLUSTERED 
        (
            [DiagnosisId] ASC
        )
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PatientDiagnoses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PatientDiagnoses](
        [PatientDiagnosisId] [int] IDENTITY(1,1) NOT NULL,
        [PatientId] [int] NOT NULL,
        [DiagnosisId] [int] NOT NULL,
        [DiagnosisDate] [datetime2](7) NOT NULL,
        [DiagnosisType] [nvarchar](50) NULL,
        [DiagnosticDoctor] [nvarchar](50) NULL,
        [Remarks] [nvarchar](500) NULL,
        CONSTRAINT [PK_PatientDiagnoses] PRIMARY KEY CLUSTERED 
        (
            [PatientDiagnosisId] ASC
        )
    )
END
GO

-- 添加外键约束
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PatientDiagnoses_Patients]'))
BEGIN
    ALTER TABLE [dbo].[PatientDiagnoses] WITH CHECK ADD CONSTRAINT [FK_PatientDiagnoses_Patients] FOREIGN KEY([PatientId])
    REFERENCES [dbo].[Patients] ([PatientId])
    ON DELETE CASCADE
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PatientDiagnoses_Diagnoses]'))
BEGIN
    ALTER TABLE [dbo].[PatientDiagnoses] WITH CHECK ADD CONSTRAINT [FK_PatientDiagnoses_Diagnoses] FOREIGN KEY([DiagnosisId])
    REFERENCES [dbo].[Diagnoses] ([DiagnosisId])
END
GO

-- 添加示例数据
INSERT INTO [dbo].[Diagnoses] ([ICD10Code], [ChineseName], [EnglishName], [Category], [Description])
VALUES
    ('A01.0', '伤寒', 'Typhoid fever', '传染病', '伤寒是由伤寒沙门菌引起的全身性感染'),
    ('E11', '2型糖尿病', 'Type 2 diabetes mellitus', '代谢疾病', '2型糖尿病是一种以胰岛素抵抗和相对胰岛素分泌不足为特征的代谢紊乱'),
    ('I10', '原发性高血压', 'Essential (primary) hypertension', '心血管疾病', '高血压是一种常见的心血管疾病，以体循环动脉压升高为特征'),
    ('J45', '哮喘', 'Asthma', '呼吸系统疾病', '哮喘是一种常见的慢性炎症性气道疾病'),
    ('K29.0', '急性胃炎', 'Acute gastritis', '消化系统疾病', '急性胃炎是胃黏膜的急性炎症')
GO

-- 添加示例病人数据
INSERT INTO [dbo].[Patients] ([PatientNo], [Name], [Gender], [Age], [BirthDate], [Phone], [Address], [AdmissionDate], [DischargeDate], [Department], [Diagnosis], [CreatedAt], [UpdatedAt])
VALUES
    ('P20230001', '张三', '男', 45, '1978-05-12', '13800138001', '北京市海淀区', '2023-01-10', '2023-01-15', '内科', '高血压', GETDATE(), GETDATE()),
    ('P20230002', '李四', '女', 35, '1988-10-25', '13800138002', '上海市浦东新区', '2023-02-05', '2023-02-15', '内分泌科', '2型糖尿病', GETDATE(), GETDATE()),
    ('P20230003', '王五', '男', 28, '1995-03-18', '13800138003', '广州市天河区', '2023-03-20', '2023-03-25', '呼吸科', '哮喘', GETDATE(), GETDATE()),
    ('P20230004', '赵六', '女', 52, '1971-12-01', '13800138004', '深圳市南山区', '2023-04-15', NULL, '消化科', '急性胃炎', GETDATE(), GETDATE()),
    ('P20230005', '钱七', '男', 62, '1961-07-30', '13800138005', '南京市玄武区', '2023-05-01', '2023-05-10', '传染科', '伤寒', GETDATE(), GETDATE())
GO

-- 添加病人诊断关联
INSERT INTO [dbo].[PatientDiagnoses] ([PatientId], [DiagnosisId], [DiagnosisDate], [DiagnosisType], [DiagnosticDoctor], [Remarks])
VALUES
    (1, 3, '2023-01-10', '初诊', '张医生', '血压偏高，需要控制饮食和服药'),
    (2, 2, '2023-02-05', '初诊', '李医生', '血糖偏高，需要控制饮食和服药'),
    (3, 4, '2023-03-20', '复诊', '王医生', '哮喘症状加重，需要加强治疗'),
    (4, 5, '2023-04-15', '初诊', '赵医生', '胃部不适，需要服用保护胃黏膜的药物'),
    (5, 1, '2023-05-01', '初诊', '钱医生', '发热，需要抗生素治疗')
GO

PRINT '数据库初始化完成'
GO 