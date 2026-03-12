-- Add Security Officer role to User_Roles lookup
-- Run this if migrating from 3-role (admin, manager, store) to 4-role model

-- 1. Add Security Officer to User_Roles lookup (if not exists)
INSERT INTO LookupTables (Category, Value, Description, Code, SortOrder, IsActive, CreatedAt)
SELECT 'User_Roles', 'security-officer', 'Security Officer with incident report plus admin-configurable pages', '', 2, 1, GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM LookupTables WHERE Category = 'User_Roles' AND Value = 'security-officer');

-- 2. Update store role sort order
UPDATE LookupTables SET SortOrder = 3, Description = 'Store user with incident reporting access only'
WHERE Category = 'User_Roles' AND Value = 'store';

-- 3. Deactivate old 'officer' role if it exists (replaced by security-officer)
UPDATE LookupTables SET IsActive = 0, UpdatedAt = GETUTCDATE()
WHERE Category = 'User_Roles' AND Value = 'officer';

-- 4. Roles are created by ASP.NET Identity via DataSeedingService.SeedRolesAsync on startup
-- 5. RolePageAccess is added by PageAccessService.EnsureOfficerRoleAccessAsync on startup
