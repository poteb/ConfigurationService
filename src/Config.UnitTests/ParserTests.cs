using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using pote.Config.Shared;

namespace pote.Config.UnitTests
{
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
            var response = await parser.Parse("{\"Value1\":\"$ref:Wagga#Wagga\"}", "unittest", "test", s => { }, CancellationToken.None);
            var dyn = JsonConvert.DeserializeObject<dynamic>(response);
            var value1 = dyn?.Value1.ToString();
            Assert.AreEqual("Mama", value1);
            response = await parser.Parse("{\"Value1\":\"$ref:Wagga#\"}", "unittest", "test", s => { }, CancellationToken.None);
            dyn = JsonConvert.DeserializeObject<dynamic>(response);
            value1 = dyn?.Value1.Wagga.ToString();
            Assert.AreEqual("Mama", value1);
        }

        [Test]
        public async Task TestNested()
        {
            var dataProvider = new TestDataProvider();
            var parser = new Parser.Parser(dataProvider);
            var response = await parser.Parse("{\"Value1\":\"$ref:Wagga_nested#\"}", "unittest", "test", s => { }, CancellationToken.None);
            var dyn = JsonConvert.DeserializeObject<dynamic>(response);
            var value1 = dyn?.Value1.Wagga.Super.ToString();
            Assert.AreEqual("mule", value1);
        }
    }

    public class TestDataProvider : IDataProvider
    {
        public Task<string> GetConfigurationJson(string name, string system, string environment, CancellationToken cancellationToken)
        {
            return name switch
            {
                "Wagga" => Task.FromResult("{\"Wagga\":\"Mama\"}"),
                "Wagga_nested" => Task.FromResult("{\"Wagga\":\"$ref:Super#\"}"),
                "Super" => Task.FromResult("{\"Super\":\"mule\"}"),
                _ => Task.FromResult("")
            };
        }
    }
}