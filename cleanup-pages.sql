-- Script to remove non-existent pages from PageAccess table
-- Run this in SQL Server Management Studio or Azure Data Studio

-- Pages that have been deleted:
-- ✗ Bank Holiday
-- ✗ Holiday Request
-- ✗ Incident List
-- ✗ Management Customer Reporting
-- ✗ All Customer/* pages (moved to customer detail view)

-- STEP 1: First, let's see what pages currently exist in the database
SELECT 
    Id,
    Title,
    Path,
    Category,
    IsActive,
    CASE 
        WHEN Path IN (
            '/',
            '/dashboard',
            '/action-calendar',
            '/profile',
            '/settings',
            '/administration/user-setup',
            '/administration/employee-registration',
            '/administration/customer-setup',
            '/operations/incident-report',
            '/operations/incident-graph',
            '/operations/crime-intelligence',
            '/operations/alert-rules',
            '/analytics/data-analytics-hub'
        ) THEN 'EXISTS IN APP ✓'
        WHEN Path LIKE '/operations/bank-holiday%' THEN 'DELETED - BANK HOLIDAY ✗'
        WHEN Path LIKE '/operations/holiday-request%' THEN 'DELETED - HOLIDAY REQUEST ✗'
        WHEN Path LIKE '/operations/incident-list%' THEN 'DELETED - INCIDENT LIST ✗'
        WHEN Path LIKE '/customer/%' THEN 'DELETED - CUSTOMER PAGE (now in detail view) ✗'
        WHEN Path LIKE '/management/%' THEN 'DELETED - MANAGEMENT PAGE ✗'
        ELSE 'DOES NOT EXIST - WILL BE DELETED ✗'
    END AS Status
FROM PageAccess
ORDER BY Status DESC, Path;

-- STEP 2: Delete RolePageAccess entries for pages that don't exist anymore
-- (This is the linking table between roles and pages)
DELETE FROM RolePageAccess
WHERE PageAccessId IN (
    SELECT Id 
    FROM PageAccess 
    WHERE Path NOT IN (
        '/',
        '/dashboard',
        '/action-calendar',
        '/profile',
        '/settings',
        '/administration/user-setup',
        '/administration/employee-registration',
        '/administration/customer-setup',
        '/operations/incident-report',
        '/operations/incident-graph',
        '/operations/crime-intelligence',
        '/operations/alert-rules',
        '/analytics/data-analytics-hub'
    )
);

-- STEP 3: Delete CustomerPageAccess entries for pages that don't exist anymore
DELETE FROM CustomerPageAccess
WHERE PageAccessId IN (
    SELECT Id 
    FROM PageAccess 
    WHERE Path NOT IN (
        '/',
        '/dashboard',
        '/action-calendar',
        '/profile',
        '/settings',
        '/administration/user-setup',
        '/administration/employee-registration',
        '/administration/customer-setup',
        '/operations/incident-report',
        '/operations/incident-graph',
        '/operations/crime-intelligence',
        '/operations/alert-rules',
        '/analytics/data-analytics-hub'
    )
);

-- STEP 4: Finally, delete the PageAccess entries themselves
DELETE FROM PageAccess
WHERE Path NOT IN (
    '/',
    '/dashboard',
    '/action-calendar',
    '/profile',
    '/settings',
    '/administration/user-setup',
    '/administration/employee-registration',
    '/administration/customer-setup',
    '/operations/incident-report',
    '/operations/incident-graph',
    '/operations/crime-intelligence',
    '/operations/alert-rules',
    '/analytics/data-analytics-hub'
)
-- Extra safety: explicitly delete known removed pages
OR PageId IN ('bank-holiday', 'holiday-requests', 'incident-list', 'management-customer-reporting')
OR Path LIKE '/customer/%'
OR Path LIKE '/management/%';

-- STEP 5: Verify the cleanup
SELECT 
    Category,
    COUNT(*) AS PageCount
FROM PageAccess
GROUP BY Category
ORDER BY Category;

SELECT 
    Title,
    Path,
    Category
FROM PageAccess 
ORDER BY Category, Path;

PRINT '';
PRINT '✓ Cleanup complete! Only these 13 pages should remain:';
PRINT '  Dashboard: /, /dashboard';
PRINT '  Action Calendar: /action-calendar';
PRINT '  Settings: /profile, /settings';
PRINT '  Administration: user-setup, employee-registration, customer-setup';
PRINT '  Operations: incident-report, incident-graph, crime-intelligence, alert-rules';
PRINT '  Analytics: data-analytics-hub';
