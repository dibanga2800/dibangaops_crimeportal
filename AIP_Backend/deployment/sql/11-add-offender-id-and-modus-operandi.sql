-- Add OffenderId and ModusOperandi columns to Incidents table for analytics
-- Run after migrations 1-10

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Incidents') AND name = 'OffenderId'
)
BEGIN
    ALTER TABLE Incidents ADD OffenderId NVARCHAR(100) NULL;
    PRINT 'Added OffenderId column to Incidents';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Incidents') AND name = 'ModusOperandi'
)
BEGIN
    ALTER TABLE Incidents ADD ModusOperandi NVARCHAR(2000) NULL;
    PRINT 'Added ModusOperandi column to Incidents';
END
GO
