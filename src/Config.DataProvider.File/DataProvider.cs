using Newtonsoft.Json;
using pote.Config.DbModel;
using pote.Config.Shared;

namespace pote.Config.DataProvider.File;

public class DataProvider : IDataProvider
{
    private readonly string _configurationsDir;
    private readonly string _environmentsDir;
    private readonly string _systemsDir;

    public DataProvider(string directory)
    {
        _configurationsDir = Path.Combine(directory, "configurations");
        _environmentsDir = Path.Combine(directory, "environments");
        _systemsDir = Path.Combine(directory, "systems");
    }

    public async Task<string> GetConfigurationJson(string gid, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_configurationsDir, gid), ".txt");
        if (!System.IO.File.Exists(file)) throw new FileNotFoundException();
        var configuration = JsonConvert.DeserializeObject<Configuration>(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
        if (configuration == null) throw new InvalidOperationException($"Could not read json from file {file}");
        return configuration.Json;
    }

    public async Task<string> GetConfigurationJson(string name, string system, string environment, CancellationToken cancellationToken)
    {
        return "";
        //var systems
        //foreach (var file in Directory.GetFiles(_configurationsDir))
        //{
        //    var configuration = JsonConvert.DeserializeObject<Configuration>(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
        //    if (!configuration.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) || !configuration.Systems.Any(system => system.Equals(system)))
        //}
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