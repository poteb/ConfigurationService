using pote.Config.DbModel;

namespace pote.Config.DataProvider.File;

public interface IFileHandler
{
    string[] GetConfigurationFiles();
    Task<string> GetConfigurationContent(string id, CancellationToken cancellationToken);
    Task<List<string>> GetHeaderHistory(string id, int page, int pageSize, CancellationToken cancellationToken);
    Task WriteConfigurationContent(string id, string content, CancellationToken cancellationToken);
    string[] GetEnvironmentFiles();
    Task<string> GetEnvironmentContentAbsolutePath(string file, CancellationToken cancellationToken);
    Task<string> GetEnvironmentContent(string id, CancellationToken cancellationToken);
    void DeleteConfiguration(string id, bool permanent);
    Task WriteEnvironmentContent(string id, string content, CancellationToken cancellationToken);
    void DeleteEnvironment(string id);
    string[] GetApplicationFiles();
    Task<string> GetApplicationContentAbsolutePath(string file, CancellationToken cancellationToken);
    Task<string> GetApplicationContent(string id, CancellationToken cancellationToken);
    Task WriteApplicationContent(string id, string content, CancellationToken cancellationToken);
    void DeleteApplication(string id);

    Task AuditLogConfiguration(string id, string content);
    Task AuditLogEnvironment(string id, string content);
    Task AuditLogApplication(string id, string content);
    Task AuditLogSettings(string content);
    Task AuditLogApiKeys(string content);
    Task AuditLogSecrets(string id, string content);
    
    Task<string> GetSettings(CancellationToken cancellationToken);
    Task SaveSettings(string settings, CancellationToken cancellationToken);
    Task<string> GetApiKeys(CancellationToken cancellationToken);
    Task SaveApiKeys(string apiKeys, CancellationToken cancellationToken);
    
    string[] GetSecretFiles();
    Task<string> GetSecretContentAbsolutePath(string file, CancellationToken cancellationToken);
    Task WriteSecretContent(string secretId, string serializeObject, CancellationToken cancellationToken);
    void DeleteSecret(string id);
    Task<string> GetSecretContent(string id, CancellationToken cancellationToken);
}