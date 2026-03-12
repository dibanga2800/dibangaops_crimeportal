-- Migration 13: Add Department column to Products table and seed ProductDepartments lookup values

-- Add Department column to Products table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'Department'
)
BEGIN
    ALTER TABLE Products ADD Department NVARCHAR(100) NULL;
    PRINT 'Added Department column to Products table';
END
ELSE
BEGIN
    PRINT 'Department column already exists on Products table';
END

-- Seed ProductDepartments into LookupTables
DECLARE @departments TABLE (Value NVARCHAR(100), SortOrder INT)
INSERT INTO @departments VALUES
    ('BABY FOOD',               0),
    ('BAKERY',                  1),
    ('BEERS/WINES/SPIRITS',     2),
    ('BUTTER & CHEESE',         3),
    ('CIGARETTES & TOBACCO',    4),
    ('COFFEE',                  5),
    ('CONFECTIONARY',           6),
    ('FROZEN FOODS',            7),
    ('FRUIT & VEGETABLES',      8),
    ('GROCERY',                 9),
    ('HEALTH & BEAUTY',         10),
    ('HOMEWARES',               11),
    ('LAUNDRY DETERGENT',       12),
    ('MEAT & POULTRY',          13),
    ('MEDICINE',                14),
    ('NEWSPAPERS & MAGAZINES',  15),
    ('NON FOOD',                16),
    ('SEASONAL',                17),
    ('SOFT BEVERAGES',          18),
    ('SPORTS & ENERGY DRINKS',  19)

INSERT INTO LookupTables (Category, Value, Description, Code, SortOrder, IsActive, CreatedBy, CreatedAt)
SELECT
    'ProductDepartments',
    d.Value,
    'Product Department: ' + d.Value,
    '',
    d.SortOrder,
    1,
    'System',
    GETUTCDATE()
FROM @departments d
WHERE NOT EXISTS (
    SELECT 1 FROM LookupTables lt
    WHERE lt.Category = 'ProductDepartments' AND lt.Value = d.Value
)

PRINT 'Seeded ProductDepartments lookup values';
PRINT 'Migration 13 complete.';
