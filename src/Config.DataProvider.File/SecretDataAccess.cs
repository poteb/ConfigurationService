using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

namespace pote.Config.DataProvider.File;

public class SecretDataAccess : ISecretDataAccess
{
    private readonly IFileHandler _fileHandler;

    public SecretDataAccess(IFileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }

    public async IAsyncEnumerable<Secret> GetSecrets([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var files = _fileHandler.GetSecretFiles();
        foreach (var file in files)
        {
            var secret = JsonConvert.DeserializeObject<Secret>(await _fileHandler.GetSecretContentAbsolutePath(file, cancellationToken));
            if (secret == null) continue;
            yield return secret;
        }
    }
}