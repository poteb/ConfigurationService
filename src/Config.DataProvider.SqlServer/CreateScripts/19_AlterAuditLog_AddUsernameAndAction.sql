-- Idempotent: safe on fresh databases (where 13_AuditLog.sql already created the columns)
-- and on existing databases being upgraded.
IF COL_LENGTH('dbo.AuditLog', 'Username') IS NULL
    ALTER TABLE [dbo].[AuditLog] ADD [Username] NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.AuditLog', 'Action') IS NULL
    ALTER TABLE [dbo].[AuditLog] ADD [Action] NVARCHAR(50) NULL;
GO

-- Legacy rows stored the action verb in Content; backfill Action and keep Content as detail.
UPDATE [dbo].[AuditLog]
SET [Action] = [Content]
WHERE [Action] IS NULL AND LEN([Content]) <= 50;
