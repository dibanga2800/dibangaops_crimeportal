-- Add security preference columns to AspNetUsers (persisted in DB instead of localStorage)
-- Idempotent: safe to run multiple times

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'EmailNotificationsEnabled')
BEGIN
    ALTER TABLE AspNetUsers ADD EmailNotificationsEnabled BIT NOT NULL DEFAULT 1
    PRINT 'Added EmailNotificationsEnabled column'
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'LoginAlertsEnabled')
BEGIN
    ALTER TABLE AspNetUsers ADD LoginAlertsEnabled BIT NOT NULL DEFAULT 1
    PRINT 'Added LoginAlertsEnabled column'
END
