using pote.Config.DbModel;

namespace pote.Config.DataProvider.Interfaces;

public interface ISecretDataAccess
{
    IAsyncEnumerable<Secret> GetSecrets(CancellationToken cancellationToken);
}