using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace pote.Config.UnitTests;

[TestFixture]
public class SecretTests
{
    [Test]
    public async Task Test_Refs_Should_Not_Parse()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Value1\":\"$refs:Secret1#\"}", "unittest", "test", _ => { }, CancellationToken.None, "");
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.ToString();
        Assert.AreEqual("$refs:Secret1#", value1); // still not resolved because it's a promise
    }
}