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

    public AdminDataProvider(IFileHandler fileHandler, IApplicationDataAccess applicationDataAccess, IEnvironmentDataAccess environmentDataAccess, ISecretDataAccess secretDataAccess, EncryptionSettings encryptionSettings)
    {
        _fileHandler = fileHandler;
        _applicationDataAccess = applicationDataAccess;
        _environmentDataAccess = environmentDataAccess;
        _encryptionSettings = encryptionSettings;

        _dataProvider = new DataProvider(fileHandler, environmentDataAccess, applicationDataAccess, secretDataAccess, encryptionSettings);
    }

    public async Task<List<ConfigurationHeader>> GetAllConfigurationHeaders(CancellationToken cancellationToken)
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

    public async Task<ApiKeys> GetApiKeys(CancellationToken cancellationToken)
    {
        var apiKeyString = await _fileHandler.GetApiKeys(cancellationToken);
        var apiKeys = JsonConvert.DeserializeObject<ApiKeys>(apiKeyString);
        return apiKeys ?? new ApiKeys();
    }

    public async Task<string> GetSecretValue(string name, string applicationId, string environmentId, CancellationToken cancellationToken)
    {
        return await _dataProvider.GetSecretValue(name, applicationId, applicationId, cancellationToken);
    }

    public async Task SaveApiKeys(ApiKeys apiKeys, CancellationToken cancellationToken)
    {
        await _fileHandler.SaveApiKeys(JsonConvert.SerializeObject(apiKeys), cancellationToken);
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

    public async Task InsertConfiguration(ConfigurationHeader header, CancellationToken cancellationToken)
    {
        var settings = await GetSettings(cancellationToken);
        header.Configurations.ForEach(c =>
        {
            c.CreatedUtc = header.CreatedUtc;
            c.IsJsonEncrypted = c.IsJsonEncrypted || header.IsJsonEncrypted || settings.EncryptAllJson;
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

    public async Task<Application> GetApplication(string idOrName, CancellationToken cancellationToken)
    {
        return await _applicationDataAccess.GetApplication(idOrName, cancellationToken);
    }

    public async Task<List<Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        return await _environmentDataAccess.GetEnvironments(cancellationToken);
    }

    public Task<Environment> GetEnvironment(string idOrName, CancellationToken cancellationToken)
    {
        return _environmentDataAccess.GetEnvironment(idOrName, cancellationToken);
    }


    public async Task<Settings> GetSettings(CancellationToken cancellationToken)
    {
        var json = await _fileHandler.GetSettings(cancellationToken);
        if (string.IsNullOrWhiteSpace(json)) return new Settings();
        return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
    }

    public async Task SaveSettings(Settings settings, CancellationToken cancellationToken)
    {
        await _fileHandler.SaveSettings(JsonConvert.SerializeObject(settings), cancellationToken);
    }

    public async Task InsertSecret(SecretHeader header, CancellationToken cancellationToken)
    {
        header.Secrets.ForEach(c =>
        {
            c.CreatedUtc = header.CreatedUtc;
            EncryptionHandler.Encrypt(c, _encryptionSettings.JsonEncryptionKey);
        });
        await _fileHandler.WriteSecretContent(header.Id, JsonConvert.SerializeObject(header), cancellationToken);
    }

    public Task DeleteSecret(string id, CancellationToken cancellationToken)
    {
        _fileHandler.DeleteSecret(id);
        return Task.CompletedTask;
    }

    public async Task<List<SecretHeader>> GetAllSecretHeaders(CancellationToken cancellationToken)
    {
        var files = _fileHandler.GetSecretFiles();
        var result = new List<SecretHeader>();
        foreach (var file in files)
        {
            try
            {
                var header = await GetSecret(Path.GetFileNameWithoutExtension(file), cancellationToken, false);
                result.Add(header);
            }
            catch (Exception)
            {
                /* ignore */
            }
        }

        return result;
    }

    public async Task<SecretHeader> GetSecret(string id, CancellationToken cancellationToken, bool includeHistory = true)
    {
        var header = JsonConvert.DeserializeObject<SecretHeader>(await _fileHandler.GetSecretContent(id, cancellationToken));
        if (header == null) throw new KeyNotFoundException($"Could not read json from file {id}");
        EncryptionHandler.Decrypt(header.Secrets, _encryptionSettings.JsonEncryptionKey);
        return header;
    }
}