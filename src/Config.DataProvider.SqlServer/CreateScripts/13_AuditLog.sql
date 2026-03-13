CREATE TABLE [dbo].[AuditLog] (
    [Id]         BIGINT IDENTITY(1,1) NOT NULL,
    [EntityType] NVARCHAR(50)  NOT NULL,
    [EntityId]   NVARCHAR(36)  NOT NULL,
    [CallerIp]   NVARCHAR(100) NOT NULL,
    [Content]    NVARCHAR(MAX) NOT NULL,
    [CreatedUtc] DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id])
);

CREATE INDEX [IX_AuditLog_EntityType_EntityId] ON [dbo].[AuditLog]([EntityType], [EntityId]);
