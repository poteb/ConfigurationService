using pote.Config.DbModel;

namespace pote.Config.DataProvider.Interfaces;

public interface IAdminDataProvider : IDataProvider
{
    /// <summary>Returns all configurations (without history)</summary>
    Task<List<ConfigurationHeader>> GetAll(CancellationToken cancellationToken);
    /// <summary>Returns a configuration (with its history)</summary>
    Task<ConfigurationHeader> GetConfiguration(string id, CancellationToken cancellationToken, bool includeHistory = true);
    Task<List<ConfigurationHeader>> GetConfigurationHistory(string id, int page, int pageSize, CancellationToken cancellationToken);

    void DeleteConfiguration(string id, bool permanent);
    Task Insert(ConfigurationHeader header, CancellationToken cancellationToken);

    //Task<Configuration> Update(Configuration configuration, CancellationToken cancellationToken);
    
    Task UpsertEnvironment(DbModel.Environment environment, CancellationToken cancellationToken);
    Task DeleteEnvironment(string id, CancellationToken cancellationToken);
    
    Task UpsertApplication(DbModel.Application application, CancellationToken cancellationToken);
    Task DeleteApplication(string id, CancellationToken cancellationToken);
}