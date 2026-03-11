CREATE TABLE [dbo].[Secrets] (
    [Id]        NVARCHAR(36)  NOT NULL,
    [HeaderId]  NVARCHAR(36)  NOT NULL,
    [Value]     NVARCHAR(MAX) NOT NULL,
    [ValueType] NVARCHAR(100) NOT NULL DEFAULT '',
    [CreatedUtc] DATETIME2(7) NOT NULL,
    [IsActive]  BIT           NOT NULL DEFAULT 1,
    [Deleted]   BIT           NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Secrets] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Secrets_SecretHeaders]
        FOREIGN KEY ([HeaderId]) REFERENCES [dbo].[SecretHeaders]([Id])
);

CREATE INDEX [IX_Secrets_HeaderId] ON [dbo].[Secrets]([HeaderId]);
