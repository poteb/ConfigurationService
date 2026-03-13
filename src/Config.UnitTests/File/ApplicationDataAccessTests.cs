using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using pote.Config.DataProvider.File;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.UnitTests;

[TestFixture]
public class ApplicationDataAccessTests
{
    private IFileHandler _fileHandler = null!;
    private ApplicationDataAccess _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fileHandler = Substitute.For<IFileHandler>();
        _sut = new ApplicationDataAccess(_fileHandler);
    }

    [Test]
    public async Task GetApplications_ReturnsAllApplications()
    {
        _fileHandler.GetApplicationFiles().Returns(new[] { "file1", "file2" });
        _fileHandler.GetApplicationContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"app1\",\"Name\":\"WebApp\"}");
        _fileHandler.GetApplicationContentAbsolutePath("file2", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"app2\",\"Name\":\"ApiApp\"}");

        var result = await _sut.GetApplications(CancellationToken.None);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("app1", result[0].Id);
        Assert.AreEqual("WebApp", result[0].Name);
        Assert.AreEqual("app2", result[1].Id);
        Assert.AreEqual("ApiApp", result[1].Name);
    }

    [Test]
    public async Task GetApplications_EmptyFiles_ReturnsEmptyList()
    {
        _fileHandler.GetApplicationFiles().Returns(Array.Empty<string>());

        var result = await _sut.GetApplications(CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetApplications_SkipsNullDeserialization()
    {
        _fileHandler.GetApplicationFiles().Returns(new[] { "file1", "file2" });
        _fileHandler.GetApplicationContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("null");
        _fileHandler.GetApplicationContentAbsolutePath("file2", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"app2\",\"Name\":\"ApiApp\"}");

        var result = await _sut.GetApplications(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("app2", result[0].Id);
    }

    [Test]
    public async Task GetApplications_SkipsBadJson()
    {
        _fileHandler.GetApplicationFiles().Returns(new[] { "file1", "file2" });
        _fileHandler.GetApplicationContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("bad json {{{");
        _fileHandler.GetApplicationContentAbsolutePath("file2", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"app2\",\"Name\":\"ApiApp\"}");

        var result = await _sut.GetApplications(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("app2", result[0].Id);
    }

    [Test]
    public async Task GetApplication_ById_ReturnsMatch()
    {
        _fileHandler.GetApplicationFiles().Returns(new[] { "file1", "file2" });
        _fileHandler.GetApplicationContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"app1\",\"Name\":\"WebApp\"}");
        _fileHandler.GetApplicationContentAbsolutePath("file2", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"app2\",\"Name\":\"ApiApp\"}");

        var result = await _sut.GetApplication("app2", CancellationToken.None);

        Assert.AreEqual("app2", result.Id);
        Assert.AreEqual("ApiApp", result.Name);
    }

    [Test]
    public async Task GetApplication_ByName_CaseInsensitive()
    {
        _fileHandler.GetApplicationFiles().Returns(new[] { "file1" });
        _fileHandler.GetApplicationContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"app1\",\"Name\":\"WebApp\"}");

        var result = await _sut.GetApplication("webapp", CancellationToken.None);

        Assert.AreEqual("app1", result.Id);
    }

    [Test]
    public void GetApplication_NotFound_ThrowsKeyNotFoundException()
    {
        _fileHandler.GetApplicationFiles().Returns(new[] { "file1" });
        _fileHandler.GetApplicationContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"app1\",\"Name\":\"WebApp\"}");

        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetApplication("nonexistent", CancellationToken.None));
    }

    [Test]
    public void GetApplication_NoFiles_ThrowsKeyNotFoundException()
    {
        _fileHandler.GetApplicationFiles().Returns(Array.Empty<string>());

        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetApplication("anything", CancellationToken.None));
    }
}
