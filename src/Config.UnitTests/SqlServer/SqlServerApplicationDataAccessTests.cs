using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;
using pote.Config.DataProvider.SqlServer;

namespace pote.Config.UnitTests.SqlServer;

[TestFixture]
public class SqlServerApplicationDataAccessTests
{
    private InMemorySqlConnectionFactory _factory = null!;
    private ApplicationDataAccess _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new InMemorySqlConnectionFactory();
        _sut = new ApplicationDataAccess(_factory);
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task GetApplications_ReturnsAll()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Applications] ([Id], [Name]) VALUES ('a1', 'WebApp')");
        await conn.ExecuteAsync("INSERT INTO [Applications] ([Id], [Name]) VALUES ('a2', 'ApiApp')");

        var result = await _sut.GetApplications(CancellationToken.None);

        Assert.AreEqual(2, result.Count);
    }

    [Test]
    public async Task GetApplications_Empty_ReturnsEmptyList()
    {
        var result = await _sut.GetApplications(CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetApplication_ById_ReturnsMatch()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Applications] ([Id], [Name]) VALUES ('a1', 'WebApp')");
        await conn.ExecuteAsync("INSERT INTO [Applications] ([Id], [Name]) VALUES ('a2', 'ApiApp')");

        var result = await _sut.GetApplication("a2", CancellationToken.None);

        Assert.AreEqual("a2", result.Id);
        Assert.AreEqual("ApiApp", result.Name);
    }

    [Test]
    public async Task GetApplication_ByName_ReturnsMatch()
    {
        var conn = await _factory.CreateOpenConnection();
        await conn.ExecuteAsync("INSERT INTO [Applications] ([Id], [Name]) VALUES ('a1', 'WebApp')");

        var result = await _sut.GetApplication("WebApp", CancellationToken.None);

        Assert.AreEqual("a1", result.Id);
    }

    [Test]
    public void GetApplication_NotFound_ThrowsKeyNotFoundException()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetApplication("nonexistent", CancellationToken.None));
    }
}
