using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using NSubstitute;
using NUnit.Framework;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DataProvider.SqlServer;
using pote.Config.DbModel;
using pote.Config.Encryption;
using pote.Config.Shared;
using Environment = pote.Config.DbModel.Environment;

namespace pote.Config.UnitTests.SqlServer;

[TestFixture]
public class SqlServerAdminDataProviderTests
{
    private const string EncryptionKey = "detteErEnVildtGodEncryptionKey11";

    private InMemorySqlConnectionFactory _factory = null!;
    private IApplicationDataAccess _applicationDataAccess = null!;
    private IEnvironmentDataAccess _environmentDataAccess = null!;
    private ISecretDataAccess _secretDataAccess = null!;
    private AdminDataProvider _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new InMemorySqlConnectionFactory();
        _applicationDataAccess = Substitute.For<IApplicationDataAccess>();
        _environmentDataAccess = Substitute.For<IEnvironmentDataAccess>();
        _secretDataAccess = Substitute.For<ISecretDataAccess>();

        _sut = new AdminDataProvider(
            _factory,
            _applicationDataAccess,
            _environmentDataAccess,
            _secretDataAccess,
            new EncryptionSettings { JsonEncryptionKey = EncryptionKey });
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    #region GetAllConfigurationHeaders

    [Test]
    public async Task GetAllConfigurationHeaders_ReturnsNonDeleted()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted]) VALUES ('h1', 'Config1', datetime('now'), datetime('now'), 0)");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted]) VALUES ('h2', 'Config2', datetime('now'), datetime('now'), 0)");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted]) VALUES ('h3', 'Deleted', datetime('now'), datetime('now'), 1)");

        var result = await _sut.GetAllConfigurationHeaders(CancellationToken.None);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(h => h.Name != "Deleted"));
    }

    [Test]
    public async Task GetAllConfigurationHeaders_Empty_ReturnsEmptyList()
    {
        var result = await _sut.GetAllConfigurationHeaders(CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region GetConfiguration (by id)

    [Test]
    public async Task GetConfiguration_ById_ReturnsHeaderWithConfigurations()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc]) VALUES ('h1', 'TestConfig', datetime('now'), datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [Configurations] ([Id], [HeaderId], [Json], [CreatedUtc]) VALUES ('c1', 'h1', '{\"key\":\"value\"}', datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationApplications] ([ConfigurationId], [ApplicationId]) VALUES ('c1', 'app1')");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationEnvironments] ([ConfigurationId], [EnvironmentId]) VALUES ('c1', 'env1')");

        var result = await _sut.GetConfiguration("h1", CancellationToken.None);

        Assert.AreEqual("h1", result.Id);
        Assert.AreEqual("TestConfig", result.Name);
        Assert.AreEqual(1, result.Configurations.Count);
        Assert.AreEqual("{\"key\":\"value\"}", result.Configurations[0].Json);
        Assert.Contains("app1", result.Configurations[0].Applications);
        Assert.Contains("env1", result.Configurations[0].Environments);
    }

    [Test]
    public async Task GetConfiguration_ById_DecryptsEncryptedJson()
    {
        var originalJson = "{\"secret\":\"data\"}";
        var encrypted = EncryptionHandler.Encrypt(originalJson, EncryptionKey);

        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc]) VALUES ('h1', 'Test', datetime('now'), datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [Configurations] ([Id], [HeaderId], [Json], [CreatedUtc], [IsJsonEncrypted]) VALUES ('c1', 'h1', @Json, datetime('now'), 1)", new { Json = encrypted });

        var result = await _sut.GetConfiguration("h1", CancellationToken.None);

        Assert.AreEqual(originalJson, result.Configurations[0].Json);
    }

    [Test]
    public void GetConfiguration_ById_NotFound_ThrowsKeyNotFoundException()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetConfiguration("nonexistent", CancellationToken.None));
    }

    #endregion

    #region InsertConfiguration

    [Test]
    public async Task InsertConfiguration_InsertsNewHeader()
    {
        var header = new ConfigurationHeader
        {
            Id = "h1",
            Name = "NewConfig",
            CreatedUtc = DateTime.UtcNow,
            UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", HeaderId = "h1", Json = "{\"a\":\"b\"}", Applications = new List<string> { "app1" }, Environments = new List<string> { "env1" } }
            }
        };

        await _sut.InsertConfiguration(header, CancellationToken.None);

        var conn = await _factory.CreateOpenConnection();
        var dbHeader = await conn.QueryFirstAsync("SELECT * FROM [ConfigurationHeaders] WHERE [Id] = 'h1'");
        Assert.AreEqual("NewConfig", (string)dbHeader.Name);

        var dbConfig = await conn.QueryFirstAsync("SELECT * FROM [Configurations] WHERE [Id] = 'c1'");
        Assert.IsNotNull(dbConfig);

        var apps = (await conn.QueryAsync<string>("SELECT [ApplicationId] FROM [ConfigurationApplications] WHERE [ConfigurationId] = 'c1'")).ToList();
        Assert.Contains("app1", apps);

        var envs = (await conn.QueryAsync<string>("SELECT [EnvironmentId] FROM [ConfigurationEnvironments] WHERE [ConfigurationId] = 'c1'")).ToList();
        Assert.Contains("env1", envs);
    }

    [Test]
    public async Task InsertConfiguration_UpdatesExistingHeader()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc]) VALUES ('h1', 'OldName', datetime('now'), datetime('now'))");

        var header = new ConfigurationHeader
        {
            Id = "h1",
            Name = "UpdatedName",
            CreatedUtc = DateTime.UtcNow,
            UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration>()
        };

        await _sut.InsertConfiguration(header, CancellationToken.None);

        var dbHeader = await conn.QueryFirstAsync("SELECT * FROM [ConfigurationHeaders] WHERE [Id] = 'h1'");
        Assert.AreEqual("UpdatedName", (string)dbHeader.Name);
    }

    [Test]
    public async Task InsertConfiguration_SetsCreatedUtcOnConfigurations()
    {
        var createdUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var header = new ConfigurationHeader
        {
            Id = "h1",
            Name = "Test",
            CreatedUtc = createdUtc,
            UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", HeaderId = "h1", Json = "{}" }
            }
        };

        await _sut.InsertConfiguration(header, CancellationToken.None);

        Assert.AreEqual(createdUtc, header.Configurations[0].CreatedUtc);
    }

    [Test]
    public async Task InsertConfiguration_EncryptsWhenHeaderIsJsonEncrypted()
    {
        var header = new ConfigurationHeader
        {
            Id = "h1",
            Name = "Test",
            IsJsonEncrypted = true,
            CreatedUtc = DateTime.UtcNow,
            UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", HeaderId = "h1", Json = "{\"key\":\"value\"}" }
            }
        };

        await _sut.InsertConfiguration(header, CancellationToken.None);

        Assert.IsTrue(header.Configurations[0].IsJsonEncrypted);

        // Verify stored JSON is not plaintext
        var conn = await _factory.CreateOpenConnection();
        var storedJson = await conn.QueryFirstAsync<string>("SELECT [Json] FROM [Configurations] WHERE [Id] = 'c1'");
        Assert.AreNotEqual("{\"key\":\"value\"}", storedJson);
    }

    [Test]
    public async Task InsertConfiguration_EncryptsWhenSettingsEncryptAllJson()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Settings] ([Id], [EncryptAllJson]) VALUES (1, 1)");

        var header = new ConfigurationHeader
        {
            Id = "h1",
            Name = "Test",
            CreatedUtc = DateTime.UtcNow,
            UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", HeaderId = "h1", Json = "{\"key\":\"value\"}" }
            }
        };

        await _sut.InsertConfiguration(header, CancellationToken.None);

        Assert.IsTrue(header.Configurations[0].IsJsonEncrypted);
    }

    #endregion

    #region DeleteConfiguration

    [Test]
    public async Task DeleteConfiguration_Permanent_DeletesAllRelatedRows()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc]) VALUES ('h1', 'Test', datetime('now'), datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [Configurations] ([Id], [HeaderId], [Json], [CreatedUtc]) VALUES ('c1', 'h1', '{}', datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationApplications] ([ConfigurationId], [ApplicationId]) VALUES ('c1', 'app1')");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationEnvironments] ([ConfigurationId], [EnvironmentId]) VALUES ('c1', 'env1')");

        _sut.DeleteConfiguration("h1", true);

        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ConfigurationHeaders] WHERE [Id] = 'h1'"));
        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Configurations] WHERE [HeaderId] = 'h1'"));
        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ConfigurationApplications] WHERE [ConfigurationId] = 'c1'"));
        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ConfigurationEnvironments] WHERE [ConfigurationId] = 'c1'"));
    }

    [Test]
    public async Task DeleteConfiguration_NonPermanent_SoftDeletes()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted]) VALUES ('h1', 'Test', datetime('now'), datetime('now'), 0)");

        _sut.DeleteConfiguration("h1", false);

        var deleted = await conn.ExecuteScalarAsync<int>("SELECT [Deleted] FROM [ConfigurationHeaders] WHERE [Id] = 'h1'");
        Assert.AreEqual(1, deleted);
    }

    #endregion

    #region GetHeaderHistory / GetConfigurationHistory

    [Test]
    public async Task GetHeaderHistory_NoHistory_ReturnsEmptyList()
    {
        var result = await _sut.GetHeaderHistory("h1", 1, 10, CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetHeaderHistory_SnapshotCreatedOnInsert()
    {
        // First insert creates the header (no snapshot yet since it's new)
        var header = new ConfigurationHeader
        {
            Id = "h1", Name = "V1", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{\"v\":1}" } }
        };
        await _sut.InsertConfiguration(header, CancellationToken.None);

        // Second insert triggers snapshot of V1 state
        var header2 = new ConfigurationHeader
        {
            Id = "h1", Name = "V2", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", HeaderId = "h1", Json = "{\"v\":1}" },
                new() { Id = "c2", HeaderId = "h1", Json = "{\"v\":2}" }
            }
        };
        await _sut.InsertConfiguration(header2, CancellationToken.None);

        var history = await _sut.GetHeaderHistory("h1", 1, 10, CancellationToken.None);

        Assert.AreEqual(1, history.Count);
        Assert.AreEqual("V1", history[0].Name);
        Assert.AreEqual(1, history[0].Configurations.Count);
    }

    [Test]
    public async Task GetHeaderHistory_MultipleSnapshots_ReturnedNewestFirst()
    {
        // Insert V1
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V1", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{\"v\":1}" } }
        }, CancellationToken.None);

        // Insert V2 (snapshots V1)
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V2", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{\"v\":2}" } }
        }, CancellationToken.None);

        // Insert V3 (snapshots V2)
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V3", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{\"v\":3}" } }
        }, CancellationToken.None);

        var history = await _sut.GetHeaderHistory("h1", 1, 10, CancellationToken.None);

        Assert.AreEqual(2, history.Count);
        // Newest snapshot first (V2), then V1
        Assert.AreEqual("V2", history[0].Name);
        Assert.AreEqual("V1", history[1].Name);
    }

    [Test]
    public async Task GetHeaderHistory_Pagination_FirstPage()
    {
        // Create 3 snapshots by inserting 4 times
        for (var i = 1; i <= 4; i++)
        {
            await _sut.InsertConfiguration(new ConfigurationHeader
            {
                Id = "h1", Name = $"V{i}", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
                Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = $"{{\"v\":{i}}}" } }
            }, CancellationToken.None);
        }

        // 3 snapshots total (V1, V2, V3), page 1 size 2 should return V3, V2
        var page1 = await _sut.GetHeaderHistory("h1", 1, 2, CancellationToken.None);
        Assert.AreEqual(2, page1.Count);
        Assert.AreEqual("V3", page1[0].Name);
        Assert.AreEqual("V2", page1[1].Name);
    }

    [Test]
    public async Task GetHeaderHistory_Pagination_SecondPage()
    {
        for (var i = 1; i <= 4; i++)
        {
            await _sut.InsertConfiguration(new ConfigurationHeader
            {
                Id = "h1", Name = $"V{i}", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
                Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = $"{{\"v\":{i}}}" } }
            }, CancellationToken.None);
        }

        var page2 = await _sut.GetHeaderHistory("h1", 2, 2, CancellationToken.None);
        Assert.AreEqual(1, page2.Count);
        Assert.AreEqual("V1", page2[0].Name);
    }

    [Test]
    public async Task GetHeaderHistory_PageBeyondData_ReturnsEmptyList()
    {
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V1", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{}" } }
        }, CancellationToken.None);
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V2", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{}" } }
        }, CancellationToken.None);

        var result = await _sut.GetHeaderHistory("h1", 5, 10, CancellationToken.None);
        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetHeaderHistory_PreservesJunctionTableData()
    {
        // Insert with junction data
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V1", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", HeaderId = "h1", Json = "{}", Applications = new List<string> { "app1", "app2" }, Environments = new List<string> { "env1" } }
            }
        }, CancellationToken.None);

        // Second insert to trigger snapshot
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V2", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{}" } }
        }, CancellationToken.None);

        var history = await _sut.GetHeaderHistory("h1", 1, 10, CancellationToken.None);
        var config = history[0].Configurations[0];
        Assert.AreEqual(2, config.Applications.Count);
        Assert.Contains("app1", config.Applications);
        Assert.Contains("app2", config.Applications);
        Assert.Contains("env1", config.Environments);
    }

    [Test]
    public async Task GetHeaderHistory_DecryptsEncryptedConfigurations()
    {
        // Insert with encryption enabled
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V1", IsJsonEncrypted = true, CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{\"secret\":\"data\"}" } }
        }, CancellationToken.None);

        // Second insert to create snapshot
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V2", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{}" } }
        }, CancellationToken.None);

        var history = await _sut.GetHeaderHistory("h1", 1, 10, CancellationToken.None);

        // The snapshot stored encrypted JSON; GetHeaderHistory should decrypt it
        Assert.AreEqual("{\"secret\":\"data\"}", history[0].Configurations[0].Json);
    }

    [Test]
    public async Task GetHeaderHistory_SnapshotCreatedOnSoftDelete()
    {
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V1", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{\"v\":1}" } }
        }, CancellationToken.None);

        _sut.DeleteConfiguration("h1", false);

        var history = await _sut.GetHeaderHistory("h1", 1, 10, CancellationToken.None);
        Assert.AreEqual(1, history.Count);
        Assert.AreEqual("V1", history[0].Name);
    }

    [Test]
    public async Task GetHeaderHistory_PermanentDelete_RemovesHistory()
    {
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V1", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{}" } }
        }, CancellationToken.None);
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V2", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{}" } }
        }, CancellationToken.None);

        _sut.DeleteConfiguration("h1", true);

        var conn = await _factory.CreateOpenConnection();
        var historyCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ConfigurationHeaderHistory] WHERE [HeaderId] = 'h1'");
        Assert.AreEqual(0, historyCount);
    }

    [Test]
    public async Task GetConfigurationHistory_ReturnsMatchingConfiguration()
    {
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V1", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", HeaderId = "h1", Json = "{\"v\":1}" },
                new() { Id = "c2", HeaderId = "h1", Json = "{\"v\":2}" }
            }
        }, CancellationToken.None);
        // Second insert to create snapshot
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V2", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{\"v\":3}" } }
        }, CancellationToken.None);

        var result = await _sut.GetConfigurationHistory("h1", "c1", 1, 10, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("c1", result[0].Id);
        Assert.AreEqual("{\"v\":1}", result[0].Json);
    }

    [Test]
    public async Task GetConfigurationHistory_NoMatch_ReturnsEmptyList()
    {
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V1", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{}" } }
        }, CancellationToken.None);
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V2", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{}" } }
        }, CancellationToken.None);

        var result = await _sut.GetConfigurationHistory("h1", "nonexistent", 1, 10, CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetConfigurationHistory_PreservesJunctionTableData()
    {
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V1", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", HeaderId = "h1", Json = "{}", Applications = new List<string> { "app1" }, Environments = new List<string> { "env1" } }
            }
        }, CancellationToken.None);
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V2", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{}" } }
        }, CancellationToken.None);

        var result = await _sut.GetConfigurationHistory("h1", "c1", 1, 10, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.Contains("app1", result[0].Applications);
        Assert.Contains("env1", result[0].Environments);
    }

    [Test]
    public async Task GetConfigurationHistory_DecryptsEncryptedJson()
    {
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V1", IsJsonEncrypted = true, CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{\"secret\":\"data\"}" } }
        }, CancellationToken.None);
        await _sut.InsertConfiguration(new ConfigurationHeader
        {
            Id = "h1", Name = "V2", CreatedUtc = DateTime.UtcNow, UpdateUtc = DateTime.UtcNow,
            Configurations = new List<Configuration> { new() { Id = "c1", HeaderId = "h1", Json = "{}" } }
        }, CancellationToken.None);

        var result = await _sut.GetConfigurationHistory("h1", "c1", 1, 10, CancellationToken.None);

        Assert.AreEqual("{\"secret\":\"data\"}", result[0].Json);
    }

    #endregion

    #region UpsertEnvironment / DeleteEnvironment

    [Test]
    public async Task UpsertEnvironment_InsertsNew()
    {
        var env = new Environment { Id = "e1", Name = "Staging" };

        await _sut.UpsertEnvironment(env, CancellationToken.None);

        var conn = await _factory.CreateOpenConnection();
        var name = await conn.ExecuteScalarAsync<string>("SELECT [Name] FROM [Environments] WHERE [Id] = 'e1'");
        Assert.AreEqual("Staging", name);
    }

    [Test]
    public async Task UpsertEnvironment_UpdatesExisting()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Environments] ([Id], [Name]) VALUES ('e1', 'OldName')");

        await _sut.UpsertEnvironment(new Environment { Id = "e1", Name = "NewName" }, CancellationToken.None);

        var name = await conn.ExecuteScalarAsync<string>("SELECT [Name] FROM [Environments] WHERE [Id] = 'e1'");
        Assert.AreEqual("NewName", name);
    }

    [Test]
    public async Task DeleteEnvironment_RemovesEnvironmentAndJoinRows()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Environments] ([Id], [Name]) VALUES ('e1', 'Dev')");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationEnvironments] ([ConfigurationId], [EnvironmentId]) VALUES ('c1', 'e1')");
        await conn.ExecuteAsync("INSERT INTO [SecretEnvironments] ([SecretId], [EnvironmentId]) VALUES ('s1', 'e1')");

        await _sut.DeleteEnvironment("e1", CancellationToken.None);

        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Environments] WHERE [Id] = 'e1'"));
        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ConfigurationEnvironments] WHERE [EnvironmentId] = 'e1'"));
        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [SecretEnvironments] WHERE [EnvironmentId] = 'e1'"));
    }

    #endregion

    #region UpsertApplication / DeleteApplication

    [Test]
    public async Task UpsertApplication_InsertsNew()
    {
        var app = new Application { Id = "a1", Name = "MyApp" };

        await _sut.UpsertApplication(app, CancellationToken.None);

        var conn = await _factory.CreateOpenConnection();
        var name = await conn.ExecuteScalarAsync<string>("SELECT [Name] FROM [Applications] WHERE [Id] = 'a1'");
        Assert.AreEqual("MyApp", name);
    }

    [Test]
    public async Task UpsertApplication_UpdatesExisting()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Applications] ([Id], [Name]) VALUES ('a1', 'OldName')");

        await _sut.UpsertApplication(new Application { Id = "a1", Name = "NewName" }, CancellationToken.None);

        var name = await conn.ExecuteScalarAsync<string>("SELECT [Name] FROM [Applications] WHERE [Id] = 'a1'");
        Assert.AreEqual("NewName", name);
    }

    [Test]
    public async Task DeleteApplication_RemovesApplicationAndJoinRows()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Applications] ([Id], [Name]) VALUES ('a1', 'MyApp')");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationApplications] ([ConfigurationId], [ApplicationId]) VALUES ('c1', 'a1')");
        await conn.ExecuteAsync("INSERT INTO [SecretApplications] ([SecretId], [ApplicationId]) VALUES ('s1', 'a1')");

        await _sut.DeleteApplication("a1", CancellationToken.None);

        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Applications] WHERE [Id] = 'a1'"));
        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ConfigurationApplications] WHERE [ApplicationId] = 'a1'"));
        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [SecretApplications] WHERE [ApplicationId] = 'a1'"));
    }

    #endregion

    #region GetApiKeys / SaveApiKeys

    [Test]
    public async Task GetApiKeys_ReturnsAllKeys()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ApiKeys] ([Name], [Key]) VALUES ('Prod', 'key1')");
        await conn.ExecuteAsync("INSERT INTO [ApiKeys] ([Name], [Key]) VALUES ('Dev', 'key2')");

        var result = await _sut.GetApiKeys(CancellationToken.None);

        Assert.AreEqual(2, result.Keys.Count);
    }

    [Test]
    public async Task SaveApiKeys_ReplacesAllKeys()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ApiKeys] ([Name], [Key]) VALUES ('Old', 'old-key')");

        var apiKeys = new ApiKeys
        {
            Keys = new List<ApiKeyEntry>
            {
                new() { Name = "New1", Key = "new-key-1" },
                new() { Name = "New2", Key = "new-key-2" }
            }
        };

        await _sut.SaveApiKeys(apiKeys, CancellationToken.None);

        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [ApiKeys]");
        Assert.AreEqual(2, count);

        var keys = (await conn.QueryAsync<string>("SELECT [Key] FROM [ApiKeys]")).ToList();
        Assert.Contains("new-key-1", keys);
        Assert.Contains("new-key-2", keys);
        Assert.IsFalse(keys.Contains("old-key"));
    }

    #endregion

    #region GetSettings / SaveSettings

    [Test]
    public async Task GetSettings_NoRow_ReturnsDefault()
    {
        var result = await _sut.GetSettings(CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.EncryptAllJson);
    }

    [Test]
    public async Task GetSettings_ExistingRow_ReturnsSettings()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Settings] ([Id], [EncryptAllJson]) VALUES (1, 1)");

        var result = await _sut.GetSettings(CancellationToken.None);

        Assert.IsTrue(result.EncryptAllJson);
    }

    [Test]
    public async Task SaveSettings_InsertsNewRow()
    {
        await _sut.SaveSettings(new Settings { EncryptAllJson = true }, CancellationToken.None);

        var conn = await _factory.CreateOpenConnection();
        var val = await conn.ExecuteScalarAsync<int>("SELECT [EncryptAllJson] FROM [Settings] WHERE [Id] = 1");
        Assert.AreEqual(1, val);
    }

    [Test]
    public async Task SaveSettings_UpdatesExistingRow()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Settings] ([Id], [EncryptAllJson]) VALUES (1, 0)");

        await _sut.SaveSettings(new Settings { EncryptAllJson = true }, CancellationToken.None);

        var val = await conn.ExecuteScalarAsync<int>("SELECT [EncryptAllJson] FROM [Settings] WHERE [Id] = 1");
        Assert.AreEqual(1, val);
    }

    #endregion

    #region InsertSecret / DeleteSecret

    [Test]
    public async Task InsertSecret_InsertsHeaderAndSecrets()
    {
        var header = new SecretHeader
        {
            Id = "sh1",
            Name = "MySecret",
            CreatedUtc = DateTime.UtcNow,
            UpdateUtc = DateTime.UtcNow,
            Secrets = new List<Secret>
            {
                new() { Id = "s1", HeaderId = "sh1", Value = "secretvalue", Applications = new List<string> { "app1" }, Environments = new List<string> { "env1" } }
            }
        };

        await _sut.InsertSecret(header, CancellationToken.None);

        var conn = await _factory.CreateOpenConnection();
        var dbHeader = await conn.QueryFirstAsync("SELECT * FROM [SecretHeaders] WHERE [Id] = 'sh1'");
        Assert.AreEqual("MySecret", (string)dbHeader.Name);

        Assert.AreEqual(1, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Secrets] WHERE [HeaderId] = 'sh1'"));
        Assert.AreEqual(1, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [SecretApplications] WHERE [SecretId] = 's1'"));
        Assert.AreEqual(1, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [SecretEnvironments] WHERE [SecretId] = 's1'"));
    }

    [Test]
    public async Task InsertSecret_EncryptsSecretValues()
    {
        var header = new SecretHeader
        {
            Id = "sh1",
            Name = "MySecret",
            CreatedUtc = DateTime.UtcNow,
            UpdateUtc = DateTime.UtcNow,
            Secrets = new List<Secret>
            {
                new() { Id = "s1", HeaderId = "sh1", Value = "plaintext" }
            }
        };

        await _sut.InsertSecret(header, CancellationToken.None);

        var conn = await _factory.CreateOpenConnection();
        var storedValue = await conn.ExecuteScalarAsync<string>("SELECT [Value] FROM [Secrets] WHERE [Id] = 's1'");
        Assert.AreNotEqual("plaintext", storedValue);
    }

    [Test]
    public async Task InsertSecret_SetsCreatedUtcOnSecrets()
    {
        var createdUtc = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var header = new SecretHeader
        {
            Id = "sh1",
            Name = "Test",
            CreatedUtc = createdUtc,
            UpdateUtc = DateTime.UtcNow,
            Secrets = new List<Secret>
            {
                new() { Id = "s1", HeaderId = "sh1", Value = "val" }
            }
        };

        await _sut.InsertSecret(header, CancellationToken.None);

        Assert.AreEqual(createdUtc, header.Secrets[0].CreatedUtc);
    }

    [Test]
    public async Task DeleteSecret_RemovesAllRelatedRows()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [SecretHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc]) VALUES ('sh1', 'Test', datetime('now'), datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [Secrets] ([Id], [HeaderId], [Value], [CreatedUtc]) VALUES ('s1', 'sh1', 'val', datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [SecretApplications] ([SecretId], [ApplicationId]) VALUES ('s1', 'app1')");
        await conn.ExecuteAsync("INSERT INTO [SecretEnvironments] ([SecretId], [EnvironmentId]) VALUES ('s1', 'env1')");

        await _sut.DeleteSecret("sh1", CancellationToken.None);

        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [SecretHeaders] WHERE [Id] = 'sh1'"));
        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Secrets] WHERE [HeaderId] = 'sh1'"));
        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [SecretApplications] WHERE [SecretId] = 's1'"));
        Assert.AreEqual(0, await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [SecretEnvironments] WHERE [SecretId] = 's1'"));
    }

    #endregion

    #region GetAllSecretHeaders / GetSecret

    [Test]
    public async Task GetAllSecretHeaders_ReturnsNonDeleted()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [SecretHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted]) VALUES ('sh1', 'Secret1', datetime('now'), datetime('now'), 0)");
        await conn.ExecuteAsync("INSERT INTO [SecretHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted]) VALUES ('sh2', 'Deleted', datetime('now'), datetime('now'), 1)");

        var result = await _sut.GetAllSecretHeaders(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Secret1", result[0].Name);
    }

    [Test]
    public async Task GetSecret_ById_ReturnsHeaderWithDecryptedSecrets()
    {
        var encrypted = EncryptionHandler.Encrypt("mysecret", EncryptionKey);

        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [SecretHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc]) VALUES ('sh1', 'MySecret', datetime('now'), datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [Secrets] ([Id], [HeaderId], [Value], [CreatedUtc]) VALUES ('s1', 'sh1', @Value, datetime('now'))", new { Value = encrypted });
        await conn.ExecuteAsync("INSERT INTO [SecretApplications] ([SecretId], [ApplicationId]) VALUES ('s1', 'app1')");
        await conn.ExecuteAsync("INSERT INTO [SecretEnvironments] ([SecretId], [EnvironmentId]) VALUES ('s1', 'env1')");

        var result = await _sut.GetSecret("sh1", CancellationToken.None);

        Assert.AreEqual("sh1", result.Id);
        Assert.AreEqual("MySecret", result.Name);
        Assert.AreEqual(1, result.Secrets.Count);
        Assert.AreEqual("mysecret", result.Secrets[0].Value);
        Assert.Contains("app1", result.Secrets[0].Applications);
        Assert.Contains("env1", result.Secrets[0].Environments);
    }

    [Test]
    public void GetSecret_ById_NotFound_ThrowsKeyNotFoundException()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetSecret("nonexistent", CancellationToken.None));
    }

    #endregion

    #region Delegation tests

    [Test]
    public async Task GetApplications_DelegatesToApplicationDataAccess()
    {
        var apps = new List<Application> { new() { Id = "a1", Name = "App1" } };
        _applicationDataAccess.GetApplications(Arg.Any<CancellationToken>()).Returns(apps);

        var result = await _sut.GetApplications(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
    }

    [Test]
    public async Task GetApplication_DelegatesToApplicationDataAccess()
    {
        _applicationDataAccess.GetApplication("a1", Arg.Any<CancellationToken>()).Returns(new Application { Id = "a1" });

        var result = await _sut.GetApplication("a1", CancellationToken.None);

        Assert.AreEqual("a1", result.Id);
    }

    [Test]
    public async Task GetEnvironments_DelegatesToEnvironmentDataAccess()
    {
        _environmentDataAccess.GetEnvironments(Arg.Any<CancellationToken>()).Returns(new List<Environment> { new() { Id = "e1" } });

        var result = await _sut.GetEnvironments(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
    }

    [Test]
    public async Task GetEnvironment_DelegatesToEnvironmentDataAccess()
    {
        _environmentDataAccess.GetEnvironment("e1", Arg.Any<CancellationToken>()).Returns(new Environment { Id = "e1" });

        var result = await _sut.GetEnvironment("e1", CancellationToken.None);

        Assert.AreEqual("e1", result.Id);
    }

    #endregion
}
