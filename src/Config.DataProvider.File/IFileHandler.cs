namespace pote.Config.DataProvider.File;

public interface IFileHandler
{
    string[] GetConfigurationFiles();
    Task<string> GetConfigurationContent(string id, CancellationToken cancellationToken);
    Task WriteConfigurationContent(string id, string content, CancellationToken cancellationToken);
    string[] GetEnvironmentFiles();
    Task<string> GetEnvironmentContentAbsoluePath(string file, CancellationToken cancellationToken);
    Task<string> GetEnvironmentContent(string id, CancellationToken cancellationToken);
    Task WriteEnvironmentContent(string id, string content, CancellationToken cancellationToken);
    void DeleteEnvironment(string id);
    string[] GetSystemFiles();
    Task<string> GetSystemContentAbsolutePath(string file, CancellationToken cancellationToken);
    Task<string> GetSystemContent(string id, CancellationToken cancellationToken);
    Task WriteSystemContent(string id, string content, CancellationToken cancellationToken);
    void DeleteSystem(string id);
}