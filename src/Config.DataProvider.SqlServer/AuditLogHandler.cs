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

    public Task AuditLogConfiguration(string id, string callerIp, string? username, string action, string content) => InsertAuditLog("Configuration", id, callerIp, username, action, content);

    public Task AuditLogEnvironment(string id, string callerIp, string? username, string action, string content) => InsertAuditLog("Environment", id, callerIp, username, action, content);

    public Task AuditLogApplication(string id, string callerIp, string? username, string action, string content) => InsertAuditLog("Application", id, callerIp, username, action, content);

    public Task AuditLogSettings(string id, string callerIp, string? username, string action, string content) => InsertAuditLog("Settings", id, callerIp, username, action, content);

    public Task AuditLogApiKeys(string id, string callerIp, string? username, string action, string content) => InsertAuditLog("ApiKeys", id, callerIp, username, action, content);

    public Task AuditLogSecrets(string id, string callerIp, string? username, string action, string content) => InsertAuditLog("Secrets", id, callerIp, username, action, content);

    public Task AuditLogUser(string entityId, string callerIp, string? username, string action, string content) => InsertAuditLog("User", entityId, callerIp, username, action, content);

    private async Task InsertAuditLog(string entityType, string entityId, string callerIp, string? username, string action, string content)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [AuditLog] ([EntityType], [EntityId], [CallerIp], [Username], [Action], [Content], [CreatedUtc]) VALUES (@entityType, @entityId, @callerIp, @username, @action, @content, GETUTCDATE())",
            new { entityType, entityId, callerIp, username, action, content });
    }
}
