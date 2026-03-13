CREATE TABLE [dbo].[Environments] (
    [Id]   NVARCHAR(36)  NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    CONSTRAINT [PK_Environments] PRIMARY KEY CLUSTERED ([Id])
);
