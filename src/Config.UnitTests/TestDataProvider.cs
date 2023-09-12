﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

namespace pote.Config.UnitTests;

public class TestDataProvider : IAdminDataProvider
{
    public Task<Configuration> GetConfiguration(string name, string applicationId, string environment, CancellationToken cancellationToken)
    {
        return name switch
        {
            "Wagga" => Task.FromResult(new Configuration {Json = "{\"Wagga\":\"Mama\"}"}),
            "Wagga_nested" => Task.FromResult(new Configuration {Json = "{\"Wagga\":\"$ref:Super#\"}"}),
            "Super" => Task.FromResult(new Configuration {Json = "{\"Super\":\"mule\",\"Super2\":\"mule2\"}"}),
            "Circular" => Task.FromResult(new Configuration {Id = "0dfa086a-da82-4ed4-916c-a604aed33fbf", Json = "{\"Wagga\":\"$ref:RefCircular#\"}"}),
            "RefCircular" => Task.FromResult(new Configuration {Id = "2b338c20-709e-4c56-a823-47fbdad051a8", Json = "{\"Dingo\":\"$ref:Circular#\"}"}),
            "MultiRef" => Task.FromResult(new Configuration {Json = "{\"Wagga\":\"$ref:Wagga#\",\"Super\":\"$ref:Super#Super\",\"Super\":\"$ref:Super#Super2\"}", Applications = new List<string> {"AppId1"}, Environments = new List<string> {"EnvId1"}}),
            "ExistingSection" => Task.FromResult(new Configuration {Json = "{\"Wagga\":\"TheRealMama\",\"Foo\":{\"Baa\":false}}" }),
            "EncryptedSimple" => Task.FromResult(new Configuration {Json = "ddiWml5jx2W7XivdnZ+uiVG8Ok2PJ+CJh+q60CY7rKA=", IsJsonEncrypted = true}),
            "EncryptedButNot" => Task.FromResult(new Configuration {Json = "{\"Wagga\":\"Mama\"}", IsJsonEncrypted = true}),
            _ => Task.FromResult(new Configuration())
        };
    }

    public Task<List<Environment>> GetEnvironments(CancellationToken cancellationToken)
    {
        var list = new List<Environment> {new() {Id = "EnvId1", Name = "test"}};
        return Task.FromResult(list);
    }

    public Task<List<Application>> GetApplications(CancellationToken cancellationToken)
    {
        var list = new List<Application> {new() {Id = "AppId1", Name = "unittest"}};
        return Task.FromResult(list);
    }

    public Task<List<ConfigurationHeader>> GetAll(CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public async Task<ConfigurationHeader> GetConfiguration(string id, CancellationToken cancellationToken, bool includeHistory = true)
    {
        return await (id switch
        {
            "HeaderId1" => Task.FromResult(new ConfigurationHeader
            {
                Id = id, Configurations = new List<Configuration> {GetConfiguration("MultiRef", "", "", CancellationToken.None).Result}
            }),
            _ => Task.FromResult(new ConfigurationHeader())
        });
    }

    public async Task<List<ConfigurationHeader>> GetHeaderHistory(string id, int page, int pageSize, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<Configuration>> GetConfigurationHistory(string headerId, string id, int page, int pageSize, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public void DeleteConfiguration(string id, bool permanent)
    {
        throw new System.NotImplementedException();
    }

    public Task Insert(ConfigurationHeader header, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public Task UpsertEnvironment(Environment environment, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public Task DeleteEnvironment(string id, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public Task UpsertApplication(Application application, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public Task DeleteApplication(string id, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}