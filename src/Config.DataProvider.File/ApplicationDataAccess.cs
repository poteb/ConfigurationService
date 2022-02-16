using Newtonsoft.Json;
using pote.Config.DbModel;
using pote.Config.Shared;

namespace pote.Config.DataProvider.File;

public class ApplicationDataAccess : IApplicationDataAccess
{
    private readonly IFileHandler _fileHandler;

    public ApplicationDataAccess(IFileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }
    public async Task<List<Application>> GetApplications(CancellationToken cancellationToken)
    {
        var files = _fileHandler.GetApplicationFiles();
        var result = new List<Application>();
        foreach (var file in files)
        {
            try
            {
                var application = JsonConvert.DeserializeObject<Application>(await _fileHandler.GetApplicationContentAbsolutePath(file, cancellationToken));
                if (application == null) continue;
                result.Add(application);
            }
            catch (Exception) { /* ignore */ }
        }

        return result;
    }
}