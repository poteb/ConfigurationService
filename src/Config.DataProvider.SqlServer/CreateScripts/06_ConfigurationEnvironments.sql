CREATE TABLE [dbo].[ConfigurationEnvironments] (
    [ConfigurationId] NVARCHAR(36) NOT NULL,
    [EnvironmentId]   NVARCHAR(36) NOT NULL,
    CONSTRAINT [PK_ConfigurationEnvironments]
        PRIMARY KEY CLUSTERED ([ConfigurationId], [EnvironmentId]),
    CONSTRAINT [FK_ConfigEnvs_Configurations]
        FOREIGN KEY ([ConfigurationId]) REFERENCES [dbo].[Configurations]([Id]),
    CONSTRAINT [FK_ConfigEnvs_Environments]
        FOREIGN KEY ([EnvironmentId]) REFERENCES [dbo].[Environments]([Id])
);
