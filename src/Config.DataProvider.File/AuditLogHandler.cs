using pote.Config.DataProvider.Interfaces;

namespace pote.Config.DataProvider.File;

public class AuditLogHandler : IAuditLogHandler
{
    private readonly IFileHandler _fileHandler;

    public AuditLogHandler(IFileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }

    public async Task AuditLogConfiguration(string id, string callerIp, string? username, string action, string content)
    {
        await _fileHandler.AuditLogConfiguration(id, Format(callerIp, username, action, content));
    }

    public async Task AuditLogEnvironment(string id, string callerIp, string? username, string action, string content)
    {
        await _fileHandler.AuditLogEnvironment(id, Format(callerIp, username, action, content));
    }

    public async Task AuditLogApplication(string id, string callerIp, string? username, string action, string content)
    {
        await _fileHandler.AuditLogApplication(id, Format(callerIp, username, action, content));
    }

    public async Task AuditLogSettings(string id, string callerIp, string? username, string action, string content)
    {
        await _fileHandler.AuditLogSettings(Format(callerIp, username, action, content));
    }

    public async Task AuditLogApiKeys(string id, string callerIp, string? username, string action, string content)
    {
        await _fileHandler.AuditLogApiKeys(Format(callerIp, username, action, content));
    }

    public Task AuditLogSecrets(string id, string callerIp, string? username, string action, string content)
    {
        return _fileHandler.AuditLogSecrets(id, Format(callerIp, username, action, content));
    }

    public Task AuditLogUser(string entityId, string callerIp, string? username, string action, string content)
    {
        // The file provider has no user store (see UserDataAccess), but user audit events
        // are harmless to record; reuse the settings audit directory.
        return _fileHandler.AuditLogSettings($"User {entityId}{Environment.NewLine}{Format(callerIp, username, action, content)}");
    }

    private static string Format(string callerIp, string? username, string action, string content)
    {
        return $"{callerIp} {username ?? "-"} {action}{Environment.NewLine}{content}";
    }
}
