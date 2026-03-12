-- Migration: Update User_Roles lookup table to 3-tier role model
-- Date: 2026-03-03
-- Description: Replaces all User_Roles lookup entries with the new 3-tier model
--              (administrator, manager, store). Works regardless of current values.

BEGIN TRANSACTION;

BEGIN TRY
    -- Step 1: Deactivate ALL User_Roles that are NOT the new 3-tier values
    -- (Handles Administrator, AdvantageOneOff, AdvantageOneHO, CustomerSiteMan, CustomerHOMan, etc.)
    DECLARE @AdminUserId NVARCHAR(450) = (SELECT TOP 1 Id FROM AspNetUsers WHERE NormalizedUserName = 'ADMIN' OR Email LIKE '%admin%' ORDER BY Id);
    IF @AdminUserId IS NULL SET @AdminUserId = (SELECT TOP 1 Id FROM AspNetUsers ORDER BY Id);

    UPDATE LookupTables
    SET IsActive = 0, UpdatedAt = GETUTCDATE(), UpdatedBy = @AdminUserId
    WHERE Category = 'User_Roles'
      AND LOWER(LTRIM(RTRIM(Value))) NOT IN ('administrator', 'manager', 'store');

    PRINT 'Deactivated old User_Roles entries: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- Step 2: Update existing admin/manager/store to correct Value (lowercase) and ensure active
    UPDATE LookupTables SET Value = 'administrator', IsActive = 1, SortOrder = 0,
        Description = 'System administrator with full access', UpdatedAt = GETUTCDATE()
    WHERE Category = 'User_Roles' AND LOWER(LTRIM(RTRIM(Value))) = 'administrator';

    UPDATE LookupTables SET Value = 'manager', IsActive = 1, SortOrder = 1,
        Description = 'Manager with operational and analytical access', UpdatedAt = GETUTCDATE()
    WHERE Category = 'User_Roles' AND LOWER(LTRIM(RTRIM(Value))) = 'manager';

    UPDATE LookupTables SET Value = 'store', IsActive = 1, SortOrder = 2,
        Description = 'Store user with incident reporting access', UpdatedAt = GETUTCDATE()
    WHERE Category = 'User_Roles' AND LOWER(LTRIM(RTRIM(Value))) = 'store';

    -- Step 3: Insert any of the 3 new roles that don't exist
    IF NOT EXISTS (SELECT 1 FROM LookupTables WHERE Category = 'User_Roles' AND LOWER(Value) = 'administrator')
    BEGIN
        INSERT INTO LookupTables (Category, Value, Description, Code, SortOrder, IsActive, RecordIsDeletedYN, CreatedBy, CreatedAt, UpdatedBy)
        VALUES ('User_Roles', 'administrator', 'System administrator with full access', '', 0, 1, 0, @AdminUserId, GETUTCDATE(), @AdminUserId);
        PRINT 'Inserted User_Roles: administrator';
    END

    IF NOT EXISTS (SELECT 1 FROM LookupTables WHERE Category = 'User_Roles' AND LOWER(Value) = 'manager')
    BEGIN
        INSERT INTO LookupTables (Category, Value, Description, Code, SortOrder, IsActive, RecordIsDeletedYN, CreatedBy, CreatedAt, UpdatedBy)
        VALUES ('User_Roles', 'manager', 'Manager with operational and analytical access', '', 1, 1, 0, @AdminUserId, GETUTCDATE(), @AdminUserId);
        PRINT 'Inserted User_Roles: manager';
    END

    IF NOT EXISTS (SELECT 1 FROM LookupTables WHERE Category = 'User_Roles' AND LOWER(Value) = 'store')
    BEGIN
        INSERT INTO LookupTables (Category, Value, Description, Code, SortOrder, IsActive, RecordIsDeletedYN, CreatedBy, CreatedAt, UpdatedBy)
        VALUES ('User_Roles', 'store', 'Store user with incident reporting access', '', 2, 1, 0, @AdminUserId, GETUTCDATE(), @AdminUserId);
        PRINT 'Inserted User_Roles: store';
    END

    -- Step 4: Update Employee.AipAccessLevel to new values (broad match for any old variants)
    UPDATE Employees SET AipAccessLevel = 'administrator' WHERE LOWER(LTRIM(RTRIM(ISNULL(AipAccessLevel,'')))) IN ('administrator','advantageoneadmin');
    UPDATE Employees SET AipAccessLevel = 'manager' WHERE LOWER(LTRIM(RTRIM(ISNULL(AipAccessLevel,'')))) IN ('advantageonemanager','advantageoneho','advantageonehoofficer','customerhoman','customerhomemanager');
    UPDATE Employees SET AipAccessLevel = 'store' WHERE LOWER(LTRIM(RTRIM(ISNULL(AipAccessLevel,'')))) IN ('advantageoneoff','advantageoneofficer','customersiteman','customersitemanager');

    PRINT 'Updated Employee AipAccessLevel values';

    COMMIT TRANSACTION;
    PRINT 'Successfully updated User_Roles lookup table to 3-tier model.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH;

-- Verification
SELECT LookupId, Category, Value, Description, SortOrder, IsActive
FROM LookupTables WHERE Category = 'User_Roles' AND IsActive = 1 ORDER BY SortOrder;
