CREATE TABLE [dbo].[SecretApplications] (
    [SecretId]      NVARCHAR(36) NOT NULL,
    [ApplicationId] NVARCHAR(36) NOT NULL,
    CONSTRAINT [PK_SecretApplications]
        PRIMARY KEY CLUSTERED ([SecretId], [ApplicationId]),
    CONSTRAINT [FK_SecretApps_Secrets]
        FOREIGN KEY ([SecretId]) REFERENCES [dbo].[Secrets]([Id]),
    CONSTRAINT [FK_SecretApps_Applications]
        FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications]([Id])
);
