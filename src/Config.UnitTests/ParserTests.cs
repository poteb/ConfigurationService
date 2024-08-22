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
        var response = await parser.Parse("{\"Value1\":\"$ref:Wagga#Wagga\"}", "unittest", "test", _ => { }, CancellationToken.None, "", false);
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.ToString();
        Assert.AreEqual("Mama", value1);
        response = await parser.Parse("{\"Value1\":\"$ref:Wagga#\"}", "unittest", "test", _ => { }, CancellationToken.None, "", false);
        dyn = JsonConvert.DeserializeObject<dynamic>(response);
        value1 = dyn?.Value1.Wagga.ToString();
        Assert.AreEqual("Mama", value1);
    }

    [Test]
    public async Task TestNested()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Value1\":\"$ref:Wagga_nested#\"}", "unittest", "test", _ => { }, CancellationToken.None, "", false);
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.Wagga.Super.ToString();
        Assert.AreEqual("mule", value1);
    }
    
    [Test]
    public async Task TestDeep()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Value1\":\"$ref:Deep#Deep.Foo\"}", "unittest", "test", _ => { }, CancellationToken.None, "", false);
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.ToString();
        Assert.AreEqual("Baa", value1);
    }
    
    [Test]
    public async Task TestParentheses()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Value1\":\"this is my text:($ref:Wagga#Wagga); with some more text\"}", "unittest", "test", _ => { }, CancellationToken.None, "", false);
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.ToString();
        Assert.AreEqual("this is my text:Mama; with some more text", value1);
    }
    
    [Test]
    public async Task TestParenthesesDeep()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Value1\":\"this is my text:($ref:Deep#Deep.Foo); with some more text\"}", "unittest", "test", _ => { }, CancellationToken.None, "", false);
        var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        var value1 = dyn?.Value1.ToString();
        Assert.AreEqual("this is my text:Baa; with some more text", value1);
    }

    [Test, Explicit]
    public async Task CircularReference_ShouldNot_StackOverflow()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Wagga\":\"$ref:Circular#\"}", "unittest", "test", _ => { }, CancellationToken.None, "", false);
        var _ = JsonConvert.DeserializeObject<dynamic>(response);
    }

    [Test]
    public async Task SectionAlreadyInInput_Should_Overwrite()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"Base\":\"$ref:ExistingSection#\",\"Wagga\":\"Mama\",\"Foo\":{\"Baa\":true}}", "unittest", "test", _ => { }, CancellationToken.None, "", false);
        var obj = JsonConvert.DeserializeObject<dynamic>(response);
        Assert.AreEqual("TheRealMama", obj?.Wagga.ToString());
        Assert.IsTrue(bool.TryParse(obj?.Foo.Baa.ToString(), out bool baa));
        Assert.AreEqual(false, baa);
    }
}