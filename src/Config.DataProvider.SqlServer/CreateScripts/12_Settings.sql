CREATE TABLE [dbo].[Settings] (
    [Id]             INT NOT NULL DEFAULT 1,
    [EncryptAllJson] BIT NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Settings] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_Settings_SingleRow] CHECK ([Id] = 1)
);

INSERT INTO [dbo].[Settings] ([Id], [EncryptAllJson]) VALUES (1, 0);
