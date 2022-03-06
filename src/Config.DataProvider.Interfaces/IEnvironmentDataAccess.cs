namespace pote.Config.Shared;

public interface IEnvironmentDataAccess
{
    Task<List<DbModel.Environment>> GetEnvironments(CancellationToken cancellationToken);
}