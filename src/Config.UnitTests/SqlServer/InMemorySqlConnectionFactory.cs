using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using pote.Config.DataProvider.SqlServer;

namespace pote.Config.UnitTests.SqlServer;

/// <summary>
/// A test-only SqlConnectionFactory that returns an in-memory SQLite connection.
/// A single shared connection is kept open for the lifetime of the factory to
/// preserve the in-memory database between calls. A non-disposing wrapper is
/// returned so that 'await using' in production code does not close the connection.
/// </summary>
public class InMemorySqlConnectionFactory : SqlConnectionFactory, IDisposable
{
    private readonly SqliteConnection _sharedConnection;

    public InMemorySqlConnectionFactory() : base("not-used")
    {
        _sharedConnection = new SqliteConnection("Data Source=:memory:");
        _sharedConnection.Open();
        CreateSchema();
    }

    public override Task<DbConnection> CreateOpenConnection(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DbConnection>(new NonDisposingConnection(_sharedConnection));
    }

    private void CreateSchema()
    {
        using var cmd = _sharedConnection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE [Environments] (
                [Id] TEXT PRIMARY KEY,
                [Name] TEXT NOT NULL
            );

            CREATE TABLE [Applications] (
                [Id] TEXT PRIMARY KEY,
                [Name] TEXT NOT NULL
            );

            CREATE TABLE [ConfigurationHeaders] (
                [Id] TEXT PRIMARY KEY,
                [Name] TEXT NOT NULL,
                [CreatedUtc] TEXT NOT NULL DEFAULT (datetime('now')),
                [UpdateUtc] TEXT NOT NULL DEFAULT (datetime('now')),
                [Deleted] INTEGER NOT NULL DEFAULT 0,
                [IsActive] INTEGER NOT NULL DEFAULT 1,
                [IsJsonEncrypted] INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE [Configurations] (
                [Id] TEXT PRIMARY KEY,
                [HeaderId] TEXT NOT NULL,
                [Json] TEXT NOT NULL DEFAULT '',
                [CreatedUtc] TEXT NOT NULL DEFAULT (datetime('now')),
                [IsActive] INTEGER NOT NULL DEFAULT 1,
                [Deleted] INTEGER NOT NULL DEFAULT 0,
                [IsJsonEncrypted] INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY ([HeaderId]) REFERENCES [ConfigurationHeaders]([Id])
            );

            CREATE TABLE [ConfigurationApplications] (
                [ConfigurationId] TEXT NOT NULL,
                [ApplicationId] TEXT NOT NULL,
                PRIMARY KEY ([ConfigurationId], [ApplicationId])
            );

            CREATE TABLE [ConfigurationEnvironments] (
                [ConfigurationId] TEXT NOT NULL,
                [EnvironmentId] TEXT NOT NULL,
                PRIMARY KEY ([ConfigurationId], [EnvironmentId])
            );

            CREATE TABLE [SecretHeaders] (
                [Id] TEXT PRIMARY KEY,
                [Name] TEXT NOT NULL,
                [CreatedUtc] TEXT NOT NULL DEFAULT (datetime('now')),
                [UpdateUtc] TEXT NOT NULL DEFAULT (datetime('now')),
                [Deleted] INTEGER NOT NULL DEFAULT 0,
                [IsActive] INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE [Secrets] (
                [Id] TEXT PRIMARY KEY,
                [HeaderId] TEXT NOT NULL,
                [Value] TEXT NOT NULL DEFAULT '',
                [ValueType] TEXT NOT NULL DEFAULT '',
                [CreatedUtc] TEXT NOT NULL DEFAULT (datetime('now')),
                [IsActive] INTEGER NOT NULL DEFAULT 1,
                [Deleted] INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY ([HeaderId]) REFERENCES [SecretHeaders]([Id])
            );

            CREATE TABLE [SecretApplications] (
                [SecretId] TEXT NOT NULL,
                [ApplicationId] TEXT NOT NULL,
                PRIMARY KEY ([SecretId], [ApplicationId])
            );

            CREATE TABLE [SecretEnvironments] (
                [SecretId] TEXT NOT NULL,
                [EnvironmentId] TEXT NOT NULL,
                PRIMARY KEY ([SecretId], [EnvironmentId])
            );

            CREATE TABLE [Settings] (
                [Id] INTEGER PRIMARY KEY,
                [EncryptAllJson] INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE [ApiKeys] (
                [Name] TEXT NOT NULL DEFAULT '',
                [Key] TEXT NOT NULL
            );

            CREATE TABLE [AuditLog] (
                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                [EntityType] TEXT NOT NULL,
                [EntityId] TEXT NOT NULL,
                [CallerIp] TEXT NOT NULL,
                [Content] TEXT NOT NULL,
                [CreatedUtc] TEXT NOT NULL DEFAULT (datetime('now'))
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _sharedConnection.Dispose();
    }

    /// <summary>
    /// Wraps a DbConnection so that Dispose/DisposeAsync are no-ops,
    /// preventing 'await using' from closing the shared in-memory connection.
    /// </summary>
    private class NonDisposingConnection : DbConnection
    {
        private readonly DbConnection _inner;

        public NonDisposingConnection(DbConnection inner)
        {
            _inner = inner;
        }

        public override string ConnectionString
        {
            get => _inner.ConnectionString!;
            [param: AllowNull] set => _inner.ConnectionString = value;
        }

        public override string Database => _inner.Database;
        public override string DataSource => _inner.DataSource;
        public override string ServerVersion => _inner.ServerVersion;
        public override ConnectionState State => _inner.State;

        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
        public override void Close() { /* no-op */ }
        public override void Open() => _inner.Open();
        public override Task OpenAsync(CancellationToken cancellationToken) => _inner.OpenAsync(cancellationToken);

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => _inner.BeginTransaction(isolationLevel);

        protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
            => await _inner.BeginTransactionAsync(isolationLevel, cancellationToken);

        protected override DbCommand CreateDbCommand()
        {
            var cmd = _inner.CreateCommand();
            return cmd;
        }

        protected override void Dispose(bool disposing) { /* no-op */ }
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
