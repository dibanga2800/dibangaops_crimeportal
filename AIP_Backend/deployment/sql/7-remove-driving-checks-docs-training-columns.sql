-- Migration: Remove Driving License, Checks & References, Employment Documentation, and Training & Induction columns
-- Date: 2026-03-03
-- Description: Drops columns from the Employees table that are no longer used in the application.
--              These sections have been removed from the employee registration form.

-- IMPORTANT: Back up the Employees table before running this migration.
-- Run this AFTER deploying the updated backend code.

BEGIN TRANSACTION;

BEGIN TRY
    -- Step 1: Drop default constraints that may exist on these columns
    -- (SQL Server creates these when columns have default values)
    DECLARE @ConstraintName NVARCHAR(200);
    DECLARE @ColumnName NVARCHAR(128);
    DECLARE @Sql NVARCHAR(500);

    DECLARE constraint_cursor CURSOR LOCAL FOR
        SELECT c.name, dc.name
        FROM sys.columns c
        INNER JOIN sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE c.object_id = OBJECT_ID('Employees')
        AND c.name IN (
            'DrivingLicenceType','DateDLChecked','DrivingLicenceCopyTakenYN','SixMonthlyCheck',
            'GraydonCheckAuthorised','GraydonCheckDetails','InitialOralReferencesComplete','InitialOralReferencesDate',
            'WrittenRefsComplete','WrittenRefsCompleteDate','QuickStarterFormCompletedYN',
            'WorkingTimeDirective','WorkingTimeDirectiveComplete','ContractOfEmploymentSignedYN','PhotoTakenYN',
            'PhotoFile','IDCardIssuedYN','EquipmentIssuedYN','UniformIssuedYN','NextOfKinDetailsComplete','PeopleHoursPin',
            'FullRotasIssued','InductionAndTrainingBooked','Location','Trainer'
        );

    OPEN constraint_cursor;
    FETCH NEXT FROM constraint_cursor INTO @ColumnName, @ConstraintName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Sql = 'ALTER TABLE [Employees] DROP CONSTRAINT [' + @ConstraintName + ']';
        EXEC sp_executesql @Sql;
        PRINT 'Dropped constraint: ' + @ConstraintName;
        FETCH NEXT FROM constraint_cursor INTO @ColumnName, @ConstraintName;
    END
    CLOSE constraint_cursor;
    DEALLOCATE constraint_cursor;

    -- Step 2: Drop Driving License Information columns
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'DrivingLicenceType')
        ALTER TABLE [Employees] DROP COLUMN [DrivingLicenceType];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'DateDLChecked')
        ALTER TABLE [Employees] DROP COLUMN [DateDLChecked];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'DrivingLicenceCopyTakenYN')
        ALTER TABLE [Employees] DROP COLUMN [DrivingLicenceCopyTakenYN];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'SixMonthlyCheck')
        ALTER TABLE [Employees] DROP COLUMN [SixMonthlyCheck];

    -- Drop Checks and References columns
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'GraydonCheckAuthorised')
        ALTER TABLE [Employees] DROP COLUMN [GraydonCheckAuthorised];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'GraydonCheckDetails')
        ALTER TABLE [Employees] DROP COLUMN [GraydonCheckDetails];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'InitialOralReferencesComplete')
        ALTER TABLE [Employees] DROP COLUMN [InitialOralReferencesComplete];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'InitialOralReferencesDate')
        ALTER TABLE [Employees] DROP COLUMN [InitialOralReferencesDate];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'WrittenRefsComplete')
        ALTER TABLE [Employees] DROP COLUMN [WrittenRefsComplete];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'WrittenRefsCompleteDate')
        ALTER TABLE [Employees] DROP COLUMN [WrittenRefsCompleteDate];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'QuickStarterFormCompletedYN')
        ALTER TABLE [Employees] DROP COLUMN [QuickStarterFormCompletedYN];

    -- Drop Employment Documentation columns
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'WorkingTimeDirective')
        ALTER TABLE [Employees] DROP COLUMN [WorkingTimeDirective];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'WorkingTimeDirectiveComplete')
        ALTER TABLE [Employees] DROP COLUMN [WorkingTimeDirectiveComplete];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'ContractOfEmploymentSignedYN')
        ALTER TABLE [Employees] DROP COLUMN [ContractOfEmploymentSignedYN];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'PhotoTakenYN')
        ALTER TABLE [Employees] DROP COLUMN [PhotoTakenYN];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'PhotoFile')
        ALTER TABLE [Employees] DROP COLUMN [PhotoFile];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'IDCardIssuedYN')
        ALTER TABLE [Employees] DROP COLUMN [IDCardIssuedYN];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'EquipmentIssuedYN')
        ALTER TABLE [Employees] DROP COLUMN [EquipmentIssuedYN];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'UniformIssuedYN')
        ALTER TABLE [Employees] DROP COLUMN [UniformIssuedYN];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'NextOfKinDetailsComplete')
        ALTER TABLE [Employees] DROP COLUMN [NextOfKinDetailsComplete];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'PeopleHoursPin')
        ALTER TABLE [Employees] DROP COLUMN [PeopleHoursPin];

    -- Drop Training and Induction columns
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'FullRotasIssued')
        ALTER TABLE [Employees] DROP COLUMN [FullRotasIssued];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'InductionAndTrainingBooked')
        ALTER TABLE [Employees] DROP COLUMN [InductionAndTrainingBooked];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'Location')
        ALTER TABLE [Employees] DROP COLUMN [Location];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'Trainer')
        ALTER TABLE [Employees] DROP COLUMN [Trainer];

    COMMIT TRANSACTION;
    PRINT 'Successfully removed all Driving License, Checks & References, Employment Documentation, and Training & Induction columns from Employees table.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
