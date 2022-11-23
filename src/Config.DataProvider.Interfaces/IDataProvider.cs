using pote.Config.DbModel;

namespace pote.Config.Shared
{
    public interface IDataProvider : IEnvironmentDataAccess, IApplicationDataAccess
    {
        //Task<string> GetConfigurationJson(string name, string applicationId, string environment, CancellationToken cancellationToken);
        Task<Configuration> GetConfiguration(string name, string applicationId, string environment, CancellationToken cancellationToken);
    }
}