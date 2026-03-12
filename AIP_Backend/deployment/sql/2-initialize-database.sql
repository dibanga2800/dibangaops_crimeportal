-- ========================================
-- 2️⃣ DATABASE INITIALIZATION
-- ========================================
-- This script initializes the database structure
-- Run this AFTER running EF Core migrations

USE COOP_PRODUCTION;
GO

PRINT '========================================';
PRINT 'DATABASE INITIALIZATION';
PRINT '========================================';
PRINT '';

-- This file is a placeholder because your Entity Framework migrations
-- will handle the actual database schema creation.

-- To initialize the database:
-- 1. Ensure your connection string in appsettings.Production.json is correct
-- 2. Run migrations from your development machine or build server:

PRINT '📝 To initialize the database, run these commands:';
PRINT '';
PRINT 'cd C:\path\to\AIP_Backend';
PRINT 'dotnet ef database update --connection "Server=YOUR_SERVER;Database=COOP_PRODUCTION;User Id=aip_app_user;Password=YourPassword;TrustServerCertificate=True;" ';
PRINT '';
PRINT 'OR from Package Manager Console in Visual Studio:';
PRINT '';
PRINT 'Update-Database -Context ApplicationDbContext';
PRINT '';

-- Verify tables exist
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES)
BEGIN
    PRINT '✅ Database tables found:';
    SELECT 
        TABLE_SCHEMA,
        TABLE_NAME,
        TABLE_TYPE
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_TYPE = 'BASE TABLE'
    ORDER BY TABLE_NAME;
END
ELSE
BEGIN
    PRINT '⚠️  No tables found. Run EF Core migrations to create database schema.';
END
GO

PRINT '';
PRINT '========================================';
