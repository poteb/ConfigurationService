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
        await _sut.AuditLogConfiguration("id1", "192.168.1.1", "tester", "Insert", "config content");

        await _fileHandler.Received(1).AuditLogConfiguration("id1",
            Arg.Is<string>(s => s.Contains("192.168.1.1") && s.Contains("config content")));
    }

    [Test]
    public async Task AuditLogConfiguration_IncludesNewlineBetweenHeaderAndContent()
    {
        await _sut.AuditLogConfiguration("id1", "10.0.0.1", "tester", "Insert", "payload");

        await _fileHandler.Received(1).AuditLogConfiguration("id1",
            Arg.Is<string>(s => s.Contains("10.0.0.1 tester Insert" + Environment.NewLine + "payload")));
    }

    [Test]
    public async Task AuditLogConfiguration_NullUsername_WritesDash()
    {
        await _sut.AuditLogConfiguration("id1", "10.0.0.1", null, "Insert", "payload");

        await _fileHandler.Received(1).AuditLogConfiguration("id1",
            Arg.Is<string>(s => s.Contains("10.0.0.1 - Insert")));
    }

    [Test]
    public async Task AuditLogUser_DelegatesToSettingsAudit()
    {
        await _sut.AuditLogUser("00000000-0000-0000-0000-000000000001", "10.0.0.1", "admin1", "UserCreated", "anna");

        await _fileHandler.Received(1).AuditLogSettings(
            Arg.Is<string>(s => s.Contains("UserCreated") && s.Contains("anna") && s.Contains("admin1")));
    }

    [Test]
    public async Task AuditLogEnvironment_FormatsAndDelegates()
    {
        await _sut.AuditLogEnvironment("env1", "10.0.0.1", "tester", "Insert", "env content");

        await _fileHandler.Received(1).AuditLogEnvironment("env1",
            Arg.Is<string>(s => s.Contains("10.0.0.1") && s.Contains("env content")));
    }

    [Test]
    public async Task AuditLogApplication_FormatsAndDelegates()
    {
        await _sut.AuditLogApplication("app1", "10.0.0.1", "tester", "Insert", "app content");

        await _fileHandler.Received(1).AuditLogApplication("app1",
            Arg.Is<string>(s => s.Contains("10.0.0.1") && s.Contains("app content")));
    }

    [Test]
    public async Task AuditLogSettings_FormatsAndDelegates()
    {
        await _sut.AuditLogSettings("settings1", "10.0.0.1", "tester", "Save", "settings content");

        await _fileHandler.Received(1).AuditLogSettings(
            Arg.Is<string>(s => s.Contains("10.0.0.1") && s.Contains("settings content")));
    }

    [Test]
    public async Task AuditLogApiKeys_FormatsAndDelegates()
    {
        await _sut.AuditLogApiKeys("keys1", "10.0.0.1", "tester", "Save", "apikeys content");

        await _fileHandler.Received(1).AuditLogApiKeys(
            Arg.Is<string>(s => s.Contains("10.0.0.1") && s.Contains("apikeys content")));
    }

    [Test]
    public async Task AuditLogSecrets_FormatsAndDelegates()
    {
        await _sut.AuditLogSecrets("sec1", "10.0.0.1", "tester", "Insert", "secret content");

        await _fileHandler.Received(1).AuditLogSecrets("sec1",
            Arg.Is<string>(s => s.Contains("10.0.0.1") && s.Contains("secret content")));
    }
}
