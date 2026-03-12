-- Migration 12: Rename Incidents table columns to use correct field names
-- OfficerName  → StaffMemberName
-- OfficerRole  → StaffMemberRole
-- SiteName     → StoreName

-- Rename OfficerName to StaffMemberName
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Incidents' AND COLUMN_NAME = 'OfficerName'
)
BEGIN
    EXEC sp_rename 'Incidents.OfficerName', 'StaffMemberName', 'COLUMN';
    PRINT 'Renamed OfficerName to StaffMemberName';
END
ELSE
BEGIN
    PRINT 'Column OfficerName not found (already renamed?)';
END

-- Rename OfficerRole to StaffMemberRole
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Incidents' AND COLUMN_NAME = 'OfficerRole'
)
BEGIN
    EXEC sp_rename 'Incidents.OfficerRole', 'StaffMemberRole', 'COLUMN';
    PRINT 'Renamed OfficerRole to StaffMemberRole';
END
ELSE
BEGIN
    PRINT 'Column OfficerRole not found (already renamed?)';
END

-- Rename SiteName to StoreName
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Incidents' AND COLUMN_NAME = 'SiteName'
)
BEGIN
    EXEC sp_rename 'Incidents.SiteName', 'StoreName', 'COLUMN';
    PRINT 'Renamed SiteName to StoreName';
END
ELSE
BEGIN
    PRINT 'Column SiteName not found (already renamed?)';
END

PRINT 'Migration 12 complete.';
