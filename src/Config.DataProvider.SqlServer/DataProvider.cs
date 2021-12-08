using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using pote.Config.Shared;

namespace pote.Config.DataProvider.SqlServer
{
    public class DataProvider : IDataProvider
    {
        private readonly string _connectionString;

        public DataProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<string> GetConfigurationJson(int id, CancellationToken cancellationToken)
        {
            const string query = "SELECT [json] FROM [Configurations] WHERE [id]=@id AND [Deleted]=0";
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            var dp = new DynamicParameters();
            dp.Add("id", id);
            return await conn.QueryFirstOrDefaultAsync<string>(new CommandDefinition(query, dp, cancellationToken: cancellationToken));
        }

        public async Task<string> GetConfigurationJson(string name, string system, string environment, CancellationToken cancellationToken)
        {
            const string query = "SELECT [Json],[Integrations] FROM [Configurations] WHERE [Name]='@name' AND [Integrations] <> '' AND [IsActive]=1 AND [Deleted]=0 ORDER BY [Created] DESC";
            await using var conn = new SqlConnection(_connectionString);
            var dp = new DynamicParameters();
            dp.Add("name", name);
            var result = conn.Query<dynamic>(new CommandDefinition(query, dp, cancellationToken: cancellationToken));
            foreach (var r in result)
            {
                var dyn = JsonConvert.DeserializeObject(r.Integrations);
                if (dyn == null) continue;
                var envFound = false;
                foreach (var env in new List<string>(dyn.Environments.ToString().Split(",")))
                {
                    if (!environment.Equals(env, StringComparison.InvariantCultureIgnoreCase)) continue;
                    envFound = true;
                }

                var sysFound = false;
                foreach (var sys in new List<string>(dyn.Systems.ToString().Split(",")))
                {
                    if (!system.Equals(sys, StringComparison.InvariantCultureIgnoreCase)) continue;
                    sysFound = true;
                }
                if (envFound && sysFound)
                    return r.Json;
            }

            return string.Empty;
        }
    }
}