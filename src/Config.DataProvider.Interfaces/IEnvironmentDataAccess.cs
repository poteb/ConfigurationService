namespace pote.Config.DataProvider.Interfaces;

public interface IEnvironmentDataAccess
{
    Task<List<DbModel.Environment>> GetEnvironments(CancellationToken cancellationToken);
}