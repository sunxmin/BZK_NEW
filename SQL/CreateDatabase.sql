-- ���� ICD_JZCW ���ݿ�
IF NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'ICD_JZCW')
BEGIN
    CREATE DATABASE [ICD_JZCW]
END
GO

USE [ICD_JZCW]
GO

-- �����û���
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

-- ������ɫ��
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

-- �����û���ɫ������
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

-- ������ɫȨ�ޱ�
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

-- ��������Ϣ��
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

-- ��������Ϣ��
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

-- ��������Ĳ�ѯ��
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

-- ������Ȩ�ޱ�
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

-- ������Լ��
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

-- ���Ψһ����
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

-- ����Ĭ�Ϲ���Ա��ɫ���û�
DECLARE @AdminRoleId NVARCHAR(450) = NEWID()
DECLARE @AdminUserId NVARCHAR(450) = NEWID()

-- �������Ա��ɫ
IF NOT EXISTS (SELECT * FROM [dbo].[Roles] WHERE [Name] = 'Administrator')
BEGIN
    INSERT INTO [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [Description])
    VALUES (@AdminRoleId, 'Administrator', 'ADMINISTRATOR', NEWID(), 'ϵͳ����Ա��ӵ������Ȩ��')
END
ELSE
BEGIN
    SELECT @AdminRoleId = [Id] FROM [dbo].[Roles] WHERE [Name] = 'Administrator'
END

-- ����ϵͳ����ԱȨ��
IF NOT EXISTS (SELECT * FROM [dbo].[RoleClaims] WHERE [RoleId] = @AdminRoleId AND [ClaimValue] = 'SystemAdmin')
BEGIN
    INSERT INTO [dbo].[RoleClaims] ([RoleId], [ClaimType], [ClaimValue])
    VALUES (@AdminRoleId, 'Permission', 'SystemAdmin')
END

-- ����Admin�û�
IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [UserName] = 'admin')
BEGIN
    -- ���룺123
    -- ע�⣺ʵ��Ӧ����Ӧ��ʹ�ø�ǿ�������ϣ����
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
        'ϵͳ����Ա', 'IT����', GETDATE(), GETDATE(), 1
    )
END
ELSE
BEGIN
    SELECT @AdminUserId = [Id] FROM [dbo].[Users] WHERE [UserName] = 'admin'
END

-- ��Admin�û���ӵ�����Ա��ɫ
IF NOT EXISTS (SELECT * FROM [dbo].[UserRoles] WHERE [UserId] = @AdminUserId AND [RoleId] = @AdminRoleId)
BEGIN
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
    VALUES (@AdminUserId, @AdminRoleId)
END

-- ����ʾ�����ݱ�
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

-- ������Լ��
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

-- ���ʾ������
INSERT INTO [dbo].[Diagnoses] ([ICD10Code], [ChineseName], [EnglishName], [Category], [Description])
VALUES
    ('A01.0', '�˺�', 'Typhoid fever', '��Ⱦ��', '�˺������˺�ɳ�ž������ȫ���Ը�Ⱦ'),
    ('E11', '2������', 'Type 2 diabetes mellitus', '��л����', '2��������һ�����ȵ��صֿ�������ȵ��ط��ڲ���Ϊ�����Ĵ�л����'),
    ('I10', 'ԭ���Ը�Ѫѹ', 'Essential (primary) hypertension', '��Ѫ�ܼ���', '��Ѫѹ��һ�ֳ�������Ѫ�ܼ���������ѭ������ѹ����Ϊ����'),
    ('J45', '����', 'Asthma', '����ϵͳ����', '������һ�ֳ�����������֢����������'),
    ('K29.0', '����θ��', 'Acute gastritis', '����ϵͳ����', '����θ����θ�Ĥ�ļ�����֢')
GO

-- ���ʾ����������
INSERT INTO [dbo].[Patients] ([PatientNo], [Name], [Gender], [Age], [BirthDate], [Phone], [Address], [AdmissionDate], [DischargeDate], [Department], [Diagnosis], [CreatedAt], [UpdatedAt])
VALUES
    ('P20230001', '����', '��', 45, '1978-05-12', '13800138001', '�����к�����', '2023-01-10', '2023-01-15', '�ڿ�', '��Ѫѹ', GETDATE(), GETDATE()),
    ('P20230002', '����', 'Ů', 35, '1988-10-25', '13800138002', '�Ϻ����ֶ�����', '2023-02-05', '2023-02-15', '�ڷ��ڿ�', '2������', GETDATE(), GETDATE()),
    ('P20230003', '����', '��', 28, '1995-03-18', '13800138003', '�����������', '2023-03-20', '2023-03-25', '������', '����', GETDATE(), GETDATE()),
    ('P20230004', '����', 'Ů', 52, '1971-12-01', '13800138004', '��������ɽ��', '2023-04-15', NULL, '������', '����θ��', GETDATE(), GETDATE()),
    ('P20230005', 'Ǯ��', '��', 62, '1961-07-30', '13800138005', '�Ͼ���������', '2023-05-01', '2023-05-10', '��Ⱦ��', '�˺�', GETDATE(), GETDATE())
GO

-- ��Ӳ�����Ϲ���
INSERT INTO [dbo].[PatientDiagnoses] ([PatientId], [DiagnosisId], [DiagnosisDate], [DiagnosisType], [DiagnosticDoctor], [Remarks])
VALUES
    (1, 3, '2023-01-10', '����', '��ҽ��', 'Ѫѹƫ�ߣ���Ҫ������ʳ�ͷ�ҩ'),
    (2, 2, '2023-02-05', '����', '��ҽ��', 'Ѫ��ƫ�ߣ���Ҫ������ʳ�ͷ�ҩ'),
    (3, 4, '2023-03-20', '����', '��ҽ��', '����֢״���أ���Ҫ��ǿ����'),
    (4, 5, '2023-04-15', '����', '��ҽ��', 'θ�����ʣ���Ҫ���ñ���θ�Ĥ��ҩ��'),
    (5, 1, '2023-05-01', '����', 'Ǯҽ��', '���ȣ���Ҫ����������')
GO

PRINT '���ݿ��ʼ�����'
GO 