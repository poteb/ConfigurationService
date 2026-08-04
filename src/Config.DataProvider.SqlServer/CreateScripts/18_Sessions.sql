CREATE TABLE [dbo].[Sessions] (
    [Token]      NVARCHAR(100) COLLATE Latin1_General_100_BIN2 NOT NULL,
    [UserId]     UNIQUEIDENTIFIER NOT NULL,
    [CreatedUtc] DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    [ExpiresUtc] DATETIME2(7)  NOT NULL,
    CONSTRAINT [PK_Sessions] PRIMARY KEY CLUSTERED ([Token]),
    CONSTRAINT [FK_Sessions_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Sessions_UserId] ON [dbo].[Sessions]([UserId]);
CREATE INDEX [IX_Sessions_ExpiresUtc] ON [dbo].[Sessions]([ExpiresUtc]);
