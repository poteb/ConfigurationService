using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace pote.Config.UnitTests;

[TestFixture]
public class PromiseParserTests
{
    [Test]
    public async Task Test_Refp()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Value1\":\"$ref:Refp#\"}", "unittest", "test", _ => { }, CancellationToken.None, "");
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.Wagga.ToString();
        Assert.AreEqual("$refp:Super#Super", value1); // still not resolved because it's a promise
    }
}