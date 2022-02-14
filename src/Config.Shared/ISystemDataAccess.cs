namespace pote.Config.Shared;

public interface ISystemDataAccess
{
    Task<List<DbModel.System>> GetSystems(CancellationToken cancellationToken);
}