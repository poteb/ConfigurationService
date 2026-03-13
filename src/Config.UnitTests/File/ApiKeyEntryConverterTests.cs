using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using pote.Config.DataProvider.File;
using pote.Config.DbModel;

namespace pote.Config.UnitTests;

[TestFixture]
public class ApiKeyEntryConverterTests
{
    private readonly ApiKeyEntryConverter _converter = new();

    [Test]
    public void ReadJson_StringToken_ReturnsApiKeyEntryWithKeyOnly()
    {
        var json = """{"Keys":["my-api-key-123"]}""";
        var result = JsonConvert.DeserializeObject<ApiKeys>(json, _converter);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result!.Keys.Count);
        Assert.AreEqual("my-api-key-123", result.Keys[0].Key);
        Assert.AreEqual(string.Empty, result.Keys[0].Name);
    }

    [Test]
    public void ReadJson_ObjectToken_ReturnsApiKeyEntryWithNameAndKey()
    {
        var json = """{"Keys":[{"Name":"Production","Key":"prod-key-456"}]}""";
        var result = JsonConvert.DeserializeObject<ApiKeys>(json, _converter);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result!.Keys.Count);
        Assert.AreEqual("prod-key-456", result.Keys[0].Key);
        Assert.AreEqual("Production", result.Keys[0].Name);
    }

    [Test]
    public void ReadJson_MixedTokens_HandlesBothFormats()
    {
        var json = """{"Keys":["legacy-key",{"Name":"New","Key":"new-key"}]}""";
        var result = JsonConvert.DeserializeObject<ApiKeys>(json, _converter);

        Assert.IsNotNull(result);
        Assert.AreEqual(2, result!.Keys.Count);
        Assert.AreEqual("legacy-key", result.Keys[0].Key);
        Assert.AreEqual(string.Empty, result.Keys[0].Name);
        Assert.AreEqual("new-key", result.Keys[1].Key);
        Assert.AreEqual("New", result.Keys[1].Name);
    }

    [Test]
    public void ReadJson_ObjectMissingName_DefaultsToEmpty()
    {
        var json = """{"Keys":[{"Key":"only-key"}]}""";
        var result = JsonConvert.DeserializeObject<ApiKeys>(json, _converter);

        Assert.IsNotNull(result);
        Assert.AreEqual("only-key", result!.Keys[0].Key);
        Assert.AreEqual(string.Empty, result.Keys[0].Name);
    }

    [Test]
    public void ReadJson_ObjectMissingKey_DefaultsToEmpty()
    {
        var json = """{"Keys":[{"Name":"NameOnly"}]}""";
        var result = JsonConvert.DeserializeObject<ApiKeys>(json, _converter);

        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result!.Keys[0].Key);
        Assert.AreEqual("NameOnly", result.Keys[0].Name);
    }

    [Test]
    public void ReadJson_EmptyKeysArray_ReturnsEmptyList()
    {
        var json = """{"Keys":[]}""";
        var result = JsonConvert.DeserializeObject<ApiKeys>(json, _converter);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result!.Keys.Count);
    }

    [Test]
    public void WriteJson_SerializesAsObject()
    {
        var apiKeys = new ApiKeys
        {
            Keys = new List<ApiKeyEntry>
            {
                new() { Name = "Test", Key = "test-key" }
            }
        };

        var json = JsonConvert.SerializeObject(apiKeys, new ApiKeyEntryConverter());

        Assert.IsTrue(json.Contains("\"Name\":\"Test\""));
        Assert.IsTrue(json.Contains("\"Key\":\"test-key\""));
    }

    [Test]
    public void WriteJson_RoundTrip_PreservesData()
    {
        var original = new ApiKeys
        {
            Keys = new List<ApiKeyEntry>
            {
                new() { Name = "Prod", Key = "prod-key" },
                new() { Name = "Dev", Key = "dev-key" }
            }
        };

        var json = JsonConvert.SerializeObject(original, new ApiKeyEntryConverter());
        var deserialized = JsonConvert.DeserializeObject<ApiKeys>(json, new ApiKeyEntryConverter());

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(2, deserialized!.Keys.Count);
        Assert.AreEqual("Prod", deserialized.Keys[0].Name);
        Assert.AreEqual("prod-key", deserialized.Keys[0].Key);
        Assert.AreEqual("Dev", deserialized.Keys[1].Name);
        Assert.AreEqual("dev-key", deserialized.Keys[1].Key);
    }

    [Test]
    public void WriteJson_EmptyEntry_WritesEmptyStrings()
    {
        var apiKeys = new ApiKeys
        {
            Keys = new List<ApiKeyEntry> { new() }
        };

        var json = JsonConvert.SerializeObject(apiKeys, new ApiKeyEntryConverter());

        Assert.IsTrue(json.Contains("\"Name\":\"\""));
        Assert.IsTrue(json.Contains("\"Key\":\"\""));
    }
}
