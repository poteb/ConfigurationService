using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace pote.Config.UnitTests;

[TestFixture]
public class ParserTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task TestBasic()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Value1\":\"$ref:Wagga#Wagga\"}", "unittest", "test", _ => { }, CancellationToken.None, "");
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.ToString();
        Assert.AreEqual("Mama", value1);
        response = await parser.Parse("{\"Value1\":\"$ref:Wagga#\"}", "unittest", "test", _ => { }, CancellationToken.None, "");
        dyn = JsonConvert.DeserializeObject<dynamic>(response);
        value1 = dyn?.Value1.Wagga.ToString();
        Assert.AreEqual("Mama", value1);
    }

    [Test]
    public async Task TestNested()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Value1\":\"$ref:Wagga_nested#\"}", "unittest", "test", _ => { }, CancellationToken.None, "");
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.Wagga.Super.ToString();
        Assert.AreEqual("mule", value1);
    }

    [Test, Explicit]
    public async Task CircularReference_ShouldNot_StackOverflow()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Wagga\":\"$ref:Circular#\"}", "unittest", "test", _ => { }, CancellationToken.None, "");
        var _ = JsonConvert.DeserializeObject<dynamic>(response);
    }

    [Test]
    public async Task SectionAlreadyInInput_Should_Overwrite()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Base\":\"$ref:ExistingSection#\",\"Wagga\":\"Mama\",\"Foo\":{\"Baa\":true}}", "unittest", "test", _ => { }, CancellationToken.None, "");
        var obj = JsonConvert.DeserializeObject<dynamic>(response);
        Assert.AreEqual("TheRealMama", obj?.Wagga.ToString());
        Assert.IsTrue(bool.TryParse(obj?.Foo.Baa.ToString(), out bool baa));
        Assert.AreEqual(false, baa);
    }
}