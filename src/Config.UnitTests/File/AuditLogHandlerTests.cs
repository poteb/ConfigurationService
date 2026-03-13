using System;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using pote.Config.DataProvider.File;

namespace pote.Config.UnitTests;

[TestFixture]
public class AuditLogHandlerTests
{
    private IFileHandler _fileHandler = null!;
    private AuditLogHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fileHandler = Substitute.For<IFileHandler>();
        _sut = new AuditLogHandler(_fileHandler);
    }

    [Test]
    public async Task AuditLogConfiguration_FormatsAndDelegates()
    {
        await _sut.AuditLogConfiguration("id1", "192.168.1.1", "config content");

        await _fileHandler.Received(1).AuditLogConfiguration("id1",
            Arg.Is<string>(s => s.Contains("192.168.1.1") && s.Contains("config content")));
    }

    [Test]
    public async Task AuditLogConfiguration_IncludesNewlineBetweenIpAndContent()
    {
        await _sut.AuditLogConfiguration("id1", "10.0.0.1", "payload");

        await _fileHandler.Received(1).AuditLogConfiguration("id1",
            Arg.Is<string>(s => s.Contains("10.0.0.1" + Environment.NewLine + "payload")));
    }

    [Test]
    public async Task AuditLogEnvironment_FormatsAndDelegates()
    {
        await _sut.AuditLogEnvironment("env1", "10.0.0.1", "env content");

        await _fileHandler.Received(1).AuditLogEnvironment("env1",
            Arg.Is<string>(s => s.Contains("10.0.0.1") && s.Contains("env content")));
    }

    [Test]
    public async Task AuditLogApplication_FormatsAndDelegates()
    {
        await _sut.AuditLogApplication("app1", "10.0.0.1", "app content");

        await _fileHandler.Received(1).AuditLogApplication("app1",
            Arg.Is<string>(s => s.Contains("10.0.0.1") && s.Contains("app content")));
    }

    [Test]
    public async Task AuditLogSettings_FormatsAndDelegates()
    {
        await _sut.AuditLogSettings("settings1", "10.0.0.1", "settings content");

        await _fileHandler.Received(1).AuditLogSettings(
            Arg.Is<string>(s => s.Contains("10.0.0.1") && s.Contains("settings content")));
    }

    [Test]
    public async Task AuditLogApiKeys_FormatsAndDelegates()
    {
        await _sut.AuditLogApiKeys("keys1", "10.0.0.1", "apikeys content");

        await _fileHandler.Received(1).AuditLogApiKeys(
            Arg.Is<string>(s => s.Contains("10.0.0.1") && s.Contains("apikeys content")));
    }

    [Test]
    public async Task AuditLogSecrets_FormatsAndDelegates()
    {
        await _sut.AuditLogSecrets("sec1", "10.0.0.1", "secret content");

        await _fileHandler.Received(1).AuditLogSecrets("sec1",
            Arg.Is<string>(s => s.Contains("10.0.0.1") && s.Contains("secret content")));
    }
}
