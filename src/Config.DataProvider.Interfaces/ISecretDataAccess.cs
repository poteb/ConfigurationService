using pote.Config.DbModel;

namespace pote.Config.DataProvider.Interfaces;

public interface ISecretDataAccess
{
    Task<Secret> GetSecret(string name, string applicationId, string environmentId, CancellationToken cancellationToken);
}