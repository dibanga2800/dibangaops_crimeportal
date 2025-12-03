-- Run this SQL to check your alert rules setup

-- 1. Check if AlertRules table exists
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'AlertRules';

-- 2. View all alert rules
SELECT 
    AlertRuleId,
    Name,
    RuleType,
    Keywords,
    IncidentTypes,
    LpmRegion,
    RegionId,
    TriggerCondition,
    Channels,
    EmailRecipients,
    IsActive,
    IsDeleted,
    CreatedAt,
    TriggerCount,
    LastTriggered
FROM AlertRules
ORDER BY CreatedAt DESC;

-- 3. Check active rules only
SELECT 
    AlertRuleId,
    Name,
    IsActive,
    IsDeleted,
    Keywords,
    IncidentTypes,
    EmailRecipients
FROM AlertRules
WHERE IsActive = 1 AND IsDeleted = 0;

-- 4. Check recent incidents
SELECT TOP 5
    IncidentId,
    IncidentType,
    IncidentDescription,
    DateOfIncident,
    CreatedAt
FROM Incidents
ORDER BY CreatedAt DESC;

-- 5. Check if services are registered (via recent incidents)
-- This will show if the incident was created
SELECT COUNT(*) AS TotalIncidents FROM Incidents WHERE CreatedAt > DATEADD(MINUTE, -10, GETDATE());
