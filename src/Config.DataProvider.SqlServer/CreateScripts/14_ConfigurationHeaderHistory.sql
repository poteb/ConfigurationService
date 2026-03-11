IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConfigurationHeaderHistory')
CREATE TABLE [ConfigurationHeaderHistory] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [HeaderId] NVARCHAR(36) NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [CreatedUtc] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX [IX_ConfigurationHeaderHistory_HeaderId] NONCLUSTERED ([HeaderId]),
    CONSTRAINT [FK_ConfigurationHeaderHistory_ConfigurationHeaders] FOREIGN KEY ([HeaderId]) REFERENCES [ConfigurationHeaders]([Id])
);
