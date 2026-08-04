-- No FK to Users: an invite targets a username that has no user row yet.
CREATE TABLE [dbo].[UserInvites] (
    [Id]         UNIQUEIDENTIFIER NOT NULL,
    [Token]      NVARCHAR(100) NOT NULL,
    [Username]   NVARCHAR(100) COLLATE Latin1_General_100_CI_AS NOT NULL,
    [Role]       NVARCHAR(20)  NOT NULL,
    [CreatedBy]  NVARCHAR(100) NOT NULL,
    [ExpiresUtc] DATETIME2(7)  NOT NULL,
    CONSTRAINT [PK_UserInvites] PRIMARY KEY CLUSTERED ([Token]),
    CONSTRAINT [CK_UserInvites_Role] CHECK ([Role] IN (N'Admin', N'User'))
);

-- One active invite per username.
CREATE UNIQUE INDEX [UX_UserInvites_Username] ON [dbo].[UserInvites]([Username]);
CREATE INDEX [IX_UserInvites_ExpiresUtc] ON [dbo].[UserInvites]([ExpiresUtc]);
