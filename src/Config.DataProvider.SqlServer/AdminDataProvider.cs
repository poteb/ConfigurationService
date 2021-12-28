using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.SqlClient;
using pote.Config.DbModel;
using pote.Config.Shared;
using Environment = pote.Config.DbModel.Environment;

namespace pote.Config.DataProvider.SqlServer;

public class AdminDataProvider : IAdminDataProvider
{
    private readonly string _connectionString;

    public AdminDataProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<Configuration>> GetAll(CancellationToken cancellationToken)
    {
        const string query = "SELECT * FROM [Configurations] c INNER JOIN (SELECT [Gid], MAX([CreatedUtc]) AS MaxDate FROM [Configurations] GROUP BY [Gid]) t ON t.Gid=c.Gid AND c.CreatedUtc=t.MaxDate WHERE c.Deleted=0";
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        return (await conn.QueryAsync<Configuration>(new CommandDefinition(query, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<(Configuration configuration, List<Configuration> history)> GetConfiguration(string gid, CancellationToken cancellationToken)
    {
        const string query = "SELECT * FROM [Configurations] WHERE [Gid]=@gid ORDER BY [CreatedUtc] DESC";
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var dp = new DynamicParameters();
        dp.Add("gid", gid);
        var configs = (await conn.QueryAsync<Configuration>(new CommandDefinition(query, dp, cancellationToken: cancellationToken))).ToList();
        if (!configs.Any()) return (null, null);
        var config = configs.First();
        configs.RemoveAt(0);
        return (config, configs);
    }

    public async Task Insert(Configuration configuration, CancellationToken cancellationToken)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.InsertAsync(configuration);
    }

    public Task<List<Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task UpsertEnvironment(Environment environment, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteEnvironment(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<DbModel.System>> GetSystems(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task UpsertSystem(DbModel.System system, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteSystem(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public async Task<Configuration> Update(Configuration configuration, CancellationToken cancellationToken)
    //{
    //    await using var conn = new SqlConnection(_connectionString);
    //    await conn.OpenAsync(cancellationToken);
    //    await conn.UpdateAsync(configuration);
    //    return configuration;
    //}
}