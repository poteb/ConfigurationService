CREATE TABLE [dbo].[Users] (
    [Id]           UNIQUEIDENTIFIER NOT NULL,
    [Username]     NVARCHAR(100) COLLATE Latin1_General_100_CI_AS NOT NULL,
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [Role]         NVARCHAR(20)  NOT NULL,
    [IsGuest]      BIT           NOT NULL DEFAULT 0,
    [Deleted]      BIT           NOT NULL DEFAULT 0,
    [CreatedUtc]   DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    [LastLoginUtc] DATETIME2(7)  NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_Users_Role] CHECK ([Role] IN (N'Admin', N'User'))
);

-- Uniqueness spans live and soft-deleted users (case-insensitive via column collation).
CREATE UNIQUE INDEX [UX_Users_Username] ON [dbo].[Users]([Username]);
