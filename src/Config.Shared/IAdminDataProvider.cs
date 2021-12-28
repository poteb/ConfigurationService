using pote.Config.DbModel;

namespace pote.Config.Shared;

public interface IAdminDataProvider
{
    /// <summary>Returns all configurations (without history)</summary>
    Task<List<Configuration>> GetAll(CancellationToken cancellationToken);
    /// <summary>Returns a configuration (with its history)</summary>
    Task<(Configuration configuration, List<Configuration> history)> GetConfiguration(string guid, CancellationToken cancellationToken);

    Task Insert(Configuration configuration, CancellationToken cancellationToken);

    //Task<Configuration> Update(Configuration configuration, CancellationToken cancellationToken);

    Task<List<DbModel.Environment>> GetEnvironments(CancellationToken cancellationToken);
    Task UpsertEnvironment(DbModel.Environment environment, CancellationToken cancellationToken);
    Task DeleteEnvironment(string id, CancellationToken cancellationToken);

    Task<List<DbModel.System>> GetSystems(CancellationToken cancellationToken);
    Task UpsertSystem(DbModel.System system, CancellationToken cancellationToken);
    Task DeleteSystem(string id, CancellationToken cancellationToken);
}