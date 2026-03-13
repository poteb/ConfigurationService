using Dapper;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.DataProvider.SqlServer;

public class AuditLogHandler : IAuditLogHandler
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AuditLogHandler(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task AuditLogConfiguration(string id, string callerIp, string content) => InsertAuditLog("Configuration", id, callerIp, content);

    public Task AuditLogEnvironment(string id, string callerIp, string content) => InsertAuditLog("Environment", id, callerIp, content);

    public Task AuditLogApplication(string id, string callerIp, string content) => InsertAuditLog("Application", id, callerIp, content);

    public Task AuditLogSettings(string id, string callerIp, string content) => InsertAuditLog("Settings", id, callerIp, content);

    public Task AuditLogApiKeys(string id, string callerIp, string content) => InsertAuditLog("ApiKeys", id, callerIp, content);

    public Task AuditLogSecrets(string id, string callerIp, string content) => InsertAuditLog("Secrets", id, callerIp, content);

    private async Task InsertAuditLog(string entityType, string entityId, string callerIp, string content)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [AuditLog] ([EntityType], [EntityId], [CallerIp], [Content], [CreatedUtc]) VALUES (@entityType, @entityId, @callerIp, @content, GETUTCDATE())",
            new { entityType, entityId, callerIp, content });
    }
}
