using Newtonsoft.Json;
using pote.Config.Shared;

namespace pote.Config.DataProvider.File;

public class EnvionmentDataAccess : IEnvironmentDataAccess
{
    private readonly IFileHandler _fileHandler;

    public EnvionmentDataAccess(IFileHandler fileHandler)
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
                var env = JsonConvert.DeserializeObject<DbModel.Environment>(await _fileHandler.GetEnvironmentContentAbsoluePath(file, cancellationToken));
                if (env == null) continue;
                result.Add(env);
            }
            catch (Exception) { /* ignore */ }
        }

        return result;
    }
}