using Newtonsoft.Json;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;
using Environment = pote.Config.DbModel.Environment;

namespace pote.Config.DataProvider.File;

public class AdminDataProvider : IAdminDataProvider
{
    private readonly IFileHandler _fileHandler;
    private readonly IApplicationDataAccess _applicationDataAccess;
    private readonly IEnvironmentDataAccess _environmentDataAccess;

    public AdminDataProvider(IFileHandler fileHandler, IApplicationDataAccess applicationDataAccess, IEnvironmentDataAccess environmentDataAccess)
    {
        _fileHandler = fileHandler;
        _applicationDataAccess = applicationDataAccess;
        _environmentDataAccess = environmentDataAccess;
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
        if (header == null) throw new KeyNotFoundException($"Could not read json from file {id}");
        return header;
    }

    public void DeleteConfiguration(string id, bool permanent)
    {
        _fileHandler.DeleteConfiguration(id, permanent);
    }
    public async Task Insert(ConfigurationHeader header, CancellationToken cancellationToken)
    {
        await _fileHandler.WriteConfigurationContent(header.Id, JsonConvert.SerializeObject(header), cancellationToken);
    }

    
    public async Task UpsertEnvironment(Environment environment, CancellationToken cancellationToken)
    {
        await _fileHandler.WriteEnvironmentContent(environment.Id, JsonConvert.SerializeObject(environment), cancellationToken);
    }

    public Task DeleteEnvironment(string id, CancellationToken cancellationToken)
    {
        _fileHandler.DeleteEnvironment(id);
        return Task.CompletedTask;
    }

    
    public async Task UpsertApplication(Application application, CancellationToken cancellationToken)
    {
        await _fileHandler.WriteApplicationContent(application.Id, JsonConvert.SerializeObject(application), cancellationToken);
    }

    public Task DeleteApplication(string id, CancellationToken cancellationToken)
    {
        _fileHandler.DeleteApplication(id);
        return Task.CompletedTask;
    }

    public async Task<List<Application>> GetApplications(CancellationToken cancellationToken)
    {
        return await _applicationDataAccess.GetApplications(cancellationToken);
    }

    public async Task<List<Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        return await _environmentDataAccess.GetEnvironments(cancellationToken);
    }
}