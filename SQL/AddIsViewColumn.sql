-- ��TableInfos�����IsView��
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'TableInfos' AND COLUMN_NAME = 'IsView'
)
BEGIN
    ALTER TABLE TableInfos
    ADD IsView BIT NOT NULL DEFAULT 0;
    
    PRINT '�ѳɹ����IsView��';
END
ELSE
BEGIN
    PRINT 'IsView���Ѵ��ڣ��������';
END 