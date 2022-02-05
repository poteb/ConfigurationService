namespace pote.Config.DataProvider.File;

public class FileHandler : IFileHandler
{
    private readonly string _configurationRootDir;
    private readonly string _environmentsDir;
    private readonly string _systemsDir;

    public FileHandler(string directory)
    {
        _configurationRootDir = Path.Combine(directory, "configurations");
        //_configurationHistoryDir = Path.Combine(_configurationRootDir, "history");
        _environmentsDir = Path.Combine(directory, "environments");
        _systemsDir = Path.Combine(directory, "systems");
        
        if (!Directory.Exists(_configurationRootDir))
            Directory.CreateDirectory(_configurationRootDir);
        //if (!Directory.Exists(_configurationHistoryDir))
        //    Directory.CreateDirectory(_configurationHistoryDir);
        if (!Directory.Exists(_environmentsDir))
            Directory.CreateDirectory(_environmentsDir);
        if (!Directory.Exists(_systemsDir))
            Directory.CreateDirectory(_systemsDir);
    }

    public string[] GetConfigurationFiles()
    {
        return Directory.GetFiles(_configurationRootDir);
    }

    public async Task<string> GetConfigurationContent(string id, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_configurationRootDir, id), ".txt");
        if (!System.IO.File.Exists(file)) throw new FileNotFoundException();
        return await System.IO.File.ReadAllTextAsync(file, cancellationToken);
    }

    public async Task WriteConfigurationContent(string id, string content, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_configurationRootDir, id), ".txt");
        if (System.IO.File.Exists(file))
        {
            var historyDir = Path.Combine(_configurationRootDir, id, "history");
            if (!Directory.Exists(historyDir))
                Directory.CreateDirectory(historyDir);
            var historyFile = Path.ChangeExtension(Path.Combine(historyDir, $"{id}_{Guid.NewGuid().ToString()}"), ".txt");
            System.IO.File.Move(file, historyFile);
        }
        await System.IO.File.WriteAllTextAsync(file, content, cancellationToken);
    }

    public string[] GetEnvironmentFiles()
    {
        return Directory.GetFiles(_environmentsDir);
    }
    public async Task<string> GetEnvironmentContentAbsoluePath(string file, CancellationToken cancellationToken)
    {
        return await System.IO.File.ReadAllTextAsync(file, cancellationToken);
    }
    public async Task<string> GetEnvironmentContent(string id, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_environmentsDir, id), ".txt");
        return await GetEnvironmentContentAbsoluePath(file, cancellationToken);
    }
    public async Task WriteEnvironmentContent(string id, string content, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_environmentsDir, id), ".txt");
        await System.IO.File.WriteAllTextAsync(file, content, cancellationToken);
    }
    public void DeleteEnvironment(string id)
    {
        var file = Path.ChangeExtension(Path.Combine(_environmentsDir, id), ".txt");
        if (!System.IO.File.Exists(file)) return;
        System.IO.File.Delete(file);
    }

    public string[] GetSystemFiles()
    {
        return Directory.GetFiles(_systemsDir);
    }
    public async Task<string> GetSystemContentAbsolutePath(string file, CancellationToken cancellationToken)
    {
        return await System.IO.File.ReadAllTextAsync(file, cancellationToken);
    }
    public async Task<string> GetSystemContent(string id, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_systemsDir, id), ".txt");
        return await GetSystemContentAbsolutePath(file, cancellationToken);
    }
    public async Task WriteSystemContent(string id, string content, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_systemsDir, id), ".txt");
        await System.IO.File.WriteAllTextAsync(file, content, cancellationToken);
    }
    public void DeleteSystem(string id)
    {
        var file = Path.ChangeExtension(Path.Combine(_systemsDir, id), ".txt");
        if (!System.IO.File.Exists(file)) return;
        System.IO.File.Delete(file);
    }
}