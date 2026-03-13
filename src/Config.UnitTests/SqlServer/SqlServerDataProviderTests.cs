using System.Collections.Generic;
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
public class SqlServerDataProviderTests
{
    private const string EncryptionKey = "detteErEnVildtGodEncryptionKey11";

    private InMemorySqlConnectionFactory _factory = null!;
    private IEnvironmentDataAccess _environmentDataAccess = null!;
    private IApplicationDataAccess _applicationDataAccess = null!;
    private ISecretDataAccess _secretDataAccess = null!;
    private pote.Config.DataProvider.SqlServer.DataProvider _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new InMemorySqlConnectionFactory();
        _environmentDataAccess = Substitute.For<IEnvironmentDataAccess>();
        _applicationDataAccess = Substitute.For<IApplicationDataAccess>();
        _secretDataAccess = Substitute.For<ISecretDataAccess>();

        _sut = new pote.Config.DataProvider.SqlServer.DataProvider(
            _factory,
            _environmentDataAccess,
            _applicationDataAccess,
            _secretDataAccess,
            new EncryptionSettings { JsonEncryptionKey = EncryptionKey });
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    #region GetConfiguration

    [Test]
    public async Task GetConfiguration_MatchingRecord_ReturnsConfiguration()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc]) VALUES ('h1', 'MyConfig', datetime('now'), datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [Configurations] ([Id], [HeaderId], [Json], [CreatedUtc], [IsActive], [Deleted]) VALUES ('c1', 'h1', '{\"key\":\"value\"}', datetime('now'), 1, 0)");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationApplications] ([ConfigurationId], [ApplicationId]) VALUES ('c1', 'app1')");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationEnvironments] ([ConfigurationId], [EnvironmentId]) VALUES ('c1', 'env1')");

        var result = await _sut.GetConfiguration("MyConfig", "app1", "env1", CancellationToken.None);

        Assert.AreEqual("c1", result.Id);
        Assert.AreEqual("{\"key\":\"value\"}", result.Json);
        Assert.Contains("app1", result.Applications);
        Assert.Contains("env1", result.Environments);
    }

    [Test]
    public async Task GetConfiguration_NoMatch_ReturnsEmptyId()
    {
        var result = await _sut.GetConfiguration("NonExistent", "app1", "env1", CancellationToken.None);

        Assert.AreEqual(string.Empty, result.Id);
    }

    [Test]
    public async Task GetConfiguration_DeletedRecord_ReturnsEmptyId()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc], [Deleted]) VALUES ('h1', 'MyConfig', datetime('now'), datetime('now'), 1)");
        await conn.ExecuteAsync("INSERT INTO [Configurations] ([Id], [HeaderId], [Json], [CreatedUtc], [IsActive], [Deleted]) VALUES ('c1', 'h1', '{\"key\":\"value\"}', datetime('now'), 1, 0)");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationApplications] ([ConfigurationId], [ApplicationId]) VALUES ('c1', 'app1')");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationEnvironments] ([ConfigurationId], [EnvironmentId]) VALUES ('c1', 'env1')");

        var result = await _sut.GetConfiguration("MyConfig", "app1", "env1", CancellationToken.None);

        Assert.AreEqual(string.Empty, result.Id);
    }

    [Test]
    public async Task GetConfiguration_InactiveRecord_ReturnsEmptyId()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc]) VALUES ('h1', 'MyConfig', datetime('now'), datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [Configurations] ([Id], [HeaderId], [Json], [CreatedUtc], [IsActive], [Deleted]) VALUES ('c1', 'h1', '{\"key\":\"value\"}', datetime('now'), 0, 0)");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationApplications] ([ConfigurationId], [ApplicationId]) VALUES ('c1', 'app1')");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationEnvironments] ([ConfigurationId], [EnvironmentId]) VALUES ('c1', 'env1')");

        var result = await _sut.GetConfiguration("MyConfig", "app1", "env1", CancellationToken.None);

        Assert.AreEqual(string.Empty, result.Id);
    }

    [Test]
    public async Task GetConfiguration_WrongApplication_ReturnsEmptyId()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [ConfigurationHeaders] ([Id], [Name], [CreatedUtc], [UpdateUtc]) VALUES ('h1', 'MyConfig', datetime('now'), datetime('now'))");
        await conn.ExecuteAsync("INSERT INTO [Configurations] ([Id], [HeaderId], [Json], [CreatedUtc], [IsActive], [Deleted]) VALUES ('c1', 'h1', '{}', datetime('now'), 1, 0)");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationApplications] ([ConfigurationId], [ApplicationId]) VALUES ('c1', 'app1')");
        await conn.ExecuteAsync("INSERT INTO [ConfigurationEnvironments] ([ConfigurationId], [EnvironmentId]) VALUES ('c1', 'env1')");

        var result = await _sut.GetConfiguration("MyConfig", "wrong-app", "env1", CancellationToken.None);

        Assert.AreEqual(string.Empty, result.Id);
    }

    #endregion

    #region GetApiKeys

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
    public async Task GetApiKeys_Empty_ReturnsEmptyApiKeys()
    {
        var result = await _sut.GetApiKeys(CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Keys.Count);
    }

    #endregion

    #region GetSecretValue

    [Test]
    public async Task GetSecretValue_DelegatesToSecretDataAccess()
    {
        var encryptedValue = EncryptionHandler.Encrypt("mysecret", EncryptionKey);
        var secret = new Secret { Id = "s1", Value = encryptedValue };
        _secretDataAccess.GetSecret("SecretName", "app1", "env1", Arg.Any<CancellationToken>())
            .Returns(secret);

        var result = await _sut.GetSecretValue("SecretName", "app1", "env1", CancellationToken.None);

        Assert.AreEqual("mysecret", result);
    }

    [Test]
    public async Task GetSecretValue_NotFound_ReturnsEmpty()
    {
        _secretDataAccess.GetSecret("Missing", "app1", "env1", Arg.Any<CancellationToken>())
            .Returns(new Secret { Id = string.Empty });

        var result = await _sut.GetSecretValue("Missing", "app1", "env1", CancellationToken.None);

        Assert.AreEqual(string.Empty, result);
    }

    [Test]
    public async Task GetSecretValue_EmptyValue_ReturnsEmpty()
    {
        _secretDataAccess.GetSecret("Empty", "app1", "env1", Arg.Any<CancellationToken>())
            .Returns(new Secret { Id = "s1", Value = "" });

        var result = await _sut.GetSecretValue("Empty", "app1", "env1", CancellationToken.None);

        Assert.AreEqual(string.Empty, result);
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
        Assert.AreEqual("App1", result[0].Name);
    }

    [Test]
    public async Task GetApplication_DelegatesToApplicationDataAccess()
    {
        var app = new Application { Id = "a1", Name = "App1" };
        _applicationDataAccess.GetApplication("a1", Arg.Any<CancellationToken>()).Returns(app);

        var result = await _sut.GetApplication("a1", CancellationToken.None);

        Assert.AreEqual("a1", result.Id);
    }

    [Test]
    public async Task GetEnvironments_DelegatesToEnvironmentDataAccess()
    {
        var envs = new List<Environment> { new() { Id = "e1", Name = "Dev" } };
        _environmentDataAccess.GetEnvironments(Arg.Any<CancellationToken>()).Returns(envs);

        var result = await _sut.GetEnvironments(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Dev", result[0].Name);
    }

    [Test]
    public async Task GetEnvironment_DelegatesToEnvironmentDataAccess()
    {
        var env = new Environment { Id = "e1", Name = "Dev" };
        _environmentDataAccess.GetEnvironment("e1", Arg.Any<CancellationToken>()).Returns(env);

        var result = await _sut.GetEnvironment("e1", CancellationToken.None);

        Assert.AreEqual("e1", result.Id);
    }

    #endregion
}
