CREATE TABLE [dbo].[ConfigurationHeaders] (
    [Id]              NVARCHAR(36)  NOT NULL,
    [Name]            NVARCHAR(200) NOT NULL,
    [CreatedUtc]      DATETIME2(7)  NOT NULL,
    [UpdateUtc]       DATETIME2(7)  NOT NULL,
    [Deleted]         BIT           NOT NULL DEFAULT 0,
    [IsActive]        BIT           NOT NULL DEFAULT 0,
    [IsJsonEncrypted] BIT           NOT NULL DEFAULT 0,
    CONSTRAINT [PK_ConfigurationHeaders] PRIMARY KEY CLUSTERED ([Id])
);
