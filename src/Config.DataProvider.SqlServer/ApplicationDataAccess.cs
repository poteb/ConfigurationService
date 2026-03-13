using Dapper;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

namespace pote.Config.DataProvider.SqlServer;

public class ApplicationDataAccess : IApplicationDataAccess
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ApplicationDataAccess(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<Application>> GetApplications(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var result = await conn.QueryAsync<Application>(new CommandDefinition("SELECT [Id], [Name] FROM [Applications]", cancellationToken: cancellationToken));
        return result.ToList();
    }

    public async Task<Application> GetApplication(string idOrName, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var application = await conn.QueryFirstOrDefaultAsync<Application>(
            new CommandDefinition("SELECT [Id], [Name] FROM [Applications] WHERE [Id] = @idOrName OR [Name] = @idOrName", new { idOrName }, cancellationToken: cancellationToken));
        return application ?? throw new KeyNotFoundException($"Application not found, idOrName: {idOrName}");
    }
}
