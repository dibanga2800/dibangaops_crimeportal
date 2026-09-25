/*
  DibangOps Crime Portal: production usage and impact metrics.

  Read-only queries for the headline figures quoted in README.md and in
  impact evidence. Run against the production database (Azure Data Studio,
  SSMS or the Azure portal query editor) using a read-only login.

  Set @CustomerName to the organisation to report on. The results are
  aggregate counts only and contain no personal data.
*/

DECLARE @CustomerName NVARCHAR(200) = N'Heart of England';

DECLARE @CustomerId INT = (
    SELECT TOP 1 CustomerId FROM Customers WHERE CompanyName LIKE @CustomerName + N'%'
);

-- 1. Incident volume, active stores and reporting period
SELECT
    COUNT(*)                           AS TotalIncidents,
    COUNT(DISTINCT i.SiteId)           AS StoresThatHaveReported,
    MIN(i.DateOfIncident)              AS FirstIncident,
    MAX(i.DateOfIncident)              AS LatestIncident,
    SUM(ISNULL(i.TotalStolenValue, 0))   AS TotalValueStolen,
    SUM(ISNULL(i.TotalRecoveredValue, 0)) AS TotalValueRecovered
FROM Incidents i
WHERE i.CustomerId = @CustomerId
  AND i.RecordIsDeletedYN = 0;

-- 2. Incidents per month (adoption trend)
SELECT
    FORMAT(i.DateOfIncident, 'yyyy-MM') AS [Month],
    COUNT(*)                            AS Incidents,
    COUNT(DISTINCT i.SiteId)            AS ActiveStores
FROM Incidents i
WHERE i.CustomerId = @CustomerId
  AND i.RecordIsDeletedYN = 0
GROUP BY FORMAT(i.DateOfIncident, 'yyyy-MM')
ORDER BY [Month];

-- 3. AI classification coverage: Azure OpenAI vs rule-based fallback
SELECT
    CASE
        WHEN i.ClassificationVersion IS NULL             THEN 'Not classified'
        WHEN i.ClassificationVersion LIKE 'rule-based%'  THEN 'Rule-based (fallback or default)'
        ELSE 'Azure OpenAI'
    END                                                  AS Classifier,
    COUNT(*)                                             AS Incidents,
    CAST(100.0 * COUNT(*) / SUM(COUNT(*)) OVER () AS DECIMAL(5,1)) AS PercentOfIncidents
FROM Incidents i
WHERE i.CustomerId = @CustomerId
  AND i.RecordIsDeletedYN = 0
GROUP BY
    CASE
        WHEN i.ClassificationVersion IS NULL             THEN 'Not classified'
        WHEN i.ClassificationVersion LIKE 'rule-based%'  THEN 'Rule-based (fallback or default)'
        ELSE 'Azure OpenAI'
    END;

-- 4. Risk level distribution
SELECT ISNULL(i.RiskLevel, 'none') AS RiskLevel, COUNT(*) AS Incidents
FROM Incidents i
WHERE i.CustomerId = @CustomerId
  AND i.RecordIsDeletedYN = 0
GROUP BY i.RiskLevel;

-- 5. Barcode-linked stolen items
SELECT
    COUNT(*)                                          AS StolenItemLines,
    SUM(CASE WHEN s.Barcode IS NOT NULL AND s.Barcode <> '' THEN 1 ELSE 0 END) AS LinesWithBarcode
FROM StolenItems s
JOIN Incidents i ON i.IncidentId = s.IncidentId
WHERE i.CustomerId = @CustomerId
  AND i.RecordIsDeletedYN = 0;

-- 6. Alerts raised automatically
SELECT COUNT(*) AS AlertInstances, MIN(a.CreatedAt) AS FirstAlert, MAX(a.CreatedAt) AS LatestAlert
FROM AlertInstances a;

-- 7. Face recognition usage
SELECT
    COUNT(*)                                                    AS FaceIndexes,
    COUNT(DISTINCT f.OffenderId)                                AS DistinctOffenderIds,
    SUM(CASE WHEN f.BestMatchSimilarity IS NOT NULL THEN 1 ELSE 0 END) AS IndexesWithAMatchScore
FROM FaceEmbeddings f
JOIN Incidents i ON i.IncidentId = f.IncidentId
WHERE i.CustomerId = @CustomerId;

-- 8. User accounts
SELECT
    COUNT(*)                                                     AS ActiveAccounts,
    SUM(CASE WHEN u.LastLoginAt >= DATEADD(DAY, -30, SYSUTCDATETIME()) THEN 1 ELSE 0 END) AS LoggedInLast30Days
FROM AspNetUsers u
WHERE u.CustomerId = @CustomerId
  AND u.IsActive = 1
  AND u.RecordIsDeletedYN = 0;
