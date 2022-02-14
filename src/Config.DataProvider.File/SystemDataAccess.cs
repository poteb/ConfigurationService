using Newtonsoft.Json;
using pote.Config.Shared;

namespace pote.Config.DataProvider.File;

public class SystemDataAccess : ISystemDataAccess
{
    private readonly IFileHandler _fileHandler;

    public SystemDataAccess(IFileHandler fileHandler)
    {
        _fileHandler = fileHandler;
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
}