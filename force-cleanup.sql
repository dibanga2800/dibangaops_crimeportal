-- Force delete unwanted pages
USE AIP_Security;
GO

PRINT 'Starting cleanup...';

-- Delete RolePageAccess for unwanted pages
DELETE FROM RolePageAccess
WHERE PageAccessId IN (
    SELECT Id FROM PageAccess 
    WHERE PageId IN ('bank-holiday', 'holiday-requests', 'incident-list', 'management-customer-reporting')
    OR Path LIKE '/customer/%'
    OR Path LIKE '/management/%'
    OR Path LIKE '/operations/bank-holiday%'
    OR Path LIKE '/operations/holiday-request%'
    OR Path LIKE '/operations/incident-list%'
);

PRINT 'Deleted RolePageAccess entries';

-- Delete CustomerPageAccess for unwanted pages
DELETE FROM CustomerPageAccess
WHERE PageAccessId IN (
    SELECT Id FROM PageAccess 
    WHERE PageId IN ('bank-holiday', 'holiday-requests', 'incident-list', 'management-customer-reporting')
    OR Path LIKE '/customer/%'
    OR Path LIKE '/management/%'
    OR Path LIKE '/operations/bank-holiday%'
    OR Path LIKE '/operations/holiday-request%'
    OR Path LIKE '/operations/incident-list%'
);

PRINT 'Deleted CustomerPageAccess entries';

-- Delete the PageAccess entries
DELETE FROM PageAccess
WHERE PageId IN ('bank-holiday', 'holiday-requests', 'incident-list', 'management-customer-reporting')
OR Path LIKE '/customer/%'
OR Path LIKE '/management/%'
OR Path LIKE '/operations/bank-holiday%'
OR Path LIKE '/operations/holiday-request%'
OR Path LIKE '/operations/incident-list%';

PRINT 'Deleted PageAccess entries';
PRINT '';

-- Show remaining pages
SELECT 
    Category,
    COUNT(*) AS PageCount
FROM PageAccess
GROUP BY Category
ORDER BY Category;

PRINT '';
PRINT 'Pages remaining:';
SELECT Title, Path, PageId FROM PageAccess ORDER BY Title;
GO
