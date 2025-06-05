-- ���AllowedTables���Ƿ����
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AllowedTables]') AND type in (N'U'))
BEGIN
    -- ���CanExport���Ƿ����
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AllowedTables]') AND name = 'CanExport')
    BEGIN
        -- ���CanExport��
        ALTER TABLE [dbo].[AllowedTables] ADD [CanExport] [bit] NOT NULL DEFAULT(0);
        
        -- ��CanWrite�е�ֵ���Ƶ�CanExport��
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AllowedTables]') AND name = 'CanWrite')
        BEGIN
            UPDATE [dbo].[AllowedTables] SET [CanExport] = [CanWrite];
            PRINT '�ѽ�CanWrite�е�ֵ���Ƶ�CanExport��';
        END
        ELSE
        BEGIN
            PRINT 'CanWrite�в����ڣ�CanExport��������ΪĬ��ֵ0';
        END
        
        PRINT 'CanExport���ѳɹ���ӵ�AllowedTables��';
    END
    ELSE
    BEGIN
        PRINT 'CanExport���Ѵ��ڣ��������';
    END
END
ELSE
BEGIN
    PRINT 'AllowedTables������';
END 