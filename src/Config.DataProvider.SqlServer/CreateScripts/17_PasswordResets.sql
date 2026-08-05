CREATE TABLE [dbo].[PasswordResets] (
    [Id]         UNIQUEIDENTIFIER NOT NULL,
    [Token]      NVARCHAR(100) COLLATE Latin1_General_100_BIN2 NOT NULL,
    [UserId]     UNIQUEIDENTIFIER NOT NULL,
    [ExpiresUtc] DATETIME2(7)  NOT NULL,
    CONSTRAINT [PK_PasswordResets] PRIMARY KEY CLUSTERED ([Token]),
    CONSTRAINT [FK_PasswordResets_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
);

-- One active reset per user.
CREATE UNIQUE INDEX [UX_PasswordResets_UserId] ON [dbo].[PasswordResets]([UserId]);
CREATE INDEX [IX_PasswordResets_ExpiresUtc] ON [dbo].[PasswordResets]([ExpiresUtc]);
