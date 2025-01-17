namespace pote.Config.DataProvider.Interfaces;

public interface IAuditLogHandler
{
    Task AuditLogConfiguration(string id, string callerIp, string content);
    Task AuditLogEnvironment(string id, string callerIp, string content);
    Task AuditLogApplication(string id, string callerIp, string content);
    Task AuditLogSettings(string id, string callerIp, string content);
    Task AuditLogApiKeys(string id, string callerIp, string content);
    Task AuditLogSecrets(string id, string callerIp, string content);
}