CREATE TABLE [dbo].[Configurations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Gid] [varchar](36) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Json] [nvarchar](max) NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[Integrations] [nvarchar](1000) NOT NULL,
	[Deleted] [bit] NOT NULL,
 CONSTRAINT [PK_Configurations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))