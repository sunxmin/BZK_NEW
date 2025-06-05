-- 检查AllowedTables表是否存在
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AllowedTables]') AND type in (N'U'))
BEGIN
    -- 检查CanExport列是否存在
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AllowedTables]') AND name = 'CanExport')
    BEGIN
        -- 添加CanExport列
        ALTER TABLE [dbo].[AllowedTables] ADD [CanExport] [bit] NOT NULL DEFAULT(0);
        
        -- 将CanWrite列的值复制到CanExport列
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AllowedTables]') AND name = 'CanWrite')
        BEGIN
            UPDATE [dbo].[AllowedTables] SET [CanExport] = [CanWrite];
            PRINT '已将CanWrite列的值复制到CanExport列';
        END
        ELSE
        BEGIN
            PRINT 'CanWrite列不存在，CanExport列已设置为默认值0';
        END
        
        PRINT 'CanExport列已成功添加到AllowedTables表';
    END
    ELSE
    BEGIN
        PRINT 'CanExport列已存在，无需添加';
    END
END
ELSE
BEGIN
    PRINT 'AllowedTables表不存在';
END 