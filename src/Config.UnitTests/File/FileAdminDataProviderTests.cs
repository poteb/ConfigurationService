using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using pote.Config.DataProvider.File;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;
using pote.Config.Encryption;
using pote.Config.Shared;
using Environment = pote.Config.DbModel.Environment;

namespace pote.Config.UnitTests;

[TestFixture]
public class FileAdminDataProviderTests
{
    private const string EncryptionKey = "detteErEnVildtGodEncryptionKey11";

    private IFileHandler _fileHandler = null!;
    private IApplicationDataAccess _applicationDataAccess = null!;
    private IEnvironmentDataAccess _environmentDataAccess = null!;
    private ISecretDataAccess _secretDataAccess = null!;
    private AdminDataProvider _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fileHandler = Substitute.For<IFileHandler>();
        _applicationDataAccess = Substitute.For<IApplicationDataAccess>();
        _environmentDataAccess = Substitute.For<IEnvironmentDataAccess>();
        _secretDataAccess = Substitute.For<ISecretDataAccess>();

        _fileHandler.GetSettings(Arg.Any<CancellationToken>()).Returns("{}");

        _sut = new AdminDataProvider(
            _fileHandler,
            _applicationDataAccess,
            _environmentDataAccess,
            _secretDataAccess,
            new EncryptionSettings { JsonEncryptionKey = EncryptionKey });
    }

    #region GetEnvironments / GetEnvironment

    [Test]
    public async Task GetEnvironments_DelegatesToEnvironmentDataAccess()
    {
        var envs = new List<Environment>
        {
            new() { Id = "id1", Name = "Dev" },
            new() { Id = "id2", Name = "Prod" }
        };
        _environmentDataAccess.GetEnvironments(Arg.Any<CancellationToken>()).Returns(envs);

        var result = await _sut.GetEnvironments(CancellationToken.None);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Dev", result[0].Name);
        Assert.AreEqual("Prod", result[1].Name);
    }

    [Test]
    public async Task GetEnvironment_DelegatesToEnvironmentDataAccess()
    {
        var env = new Environment { Id = "id1", Name = "Dev" };
        _environmentDataAccess.GetEnvironment("id1", Arg.Any<CancellationToken>()).Returns(env);

        var result = await _sut.GetEnvironment("id1", CancellationToken.None);

        Assert.AreEqual("id1", result.Id);
        Assert.AreEqual("Dev", result.Name);
    }

    #endregion

    #region GetApplications / GetApplication

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

    #endregion

    #region UpsertEnvironment / DeleteEnvironment

    [Test]
    public async Task UpsertEnvironment_WritesSerializedEnvironment()
    {
        var env = new Environment { Id = "env1", Name = "Staging" };

        await _sut.UpsertEnvironment(env, CancellationToken.None);

        await _fileHandler.Received(1).WriteEnvironmentContent("env1", Arg.Is<string>(s => s.Contains("Staging")), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteEnvironment_CallsFileHandler()
    {
        await _sut.DeleteEnvironment("env1", CancellationToken.None);

        _fileHandler.Received(1).DeleteEnvironment("env1");
    }

    #endregion

    #region UpsertApplication / DeleteApplication

    [Test]
    public async Task UpsertApplication_WritesSerializedApplication()
    {
        var app = new Application { Id = "app1", Name = "MyApp" };

        await _sut.UpsertApplication(app, CancellationToken.None);

        await _fileHandler.Received(1).WriteApplicationContent("app1", Arg.Is<string>(s => s.Contains("MyApp")), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteApplication_CallsFileHandler()
    {
        await _sut.DeleteApplication("app1", CancellationToken.None);

        _fileHandler.Received(1).DeleteApplication("app1");
    }

    #endregion

    #region GetConfiguration (by id)

    [Test]
    public async Task GetConfiguration_ById_DeserializesHeader()
    {
        var header = new ConfigurationHeader
        {
            Id = "h1",
            Name = "TestConfig",
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", Json = """{"key":"value"}""" }
            }
        };
        _fileHandler.GetConfigurationContent("h1", Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(header));

        var result = await _sut.GetConfiguration("h1", CancellationToken.None);

        Assert.AreEqual("h1", result.Id);
        Assert.AreEqual("TestConfig", result.Name);
        Assert.AreEqual(1, result.Configurations.Count);
        Assert.AreEqual("c1", result.Configurations[0].Id);
    }

    [Test]
    public async Task GetConfiguration_ById_DecryptsEncryptedJson()
    {
        var originalJson = """{"secret":"data"}""";
        var encrypted = EncryptionHandler.Encrypt(originalJson, EncryptionKey);
        var header = new ConfigurationHeader
        {
            Id = "h1",
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", Json = encrypted, IsJsonEncrypted = true }
            }
        };
        _fileHandler.GetConfigurationContent("h1", Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(header));

        var result = await _sut.GetConfiguration("h1", CancellationToken.None);

        Assert.AreEqual(originalJson, result.Configurations[0].Json);
    }

    [Test]
    public void GetConfiguration_ById_NullJson_ThrowsKeyNotFoundException()
    {
        _fileHandler.GetConfigurationContent("missing", Arg.Any<CancellationToken>())
            .Returns("null");

        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetConfiguration("missing", CancellationToken.None));
    }

    #endregion

    #region GetAllConfigurationHeaders

    [Test]
    public async Task GetAllConfigurationHeaders_ReturnsAllHeaders()
    {
        var header1 = new ConfigurationHeader { Id = "h1", Name = "Config1", Configurations = new List<Configuration>() };
        var header2 = new ConfigurationHeader { Id = "h2", Name = "Config2", Configurations = new List<Configuration>() };

        _fileHandler.GetConfigurationFiles().Returns(new[] { "/configs/h1.txt", "/configs/h2.txt" });
        _fileHandler.GetConfigurationContent("h1", Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(header1));
        _fileHandler.GetConfigurationContent("h2", Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(header2));

        var result = await _sut.GetAllConfigurationHeaders(CancellationToken.None);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Config1", result[0].Name);
        Assert.AreEqual("Config2", result[1].Name);
    }

    [Test]
    public async Task GetAllConfigurationHeaders_SkipsBadFiles()
    {
        var header1 = new ConfigurationHeader { Id = "h1", Name = "Config1", Configurations = new List<Configuration>() };

        _fileHandler.GetConfigurationFiles().Returns(new[] { "/configs/h1.txt", "/configs/bad.txt" });
        _fileHandler.GetConfigurationContent("h1", Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(header1));
        _fileHandler.GetConfigurationContent("bad", Arg.Any<CancellationToken>())
            .Returns("null");

        var result = await _sut.GetAllConfigurationHeaders(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Config1", result[0].Name);
    }

    [Test]
    public async Task GetAllConfigurationHeaders_EmptyFiles_ReturnsEmptyList()
    {
        _fileHandler.GetConfigurationFiles().Returns(Array.Empty<string>());

        var result = await _sut.GetAllConfigurationHeaders(CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region InsertConfiguration

    [Test]
    public async Task InsertConfiguration_WritesSerializedHeader()
    {
        var header = new ConfigurationHeader
        {
            Id = "h1",
            Name = "TestConfig",
            CreatedUtc = DateTime.UtcNow,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", Json = """{"key":"value"}""" }
            }
        };

        await _sut.InsertConfiguration(header, CancellationToken.None);

        await _fileHandler.Received(1).WriteConfigurationContent("h1", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InsertConfiguration_SetsCreatedUtcOnConfigurations()
    {
        var createdUtc = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var header = new ConfigurationHeader
        {
            Id = "h1",
            CreatedUtc = createdUtc,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", Json = """{"key":"value"}""" }
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
            IsJsonEncrypted = true,
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", Json = """{"key":"value"}""" }
            }
        };

        await _sut.InsertConfiguration(header, CancellationToken.None);

        Assert.IsTrue(header.Configurations[0].IsJsonEncrypted);
    }

    [Test]
    public async Task InsertConfiguration_EncryptsWhenSettingsEncryptAllJson()
    {
        _fileHandler.GetSettings(Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(new Settings { EncryptAllJson = true }));

        var header = new ConfigurationHeader
        {
            Id = "h1",
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", Json = """{"key":"value"}""" }
            }
        };

        await _sut.InsertConfiguration(header, CancellationToken.None);

        Assert.IsTrue(header.Configurations[0].IsJsonEncrypted);
    }

    [Test]
    public async Task InsertConfiguration_EncryptsWhenConfigurationIsJsonEncrypted()
    {
        var header = new ConfigurationHeader
        {
            Id = "h1",
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", Json = """{"key":"value"}""", IsJsonEncrypted = true }
            }
        };

        await _sut.InsertConfiguration(header, CancellationToken.None);

        // Verify the json was encrypted (not the original plaintext)
        await _fileHandler.Received(1).WriteConfigurationContent("h1",
            Arg.Is<string>(s => !s.Contains(@"{""key"":""value""}")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteConfiguration

    [Test]
    public void DeleteConfiguration_Permanent_CallsFileHandler()
    {
        _sut.DeleteConfiguration("h1", true);

        _fileHandler.Received(1).DeleteConfiguration("h1", true);
    }

    [Test]
    public void DeleteConfiguration_NonPermanent_CallsFileHandler()
    {
        _sut.DeleteConfiguration("h1", false);

        _fileHandler.Received(1).DeleteConfiguration("h1", false);
    }

    #endregion

    #region GetHeaderHistory

    [Test]
    public async Task GetHeaderHistory_DeserializesAllEntries()
    {
        var h1 = new ConfigurationHeader { Id = "h1", Name = "V1", Configurations = new List<Configuration>() };
        var h2 = new ConfigurationHeader { Id = "h1", Name = "V2", Configurations = new List<Configuration>() };

        _fileHandler.GetHeaderHistory("h1", 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<string>
            {
                JsonConvert.SerializeObject(h1),
                JsonConvert.SerializeObject(h2)
            });

        var result = await _sut.GetHeaderHistory("h1", 1, 10, CancellationToken.None);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("V1", result[0].Name);
        Assert.AreEqual("V2", result[1].Name);
    }

    [Test]
    public async Task GetHeaderHistory_NullDeserialization_ReturnsPlaceholder()
    {
        _fileHandler.GetHeaderHistory("h1", 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "null" });

        var result = await _sut.GetHeaderHistory("h1", 1, 10, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(Guid.Empty.ToString(), result[0].Id);
        Assert.AreEqual("Unable to read json", result[0].Name);
    }

    [Test]
    public async Task GetHeaderHistory_EmptyList_ReturnsEmptyList()
    {
        _fileHandler.GetHeaderHistory("h1", 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        var result = await _sut.GetHeaderHistory("h1", 1, 10, CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetHeaderHistory_DecryptsEncryptedConfigurations()
    {
        var originalJson = """{"secret":"data"}""";
        var encrypted = EncryptionHandler.Encrypt(originalJson, EncryptionKey);
        var header = new ConfigurationHeader
        {
            Id = "h1",
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", Json = encrypted, IsJsonEncrypted = true }
            }
        };

        _fileHandler.GetHeaderHistory("h1", 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<string> { JsonConvert.SerializeObject(header) });

        var result = await _sut.GetHeaderHistory("h1", 1, 10, CancellationToken.None);

        Assert.AreEqual(originalJson, result[0].Configurations[0].Json);
    }

    #endregion

    #region GetConfigurationHistory

    [Test]
    public async Task GetConfigurationHistory_ReturnsMatchingConfigurations()
    {
        var config = new Configuration { Id = "c1", Json = """{"a":"b"}""", CreatedUtc = new DateTime(2025, 1, 1) };
        var header = new ConfigurationHeader
        {
            Id = "h1",
            Configurations = new List<Configuration> { config }
        };

        _fileHandler.GetHeaderHistory("h1", 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<string> { JsonConvert.SerializeObject(header) });

        var result = await _sut.GetConfigurationHistory("h1", "c1", 1, 10, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("c1", result[0].Id);
    }

    [Test]
    public async Task GetConfigurationHistory_SkipsEntriesWithoutMatchingConfig()
    {
        var header = new ConfigurationHeader
        {
            Id = "h1",
            Configurations = new List<Configuration>
            {
                new() { Id = "c2", Json = """{"a":"b"}""" }
            }
        };

        _fileHandler.GetHeaderHistory("h1", 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<string> { JsonConvert.SerializeObject(header) });

        var result = await _sut.GetConfigurationHistory("h1", "c1", 1, 10, CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetConfigurationHistory_OrdersByCreatedUtcDescending()
    {
        var older = new ConfigurationHeader
        {
            Id = "h1",
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", Json = """{"v":1}""", CreatedUtc = new DateTime(2025, 1, 1) }
            }
        };
        var newer = new ConfigurationHeader
        {
            Id = "h1",
            Configurations = new List<Configuration>
            {
                new() { Id = "c1", Json = """{"v":2}""", CreatedUtc = new DateTime(2025, 6, 1) }
            }
        };

        _fileHandler.GetHeaderHistory("h1", 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<string>
            {
                JsonConvert.SerializeObject(older),
                JsonConvert.SerializeObject(newer)
            });

        var result = await _sut.GetConfigurationHistory("h1", "c1", 1, 10, CancellationToken.None);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result[0].CreatedUtc > result[1].CreatedUtc);
    }

    #endregion

    #region GetApiKeys / SaveApiKeys

    [Test]
    public async Task GetApiKeys_DeserializesApiKeys()
    {
        var apiKeys = new ApiKeys { Keys = new List<ApiKeyEntry> { new() { Name = "Test", Key = "key123" } } };
        _fileHandler.GetApiKeys(Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(apiKeys));

        var result = await _sut.GetApiKeys(CancellationToken.None);

        Assert.AreEqual(1, result.Keys.Count);
        Assert.AreEqual("key123", result.Keys[0].Key);
    }

    [Test]
    public async Task GetApiKeys_NullDeserialization_ReturnsEmptyApiKeys()
    {
        _fileHandler.GetApiKeys(Arg.Any<CancellationToken>()).Returns("null");

        var result = await _sut.GetApiKeys(CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Keys.Count);
    }

    [Test]
    public async Task SaveApiKeys_SerializesAndSaves()
    {
        var apiKeys = new ApiKeys { Keys = new List<ApiKeyEntry> { new() { Name = "Test", Key = "key123" } } };

        await _sut.SaveApiKeys(apiKeys, CancellationToken.None);

        await _fileHandler.Received(1).SaveApiKeys(Arg.Is<string>(s => s.Contains("key123")), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSettings / SaveSettings

    [Test]
    public async Task GetSettings_DeserializesSettings()
    {
        _fileHandler.GetSettings(Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(new Settings { EncryptAllJson = true }));

        var result = await _sut.GetSettings(CancellationToken.None);

        Assert.IsTrue(result.EncryptAllJson);
    }

    [Test]
    public async Task GetSettings_EmptyJson_ReturnsDefaultSettings()
    {
        _fileHandler.GetSettings(Arg.Any<CancellationToken>()).Returns("");

        var result = await _sut.GetSettings(CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.EncryptAllJson);
    }

    [Test]
    public async Task GetSettings_WhitespaceJson_ReturnsDefaultSettings()
    {
        _fileHandler.GetSettings(Arg.Any<CancellationToken>()).Returns("   ");

        var result = await _sut.GetSettings(CancellationToken.None);

        Assert.IsNotNull(result);
    }

    [Test]
    public async Task GetSettings_NullDeserialization_ReturnsDefaultSettings()
    {
        _fileHandler.GetSettings(Arg.Any<CancellationToken>()).Returns("null");

        var result = await _sut.GetSettings(CancellationToken.None);

        Assert.IsNotNull(result);
    }

    [Test]
    public async Task SaveSettings_SerializesAndSaves()
    {
        var settings = new Settings { EncryptAllJson = true };

        await _sut.SaveSettings(settings, CancellationToken.None);

        await _fileHandler.Received(1).SaveSettings(Arg.Is<string>(s => s.Contains("true")), Arg.Any<CancellationToken>());
    }

    #endregion

    #region InsertSecret / DeleteSecret

    [Test]
    public async Task InsertSecret_WritesSerializedHeader()
    {
        var header = new SecretHeader
        {
            Id = "s1",
            Name = "MySecret",
            CreatedUtc = DateTime.UtcNow,
            Secrets = new List<Secret>
            {
                new() { Id = "sv1", Value = "supersecret" }
            }
        };

        await _sut.InsertSecret(header, CancellationToken.None);

        await _fileHandler.Received(1).WriteSecretContent("s1", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InsertSecret_SetsCreatedUtcOnSecrets()
    {
        var createdUtc = new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var header = new SecretHeader
        {
            Id = "s1",
            CreatedUtc = createdUtc,
            Secrets = new List<Secret>
            {
                new() { Id = "sv1", Value = "supersecret" }
            }
        };

        await _sut.InsertSecret(header, CancellationToken.None);

        Assert.AreEqual(createdUtc, header.Secrets[0].CreatedUtc);
    }

    [Test]
    public async Task InsertSecret_EncryptsSecretValues()
    {
        var header = new SecretHeader
        {
            Id = "s1",
            Secrets = new List<Secret>
            {
                new() { Id = "sv1", Value = "supersecret" }
            }
        };

        await _sut.InsertSecret(header, CancellationToken.None);

        // The value should have been encrypted (not plaintext)
        await _fileHandler.Received(1).WriteSecretContent("s1",
            Arg.Is<string>(s => !s.Contains("supersecret")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteSecret_CallsFileHandler()
    {
        await _sut.DeleteSecret("s1", CancellationToken.None);

        _fileHandler.Received(1).DeleteSecret("s1");
    }

    #endregion

    #region GetAllSecretHeaders

    [Test]
    public async Task GetAllSecretHeaders_ReturnsAllHeaders()
    {
        var header = new SecretHeader
        {
            Id = "s1",
            Name = "Secret1",
            Secrets = new List<Secret>()
        };

        _fileHandler.GetSecretFiles().Returns(new[] { "/secrets/s1.txt" });
        _fileHandler.GetSecretContent("s1", Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(header));

        var result = await _sut.GetAllSecretHeaders(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Secret1", result[0].Name);
    }

    [Test]
    public async Task GetAllSecretHeaders_SkipsBadFiles()
    {
        _fileHandler.GetSecretFiles().Returns(new[] { "/secrets/bad.txt" });
        _fileHandler.GetSecretContent("bad", Arg.Any<CancellationToken>())
            .Returns("null");

        var result = await _sut.GetAllSecretHeaders(CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public async Task GetAllSecretHeaders_EmptyFiles_ReturnsEmptyList()
    {
        _fileHandler.GetSecretFiles().Returns(Array.Empty<string>());

        var result = await _sut.GetAllSecretHeaders(CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region GetSecret (by id)

    [Test]
    public async Task GetSecret_ById_DeserializesHeader()
    {
        var originalValue = "MySecretValue";
        var encrypted = EncryptionHandler.Encrypt(originalValue, EncryptionKey);
        var header = new SecretHeader
        {
            Id = "s1",
            Name = "MySecret",
            Secrets = new List<Secret>
            {
                new() { Id = "sv1", Value = encrypted }
            }
        };
        _fileHandler.GetSecretContent("s1", Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(header));

        var result = await _sut.GetSecret("s1", CancellationToken.None);

        Assert.AreEqual("s1", result.Id);
        Assert.AreEqual("MySecret", result.Name);
        Assert.AreEqual(1, result.Secrets.Count);
        Assert.AreEqual(originalValue, result.Secrets[0].Value);
    }

    [Test]
    public async Task GetSecret_ById_DecryptsEncryptedValues()
    {
        var originalValue = "supersecret";
        var encrypted = EncryptionHandler.Encrypt(originalValue, EncryptionKey);
        var header = new SecretHeader
        {
            Id = "s1",
            Secrets = new List<Secret>
            {
                new() { Id = "sv1", Value = encrypted }
            }
        };
        _fileHandler.GetSecretContent("s1", Arg.Any<CancellationToken>())
            .Returns(JsonConvert.SerializeObject(header));

        var result = await _sut.GetSecret("s1", CancellationToken.None);

        Assert.AreEqual(originalValue, result.Secrets[0].Value);
    }

    [Test]
    public void GetSecret_ById_NullJson_ThrowsKeyNotFoundException()
    {
        _fileHandler.GetSecretContent("missing", Arg.Any<CancellationToken>())
            .Returns("null");

        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetSecret("missing", CancellationToken.None));
    }

    #endregion
}
