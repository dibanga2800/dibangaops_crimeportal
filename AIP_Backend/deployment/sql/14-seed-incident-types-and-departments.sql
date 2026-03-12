-- Migration 14: Seed IncidentTypes and ProductDepartments into LookupTables
-- Safe to re-run: only inserts values that don't already exist

-- Resolve a valid user ID to satisfy the FK constraint on CreatedBy
DECLARE @seedUserId NVARCHAR(450)
SELECT TOP 1 @seedUserId = Id FROM AspNetUsers ORDER BY Id

IF @seedUserId IS NULL
BEGIN
    RAISERROR('No users found in AspNetUsers. Seed at least one user before running this migration.', 16, 1)
    RETURN
END

-- ============================================================
-- INCIDENT TYPES
-- ============================================================
DECLARE @incidentTypes TABLE (Value NVARCHAR(100), SortOrder INT)
INSERT INTO @incidentTypes VALUES
    ('Theft',                       0),
    ('Theft Prevention',            1),
    ('Suspicious Activity',         2),
    ('Anti-Social Behaviour',       3),
    ('Arrest',                      4),
    ('Deter',                       5),
    ('Underage Purchase',           6),
    ('Criminal Damage',             7),
    ('Credit Card Fraud',           8),
    ('Colleague Assault',           9),
    ('Colleague Abuse',             10),
    ('Violent Behaviour',           11),
    ('Abusive Behaviour',           12),
    ('Shoplifting',                 13),
    ('Customer Complaint',          14),
    ('Suspicious Behaviour',        15),
    ('Self Scan Tills',             16),
    ('Scan and Go',                 17),
    ('Threats and Intimidation',    18),
    ('Ban from Store',              19),
    ('Others',                      20)

INSERT INTO LookupTables (Category, Value, Description, Code, SortOrder, IsActive, RecordIsDeletedYN, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
SELECT
    'IncidentTypes',
    t.Value,
    'Incident Type: ' + t.Value,
    '',
    t.SortOrder,
    1,
    0,
    @seedUserId,
    GETUTCDATE(),
    @seedUserId,
    GETUTCDATE()
FROM @incidentTypes t
WHERE NOT EXISTS (
    SELECT 1 FROM LookupTables lt
    WHERE lt.Category = 'IncidentTypes' AND lt.Value = t.Value
)

PRINT 'Seeded IncidentTypes lookup values';

-- ============================================================
-- PRODUCT DEPARTMENTS
-- ============================================================
DECLARE @departments TABLE (Value NVARCHAR(100), SortOrder INT)
INSERT INTO @departments VALUES
    ('BABY FOOD',                       0),
    ('BAKERY',                          1),
    ('BEERS/WINES/SPIRITS',             2),
    ('BUTTER & CHEESE',                 3),
    ('CIGARETTES & TOBACCO',            4),
    ('COFFEE',                          5),
    ('CONFECTIONARY',                   6),
    ('FROZEN FOODS',                    7),
    ('FRUIT & VEGETABLES',              8),
    ('GROCERY',                         9),
    ('HEALTH & BEAUTY',                 10),
    ('HOMEWARES',                       11),
    ('LAUNDRY DETERGENT',               12),
    ('MEAT & POULTRY',                  13),
    ('MEDICINE',                        14),
    ('NEWSPAPERS & MAGAZINES',          15),
    ('NON FOOD',                        16),
    ('SEASONAL',                        17),
    ('SOFT BEVERAGES',                  18),
    ('AMS(Toys)',                        19),
    ('PROVISIONS (Butter & Cheese)',    20),
    ('BWS',                             21),
    ('SPORTS & ENERGY DRINKS',          22)

INSERT INTO LookupTables (Category, Value, Description, Code, SortOrder, IsActive, RecordIsDeletedYN, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
SELECT
    'ProductDepartments',
    d.Value,
    'Product Department: ' + d.Value,
    '',
    d.SortOrder,
    1,
    0,
    @seedUserId,
    GETUTCDATE(),
    @seedUserId,
    GETUTCDATE()
FROM @departments d
WHERE NOT EXISTS (
    SELECT 1 FROM LookupTables lt
    WHERE lt.Category = 'ProductDepartments' AND lt.Value = d.Value
)

PRINT 'Seeded ProductDepartments lookup values';
PRINT 'Migration 14 complete.';
