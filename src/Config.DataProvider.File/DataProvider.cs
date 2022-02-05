using Newtonsoft.Json;
using pote.Config.DbModel;
using pote.Config.Shared;

namespace pote.Config.DataProvider.File;

public class DataProvider : IDataProvider
{
    private readonly IFileHandler _fileHandler;

    public DataProvider(IFileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }

    public async Task<string> GetConfigurationJson(string name, string systemId, string environmentId, CancellationToken cancellationToken)
    {
        foreach (var file in _fileHandler.GetConfigurationFiles())
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
}