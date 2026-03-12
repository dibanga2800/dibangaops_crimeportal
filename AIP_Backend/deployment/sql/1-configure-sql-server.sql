-- ========================================
-- 1️⃣ SQL SERVER CONFIGURATION
-- ========================================
-- Run this script on your SQL Server to configure it for production

-- Enable TCP/IP protocol (must be done via SQL Server Configuration Manager)
-- This script configures security and authentication

USE master;
GO

-- 1. Enable SQL Server Authentication (Mixed Mode)
EXEC xp_instance_regwrite 
    N'HKEY_LOCAL_MACHINE', 
    N'Software\Microsoft\MSSQLServer\MSSQLServer',
    N'LoginMode', 
    REG_DWORD, 
    2;
GO

PRINT '✅ SQL Server Authentication enabled';
PRINT '⚠️  You must restart SQL Server for this change to take effect';
PRINT '';

-- 2. Enable remote connections
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO

EXEC sp_configure 'remote access', 1;
RECONFIGURE;
GO

PRINT '✅ Remote connections enabled';
PRINT '';

-- 3. Configure firewall (PowerShell command - run separately as Administrator)
PRINT '📝 Firewall Configuration:';
PRINT 'Run this PowerShell command as Administrator:';
PRINT 'New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow';
PRINT '';

-- 4. Create production database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'COOP_PRODUCTION')
BEGIN
    CREATE DATABASE COOP_PRODUCTION;
    PRINT '✅ Database COOP_PRODUCTION created';
END
ELSE
BEGIN
    PRINT '⚠️  Database COOP_PRODUCTION already exists';
END
GO

USE COOP_PRODUCTION;
GO

-- 5. Create application database user
-- IMPORTANT: Replace 'YourStrongPasswordHere' with a secure password
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'aip_app_user')
BEGIN
    CREATE LOGIN aip_app_user WITH PASSWORD = 'YourStrongPasswordHere';
    PRINT '✅ Login aip_app_user created';
END
ELSE
BEGIN
    PRINT '⚠️  Login aip_app_user already exists';
END
GO

USE COOP_PRODUCTION;
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'aip_app_user')
BEGIN
    CREATE USER aip_app_user FOR LOGIN aip_app_user;
    PRINT '✅ User aip_app_user created in COOP_PRODUCTION';
END
GO

-- Grant necessary permissions
ALTER ROLE db_datareader ADD MEMBER aip_app_user;
ALTER ROLE db_datawriter ADD MEMBER aip_app_user;
ALTER ROLE db_ddladmin ADD MEMBER aip_app_user;
GO

PRINT '✅ Permissions granted to aip_app_user';
PRINT '';

-- 6. Configure database settings for production
ALTER DATABASE COOP_PRODUCTION SET RECOVERY SIMPLE;
ALTER DATABASE COOP_PRODUCTION SET AUTO_CLOSE OFF;
ALTER DATABASE COOP_PRODUCTION SET AUTO_SHRINK OFF;
ALTER DATABASE COOP_PRODUCTION SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE COOP_PRODUCTION SET AUTO_UPDATE_STATISTICS ON;
GO

PRINT '✅ Database settings configured';
PRINT '';

-- 7. Display connection string
PRINT '========================================';
PRINT 'CONNECTION STRING FOR appsettings.Production.json:';
PRINT '========================================';
PRINT '';
PRINT '"DefaultDbConnection": "Server=localhost;Database=COOP_PRODUCTION;User Id=aip_app_user;Password=YourStrongPasswordHere;TrustServerCertificate=True;Encrypt=True;"';
PRINT '';
PRINT '⚠️  IMPORTANT:';
PRINT '1. Replace YourStrongPasswordHere with the password you set above';
PRINT '2. Update appsettings.Production.json with this connection string';
PRINT '3. RESTART SQL Server for authentication changes to take effect';
PRINT '4. Run 2-initialize-database.sql to set up tables';
PRINT '';
PRINT '========================================';
