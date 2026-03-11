using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace pote.Config.DataProvider.SqlServer;

public class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public virtual async Task<DbConnection> CreateOpenConnection(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
