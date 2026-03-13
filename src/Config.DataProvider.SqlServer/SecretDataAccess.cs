using Dapper;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

namespace pote.Config.DataProvider.SqlServer;

public class SecretDataAccess : ISecretDataAccess
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SecretDataAccess(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Secret> GetSecret(string name, string applicationId, string environmentId, CancellationToken cancellationToken)
    {
        const string query = """
            SELECT s.[Id], s.[HeaderId], s.[Value], s.[ValueType], s.[CreatedUtc], s.[IsActive], s.[Deleted]
            FROM [Secrets] s
            INNER JOIN [SecretHeaders] sh ON sh.[Id] = s.[HeaderId]
            INNER JOIN [SecretApplications] sa ON sa.[SecretId] = s.[Id]
            INNER JOIN [SecretEnvironments] se ON se.[SecretId] = s.[Id]
            WHERE sh.[Name] = @name
              AND sa.[ApplicationId] = @applicationId
              AND se.[EnvironmentId] = @environmentId
              AND s.[Deleted] = 0
              AND sh.[Deleted] = 0
              AND s.[IsActive] = 1
            ORDER BY s.[CreatedUtc] DESC
            """;

        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var secret = await conn.QueryFirstOrDefaultAsync<Secret>(new CommandDefinition(query, new { name, applicationId, environmentId }, cancellationToken: cancellationToken));
        if (secret == null)
            return new Secret { Id = string.Empty };

        secret.Applications = (await conn.QueryAsync<string>(
            new CommandDefinition("SELECT [ApplicationId] FROM [SecretApplications] WHERE [SecretId] = @Id", new { secret.Id }, cancellationToken: cancellationToken))).ToList();
        secret.Environments = (await conn.QueryAsync<string>(
            new CommandDefinition("SELECT [EnvironmentId] FROM [SecretEnvironments] WHERE [SecretId] = @Id", new { secret.Id }, cancellationToken: cancellationToken))).ToList();

        return secret;
    }
}
