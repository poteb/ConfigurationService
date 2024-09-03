using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using pote.Config.Encryption;

namespace pote.Config.UnitTests;

[TestFixture]
public class EncryptDecryptTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var encrypted = EncryptionHandler.Encrypt("Hello World", "detteErEnVildtGodEncryptionKey11");
        var decrypted = EncryptionHandler.Decrypt(encrypted,     "detteErEnVildtGodEncryptionKey11");
        Assert.AreEqual("Hello World", decrypted);
    }

    [Test]
    public async Task Test_NotEncrypted_Json()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Value1\":\"$ref:EncryptedButNot#Wagga\"}", "unittest", "test", _ => { }, CancellationToken.None, "");
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.ToString();
        Assert.AreEqual("Mama", value1);
    }
    
    [Test]
    public async Task Test_Encrypted_Json()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Value1\":\"$ref:EncryptedSimple#Wagga\"}", "unittest", "test", _ => { }, CancellationToken.None, encryptionKey: "detteErEnVildtGodEncryptionKey11", rootId: "");
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.ToString();
        Assert.AreEqual("Mama", value1);
    }

    [Test, Explicit]
    public void UsedToGenerateEncryptedText()
    {
        // ReSharper disable once UnusedVariable
        var enc = EncryptionHandler.Encrypt("{\"Wagga\":\"Mama\"}", "detteErEnVildtGodEncryptionKey11");
    }
}