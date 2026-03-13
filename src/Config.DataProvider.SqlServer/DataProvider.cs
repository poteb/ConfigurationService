using Dapper;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;
using pote.Config.Encryption;
using pote.Config.Shared;
using Environment = pote.Config.DbModel.Environment;

namespace pote.Config.DataProvider.SqlServer;

public class DataProvider : IDataProvider
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly IEnvironmentDataAccess _environmentDataAccess;
    private readonly IApplicationDataAccess _applicationDataAccess;
    private readonly ISecretDataAccess _secretDataAccess;
    private readonly EncryptionSettings _encryptionSettings;

    public DataProvider(SqlConnectionFactory connectionFactory, IEnvironmentDataAccess environmentDataAccess, IApplicationDataAccess applicationDataAccess, ISecretDataAccess secretDataAccess, EncryptionSettings encryptionSettings)
    {
        _connectionFactory = connectionFactory;
        _environmentDataAccess = environmentDataAccess;
        _applicationDataAccess = applicationDataAccess;
        _secretDataAccess = secretDataAccess;
        _encryptionSettings = encryptionSettings;
    }

    public async Task<Configuration> GetConfiguration(string name, string applicationId, string environmentId, CancellationToken cancellationToken)
    {
        const string query = """
            SELECT c.[Id], c.[HeaderId], c.[Json], c.[CreatedUtc], c.[IsActive], c.[Deleted], c.[IsJsonEncrypted]
            FROM [Configurations] c
            INNER JOIN [ConfigurationHeaders] ch ON ch.[Id] = c.[HeaderId]
            INNER JOIN [ConfigurationApplications] ca ON ca.[ConfigurationId] = c.[Id]
            INNER JOIN [ConfigurationEnvironments] ce ON ce.[ConfigurationId] = c.[Id]
            WHERE ch.[Name] = @name
              AND ca.[ApplicationId] = @applicationId
              AND ce.[EnvironmentId] = @environmentId
              AND c.[Deleted] = 0
              AND ch.[Deleted] = 0
              AND c.[IsActive] = 1
            ORDER BY c.[CreatedUtc] DESC
            """;

        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var configuration = await conn.QueryFirstOrDefaultAsync<Configuration>(new CommandDefinition(query, new { name, applicationId, environmentId }, cancellationToken: cancellationToken));
        if (configuration == null)
            return new Configuration { Id = string.Empty };

        configuration.Applications = (await conn.QueryAsync<string>(
            new CommandDefinition("SELECT [ApplicationId] FROM [ConfigurationApplications] WHERE [ConfigurationId] = @Id", new { configuration.Id }, cancellationToken: cancellationToken))).ToList();
        configuration.Environments = (await conn.QueryAsync<string>(
            new CommandDefinition("SELECT [EnvironmentId] FROM [ConfigurationEnvironments] WHERE [ConfigurationId] = @Id", new { configuration.Id }, cancellationToken: cancellationToken))).ToList();

        return configuration;
    }

    public async Task<ApiKeys> GetApiKeys(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var keys = await conn.QueryAsync<ApiKeyEntry>(new CommandDefinition("SELECT [Name], [Key] FROM [ApiKeys]", cancellationToken: cancellationToken));
        return new ApiKeys { Keys = keys.ToList() };
    }

    public async Task<string> GetSecretValue(string name, string applicationId, string environmentId, CancellationToken cancellationToken)
    {
        var secret = await _secretDataAccess.GetSecret(name, applicationId, environmentId, cancellationToken);
        if (string.IsNullOrWhiteSpace(secret.Value)) return string.Empty;
        secret.Value = EncryptionHandler.Decrypt(secret.Value, _encryptionSettings.JsonEncryptionKey);
        return secret.Id == string.Empty ? string.Empty : secret.Value;
    }

    public async Task<List<Application>> GetApplications(CancellationToken cancellationToken)
    {
        return await _applicationDataAccess.GetApplications(cancellationToken);
    }

    public async Task<Application> GetApplication(string idOrName, CancellationToken cancellationToken)
    {
        return await _applicationDataAccess.GetApplication(idOrName, cancellationToken);
    }

    public async Task<List<Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        return await _environmentDataAccess.GetEnvironments(cancellationToken);
    }

    public Task<Environment> GetEnvironment(string idOrName, CancellationToken cancellationToken)
    {
        return _environmentDataAccess.GetEnvironment(idOrName, cancellationToken);
    }
}
