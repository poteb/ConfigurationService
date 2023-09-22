using Newtonsoft.Json;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;
using pote.Config.Encryption;
using pote.Config.Shared;
using Environment=pote.Config.DbModel.Environment;

namespace pote.Config.DataProvider.File;

public class AdminDataProvider : IAdminDataProvider
{
    private readonly IFileHandler _fileHandler;
    private readonly IApplicationDataAccess _applicationDataAccess;
    private readonly IEnvironmentDataAccess _environmentDataAccess;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly DataProvider _dataProvider;

    public AdminDataProvider(IFileHandler fileHandler, IApplicationDataAccess applicationDataAccess, IEnvironmentDataAccess environmentDataAccess, EncryptionSettings encryptionSettings)
    {
        _fileHandler = fileHandler;
        _applicationDataAccess = applicationDataAccess;
        _environmentDataAccess = environmentDataAccess;
        _encryptionSettings = encryptionSettings;

        _dataProvider = new DataProvider(fileHandler, environmentDataAccess, applicationDataAccess);
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
            catch (Exception)
            {
                /* ignore */
            }
        }

        return result;
    }

    public async Task<ConfigurationHeader> GetConfiguration(string id, CancellationToken cancellationToken, bool includeHistory = true)
    {
        var header = JsonConvert.DeserializeObject<ConfigurationHeader>(await _fileHandler.GetConfigurationContent(id, cancellationToken));
        if (header == null) throw new KeyNotFoundException($"Could not read json from file {id}");
        EncryptionHandler.Decrypt(header.Configurations, _encryptionSettings.JsonEncryptionKey);
        return header;
    }

    public async Task<Configuration> GetConfiguration(string name, string applicationId, string environment, CancellationToken cancellationToken)
    {
        var configuration = await _dataProvider.GetConfiguration(name, applicationId, environment, cancellationToken);
        EncryptionHandler.Decrypt(configuration, _encryptionSettings.JsonEncryptionKey);
        return configuration;
    }

    public async Task<List<ConfigurationHeader>> GetHeaderHistory(string id, int page, int pageSize, CancellationToken cancellationToken)
    {
        var historyJson = await _fileHandler.GetHeaderHistory(id, page, pageSize, cancellationToken);
        var result = new List<ConfigurationHeader>();
        foreach (var json in historyJson)
        {
            var header = JsonConvert.DeserializeObject<ConfigurationHeader>(json) 
                                ?? new ConfigurationHeader { Id = Guid.Empty.ToString(), Name = "Unable to read json"};
            EncryptionHandler.Decrypt(header.Configurations, _encryptionSettings.JsonEncryptionKey);
            result.Add(header);
        }
        return result;
    }
    
    public async Task<List<Configuration>> GetConfigurationHistory(string headerId, string id, int page, int pageSize, CancellationToken cancellationToken)
    {
        var historyJson = await _fileHandler.GetHeaderHistory(headerId, page, pageSize, cancellationToken);
        var result = new List<Configuration>();
        foreach (var json in historyJson)
        {
            var header = JsonConvert.DeserializeObject<ConfigurationHeader>(json);
            var configuration = header?.Configurations.FirstOrDefault(c => c.Id == id);
            if (configuration == null) continue;
            EncryptionHandler.Decrypt(configuration, _encryptionSettings.JsonEncryptionKey);
            result.Add(configuration);
        }
        return result.OrderByDescending(c => c.CreatedUtc).ToList();
    }

    public void DeleteConfiguration(string id, bool permanent)
    {
        _fileHandler.DeleteConfiguration(id, permanent);
    }

    public async Task Insert(ConfigurationHeader header, CancellationToken cancellationToken)
    {
        header.Configurations.ForEach(c =>
        {
            c.CreatedUtc = header.CreatedUtc;
            c.IsJsonEncrypted = c.IsJsonEncrypted || header.IsJsonEncrypted;
            EncryptionHandler.Encrypt(c, _encryptionSettings.JsonEncryptionKey);
        });
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