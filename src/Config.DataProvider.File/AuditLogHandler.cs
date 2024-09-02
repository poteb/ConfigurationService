using pote.Config.DataProvider.Interfaces;

namespace pote.Config.DataProvider.File;

public class AuditLogHandler : IAuditLogHandler
{
    private readonly IFileHandler _fileHandler;

    public AuditLogHandler(IFileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }

    public async Task AuditLogConfiguration(string id, string callerIp, string content)
    {
        await _fileHandler.AuditLogConfiguration(id, $"{callerIp}{Environment.NewLine}{content}");
    }

    public async Task AuditLogEnvironment(string id, string callerIp, string content)
    {
        await _fileHandler.AuditLogEnvironment(id, $"{callerIp}{Environment.NewLine}{content}");
    }

    public async Task AuditLogApplication(string id, string callerIp, string content)
    {
        await _fileHandler.AuditLogApplication(id, $"{callerIp}{Environment.NewLine}{content}");
    }
    
    public async Task AuditLogSettings(string id, string callerIp, string content)
    {
        await _fileHandler.AuditLogSettings($"{callerIp}{Environment.NewLine}{content}");
    }

    public async Task AuditLogApiKeys(string id, string callerIp, string content)
    {
        await _fileHandler.AuditLogApiKeys($"{callerIp}{Environment.NewLine}{content}");
    }

    public Task AuditLogSecrets(string id, string callerIp, string content)
    {
        return _fileHandler.AuditLogSecrets(id, $"{callerIp}{Environment.NewLine}{content}");
    }
}