using System.Text.Json.Serialization;
using Newtonsoft.Json;
using pote.Config.DbModel;
using pote.Config.Shared;

namespace pote.Config.DataProvider.File;

public class AdminDataProvider : IAdminDataProvider
{
    private readonly string _configurationsDir;
    private readonly string _configurationHistoryDir;
    private readonly string _environmentsDir;
    private readonly string _systemsDir;

    public AdminDataProvider(string directory)
    {
        _configurationsDir = Path.Combine(directory, "configurations");
        _configurationHistoryDir = Path.Combine(_configurationsDir, "history");
        _environmentsDir = Path.Combine(directory, "environments");
        _systemsDir = Path.Combine(directory, "systems");

        if (!Directory.Exists(_configurationsDir))
            Directory.CreateDirectory(_configurationsDir);
        if (!Directory.Exists(_configurationHistoryDir))
            Directory.CreateDirectory(_configurationHistoryDir);
        if (!Directory.Exists(_environmentsDir))
            Directory.CreateDirectory(_environmentsDir);
        if (!Directory.Exists(_systemsDir))
            Directory.CreateDirectory(_systemsDir);
    }

    public async Task<List<Configuration>> GetAll(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(_configurationsDir);
        var result = new List<Configuration>();
        foreach (var file in files)
        {
            try
            {
                var config = JsonConvert.DeserializeObject<Configuration>(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
                if (config == null) continue;
                result.Add(config);
            }
            catch (Exception) { /* ignore */ }
        }

        return result;
    }

    public async Task<(Configuration configuration, List<Configuration> history)> GetConfiguration(string guid, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_configurationsDir, guid), ".txt");
        if (!System.IO.File.Exists(file)) throw new FileNotFoundException();
        var configuration = JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText(file));
        var historyDir = Path.Combine(_configurationHistoryDir, guid);
        if (!Directory.Exists(historyDir))
            return (configuration, new());
        var historyFiles = Directory.GetFiles(historyDir);
        var historyResult = new List<Configuration>();
        foreach (var hf in historyFiles)
        {
            try
            {
                var history = JsonConvert.DeserializeObject<Configuration>(await System.IO.File.ReadAllTextAsync(hf, cancellationToken));
                if (history == null) continue;
                historyResult.Add(history);
            }
            catch (Exception) { /* ignore */ }
        }

        return (configuration, historyResult);
    }

    public async Task<Configuration> Insert(Configuration configuration, CancellationToken cancellationToken)
    {
        var file = Path.ChangeExtension(Path.Combine(_configurationsDir, configuration.Gid), ".txt");
        if (System.IO.File.Exists(file))
        {
            var historyDir = Path.Combine(_configurationHistoryDir, configuration.Gid);
            if (!Directory.Exists(historyDir))
                Directory.CreateDirectory(historyDir);
            var historyFile = Path.ChangeExtension(Path.Combine(historyDir, Guid.NewGuid().ToString()), ".txt");
            System.IO.File.Move(file, historyFile);
        }
        await System.IO.File.WriteAllTextAsync(file, JsonConvert.SerializeObject(configuration), cancellationToken);
        return configuration;
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
}