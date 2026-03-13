CREATE TABLE [dbo].[ConfigurationApplications] (
    [ConfigurationId] NVARCHAR(36) NOT NULL,
    [ApplicationId]   NVARCHAR(36) NOT NULL,
    CONSTRAINT [PK_ConfigurationApplications]
        PRIMARY KEY CLUSTERED ([ConfigurationId], [ApplicationId]),
    CONSTRAINT [FK_ConfigApps_Configurations]
        FOREIGN KEY ([ConfigurationId]) REFERENCES [dbo].[Configurations]([Id]),
    CONSTRAINT [FK_ConfigApps_Applications]
        FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications]([Id])
);
