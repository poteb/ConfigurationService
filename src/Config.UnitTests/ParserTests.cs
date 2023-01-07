using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using pote.Config.DbModel;
using pote.Config.Shared;

namespace pote.Config.UnitTests
{
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
            var dyn = JsonConvert.DeserializeObject<dynamic>(response);
        }
    }

    public class TestDataProvider : IDataProvider
    {
        public Task<Configuration> GetConfiguration(string name, string applicationId, string environment,
            CancellationToken cancellationToken)
        {
            return name switch
            {
                "Wagga" => Task.FromResult(new Configuration { Json = "{\"Wagga\":\"Mama\"}" }),
                "Wagga_nested" => Task.FromResult(new Configuration { Json = "{\"Wagga\":\"$ref:Super#\"}" }),
                "Super" => Task.FromResult(new Configuration { Json = "{\"Super\":\"mule\"}" }),
                "Circular" => Task.FromResult(new Configuration { Id="0dfa086a-da82-4ed4-916c-a604aed33fbf", Json = "{\"Wagga\":\"$ref:RefCircular#\"}" }),
                "RefCircular" => Task.FromResult(new Configuration { Id="2b338c20-709e-4c56-a823-47fbdad051a8", Json = "{\"Dingo\":\"$ref:Circular#\"}" }),
                _ => Task.FromResult(new Configuration())
            };
        }

        public Task<List<Environment>> GetEnvironments(CancellationToken cancellationToken)
        {
            var list = new List<Environment> { new() { Id = "dild_id", Name = "test" } };
            return Task.FromResult(list);
        }

        public Task<List<Application>> GetApplications(CancellationToken cancellationToken)
        {
            var list = new List<Application> { new() { Id = "dild_id", Name = "unittest" } };
            return Task.FromResult(list);
        }
    }
}