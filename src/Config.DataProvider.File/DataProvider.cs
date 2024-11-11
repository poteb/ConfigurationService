using Newtonsoft.Json;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;
using pote.Config.Encryption;
using pote.Config.Shared;
using Environment = pote.Config.DbModel.Environment;

namespace pote.Config.DataProvider.File;

public class DataProvider : IDataProvider
{
    private readonly IFileHandler _fileHandler;
    private readonly IEnvironmentDataAccess _environmentDataAccess;
    private readonly IApplicationDataAccess _applicationDataAccess;
    private readonly ISecretDataAccess _secretDataAccess;
    private readonly EncryptionSettings _encryptionSettings;

    public DataProvider(IFileHandler fileHandler, IEnvironmentDataAccess environmentDataAccess, IApplicationDataAccess applicationDataAccess, ISecretDataAccess secretDataAccess, EncryptionSettings encryptionSettings)
    {
        _fileHandler = fileHandler;
        _environmentDataAccess = environmentDataAccess;
        _applicationDataAccess = applicationDataAccess;
        _secretDataAccess = secretDataAccess;
        _encryptionSettings = encryptionSettings;
    }

    public async Task<string> GetConfigurationJson(string name, string applicationId, string environmentId, CancellationToken cancellationToken)
    {
        foreach (var file in _fileHandler.GetConfigurationFiles())
        {
            var header = JsonConvert.DeserializeObject<ConfigurationHeader>(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
            if (header == null) continue;
            if (!header.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) continue;
            foreach (var configuration in header.Configurations)
            {
                if (configuration.Applications.All(a => !string.Equals(a, applicationId, StringComparison.InvariantCultureIgnoreCase))) continue;
                if (configuration.Environments.All(e => !string.Equals(e, environmentId, StringComparison.InvariantCultureIgnoreCase))) continue;

                return configuration.Json;
            }
        }

        return string.Empty;
    }

    public async Task<Configuration> GetConfiguration(string name, string applicationId, string environmentId, CancellationToken cancellationToken)
    {
        foreach (var file in _fileHandler.GetConfigurationFiles())
        {
            var header = JsonConvert.DeserializeObject<ConfigurationHeader>(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
            if (header == null) continue;
            if (!header.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) continue;
            foreach (var configuration in header.Configurations)
            {
                if (configuration.Applications.All(a => !string.Equals(a, applicationId, StringComparison.InvariantCultureIgnoreCase))) continue;
                if (configuration.Environments.All(e => !string.Equals(e, environmentId, StringComparison.InvariantCultureIgnoreCase))) continue;

                return configuration;
            }
        }

        return new Configuration { Id = string.Empty };
    }

    public async Task<ApiKeys> GetApiKeys(CancellationToken cancellationToken)
    {
        var apiKeyString = await _fileHandler.GetApiKeys(cancellationToken);
        var apiKeys = JsonConvert.DeserializeObject<ApiKeys>(apiKeyString);
        return apiKeys ?? new ApiKeys();
    }

    public async Task<string> GetSecretValue(string name, string applicationId, string environmentId, CancellationToken cancellationToken)
    {
        var secret = await _secretDataAccess.GetSecret(name, applicationId, environmentId, cancellationToken);
        if (string.IsNullOrWhiteSpace(secret.Value)) return string.Empty;
        secret.Value = EncryptionHandler.Decrypt(secret.Value, _encryptionSettings.JsonEncryptionKey);
        return secret.Id == string.Empty ? string.Empty : secret.Value;
    }
    
    public async Task<List<Application>> GetApplications(CancellationToken cancellationToken)
    {
        return await _applicationDataAccess.GetApplications(cancellationToken);
    }
    
    public async Task<Application> GetApplication(string idOrName, CancellationToken cancellationToken)
    {
        return await _applicationDataAccess.GetApplication(idOrName, cancellationToken);
    }

    public async Task<List<DbModel.Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        return await _environmentDataAccess.GetEnvironments(cancellationToken);
    }

    public Task<Environment> GetEnvironment(string idOrName, CancellationToken cancellationToken)
    {
        return _environmentDataAccess.GetEnvironment(idOrName, cancellationToken);
    }
}