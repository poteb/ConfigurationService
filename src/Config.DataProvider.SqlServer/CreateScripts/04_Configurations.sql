CREATE TABLE [dbo].[Configurations] (
    [Id]              NVARCHAR(36)  NOT NULL,
    [HeaderId]        NVARCHAR(36)  NOT NULL,
    [Json]            NVARCHAR(MAX) NOT NULL,
    [CreatedUtc]      DATETIME2(7)  NOT NULL,
    [IsActive]        BIT           NOT NULL DEFAULT 1,
    [Deleted]         BIT           NOT NULL DEFAULT 0,
    [IsJsonEncrypted] BIT           NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Configurations] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Configurations_ConfigurationHeaders]
        FOREIGN KEY ([HeaderId]) REFERENCES [dbo].[ConfigurationHeaders]([Id])
);

CREATE INDEX [IX_Configurations_HeaderId] ON [dbo].[Configurations]([HeaderId]);
