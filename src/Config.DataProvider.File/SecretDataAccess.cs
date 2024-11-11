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

    public async Task<Secret> GetSecret(string name, string applicationId, string environmentId, CancellationToken cancellationToken)
    {
        foreach (var file in _fileHandler.GetSecretFiles())
        {
            var header = JsonConvert.DeserializeObject<SecretHeader>(await System.IO.File.ReadAllTextAsync(file, cancellationToken));
            if (header == null) continue;
            if (!header.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) continue;
            foreach (var secret in header.Secrets)
            {
                if (secret.Applications.All(a => !string.Equals(a, applicationId, StringComparison.InvariantCultureIgnoreCase))) continue;
                if (secret.Environments.All(e => !string.Equals(e, environmentId, StringComparison.InvariantCultureIgnoreCase))) continue;

                return secret;
            }
        }

        return new Secret { Id = string.Empty };
    }
}