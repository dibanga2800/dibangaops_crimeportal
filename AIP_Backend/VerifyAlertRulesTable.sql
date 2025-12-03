-- Verify AlertRules table was created successfully

-- 1. Check if AlertRules table exists
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'AlertRules';

-- 2. Show table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AlertRules'
ORDER BY ORDINAL_POSITION;

-- 3. Show foreign key constraints
SELECT 
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fk.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fk.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn,
    fk.name AS ConstraintName
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc 
    ON fk.object_id = fkc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) = 'AlertRules';

-- 4. Show indexes
SELECT 
    i.name AS IndexName,
    COL_NAME(ic.object_id, ic.column_id) AS ColumnName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique
FROM sys.indexes AS i
INNER JOIN sys.index_columns AS ic 
    ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('AlertRules')
ORDER BY i.name, ic.key_ordinal;

-- 5. Count rows (should be 0 initially)
SELECT COUNT(*) AS TotalAlertRules FROM AlertRules;
