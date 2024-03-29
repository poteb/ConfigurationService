﻿namespace pote.Config.DataProvider.File;

public class FileHandler : IFileHandler
{
    private readonly string _configurationRootDir;
    private readonly string _environmentsDir;
    private readonly string _applicationsDir;
    private readonly string _settingsDir;

    public FileHandler(string directory)
    {
        _configurationRootDir = Path.Combine(directory, "configurations");
        _environmentsDir = Path.Combine(directory, "environments");
        _applicationsDir = Path.Combine(directory, "applications");
        _settingsDir = Path.Combine(directory, "settings");

        if (!Directory.Exists(_configurationRootDir))
            Directory.CreateDirectory(_configurationRootDir);
        if (!Directory.Exists(_environmentsDir))
            Directory.CreateDirectory(_environmentsDir);
        if (!Directory.Exists(_applicationsDir))
            Directory.CreateDirectory(_applicationsDir);
        if (!Directory.Exists(_settingsDir))
            Directory.CreateDirectory(_settingsDir);
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

    public async Task<List<string>> GetHeaderHistory(string id, int page, int pageSize, CancellationToken cancellationToken)
    {
        var historyDir = Path.Combine(_configurationRootDir, id, "history");
        if (!Directory.Exists(historyDir))
            return new List<string>();
        var files = Directory.GetFiles(historyDir).OrderBy(f => f).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var result = new List<string>();
        foreach (var file in files)
            result.Add(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
        return result;
    }

    public void DeleteConfiguration(string id, bool permanent)
    {
        var file = Path.ChangeExtension(Path.Combine(_configurationRootDir, id), ".txt");
        if (!System.IO.File.Exists(file)) throw new FileNotFoundException();
        if (permanent)
        {
            System.IO.File.Delete(file);
            var dir = Path.Combine(_configurationRootDir, id);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        else
        {
            var historyDir = Path.Combine(_configurationRootDir, id, "history");
            if (!Directory.Exists(historyDir))
                Directory.CreateDirectory(historyDir);
            var historyFile = Path.Combine(historyDir, $"{id}_{DateTime.Now:yyyyMMddHHmmss}_deleted.txt");
            System.IO.File.Move(file, historyFile, true);
        }
    }

    public async Task WriteConfigurationContent(string id, string content, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_configurationRootDir, id), ".txt");
        if (System.IO.File.Exists(file))
        {
            var historyDir = Path.Combine(_configurationRootDir, id, "history");
            if (!Directory.Exists(historyDir))
                Directory.CreateDirectory(historyDir);
            var historyFile = Path.ChangeExtension(Path.Combine(historyDir, $"{id}_{DateTime.Now:yyyyMMddHHmmss}"), ".txt");
            System.IO.File.Move(file, historyFile);
        }
        await System.IO.File.WriteAllTextAsync(file, content, cancellationToken);
    }

    public string[] GetEnvironmentFiles()
    {
        return Directory.GetFiles(_environmentsDir);
    }
    public async Task<string> GetEnvironmentContentAbsolutePath(string file, CancellationToken cancellationToken)
    {
        return await System.IO.File.ReadAllTextAsync(file, cancellationToken);
    }
    public async Task<string> GetEnvironmentContent(string id, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_environmentsDir, id), ".txt");
        return await GetEnvironmentContentAbsolutePath(file, cancellationToken);
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

    public string[] GetApplicationFiles()
    {
        return Directory.GetFiles(_applicationsDir);
    }
    public async Task<string> GetApplicationContentAbsolutePath(string file, CancellationToken cancellationToken)
    {
        return await System.IO.File.ReadAllTextAsync(file, cancellationToken);
    }
    public async Task<string> GetApplicationContent(string id, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_applicationsDir, id), ".txt");
        return await GetApplicationContentAbsolutePath(file, cancellationToken);
    }
    public async Task WriteApplicationContent(string id, string content, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_applicationsDir, id), ".txt");
        await System.IO.File.WriteAllTextAsync(file, content, cancellationToken);
    }
    public void DeleteApplication(string id)
    {
        var file = Path.ChangeExtension(Path.Combine(_applicationsDir, id), ".txt");
        if (!System.IO.File.Exists(file)) return;
        System.IO.File.Delete(file);
    }

    public async Task AuditLogConfiguration(string id, string content)
    {
        await WriteAuditLog(Path.Combine(_configurationRootDir, id, "AuditLog"), content);
    }
    public async Task AuditLogEnvironment(string id, string content)
    {
        await WriteAuditLog(Path.Combine(_environmentsDir, "AuditLog", id), content);
    }
    public async Task AuditLogApplication(string id, string content)
    {
        await WriteAuditLog(Path.Combine(_applicationsDir, "AuditLog", id), content);
    }
    public async Task AuditLogSettings(string content)
    {
        await WriteAuditLog(Path.Combine(_settingsDir, "AuditLog"), content);
    }

    public async Task AuditLogApiKeys(string content)
    {
        await WriteAuditLog(Path.Combine(_settingsDir, "AuditLogApiKeys"), content);
    }

    public async Task<string> GetSettings(CancellationToken cancellationToken)
    {
        var file = Path.Combine(_settingsDir, "settings.json");
        if (!System.IO.File.Exists(file)) return "{}";
        return await System.IO.File.ReadAllTextAsync(file, cancellationToken);
    }

    public async Task SaveSettings(string settings, CancellationToken cancellationToken)
    {
        var file = Path.Combine(_settingsDir, "settings.json");
        await System.IO.File.WriteAllTextAsync(file, settings, cancellationToken);
    }

    public async Task<string> GetApiKeys(CancellationToken cancellationToken)
    {
        var file = Path.Combine(_settingsDir, "apikeys.json");
        if (!System.IO.File.Exists(file)) return string.Empty;
        return await System.IO.File.ReadAllTextAsync(file, cancellationToken);
    }
    
    public async Task SaveApiKeys(string apiKeys, CancellationToken cancellationToken)
    {
        var file = Path.Combine(_settingsDir, "apikeys.json");
        await System.IO.File.WriteAllTextAsync(file, apiKeys, cancellationToken);
    }

    private async Task WriteAuditLog(string dir, string content)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var auditLogFile = Path.ChangeExtension(Path.Combine(dir, $"{DateTime.UtcNow:yyyy-MM-dd HH.mm.ss.fff}"), ".txt");
        await System.IO.File.WriteAllTextAsync(auditLogFile, content);
    }
}