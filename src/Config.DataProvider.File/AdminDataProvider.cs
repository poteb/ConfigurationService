using Newtonsoft.Json;
using pote.Config.DbModel;
using pote.Config.Shared;

namespace pote.Config.DataProvider.File;

public class AdminDataProvider : IAdminDataProvider
{
    private readonly IFileHandler _fileHandler;

    public AdminDataProvider(IFileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }

    public async Task<List<ConfigurationHeader>> GetAll(CancellationToken cancellationToken)
    {
        var files = _fileHandler.GetConfigurationFiles();
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
        var header = JsonConvert.DeserializeObject<ConfigurationHeader>(await _fileHandler.GetConfigurationContent(id, cancellationToken));
        if (header == null) throw new InvalidOperationException($"Could not read json from file {id}");
        return header;
    }

    public async Task Insert(ConfigurationHeader header, CancellationToken cancellationToken)
    {
        await _fileHandler.WriteConfigurationContent(header.Id, JsonConvert.SerializeObject(header), cancellationToken);
    }

    public async Task<List<DbModel.Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        var files = _fileHandler.GetEnvironmentFiles();
        var result = new List<DbModel.Environment>();
        foreach (var file in files)
        {
            try
            {
                var env = JsonConvert.DeserializeObject<DbModel.Environment>(await _fileHandler.GetEnvironmentContentAbsoluePath(file, cancellationToken));
                if (env == null) continue;
                result.Add(env);
            }
            catch (Exception) { /* ignore */ }
        }

        return result;
    }

    public async Task UpsertEnvironment(DbModel.Environment environment, CancellationToken cancellationToken)
    {
        await _fileHandler.WriteEnvironmentContent(environment.Id, JsonConvert.SerializeObject(environment), cancellationToken);
    }

    public Task DeleteEnvironment(string id, CancellationToken cancellationToken)
    {
        _fileHandler.DeleteEnvironment(id);
        return Task.CompletedTask;
    }

    public async Task<List<DbModel.System>> GetSystems(CancellationToken cancellationToken)
    {
        var files = _fileHandler.GetSystemFiles();
        var result = new List<DbModel.System>();
        foreach (var file in files)
        {
            try
            {
                var system = JsonConvert.DeserializeObject<DbModel.System>(await _fileHandler.GetSystemContentAbsolutePath(file, cancellationToken));
                if (system == null) continue;
                result.Add(system);
            }
            catch (Exception) { /* ignore */ }
        }

        return result;
    }

    public async Task UpsertSystem(DbModel.System system, CancellationToken cancellationToken)
    {
        await _fileHandler.WriteSystemContent(system.Id, JsonConvert.SerializeObject(system), cancellationToken);
    }

    public Task DeleteSystem(string id, CancellationToken cancellationToken)
    {
        _fileHandler.DeleteSystem(id);
        return Task.CompletedTask;
    }
}