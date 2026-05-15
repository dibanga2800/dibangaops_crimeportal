/*
  Products catalog — align schema with app + CSV import contract

  CSV columns (required):
    Barcode      -> EAN
    Department   -> Department
    VMECode      -> Description
    ProductName  -> ProductName
    RetailPrice  -> Price
  CostPrice column in file is ignored.

  Table columns after alignment:
    ProductId, EAN, ProductName, Department, Description, Price,
    CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive

  Removed legacy columns (if present):
    Section, Category, Brand, Manufacturer, L8Name

  RUN ORDER (production):
    1) BACKUP the database
    2) SECTION 1 — preflight (read-only)
    3) SECTION 4 — safe drop and recreate (uncomment to execute)
    4) Import catalog CSV via Administration -> Barcode catalog import
       (or SECTION 3 truncate + re-import if table already correct shape)

  SECTION 6 is an alternative when you must keep the table object and only
  drop legacy columns in place (no full recreate).

  WARNING: SECTION 4 deletes ALL product rows. Plan a full catalog re-import.
*/

SET NOCOUNT ON;
GO

/* ========== SECTION 1 — Preflight (read-only) ========== */
PRINT '=== SECTION 1: Preflight ===';

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    PRINT 'Products table does NOT exist — run SECTION 4 to create.';
END
ELSE
BEGIN
    PRINT 'Products table exists.';

    SELECT c.name AS ColumnName, t.name AS DataType, c.max_length, c.is_nullable
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.Products')
    ORDER BY c.column_id;

    /* Expected columns for current app */
    IF COL_LENGTH('dbo.Products', 'EAN') IS NULL
        PRINT 'ERROR: EAN (barcode) column missing.';
    IF COL_LENGTH('dbo.Products', 'Department') IS NULL
        PRINT 'WARN: Department column missing — run SECTION 4.';
    IF COL_LENGTH('dbo.Products', 'Description') IS NULL
        PRINT 'WARN: Description (VMECode) column missing — run SECTION 4.';
    IF COL_LENGTH('dbo.Products', 'Price') IS NULL
        PRINT 'WARN: Price (RetailPrice) column missing — run SECTION 4.';

    IF COL_LENGTH('dbo.Products', 'Section') IS NOT NULL
        PRINT 'INFO: Legacy Section column present — SECTION 4 removes it.';
    IF COL_LENGTH('dbo.Products', 'Category') IS NOT NULL
        PRINT 'INFO: Legacy Category column present — SECTION 4 removes it.';
    IF COL_LENGTH('dbo.Products', 'Brand') IS NOT NULL
        PRINT 'INFO: Legacy Brand column present — SECTION 4 removes it.';
    IF COL_LENGTH('dbo.Products', 'Manufacturer') IS NOT NULL
        PRINT 'INFO: Legacy Manufacturer column present — SECTION 4 removes it.';
    IF COL_LENGTH('dbo.Products', 'L8Name') IS NOT NULL
        PRINT 'INFO: Legacy L8Name column present — SECTION 4 removes it.';

    SELECT COUNT(*) AS TotalRows FROM dbo.Products;
    SELECT COUNT(*) AS ActiveRows FROM dbo.Products WHERE IsActive = 1;

    SELECT EAN AS Barcode, COUNT(*) AS Cnt
    FROM dbo.Products
    GROUP BY EAN
    HAVING COUNT(*) > 1;

    /* Foreign keys referencing Products (would block DROP TABLE) */
    SELECT
        fk.name AS ForeignKeyName,
        OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ReferencingSchema,
        OBJECT_NAME(fk.parent_object_id) AS ReferencingTable
    FROM sys.foreign_keys fk
    WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.Products');
END
GO

/* ========== SECTION 2 — Target schema (informational) ========== */
/*
  dbo.Products:
    ProductId     INT IDENTITY(1,1) NOT NULL  PRIMARY KEY
    EAN           NVARCHAR(50)  NOT NULL      -- barcode from CSV; unique
    ProductName   NVARCHAR(500) NOT NULL
    Department    NVARCHAR(100) NULL          -- from CSV Department column
    Description   NVARCHAR(500) NULL          -- from CSV VMECode column
    Price         DECIMAL(18,2) NULL          -- from CSV RetailPrice column
    CreatedAt     DATETIME2 NOT NULL
    CreatedBy     NVARCHAR(450) NULL
    UpdatedAt     DATETIME2 NULL
    UpdatedBy     NVARCHAR(450) NULL
    IsActive      BIT NOT NULL DEFAULT 1

  Index: IX_Products_EAN UNIQUE (EAN)
*/

/* ========== SECTION 3 — Truncate only (empty catalog, keep schema) ========== */
-- Use after SECTION 4 or when schema already matches SECTION 2.
-- Uncomment to execute after backup:

/*
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    RAISERROR('Products table does not exist. Run SECTION 4 first.', 16, 1);
END
ELSE
BEGIN
    TRUNCATE TABLE dbo.Products;
    PRINT 'Products truncated — ready for CSV import.';
END
GO
*/

