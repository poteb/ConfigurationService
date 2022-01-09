using Newtonsoft.Json;
using pote.Config.DbModel;
using pote.Config.Shared;

namespace pote.Config.DataProvider.File;

public class AdminDataProvider : IAdminDataProvider
{
    private readonly string _configurationRootDir;
    //private readonly string _configurationHistoryDir;
    private readonly string _environmentsDir;
    private readonly string _systemsDir;

    public AdminDataProvider(string directory)
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

    public async Task<List<ConfigurationHeader>> GetAll(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(_configurationRootDir);
        var result = new List<ConfigurationHeader>();
        foreach (var file in files)
        {
            try
            {
                var header = await GetConfiguration(Path.GetFileNameWithoutExtension(file), cancellationToken, false);
                result.Add(header);
            }
            catch (Exception) { /* ignore */ }
        }

        return result;
    }

    public async Task<ConfigurationHeader> GetConfiguration(string id, CancellationToken cancellationToken, bool includeHistory = true)
    {
        var headerFile = Path.ChangeExtension(Path.Combine(_configurationRootDir, id), ".txt");
        if (!System.IO.File.Exists(headerFile)) throw new FileNotFoundException();
        var header = JsonConvert.DeserializeObject<ConfigurationHeader>(await System.IO.File.ReadAllTextAsync(headerFile, cancellationToken));
        if (header == null) throw new InvalidOperationException($"Could not read json from file {headerFile}");

        var configurationDir = Path.Combine(_configurationRootDir, id);
        if (!Directory.Exists(configurationDir))
            return (header);

        foreach (var configFile in Directory.GetFiles(configurationDir))
        {
            var configuration = JsonConvert.DeserializeObject<Configuration>(await System.IO.File.ReadAllTextAsync(configFile, cancellationToken));
            if (configuration == null) throw new InvalidOperationException($"Could not read json from file {configFile}");
            header.Configurations.Add(configuration);
        }

        if (!includeHistory) return header;
        
        foreach (var configuration in header.Configurations)
        {
            var historyDir = Path.Combine(configurationDir, configuration.Id);
            if (!Directory.Exists(historyDir))
                continue;
            foreach (var hf in Directory.GetFiles(historyDir))
            {
                try
                {
                    var history = JsonConvert.DeserializeObject<Configuration>(await System.IO.File.ReadAllTextAsync(hf, cancellationToken));
                    if (history == null) continue;
                    configuration.History.Add(history);
                }
                catch (Exception)
                {
                    /* ignore */
                }
            }
        }

        return header;
    }

    public async Task Insert(Configuration configuration, CancellationToken cancellationToken)
    {
        //var file = Path.ChangeExtension(Path.Combine(_configurationRootDir, configuration.Id), ".txt");
        //if (System.IO.File.Exists(file))
        //{
        //    var historyDir = Path.Combine(_configurationHistoryDir, configuration.Gid);
        //    if (!Directory.Exists(historyDir))
        //        Directory.CreateDirectory(historyDir);
        //    var historyFile = Path.ChangeExtension(Path.Combine(historyDir, Guid.NewGuid().ToString()), ".txt");
        //    System.IO.File.Move(file, historyFile);
        //}
        //await System.IO.File.WriteAllTextAsync(file, JsonConvert.SerializeObject(configuration), cancellationToken);
    }

    public async Task<List<DbModel.Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(_environmentsDir);
        var result = new List<DbModel.Environment>();
        foreach (var file in files)
        {
            try
            {
                var env = JsonConvert.DeserializeObject<DbModel.Environment>(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
                if (env == null) continue;
                result.Add(env);
            }
            catch (Exception) { /* ignore */ }
        }

        return result;
    }

    public async Task UpsertEnvironment(DbModel.Environment environment, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_environmentsDir, environment.Id), ".txt");
        await System.IO.File.WriteAllTextAsync(file, JsonConvert.SerializeObject(environment), cancellationToken);
    }

    public Task DeleteEnvironment(string id, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_environmentsDir, id), ".txt");
        if (!System.IO.File.Exists(file)) return Task.CompletedTask;
        System.IO.File.Delete(file);
        return Task.CompletedTask;
    }

    public async Task<List<DbModel.System>> GetSystems(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(_systemsDir);
        var result = new List<DbModel.System>();
        foreach (var file in files)
        {
            try
            {
                var system = JsonConvert.DeserializeObject<DbModel.System>(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
                if (system == null) continue;
                result.Add(system);
            }
            catch (Exception) { /* ignore */ }
        }

        return result;
    }

    public async Task UpsertSystem(DbModel.System system, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_systemsDir, system.Id), ".txt");
        await System.IO.File.WriteAllTextAsync(file, JsonConvert.SerializeObject(system), cancellationToken);
    }

    public Task DeleteSystem(string id, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_systemsDir, id), ".txt");
        if (!System.IO.File.Exists(file)) return Task.CompletedTask;
        System.IO.File.Delete(file);
        return Task.CompletedTask;
    }
}