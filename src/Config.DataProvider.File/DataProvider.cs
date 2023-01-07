using Newtonsoft.Json;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;
using pote.Config.Shared;

namespace pote.Config.DataProvider.File;

public class DataProvider : IDataProvider
{
    private readonly IFileHandler _fileHandler;
    private readonly IEnvironmentDataAccess _environmentDataAccess;
    private readonly IApplicationDataAccess _applicationDataAccess;

    public DataProvider(IFileHandler fileHandler, IEnvironmentDataAccess environmentDataAccess, IApplicationDataAccess applicationDataAccess)
    {
        _fileHandler = fileHandler;
        _environmentDataAccess = environmentDataAccess;
        _applicationDataAccess = applicationDataAccess;
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

    public async Task<List<Application>> GetApplications(CancellationToken cancellationToken)
    {
        return await _applicationDataAccess.GetApplications(cancellationToken);
    }

    public async Task<List<DbModel.Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        return await _environmentDataAccess.GetEnvironments(cancellationToken);
    }
}