/* ========== SECTION 4 — Safe drop and recreate (recommended for alignment) ========== */
-- BACKUP FIRST. Uncomment entire block to execute.
-- Drops all product data and recreates dbo.Products with the correct schema.


SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== SECTION 4: Drop and recreate dbo.Products ===';

IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL
BEGIN
    DECLARE @RowCount BIGINT;
    SELECT @RowCount = COUNT(*) FROM dbo.Products;
    PRINT CONCAT('Products row count before drop: ', @RowCount);

    -- Drop foreign keys that reference dbo.Products
    DECLARE @DropFkSql NVARCHAR(MAX) = N'';
    SELECT @DropFkSql = @DropFkSql
        + N'ALTER TABLE '
        + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
        + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id))
        + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
        + CHAR(13) + CHAR(10)
    FROM sys.foreign_keys
    WHERE referenced_object_id = OBJECT_ID(N'dbo.Products');

    IF LEN(@DropFkSql) > 0
    BEGIN
        PRINT 'Dropping foreign keys referencing Products...';
        EXEC sp_executesql @DropFkSql;
    END

    -- Drop non-PK indexes on Products
    DECLARE @DropIdxSql NVARCHAR(MAX) = N'';
    SELECT @DropIdxSql = @DropIdxSql
        + N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.Products;'
        + CHAR(13) + CHAR(10)
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'dbo.Products')
      AND i.name IS NOT NULL
      AND i.is_primary_key = 0;

    IF LEN(@DropIdxSql) > 0
    BEGIN
        PRINT 'Dropping indexes on Products...';
        EXEC sp_executesql @DropIdxSql;
    END

    DROP TABLE dbo.Products;
    PRINT 'Dropped dbo.Products (all EAN/barcode rows removed).';
END
ELSE
BEGIN
    PRINT 'Products table did not exist — creating fresh.';
END

CREATE TABLE dbo.Products (
    ProductId     INT            IDENTITY(1,1) NOT NULL,
    EAN           NVARCHAR(50)   NOT NULL,   -- barcode
    ProductName   NVARCHAR(500)  NOT NULL,
    Department    NVARCHAR(100)  NULL,
    Description   NVARCHAR(500)  NULL,       -- VMECode
    Price         DECIMAL(18,2)  NULL,       -- RetailPrice
    CreatedAt     DATETIME2      NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CreatedBy     NVARCHAR(450)  NULL,
    UpdatedAt     DATETIME2      NULL,
    UpdatedBy     NVARCHAR(450)  NULL,
    IsActive      BIT            NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (ProductId)
);

CREATE UNIQUE NONCLUSTERED INDEX IX_Products_EAN ON dbo.Products (EAN);

PRINT 'Created dbo.Products with aligned schema.';
PRINT 'Next: import catalog CSV (Barcode, Department, VMECode, ProductName, RetailPrice).';

COMMIT TRANSACTION;
PRINT 'SECTION 4 completed successfully.';
GO


/* ========== SECTION 5 — Post-recreate verification ========== */
-- Run manually after SECTION 4:

/*
PRINT '=== SECTION 5: Verification ===';

SELECT c.name AS ColumnName, t.name AS DataType, c.max_length, c.is_nullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Products')
ORDER BY c.column_id;

SELECT i.name AS IndexName, i.is_unique, i.type_desc
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.Products')
  AND i.name IS NOT NULL;

SELECT COUNT(*) AS ProductCount FROM dbo.Products;
GO
*/

/* ========== SECTION 6 — In-place legacy column removal (no table drop) ========== */
-- Use only if you cannot run SECTION 4. Uncomment after backup.

/*
PRINT '=== SECTION 6: Drop legacy columns in place ===';

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    RAISERROR('Products table does not exist.', 16, 1);
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_Category' AND object_id = OBJECT_ID(N'dbo.Products'))
    DROP INDEX IX_Products_Category ON dbo.Products;

IF COL_LENGTH('dbo.Products', 'Section') IS NOT NULL
    ALTER TABLE dbo.Products DROP COLUMN Section;

IF COL_LENGTH('dbo.Products', 'Category') IS NOT NULL
    ALTER TABLE dbo.Products DROP COLUMN Category;

IF COL_LENGTH('dbo.Products', 'Brand') IS NOT NULL
    ALTER TABLE dbo.Products DROP COLUMN Brand;

IF COL_LENGTH('dbo.Products', 'Manufacturer') IS NOT NULL
    ALTER TABLE dbo.Products DROP COLUMN Manufacturer;

IF COL_LENGTH('dbo.Products', 'L8Name') IS NOT NULL
    ALTER TABLE dbo.Products DROP COLUMN L8Name;

IF COL_LENGTH('dbo.Products', 'Department') IS NULL
    ALTER TABLE dbo.Products ADD Department NVARCHAR(100) NULL;

PRINT 'Legacy columns removed; Department ensured. Re-import catalog if data was inconsistent.';
GO
*/

PRINT '=== Script finished (read-only). Uncomment SECTION 4 to drop/recreate, then import CSV. ===';
GO
