using Dapper;
using pote.Config.DataProvider.Interfaces;
using Environment = pote.Config.DbModel.Environment;

namespace pote.Config.DataProvider.SqlServer;

public class EnvironmentDataAccess : IEnvironmentDataAccess
{
    private readonly SqlConnectionFactory _connectionFactory;

    public EnvironmentDataAccess(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var result = await conn.QueryAsync<Environment>(new CommandDefinition("SELECT [Id], [Name] FROM [Environments]", cancellationToken: cancellationToken));
        return result.ToList();
    }

    public async Task<Environment> GetEnvironment(string idOrName, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var environment = await conn.QueryFirstOrDefaultAsync<Environment>(
            new CommandDefinition("SELECT [Id], [Name] FROM [Environments] WHERE [Id] = @idOrName OR [Name] = @idOrName", new { idOrName }, cancellationToken: cancellationToken));
        return environment ?? throw new KeyNotFoundException($"Environment not found, idOrName: {idOrName}");
    }
}
