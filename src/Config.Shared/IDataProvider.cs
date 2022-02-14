namespace pote.Config.Shared
{
    public interface IDataProvider : IEnvironmentDataAccess, ISystemDataAccess
    {
        Task<string> GetConfigurationJson(string name, string system, string environment, CancellationToken cancellationToken);
    }
}