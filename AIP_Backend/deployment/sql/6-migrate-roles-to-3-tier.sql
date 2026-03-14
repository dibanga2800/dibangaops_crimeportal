-- =====================================================
-- DibangOps Crime Portal™ Role Migration: 5-role → 3-role model
-- Roles: administrator (kept), manager (new), store (new)
-- Run this ONCE against the production database.
-- Safe to re-run (idempotent via IF EXISTS checks).
-- =====================================================

BEGIN TRANSACTION;

-- Step 1: Insert new roles if they don't already exist
-- 'administrator' already exists in the database — no action needed for it

IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'MANAGER')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp, CreatedAt, IsActive, IsDeleted)
    VALUES (NEWID(), 'manager', 'MANAGER', NEWID(), SYSUTCDATETIME(), 1, 0);
    PRINT 'Created role: manager';
END

IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'STORE')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp, CreatedAt, IsActive, IsDeleted)
    VALUES (NEWID(), 'store', 'STORE', NEWID(), SYSUTCDATETIME(), 1, 0);
    PRINT 'Created role: store';
END

-- Step 2: Migrate user-role assignments from old roles to new roles

-- advantageonehoofficer → manager
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT ur.UserId, r_new.Id
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r_old ON ur.RoleId = r_old.Id AND r_old.NormalizedName = 'ADVANTAGEONEHOOFFICER'
INNER JOIN AspNetRoles r_new ON r_new.NormalizedName = 'MANAGER'
WHERE NOT EXISTS (
    SELECT 1 FROM AspNetUserRoles x WHERE x.UserId = ur.UserId AND x.RoleId = r_new.Id
);
PRINT 'Migrated advantageonehoofficer → manager';

-- customerhomanager → manager
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT ur.UserId, r_new.Id
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r_old ON ur.RoleId = r_old.Id AND r_old.NormalizedName = 'CUSTOMERHOMANAGER'
INNER JOIN AspNetRoles r_new ON r_new.NormalizedName = 'MANAGER'
WHERE NOT EXISTS (
    SELECT 1 FROM AspNetUserRoles x WHERE x.UserId = ur.UserId AND x.RoleId = r_new.Id
);
PRINT 'Migrated customerhomanager → manager';

-- advantageoneofficer → store
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT ur.UserId, r_new.Id
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r_old ON ur.RoleId = r_old.Id AND r_old.NormalizedName = 'ADVANTAGEONEOFFICER'
INNER JOIN AspNetRoles r_new ON r_new.NormalizedName = 'STORE'
WHERE NOT EXISTS (
    SELECT 1 FROM AspNetUserRoles x WHERE x.UserId = ur.UserId AND x.RoleId = r_new.Id
);
PRINT 'Migrated advantageoneofficer → store';

-- customersitemanager → store
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT ur.UserId, r_new.Id
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r_old ON ur.RoleId = r_old.Id AND r_old.NormalizedName = 'CUSTOMERSITEMANAGER'
INNER JOIN AspNetRoles r_new ON r_new.NormalizedName = 'STORE'
WHERE NOT EXISTS (
    SELECT 1 FROM AspNetUserRoles x WHERE x.UserId = ur.UserId AND x.RoleId = r_new.Id
);
PRINT 'Migrated customersitemanager → store';

-- Step 3: Update ApplicationUser.Role column for migrated users
-- NOTE: 'administrator' stays as-is — no change needed
UPDATE AspNetUsers SET Role = 'manager'  WHERE LOWER(Role) IN ('advantageonehoofficer', 'customerhomanager');
UPDATE AspNetUsers SET Role = 'store'    WHERE LOWER(Role) IN ('advantageoneofficer', 'customersitemanager');

-- Also update PageAccessRole column
UPDATE AspNetUsers SET PageAccessRole = 'manager'  WHERE LOWER(PageAccessRole) IN ('advantageonehoofficer', 'customerhomanager');
UPDATE AspNetUsers SET PageAccessRole = 'store'    WHERE LOWER(PageAccessRole) IN ('advantageoneofficer', 'customersitemanager');

PRINT 'Updated ApplicationUser Role and PageAccessRole columns';

-- Step 4: Remove old role assignments (now superseded)
DELETE ur FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.NormalizedName IN (
    'ADVANTAGEONEOFFICER',
    'ADVANTAGEONEHOOFFICER',
    'CUSTOMERSITEMANAGER',
    'CUSTOMERHOMANAGER'
);
PRINT 'Removed old role assignments';

-- Step 5: Delete old roles (keep administrator)
DELETE FROM AspNetRoles WHERE NormalizedName IN (
    'ADVANTAGEONEOFFICER',
    'ADVANTAGEONEHOOFFICER',
    'CUSTOMERSITEMANAGER',
    'CUSTOMERHOMANAGER'
);
PRINT 'Deleted old role definitions';

-- Step 6: Update RolePageAccesses role references (avoid duplicate key on IX_RolePageAccesses_RoleName_PageAccessId)
-- Multiple old roles map to one new role for the same page — update only when target (new_role, PageAccessId) does not exist; delete rest.
-- manager: advantageonehoofficer, customerhomanager (process one at a time so first update is visible to second)
UPDATE rpa SET rpa.RoleName = 'manager'
FROM RolePageAccesses rpa
WHERE LOWER(rpa.RoleName) = 'advantageonehoofficer'
AND NOT EXISTS (SELECT 1 FROM RolePageAccesses x WHERE x.RoleName = 'manager' AND x.PageAccessId = rpa.PageAccessId);
UPDATE rpa SET rpa.RoleName = 'manager'
FROM RolePageAccesses rpa
WHERE LOWER(rpa.RoleName) = 'customerhomanager'
AND NOT EXISTS (SELECT 1 FROM RolePageAccesses x WHERE x.RoleName = 'manager' AND x.PageAccessId = rpa.PageAccessId);
-- store: advantageoneofficer, customersitemanager
UPDATE rpa SET rpa.RoleName = 'store'
FROM RolePageAccesses rpa
WHERE LOWER(rpa.RoleName) = 'advantageoneofficer'
AND NOT EXISTS (SELECT 1 FROM RolePageAccesses x WHERE x.RoleName = 'store' AND x.PageAccessId = rpa.PageAccessId);
UPDATE rpa SET rpa.RoleName = 'store'
FROM RolePageAccesses rpa
WHERE LOWER(rpa.RoleName) = 'customersitemanager'
AND NOT EXISTS (SELECT 1 FROM RolePageAccesses x WHERE x.RoleName = 'store' AND x.PageAccessId = rpa.PageAccessId);
-- Remove remaining old role rows (duplicates we skipped)
DELETE FROM RolePageAccesses WHERE LOWER(RoleName) IN ('advantageonehoofficer', 'customerhomanager', 'advantageoneofficer', 'customersitemanager');
PRINT 'Updated RolePageAccesses role references';

COMMIT TRANSACTION;

-- Verification
SELECT 'Roles' AS [Table], Name, NormalizedName FROM AspNetRoles ORDER BY Name;
SELECT 'UserRoles' AS [Table], r.Name AS RoleName, COUNT(*) AS UserCount
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
GROUP BY r.Name;
PRINT '✅ Role migration complete — 3 roles: administrator, manager, store';
