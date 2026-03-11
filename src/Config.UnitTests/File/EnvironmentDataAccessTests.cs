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
public class EnvironmentDataAccessTests
{
    private IFileHandler _fileHandler = null!;
    private EnvironmentDataAccess _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fileHandler = Substitute.For<IFileHandler>();
        _sut = new EnvironmentDataAccess(_fileHandler);
    }

    [Test]
    public async Task GetEnvironments_ReturnsAllEnvironments()
    {
        _fileHandler.GetEnvironmentFiles().Returns(new[] { "file1", "file2" });
        _fileHandler.GetEnvironmentContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"id1\",\"Name\":\"Dev\"}");
        _fileHandler.GetEnvironmentContentAbsolutePath("file2", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"id2\",\"Name\":\"Prod\"}");

        var result = await _sut.GetEnvironments(CancellationToken.None);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("id1", result[0].Id);
        Assert.AreEqual("Dev", result[0].Name);
        Assert.AreEqual("id2", result[1].Id);
        Assert.AreEqual("Prod", result[1].Name);
    }

    [Test]
    public async Task GetEnvironments_EmptyFiles_ReturnsEmptyList()
    {
        _fileHandler.GetEnvironmentFiles().Returns(Array.Empty<string>());

        var result = await _sut.GetEnvironments(CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetEnvironments_SkipsNullDeserialization()
    {
        _fileHandler.GetEnvironmentFiles().Returns(new[] { "file1", "file2" });
        _fileHandler.GetEnvironmentContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("null");
        _fileHandler.GetEnvironmentContentAbsolutePath("file2", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"id2\",\"Name\":\"Prod\"}");

        var result = await _sut.GetEnvironments(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("id2", result[0].Id);
    }

    [Test]
    public async Task GetEnvironments_SkipsBadJson()
    {
        _fileHandler.GetEnvironmentFiles().Returns(new[] { "file1", "file2" });
        _fileHandler.GetEnvironmentContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("not valid json {{{");
        _fileHandler.GetEnvironmentContentAbsolutePath("file2", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"id2\",\"Name\":\"Prod\"}");

        var result = await _sut.GetEnvironments(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("id2", result[0].Id);
    }

    [Test]
    public async Task GetEnvironment_ById_ReturnsMatch()
    {
        _fileHandler.GetEnvironmentFiles().Returns(new[] { "file1", "file2" });
        _fileHandler.GetEnvironmentContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"id1\",\"Name\":\"Dev\"}");
        _fileHandler.GetEnvironmentContentAbsolutePath("file2", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"id2\",\"Name\":\"Prod\"}");

        var result = await _sut.GetEnvironment("id2", CancellationToken.None);

        Assert.AreEqual("id2", result.Id);
        Assert.AreEqual("Prod", result.Name);
    }

    [Test]
    public async Task GetEnvironment_ByName_CaseInsensitive()
    {
        _fileHandler.GetEnvironmentFiles().Returns(new[] { "file1" });
        _fileHandler.GetEnvironmentContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"id1\",\"Name\":\"Development\"}");

        var result = await _sut.GetEnvironment("development", CancellationToken.None);

        Assert.AreEqual("id1", result.Id);
    }

    [Test]
    public void GetEnvironment_NotFound_ThrowsKeyNotFoundException()
    {
        _fileHandler.GetEnvironmentFiles().Returns(new[] { "file1" });
        _fileHandler.GetEnvironmentContentAbsolutePath("file1", Arg.Any<CancellationToken>())
            .Returns("{\"Id\":\"id1\",\"Name\":\"Dev\"}");

        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetEnvironment("nonexistent", CancellationToken.None));
    }

    [Test]
    public void GetEnvironment_NoFiles_ThrowsKeyNotFoundException()
    {
        _fileHandler.GetEnvironmentFiles().Returns(Array.Empty<string>());

        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetEnvironment("anything", CancellationToken.None));
    }
}
