using Newtonsoft.Json;
using pote.Config.DbModel;
using pote.Config.Shared;

namespace pote.Config.DataProvider.File;

public class DataProvider : IDataProvider
{
    private readonly string _configurationRootDir;
    private readonly string _environmentsDir;
    private readonly string _systemsDir;

    public DataProvider(string directory)
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

    public async Task<string> GetConfigurationJson(string gid, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_configurationRootDir, gid), ".txt");
        if (!System.IO.File.Exists(file)) throw new FileNotFoundException();
        var configuration = JsonConvert.DeserializeObject<Configuration>(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
        if (configuration == null) throw new InvalidOperationException($"Could not read json from file {file}");
        return configuration.Json;
    }

    public async Task<string> GetConfigurationJson(string name, string systemId, string environmentId, CancellationToken cancellationToken)
    {
        foreach (var file in Directory.GetFiles(_configurationRootDir))
        {
            var header = JsonConvert.DeserializeObject<ConfigurationHeader>(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
            if (header == null) continue;
            if (!header.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) continue;
            foreach (var configuration in header.Configurations)
            {
                if (!configuration.Systems.Contains(systemId, StringComparison.InvariantCultureIgnoreCase)) continue;
                if (!configuration.Environments.Contains(environmentId, StringComparison.InvariantCultureIgnoreCase)) continue;

                return configuration.Json;
            }
        }

        return string.Empty;
    }

    private async Task<List<DbModel.System>> GetSystems(CancellationToken cancellationToken)
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

    private async Task<List<DbModel.Environment>> GetEnvironments(CancellationToken cancellationToken)
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
}