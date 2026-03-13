using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;
using pote.Config.DataProvider.SqlServer;

namespace pote.Config.UnitTests.SqlServer;

/// <summary>
/// Tests for SqlServer AuditLogHandler. Uses a SQLite-compatible factory that
/// replaces GETUTCDATE() with datetime('now') since the production SQL is SQL Server specific.
/// </summary>
[TestFixture]
public class SqlServerAuditLogHandlerTests
{
    private SqliteCompatibleAuditLogFactory _factory = null!;
    private AuditLogHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new SqliteCompatibleAuditLogFactory();
        _sut = new AuditLogHandler(_factory);
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task AuditLogConfiguration_InsertsRow()
    {
        await _sut.AuditLogConfiguration("cfg1", "10.0.0.1", "config content");

        var conn = await _factory.CreateOpenConnection();
        var row = await conn.QueryFirstAsync("SELECT * FROM [AuditLog]");
        Assert.AreEqual("Configuration", (string)row.EntityType);
        Assert.AreEqual("cfg1", (string)row.EntityId);
        Assert.AreEqual("10.0.0.1", (string)row.CallerIp);
        Assert.AreEqual("config content", (string)row.Content);
    }

    [Test]
    public async Task AuditLogEnvironment_InsertsRow()
    {
        await _sut.AuditLogEnvironment("env1", "10.0.0.2", "env content");

        var conn = await _factory.CreateOpenConnection();
        var row = await conn.QueryFirstAsync("SELECT * FROM [AuditLog]");
        Assert.AreEqual("Environment", (string)row.EntityType);
        Assert.AreEqual("env1", (string)row.EntityId);
    }

    [Test]
    public async Task AuditLogApplication_InsertsRow()
    {
        await _sut.AuditLogApplication("app1", "10.0.0.3", "app content");

        var conn = await _factory.CreateOpenConnection();
        var row = await conn.QueryFirstAsync("SELECT * FROM [AuditLog]");
        Assert.AreEqual("Application", (string)row.EntityType);
        Assert.AreEqual("app1", (string)row.EntityId);
    }

    [Test]
    public async Task AuditLogSettings_InsertsRow()
    {
        await _sut.AuditLogSettings("s1", "10.0.0.4", "settings content");

        var conn = await _factory.CreateOpenConnection();
        var row = await conn.QueryFirstAsync("SELECT * FROM [AuditLog]");
        Assert.AreEqual("Settings", (string)row.EntityType);
    }

    [Test]
    public async Task AuditLogApiKeys_InsertsRow()
    {
        await _sut.AuditLogApiKeys("k1", "10.0.0.5", "apikeys content");

        var conn = await _factory.CreateOpenConnection();
        var row = await conn.QueryFirstAsync("SELECT * FROM [AuditLog]");
        Assert.AreEqual("ApiKeys", (string)row.EntityType);
    }

    [Test]
    public async Task AuditLogSecrets_InsertsRow()
    {
        await _sut.AuditLogSecrets("sec1", "10.0.0.6", "secret content");

        var conn = await _factory.CreateOpenConnection();
        var row = await conn.QueryFirstAsync("SELECT * FROM [AuditLog]");
        Assert.AreEqual("Secrets", (string)row.EntityType);
        Assert.AreEqual("sec1", (string)row.EntityId);
    }

    [Test]
    public async Task MultipleAuditLogs_InsertMultipleRows()
    {
        await _sut.AuditLogConfiguration("c1", "ip1", "content1");
        await _sut.AuditLogEnvironment("e1", "ip2", "content2");

        var conn = await _factory.CreateOpenConnection();
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [AuditLog]");
        Assert.AreEqual(2, count);
    }

    /// <summary>
    /// A factory that intercepts SQL commands to replace GETUTCDATE() with
    /// SQLite-compatible datetime('now'), enabling the AuditLogHandler to
    /// work against an in-memory SQLite database.
    /// </summary>
    private class SqliteCompatibleAuditLogFactory : InMemorySqlConnectionFactory
    {
        public override Task<DbConnection> CreateOpenConnection(CancellationToken cancellationToken = default)
        {
            var baseConn = base.CreateOpenConnection(cancellationToken).Result;
            return Task.FromResult<DbConnection>(new SqlRewritingConnection(baseConn));
        }

        private class SqlRewritingConnection : DbConnection
        {
            private readonly DbConnection _inner;

            public SqlRewritingConnection(DbConnection inner) { _inner = inner; }

            public override string ConnectionString { get => _inner.ConnectionString!; set => _inner.ConnectionString = value; }
            public override string Database => _inner.Database;
            public override string DataSource => _inner.DataSource;
            public override string ServerVersion => _inner.ServerVersion;
            public override ConnectionState State => _inner.State;

            public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
            public override void Close() { }
            public override void Open() => _inner.Open();

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
                => _inner.BeginTransaction(isolationLevel);

            protected override DbCommand CreateDbCommand()
            {
                var cmd = _inner.CreateCommand();
                return new SqlRewritingCommand(cmd, this);
            }

            protected override void Dispose(bool disposing) { }
            public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

            private class SqlRewritingCommand : DbCommand
            {
                private readonly DbCommand _inner;

                public SqlRewritingCommand(DbCommand inner, DbConnection connection)
                {
                    _inner = inner;
                    DbConnection = connection;
                }

                public override string CommandText
                {
                    get => _inner.CommandText;
                    set => _inner.CommandText = value?.Replace("GETUTCDATE()", "datetime('now')") ?? value!;
                }

                public override int CommandTimeout { get => _inner.CommandTimeout; set => _inner.CommandTimeout = value; }
                public override CommandType CommandType { get => _inner.CommandType; set => _inner.CommandType = value; }
                public override bool DesignTimeVisible { get => _inner.DesignTimeVisible; set => _inner.DesignTimeVisible = value; }
                public override UpdateRowSource UpdatedRowSource { get => _inner.UpdatedRowSource; set => _inner.UpdatedRowSource = value; }
                protected override DbConnection? DbConnection { get; set; }
                protected override DbParameterCollection DbParameterCollection => _inner.Parameters;
                protected override DbTransaction? DbTransaction { get => _inner.Transaction; set => _inner.Transaction = value; }

                public override void Cancel() => _inner.Cancel();
                public override int ExecuteNonQuery() => _inner.ExecuteNonQuery();
                public override object? ExecuteScalar() => _inner.ExecuteScalar();
                public override void Prepare() => _inner.Prepare();
                protected override DbParameter CreateDbParameter() => _inner.CreateParameter();
                protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => _inner.ExecuteReader(behavior);
            }
        }
    }
}
