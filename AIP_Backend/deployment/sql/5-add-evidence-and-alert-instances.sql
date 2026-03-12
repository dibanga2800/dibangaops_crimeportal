-- DibangOps: Add Evidence Chain-of-Custody and Alert Escalation tables
-- Run this after existing migrations are applied

-- Evidence Items table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EvidenceItems')
BEGIN
    CREATE TABLE [dbo].[EvidenceItems] (
        [EvidenceItemId]    INT             IDENTITY(1,1) NOT NULL,
        [IncidentId]        INT             NOT NULL,
        [Barcode]           NVARCHAR(100)   NOT NULL,
        [EvidenceType]      NVARCHAR(100)   NOT NULL,
        [Description]       NVARCHAR(500)   NULL,
        [StorageLocation]   NVARCHAR(200)   NULL,
        [Status]            NVARCHAR(50)    NOT NULL DEFAULT 'registered',
        [RegisteredAt]      DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        [RegisteredBy]      NVARCHAR(450)   NULL,
        CONSTRAINT [PK_EvidenceItems] PRIMARY KEY CLUSTERED ([EvidenceItemId]),
        CONSTRAINT [FK_EvidenceItems_Incidents] FOREIGN KEY ([IncidentId])
            REFERENCES [dbo].[Incidents]([IncidentId]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_EvidenceItems_Barcode] ON [dbo].[EvidenceItems]([Barcode]);
    CREATE INDEX [IX_EvidenceItems_IncidentId] ON [dbo].[EvidenceItems]([IncidentId]);

    PRINT 'Created EvidenceItems table';
END
GO

-- Evidence Custody Events table (append-only audit trail)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EvidenceCustodyEvents')
BEGIN
    CREATE TABLE [dbo].[EvidenceCustodyEvents] (
        [CustodyEventId]    INT             IDENTITY(1,1) NOT NULL,
        [EvidenceItemId]    INT             NOT NULL,
        [EventType]         NVARCHAR(50)    NOT NULL,
        [Notes]             NVARCHAR(500)   NULL,
        [Location]          NVARCHAR(200)   NULL,
        [EventTimestamp]    DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        [PerformedBy]       NVARCHAR(450)   NOT NULL,
        [PerformedByName]   NVARCHAR(200)   NULL,
        CONSTRAINT [PK_EvidenceCustodyEvents] PRIMARY KEY CLUSTERED ([CustodyEventId]),
        CONSTRAINT [FK_EvidenceCustodyEvents_EvidenceItems] FOREIGN KEY ([EvidenceItemId])
            REFERENCES [dbo].[EvidenceItems]([EvidenceItemId]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_EvidenceCustodyEvents_EvidenceItemId] ON [dbo].[EvidenceCustodyEvents]([EvidenceItemId]);

    PRINT 'Created EvidenceCustodyEvents table';
END
GO

-- Alert Instances table (alert lifecycle tracking)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AlertInstances')
BEGIN
    CREATE TABLE [dbo].[AlertInstances] (
        [AlertInstanceId]   INT             IDENTITY(1,1) NOT NULL,
        [AlertRuleId]       INT             NOT NULL,
        [IncidentId]        INT             NULL,
        [Severity]          NVARCHAR(50)    NOT NULL DEFAULT 'medium',
        [Status]            NVARCHAR(50)    NOT NULL DEFAULT 'new',
        [Message]           NVARCHAR(2000)  NULL,
        [MatchDetails]      NVARCHAR(500)   NULL,
        [CreatedAt]         DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        [AcknowledgedAt]    DATETIME2       NULL,
        [AcknowledgedBy]    NVARCHAR(450)   NULL,
        [EscalatedAt]       DATETIME2       NULL,
        [EscalatedTo]       NVARCHAR(450)   NULL,
        [ResolvedAt]        DATETIME2       NULL,
        [ResolvedBy]        NVARCHAR(450)   NULL,
        [ResolutionNotes]   NVARCHAR(1000)  NULL,
        [EscalationLevel]   INT             NOT NULL DEFAULT 0,
        CONSTRAINT [PK_AlertInstances] PRIMARY KEY CLUSTERED ([AlertInstanceId]),
        CONSTRAINT [FK_AlertInstances_AlertRules] FOREIGN KEY ([AlertRuleId])
            REFERENCES [dbo].[AlertRules]([AlertRuleId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AlertInstances_Incidents] FOREIGN KEY ([IncidentId])
            REFERENCES [dbo].[Incidents]([IncidentId]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_AlertInstances_Status] ON [dbo].[AlertInstances]([Status]);
    CREATE INDEX [IX_AlertInstances_AlertRuleId] ON [dbo].[AlertInstances]([AlertRuleId]);
    CREATE INDEX [IX_AlertInstances_CreatedAt] ON [dbo].[AlertInstances]([CreatedAt]);

    PRINT 'Created AlertInstances table';
END
GO

PRINT 'DibangOps evidence and alert instance tables created successfully.';
GO
