namespace pote.Config.DataProvider.Interfaces;

public interface IAuditLogHandler
{
    Task AuditLogConfiguration(string id, string callerIp, string content);
    Task AuditLogEnvironment(string id, string callerIp, string content);
    Task AuditLogApplication(string id, string callerIp, string content);
}