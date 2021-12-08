namespace pote.Config.Shared
{
    public interface IDataProvider
    {
        Task<string> GetConfigurationJson(int id, CancellationToken cancellationToken);
        Task<string> GetConfigurationJson(string name, string system, string environment, CancellationToken cancellationToken);
    }
}