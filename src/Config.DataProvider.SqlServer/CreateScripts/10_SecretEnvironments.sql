CREATE TABLE [dbo].[SecretEnvironments] (
    [SecretId]      NVARCHAR(36) NOT NULL,
    [EnvironmentId] NVARCHAR(36) NOT NULL,
    CONSTRAINT [PK_SecretEnvironments]
        PRIMARY KEY CLUSTERED ([SecretId], [EnvironmentId]),
    CONSTRAINT [FK_SecretEnvs_Secrets]
        FOREIGN KEY ([SecretId]) REFERENCES [dbo].[Secrets]([Id]),
    CONSTRAINT [FK_SecretEnvs_Environments]
        FOREIGN KEY ([EnvironmentId]) REFERENCES [dbo].[Environments]([Id])
);
