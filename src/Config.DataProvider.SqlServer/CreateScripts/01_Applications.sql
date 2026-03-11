CREATE TABLE [dbo].[Applications] (
    [Id]   NVARCHAR(36)  NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    CONSTRAINT [PK_Applications] PRIMARY KEY CLUSTERED ([Id])
);
