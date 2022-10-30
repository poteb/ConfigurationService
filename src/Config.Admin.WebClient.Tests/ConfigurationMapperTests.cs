using System;
using System.Linq;
using NUnit.Framework;
using pote.Config.Admin.WebClient.Mappers;
using pote.Config.Admin.WebClient.Model;

namespace Config.Admin.WebClient.Tests
{
    public class ConfigurationMapperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Copy_Test()
        {
            var application1 = new Application { Name = "Application 1" };
            var application2 = new Application { Name = "Application 2" };
            var environment1 = new ConfigEnvironment { Name = "Env 1" };
            var environment2 = new ConfigEnvironment { Name = "Env 2" };
            var environment3 = new ConfigEnvironment { Name = "Env 3" };

            var header = new ConfigurationHeader
            {
                Name = "Header name",
                IsActive = true,
                CreatedUtc = DateTime.UtcNow.AddDays(-11)
            };
            header.Configurations.Add(new Configuration
            {
                Json = "{\"waga\":\"mama\"}",
                CreatedUtc = DateTime.UtcNow.AddDays(-11),
                HeaderId = header.Id
            });
            header.Configurations[0].Applications.Add(application1);
            header.Configurations[0].Environments.Add(environment1);
            header.Configurations[0].Environments.Add(environment2);

            header.Configurations.Add(new Configuration
            {
                Json = "{\"dild\":\"dingo\"}",
                CreatedUtc = DateTime.UtcNow.AddDays(-1),
                HeaderId = header.Id
            });
            header.Configurations[1].Applications.Add(application2);
            header.Configurations[1].Applications.Add(application1);
            header.Configurations[1].Environments.Add(environment3);
            header.Configurations[1].Environments.Add(environment1);

            var copy = ConfigurationMapper.Copy(header);

            header.Configurations[1].Applications = header.Configurations[1].Applications.OrderBy(x => x.Name).ToList();
            header.Configurations[1].Environments = header.Configurations[1].Environments.OrderBy(x => x.Name).ToList();

            Assert.IsNotNull(copy);
            Assert.IsTrue(header.Equals(copy));
        }

        
    }
}