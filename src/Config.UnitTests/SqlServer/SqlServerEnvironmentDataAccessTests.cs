using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;
using pote.Config.DataProvider.SqlServer;

namespace pote.Config.UnitTests.SqlServer;

[TestFixture]
public class SqlServerEnvironmentDataAccessTests
{
    private InMemorySqlConnectionFactory _factory = null!;
    private EnvironmentDataAccess _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new InMemorySqlConnectionFactory();
        _sut = new EnvironmentDataAccess(_factory);
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task GetEnvironments_ReturnsAll()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Environments] ([Id], [Name]) VALUES ('e1', 'Dev')");
        await conn.ExecuteAsync("INSERT INTO [Environments] ([Id], [Name]) VALUES ('e2', 'Prod')");

        var result = await _sut.GetEnvironments(CancellationToken.None);

        Assert.AreEqual(2, result.Count);
    }

    [Test]
    public async Task GetEnvironments_Empty_ReturnsEmptyList()
    {
        var result = await _sut.GetEnvironments(CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetEnvironment_ById_ReturnsMatch()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Environments] ([Id], [Name]) VALUES ('e1', 'Dev')");
        await conn.ExecuteAsync("INSERT INTO [Environments] ([Id], [Name]) VALUES ('e2', 'Prod')");

        var result = await _sut.GetEnvironment("e2", CancellationToken.None);

        Assert.AreEqual("e2", result.Id);
        Assert.AreEqual("Prod", result.Name);
    }

    [Test]
    public async Task GetEnvironment_ByName_ReturnsMatch()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Environments] ([Id], [Name]) VALUES ('e1', 'Development')");

        var result = await _sut.GetEnvironment("Development", CancellationToken.None);

        Assert.AreEqual("e1", result.Id);
    }

    [Test]
    public void GetEnvironment_NotFound_ThrowsKeyNotFoundException()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetEnvironment("nonexistent", CancellationToken.None));
    }
}
