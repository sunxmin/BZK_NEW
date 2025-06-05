-- 向TableInfos表添加IsView列
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'TableInfos' AND COLUMN_NAME = 'IsView'
)
BEGIN
    ALTER TABLE TableInfos
    ADD IsView BIT NOT NULL DEFAULT 0;
    
    PRINT '已成功添加IsView列';
END
ELSE
BEGIN
    PRINT 'IsView列已存在，无需添加';
END 