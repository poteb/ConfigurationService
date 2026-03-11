using Dapper;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;
using pote.Config.Encryption;
using pote.Config.Shared;
using Environment = pote.Config.DbModel.Environment;

namespace pote.Config.DataProvider.SqlServer;

public class AdminDataProvider : IAdminDataProvider
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly IApplicationDataAccess _applicationDataAccess;
    private readonly IEnvironmentDataAccess _environmentDataAccess;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly DataProvider _dataProvider;

    public AdminDataProvider(SqlConnectionFactory connectionFactory, IApplicationDataAccess applicationDataAccess, IEnvironmentDataAccess environmentDataAccess, ISecretDataAccess secretDataAccess, EncryptionSettings encryptionSettings)
    {
        _connectionFactory = connectionFactory;
        _applicationDataAccess = applicationDataAccess;
        _environmentDataAccess = environmentDataAccess;
        _encryptionSettings = encryptionSettings;

        _dataProvider = new DataProvider(connectionFactory, environmentDataAccess, applicationDataAccess, secretDataAccess, encryptionSettings);
    }

    public async Task<List<ConfigurationHeader>> GetAllConfigurationHeaders(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var headers = (await conn.QueryAsync<ConfigurationHeader>(
            new CommandDefinition("SELECT [Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted], [IsActive], [IsJsonEncrypted] FROM [ConfigurationHeaders] WHERE [Deleted] = 0", cancellationToken: cancellationToken))).ToList();

        foreach (var header in headers)
        {
            var configurations = (await conn.QueryAsync<Configuration>(
                new CommandDefinition("SELECT [Id], [HeaderId], [Json], [CreatedUtc], [IsActive], [Deleted], [IsJsonEncrypted] FROM [Configurations] WHERE [HeaderId] = @Id ORDER BY [CreatedUtc] DESC", new { header.Id }, cancellationToken: cancellationToken))).ToList();

            foreach (var config in configurations)
            {
                config.Applications = (await conn.QueryAsync<string>(
                    new CommandDefinition("SELECT [ApplicationId] FROM [ConfigurationApplications] WHERE [ConfigurationId] = @ConfigId", new { ConfigId = config.Id }, cancellationToken: cancellationToken))).ToList();
                config.Environments = (await conn.QueryAsync<string>(
                    new CommandDefinition("SELECT [EnvironmentId] FROM [ConfigurationEnvironments] WHERE [ConfigurationId] = @ConfigId", new { ConfigId = config.Id }, cancellationToken: cancellationToken))).ToList();
            }

            header.Configurations = configurations;
            EncryptionHandler.Decrypt(header.Configurations, _encryptionSettings.JsonEncryptionKey);
        }

        return headers;
    }

    public async Task<ConfigurationHeader> GetConfiguration(string id, CancellationToken cancellationToken, bool includeHistory = true)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var header = await conn.QueryFirstOrDefaultAsync<ConfigurationHeader>(
            new CommandDefinition("SELECT [Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted], [IsActive], [IsJsonEncrypted] FROM [ConfigurationHeaders] WHERE [Id] = @id", new { id }, cancellationToken: cancellationToken));
        if (header == null) throw new KeyNotFoundException($"Configuration header not found: {id}");

        var configurations = (await conn.QueryAsync<Configuration>(
            new CommandDefinition("SELECT [Id], [HeaderId], [Json], [CreatedUtc], [IsActive], [Deleted], [IsJsonEncrypted] FROM [Configurations] WHERE [HeaderId] = @id ORDER BY [CreatedUtc] DESC", new { id }, cancellationToken: cancellationToken))).ToList();

        foreach (var config in configurations)
        {
            config.Applications = (await conn.QueryAsync<string>(
                new CommandDefinition("SELECT [ApplicationId] FROM [ConfigurationApplications] WHERE [ConfigurationId] = @ConfigId", new { ConfigId = config.Id }, cancellationToken: cancellationToken))).ToList();
            config.Environments = (await conn.QueryAsync<string>(
                new CommandDefinition("SELECT [EnvironmentId] FROM [ConfigurationEnvironments] WHERE [ConfigurationId] = @ConfigId", new { ConfigId = config.Id }, cancellationToken: cancellationToken))).ToList();
        }

        header.Configurations = configurations;
        EncryptionHandler.Decrypt(header.Configurations, _encryptionSettings.JsonEncryptionKey);
        return header;
    }

    public async Task<Configuration> GetConfiguration(string name, string applicationId, string environment, CancellationToken cancellationToken)
    {
        var configuration = await _dataProvider.GetConfiguration(name, applicationId, environment, cancellationToken);
        EncryptionHandler.Decrypt(configuration, _encryptionSettings.JsonEncryptionKey);
        return configuration;
    }

    public async Task<ApiKeys> GetApiKeys(CancellationToken cancellationToken)
    {
        return await _dataProvider.GetApiKeys(cancellationToken);
    }

    public async Task<string> GetSecretValue(string name, string applicationId, string environmentId, CancellationToken cancellationToken)
    {
        return await _dataProvider.GetSecretValue(name, applicationId, environmentId, cancellationToken);
    }

    public async Task<List<ConfigurationHeader>> GetHeaderHistory(string id, int page, int pageSize, CancellationToken cancellationToken)
    {
        var header = await GetConfiguration(id, cancellationToken);
        var offset = page * pageSize;
        header.Configurations = header.Configurations.Skip(offset).Take(pageSize).ToList();
        return new List<ConfigurationHeader> { header };
    }

    public async Task<List<Configuration>> GetConfigurationHistory(string headerId, string id, int page, int pageSize, CancellationToken cancellationToken)
    {
        var header = await GetConfiguration(headerId, cancellationToken);
        var configuration = header.Configurations.FirstOrDefault(c => c.Id == id);
        if (configuration == null) return new List<Configuration>();
        return new List<Configuration> { configuration };
    }

    public void DeleteConfiguration(string id, bool permanent)
    {
        using var conn = _connectionFactory.CreateOpenConnection().GetAwaiter().GetResult();
        using var transaction = conn.BeginTransaction();
        if (permanent)
        {
            var configIds = conn.Query<string>("SELECT [Id] FROM [Configurations] WHERE [HeaderId] = @id", new { id }, transaction).ToList();
            foreach (var configId in configIds)
            {
                conn.Execute("DELETE FROM [ConfigurationApplications] WHERE [ConfigurationId] = @configId", new { configId }, transaction);
                conn.Execute("DELETE FROM [ConfigurationEnvironments] WHERE [ConfigurationId] = @configId", new { configId }, transaction);
            }
            conn.Execute("DELETE FROM [Configurations] WHERE [HeaderId] = @id", new { id }, transaction);
            conn.Execute("DELETE FROM [ConfigurationHeaders] WHERE [Id] = @id", new { id }, transaction);
        }
        else
        {
            conn.Execute("UPDATE [ConfigurationHeaders] SET [Deleted] = 1 WHERE [Id] = @id", new { id }, transaction);
        }
        transaction.Commit();
    }

    public async Task InsertConfiguration(ConfigurationHeader header, CancellationToken cancellationToken)
    {
        var settings = await GetSettings(cancellationToken);
        header.Configurations.ForEach(c =>
        {
            c.CreatedUtc = header.CreatedUtc;
            c.IsJsonEncrypted = c.IsJsonEncrypted || header.IsJsonEncrypted || settings.EncryptAllJson;
            EncryptionHandler.Encrypt(c, _encryptionSettings.JsonEncryptionKey);
        });

        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        // Upsert header
        var existingHeader = await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition("SELECT [Id] FROM [ConfigurationHeaders] WHERE [Id] = @Id", new { header.Id }, transaction, cancellationToken: cancellationToken));

        if (existingHeader != null)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE [ConfigurationHeaders] SET [Name] = @Name, [UpdateUtc] = @UpdateUtc, [Deleted] = @Deleted, [IsActive] = @IsActive, [IsJsonEncrypted] = @IsJsonEncrypted WHERE [Id] = @Id",
                new { header.Id, header.Name, header.UpdateUtc, header.Deleted, header.IsActive, header.IsJsonEncrypted }, transaction, cancellationToken: cancellationToken));
        }
        else
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted], [IsActive], [IsJsonEncrypted]) VALUES (@Id, @Name, @CreatedUtc, @UpdateUtc, @Deleted, @IsActive, @IsJsonEncrypted)",
                new { header.Id, header.Name, header.CreatedUtc, header.UpdateUtc, header.Deleted, header.IsActive, header.IsJsonEncrypted }, transaction, cancellationToken: cancellationToken));
        }

        // Insert each configuration version
        foreach (var config in header.Configurations)
        {
            var existingConfig = await conn.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition("SELECT [Id] FROM [Configurations] WHERE [Id] = @Id", new { config.Id }, transaction, cancellationToken: cancellationToken));

            if (existingConfig != null)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE [Configurations] SET [Json] = @Json, [CreatedUtc] = @CreatedUtc, [IsActive] = @IsActive, [Deleted] = @Deleted, [IsJsonEncrypted] = @IsJsonEncrypted WHERE [Id] = @Id",
                    new { config.Id, config.Json, config.CreatedUtc, config.IsActive, config.Deleted, config.IsJsonEncrypted }, transaction, cancellationToken: cancellationToken));

                await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [ConfigurationApplications] WHERE [ConfigurationId] = @Id", new { config.Id }, transaction, cancellationToken: cancellationToken));
                await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [ConfigurationEnvironments] WHERE [ConfigurationId] = @Id", new { config.Id }, transaction, cancellationToken: cancellationToken));
            }
            else
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO [Configurations] ([Id], [HeaderId], [Json], [CreatedUtc], [IsActive], [Deleted], [IsJsonEncrypted]) VALUES (@Id, @HeaderId, @Json, @CreatedUtc, @IsActive, @Deleted, @IsJsonEncrypted)",
                    new { config.Id, config.HeaderId, config.Json, config.CreatedUtc, config.IsActive, config.Deleted, config.IsJsonEncrypted }, transaction, cancellationToken: cancellationToken));
            }

            foreach (var appId in config.Applications)
                await conn.ExecuteAsync(new CommandDefinition("INSERT INTO [ConfigurationApplications] ([ConfigurationId], [ApplicationId]) VALUES (@ConfigId, @AppId)", new { ConfigId = config.Id, AppId = appId }, transaction, cancellationToken: cancellationToken));
            foreach (var envId in config.Environments)
                await conn.ExecuteAsync(new CommandDefinition("INSERT INTO [ConfigurationEnvironments] ([ConfigurationId], [EnvironmentId]) VALUES (@ConfigId, @EnvId)", new { ConfigId = config.Id, EnvId = envId }, transaction, cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpsertEnvironment(Environment environment, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var existing = await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition("SELECT [Id] FROM [Environments] WHERE [Id] = @Id", new { environment.Id }, cancellationToken: cancellationToken));
        if (existing != null)
            await conn.ExecuteAsync(new CommandDefinition("UPDATE [Environments] SET [Name] = @Name WHERE [Id] = @Id", new { environment.Id, environment.Name }, cancellationToken: cancellationToken));
        else
            await conn.ExecuteAsync(new CommandDefinition("INSERT INTO [Environments] ([Id], [Name]) VALUES (@Id, @Name)", new { environment.Id, environment.Name }, cancellationToken: cancellationToken));
    }

    public async Task DeleteEnvironment(string id, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [ConfigurationEnvironments] WHERE [EnvironmentId] = @id", new { id }, cancellationToken: cancellationToken));
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [SecretEnvironments] WHERE [EnvironmentId] = @id", new { id }, cancellationToken: cancellationToken));
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [Environments] WHERE [Id] = @id", new { id }, cancellationToken: cancellationToken));
    }

    public async Task UpsertApplication(Application application, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var existing = await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition("SELECT [Id] FROM [Applications] WHERE [Id] = @Id", new { application.Id }, cancellationToken: cancellationToken));
        if (existing != null)
            await conn.ExecuteAsync(new CommandDefinition("UPDATE [Applications] SET [Name] = @Name WHERE [Id] = @Id", new { application.Id, application.Name }, cancellationToken: cancellationToken));
        else
            await conn.ExecuteAsync(new CommandDefinition("INSERT INTO [Applications] ([Id], [Name]) VALUES (@Id, @Name)", new { application.Id, application.Name }, cancellationToken: cancellationToken));
    }

    public async Task DeleteApplication(string id, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [ConfigurationApplications] WHERE [ApplicationId] = @id", new { id }, cancellationToken: cancellationToken));
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [SecretApplications] WHERE [ApplicationId] = @id", new { id }, cancellationToken: cancellationToken));
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [Applications] WHERE [Id] = @id", new { id }, cancellationToken: cancellationToken));
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

    public async Task<Settings> GetSettings(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var settings = await conn.QueryFirstOrDefaultAsync<Settings>(
            new CommandDefinition("SELECT [EncryptAllJson] FROM [Settings] WHERE [Id] = 1", cancellationToken: cancellationToken));
        return settings ?? new Settings();
    }

    public async Task SaveSettings(Settings settings, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var existing = await conn.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition("SELECT [Id] FROM [Settings] WHERE [Id] = 1", cancellationToken: cancellationToken));
        if (existing != null)
            await conn.ExecuteAsync(new CommandDefinition("UPDATE [Settings] SET [EncryptAllJson] = @EncryptAllJson WHERE [Id] = 1", new { settings.EncryptAllJson }, cancellationToken: cancellationToken));
        else
            await conn.ExecuteAsync(new CommandDefinition("INSERT INTO [Settings] ([Id], [EncryptAllJson]) VALUES (1, @EncryptAllJson)", new { settings.EncryptAllJson }, cancellationToken: cancellationToken));
    }

    public async Task SaveApiKeys(ApiKeys apiKeys, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [ApiKeys]", transaction: transaction, cancellationToken: cancellationToken));
        foreach (var key in apiKeys.Keys)
            await conn.ExecuteAsync(new CommandDefinition("INSERT INTO [ApiKeys] ([Name], [Key]) VALUES (@Name, @Key)", new { key.Name, key.Key }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task InsertSecret(SecretHeader header, CancellationToken cancellationToken)
    {
        header.Secrets.ForEach(s =>
        {
            s.CreatedUtc = header.CreatedUtc;
            EncryptionHandler.Encrypt(s, _encryptionSettings.JsonEncryptionKey);
        });

        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        // Upsert header
        var existingHeader = await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition("SELECT [Id] FROM [SecretHeaders] WHERE [Id] = @Id", new { header.Id }, transaction, cancellationToken: cancellationToken));

        if (existingHeader != null)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE [SecretHeaders] SET [Name] = @Name, [UpdateUtc] = @UpdateUtc, [Deleted] = @Deleted, [IsActive] = @IsActive WHERE [Id] = @Id",
                new { header.Id, header.Name, header.UpdateUtc, header.Deleted, header.IsActive }, transaction, cancellationToken: cancellationToken));
        }
        else
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO [SecretHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted], [IsActive]) VALUES (@Id, @Name, @CreatedUtc, @UpdateUtc, @Deleted, @IsActive)",
                new { header.Id, header.Name, header.CreatedUtc, header.UpdateUtc, header.Deleted, header.IsActive }, transaction, cancellationToken: cancellationToken));
        }

        // Insert each secret version
        foreach (var secret in header.Secrets)
        {
            var existingSecret = await conn.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition("SELECT [Id] FROM [Secrets] WHERE [Id] = @Id", new { secret.Id }, transaction, cancellationToken: cancellationToken));

            if (existingSecret != null)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE [Secrets] SET [Value] = @Value, [ValueType] = @ValueType, [CreatedUtc] = @CreatedUtc, [IsActive] = @IsActive, [Deleted] = @Deleted WHERE [Id] = @Id",
                    new { secret.Id, secret.Value, secret.ValueType, secret.CreatedUtc, secret.IsActive, secret.Deleted }, transaction, cancellationToken: cancellationToken));

                await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [SecretApplications] WHERE [SecretId] = @Id", new { secret.Id }, transaction, cancellationToken: cancellationToken));
                await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [SecretEnvironments] WHERE [SecretId] = @Id", new { secret.Id }, transaction, cancellationToken: cancellationToken));
            }
            else
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO [Secrets] ([Id], [HeaderId], [Value], [ValueType], [CreatedUtc], [IsActive], [Deleted]) VALUES (@Id, @HeaderId, @Value, @ValueType, @CreatedUtc, @IsActive, @Deleted)",
                    new { secret.Id, secret.HeaderId, secret.Value, secret.ValueType, secret.CreatedUtc, secret.IsActive, secret.Deleted }, transaction, cancellationToken: cancellationToken));
            }

            foreach (var appId in secret.Applications)
                await conn.ExecuteAsync(new CommandDefinition("INSERT INTO [SecretApplications] ([SecretId], [ApplicationId]) VALUES (@SecretId, @AppId)", new { SecretId = secret.Id, AppId = appId }, transaction, cancellationToken: cancellationToken));
            foreach (var envId in secret.Environments)
                await conn.ExecuteAsync(new CommandDefinition("INSERT INTO [SecretEnvironments] ([SecretId], [EnvironmentId]) VALUES (@SecretId, @EnvId)", new { SecretId = secret.Id, EnvId = envId }, transaction, cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteSecret(string id, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        var secretIds = (await conn.QueryAsync<string>(
            new CommandDefinition("SELECT [Id] FROM [Secrets] WHERE [HeaderId] = @id", new { id }, transaction, cancellationToken: cancellationToken))).ToList();

        foreach (var secretId in secretIds)
        {
            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [SecretApplications] WHERE [SecretId] = @secretId", new { secretId }, transaction, cancellationToken: cancellationToken));
            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [SecretEnvironments] WHERE [SecretId] = @secretId", new { secretId }, transaction, cancellationToken: cancellationToken));
        }

        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [Secrets] WHERE [HeaderId] = @id", new { id }, transaction, cancellationToken: cancellationToken));
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [SecretHeaders] WHERE [Id] = @id", new { id }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<List<SecretHeader>> GetAllSecretHeaders(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var headers = (await conn.QueryAsync<SecretHeader>(
            new CommandDefinition("SELECT [Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted], [IsActive] FROM [SecretHeaders] WHERE [Deleted] = 0", cancellationToken: cancellationToken))).ToList();

        foreach (var header in headers)
        {
            var secrets = (await conn.QueryAsync<Secret>(
                new CommandDefinition("SELECT [Id], [HeaderId], [Value], [ValueType], [CreatedUtc], [IsActive], [Deleted] FROM [Secrets] WHERE [HeaderId] = @Id ORDER BY [CreatedUtc] DESC", new { header.Id }, cancellationToken: cancellationToken))).ToList();

            foreach (var secret in secrets)
            {
                secret.Applications = (await conn.QueryAsync<string>(
                    new CommandDefinition("SELECT [ApplicationId] FROM [SecretApplications] WHERE [SecretId] = @SecretId", new { SecretId = secret.Id }, cancellationToken: cancellationToken))).ToList();
                secret.Environments = (await conn.QueryAsync<string>(
                    new CommandDefinition("SELECT [EnvironmentId] FROM [SecretEnvironments] WHERE [SecretId] = @SecretId", new { SecretId = secret.Id }, cancellationToken: cancellationToken))).ToList();
            }

            header.Secrets = secrets;
            EncryptionHandler.Decrypt(header.Secrets, _encryptionSettings.JsonEncryptionKey);
        }

        return headers;
    }

    public async Task<SecretHeader> GetSecret(string id, CancellationToken cancellationToken, bool includeHistory = true)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var header = await conn.QueryFirstOrDefaultAsync<SecretHeader>(
            new CommandDefinition("SELECT [Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted], [IsActive] FROM [SecretHeaders] WHERE [Id] = @id", new { id }, cancellationToken: cancellationToken));
        if (header == null) throw new KeyNotFoundException($"Secret header not found: {id}");

        var secrets = (await conn.QueryAsync<Secret>(
            new CommandDefinition("SELECT [Id], [HeaderId], [Value], [ValueType], [CreatedUtc], [IsActive], [Deleted] FROM [Secrets] WHERE [HeaderId] = @id ORDER BY [CreatedUtc] DESC", new { id }, cancellationToken: cancellationToken))).ToList();

        foreach (var secret in secrets)
        {
            secret.Applications = (await conn.QueryAsync<string>(
                new CommandDefinition("SELECT [ApplicationId] FROM [SecretApplications] WHERE [SecretId] = @SecretId", new { SecretId = secret.Id }, cancellationToken: cancellationToken))).ToList();
            secret.Environments = (await conn.QueryAsync<string>(
                new CommandDefinition("SELECT [EnvironmentId] FROM [SecretEnvironments] WHERE [SecretId] = @SecretId", new { SecretId = secret.Id }, cancellationToken: cancellationToken))).ToList();
        }

        header.Secrets = secrets;
        EncryptionHandler.Decrypt(header.Secrets, _encryptionSettings.JsonEncryptionKey);
        return header;
    }
}
