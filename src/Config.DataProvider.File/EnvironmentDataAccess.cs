using Newtonsoft.Json;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.DataProvider.File;

public class EnvironmentDataAccess : IEnvironmentDataAccess
{
    private readonly IFileHandler _fileHandler;

    public EnvironmentDataAccess(IFileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }
    
    public async Task<List<DbModel.Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        var files = _fileHandler.GetEnvironmentFiles();
        var result = new List<DbModel.Environment>();
        foreach (var file in files)
        {
            try
            {
                var env = JsonConvert.DeserializeObject<DbModel.Environment>(await _fileHandler.GetEnvironmentContentAbsolutePath(file, cancellationToken));
                if (env == null) continue;
                result.Add(env);
            }
            catch (Exception) { /* ignore */ }
        }

        return result;
    }

    public async Task<DbModel.Environment> GetEnvironment(string idOrName, CancellationToken cancellationToken)
    { 
        var files = _fileHandler.GetEnvironmentFiles();
        foreach (var file in files)
        {
            try
            {
                var env = JsonConvert.DeserializeObject<DbModel.Environment>(await _fileHandler.GetEnvironmentContentAbsolutePath(file, cancellationToken));
                if (env == null) continue;
                if (env.Id == idOrName || env.Name.Equals(idOrName, StringComparison.InvariantCultureIgnoreCase))
                    return env;
            }
            catch (Exception) { /* ignore */ }
        }

        throw new KeyNotFoundException($"Environment not found, idOrName: {idOrName}");
    }
}