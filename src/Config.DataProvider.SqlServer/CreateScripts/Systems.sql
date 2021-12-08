CREATE TABLE [dbo].[Systems](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_Systems] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))