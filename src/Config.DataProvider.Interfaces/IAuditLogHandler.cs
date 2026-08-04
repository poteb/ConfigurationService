namespace pote.Config.DataProvider.Interfaces;

public interface IAuditLogHandler
{
    Task AuditLogConfiguration(string id, string callerIp, string? username, string action, string content);
    Task AuditLogEnvironment(string id, string callerIp, string? username, string action, string content);
    Task AuditLogApplication(string id, string callerIp, string? username, string action, string content);
    Task AuditLogSettings(string id, string callerIp, string? username, string action, string content);
    Task AuditLogApiKeys(string id, string callerIp, string? username, string action, string content);
    Task AuditLogSecrets(string id, string callerIp, string? username, string action, string content);
    Task AuditLogUser(string entityId, string callerIp, string? username, string action, string content);
}
