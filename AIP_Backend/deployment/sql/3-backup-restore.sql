-- ========================================
-- 3️⃣ BACKUP AND RESTORE SCRIPTS
-- ========================================

-- ========================================
-- BACKUP PRODUCTION DATABASE
-- ========================================
USE master;
GO

DECLARE @BackupPath NVARCHAR(500) = 'C:\SQLBackups\COOP_PRODUCTION_' + CONVERT(VARCHAR, GETDATE(), 112) + '_' + REPLACE(CONVERT(VARCHAR, GETDATE(), 108), ':', '') + '.bak';
DECLARE @BackupFolder NVARCHAR(500) = 'C:\SQLBackups';

-- Create backup folder if it doesn't exist (PowerShell command)
PRINT '📝 Create backup folder:';
PRINT 'Run this in PowerShell: New-Item -ItemType Directory -Path "' + @BackupFolder + '" -Force';
PRINT '';

-- Full database backup
PRINT '💾 Creating full database backup...';
PRINT 'Backup location: ' + @BackupPath;

BACKUP DATABASE COOP_PRODUCTION
TO DISK = @BackupPath
WITH 
    FORMAT,
    COMPRESSION,
    STATS = 10,
    DESCRIPTION = 'Full backup of COOP_PRODUCTION database';

PRINT '✅ Backup completed successfully!';
PRINT '';

-- ========================================
-- VERIFY BACKUP
-- ========================================
PRINT '🔍 Verifying backup...';

RESTORE VERIFYONLY
FROM DISK = @BackupPath;

PRINT '✅ Backup verification successful!';
PRINT '';

-- ========================================
-- RESTORE DATABASE (COMMENTED OUT FOR SAFETY)
-- ========================================
-- ⚠️  UNCOMMENT AND MODIFY THESE COMMANDS TO RESTORE A BACKUP
-- ⚠️  THIS WILL OVERWRITE THE CURRENT DATABASE!

/*
USE master;
GO

-- Set single user mode
ALTER DATABASE COOP_PRODUCTION SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

-- Restore database
RESTORE DATABASE COOP_PRODUCTION
FROM DISK = 'C:\SQLBackups\COOP_PRODUCTION_20241204_120000.bak'
WITH 
    REPLACE,
    STATS = 10;
GO

-- Set back to multi-user mode
ALTER DATABASE COOP_PRODUCTION SET MULTI_USER;
GO

PRINT '✅ Database restored successfully!';
*/

-- ========================================
-- AUTOMATED BACKUP SCHEDULE
-- ========================================
PRINT '========================================';
PRINT 'AUTOMATED BACKUP SCHEDULE';
PRINT '========================================';
PRINT '';
PRINT '📝 To set up automated backups:';
PRINT '';
PRINT '1. Open SQL Server Management Studio';
PRINT '2. Expand SQL Server Agent → Jobs';
PRINT '3. Right-click Jobs → New Job';
PRINT '4. Name: "Daily COOP_PRODUCTION Backup"';
PRINT '5. Steps → New:';
PRINT '   - Type: Transact-SQL script';
PRINT '   - Database: master';
PRINT '   - Command: (Use the BACKUP DATABASE command above)';
PRINT '6. Schedules → New:';
PRINT '   - Occurs: Daily';
PRINT '   - Time: 2:00 AM (off-peak hours)';
PRINT '7. Click OK';
PRINT '';
PRINT '========================================';
