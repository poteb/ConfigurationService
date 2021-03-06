using pote.Config.DbModel;

namespace pote.Config.Shared;

public interface IAdminDataProvider : IApplicationDataAccess, IEnvironmentDataAccess
{
    /// <summary>Returns all configurations (without history)</summary>
    Task<List<ConfigurationHeader>> GetAll(CancellationToken cancellationToken);
    /// <summary>Returns a configuration (with its history)</summary>
    Task<ConfigurationHeader> GetConfiguration(string id, CancellationToken cancellationToken, bool includeHistory = true);

    Task Insert(ConfigurationHeader header, CancellationToken cancellationToken);

    //Task<Configuration> Update(Configuration configuration, CancellationToken cancellationToken);
    
    Task UpsertEnvironment(DbModel.Environment environment, CancellationToken cancellationToken);
    Task DeleteEnvironment(string id, CancellationToken cancellationToken);
    
    Task UpsertApplication(DbModel.Application application, CancellationToken cancellationToken);
    Task DeleteApplication(string id, CancellationToken cancellationToken);
}