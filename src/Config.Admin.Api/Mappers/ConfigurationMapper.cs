using System.Text.Json;
using pote.Config.Admin.Api.Model;

namespace pote.Config.Admin.Api.Mappers;

public class ConfigurationMapper
{
    public static DbModel.ConfigurationHeader ToDb(ConfigurationHeader apiHeader)
    {
        return new DbModel.ConfigurationHeader
        {
            Id = apiHeader.Id,
            Name = apiHeader.Name,
            CreatedUtc = apiHeader.CreatedUtc,
            UpdateUtc = apiHeader.UpdateUtc,
            Deleted = apiHeader.Deleted,
            IsActive = apiHeader.IsActive,
            Configurations = ToDb(apiHeader.Configurations),
            IsJsonEncrypted = apiHeader.IsJsonEncrypted
        };
    }

    public static ConfigurationHeader ToApi(DbModel.ConfigurationHeader dbHeader, List<DbModel.Application> applications, List<DbModel.Environment> environments)
    {
        return new ConfigurationHeader
        {
            Id = dbHeader.Id,
            Name = dbHeader.Name,
            CreatedUtc = dbHeader.CreatedUtc,
            UpdateUtc = dbHeader.UpdateUtc,
            Deleted = dbHeader.Deleted,
            IsActive = dbHeader.IsActive,
            Configurations = ToApi(dbHeader.Configurations, applications, environments),
            IsJsonEncrypted = dbHeader.IsJsonEncrypted
        };
    }

    public static DbModel.Configuration ToDb(Configuration apiConfiguration)
    {
        return new DbModel.Configuration
        {
            HeaderId = apiConfiguration.HeaderId,
            Id = apiConfiguration.Id,
            CreatedUtc = apiConfiguration.CreatedUtc,
            Json = apiConfiguration.Json,
            Applications = JsonSerializer.Deserialize<List<Application>>(apiConfiguration.Applications)?.Select(s => s.Id).ToList() ?? new List<string>(),
            Environments = JsonSerializer.Deserialize<List<Model.Environment>>(apiConfiguration.Environments)?.Select(s => s.Id).ToList() ?? new List<string>(),
            Deleted = apiConfiguration.Deleted,
            IsActive = apiConfiguration.IsActive,
            IsJsonEncrypted = apiConfiguration.IsJsonEncrypted
        };
    }

    public static Configuration ToApi(DbModel.Configuration dbConfiguration, List<DbModel.Application> applications, List<DbModel.Environment> environments)
    {
        return new Configuration
        {
            HeaderId = dbConfiguration.HeaderId,
            Id = dbConfiguration.Id,
            CreatedUtc = dbConfiguration.CreatedUtc,
            Json = dbConfiguration.Json,
            Applications = GetFullApplications(dbConfiguration, applications),
            Environments = GetFullEnvironments(dbConfiguration, environments),
            Deleted = dbConfiguration.Deleted,
            IsActive = dbConfiguration.IsActive,
            IsJsonEncrypted = dbConfiguration.IsJsonEncrypted
        };
    }

    private static string GetFullApplications(DbModel.Configuration dbConfiguration, List<DbModel.Application> applications)
    {
        return !dbConfiguration.Applications.Any() 
            ? JsonSerializer.Serialize(dbConfiguration.Applications) 
            : JsonSerializer.Serialize(applications.Where(x => dbConfiguration.Applications.Any(y => y == x.Id)));
    }

    private static string GetFullEnvironments(DbModel.Configuration dbConfiguration, List<DbModel.Environment> environments)
    {
        return !dbConfiguration.Environments.Any()
            ? JsonSerializer.Serialize(dbConfiguration.Environments) 
            : JsonSerializer.Serialize(environments.Where(x => dbConfiguration.Environments.Any(y => y == x.Id)));
    }

    public static List<DbModel.Configuration> ToDb(List<Configuration> configurations)
    {
        return configurations.Select(ToDb).ToList();
    }

    public static List<Configuration> ToApi(List<DbModel.Configuration> configurations, List<DbModel.Application> applications, List<DbModel.Environment> environments)
    {
        return configurations.Select(c => ToApi(c, applications, environments)).ToList();
    }

    public static List<DbModel.ConfigurationHeader> ToDb(List<ConfigurationHeader> headers)
    {
        return headers.Select(ToDb).ToList();
    }

    public static List<ConfigurationHeader> ToApi(List<DbModel.ConfigurationHeader> headers, List<DbModel.Application> applications, List<DbModel.Environment> environments)
    {
        return headers.Select(h => ToApi(h, applications, environments)).ToList();
    }
}

public static class ConfigurationMapperExtensions
{
    public static Configuration ToApi(this DbModel.Configuration configuration, List<DbModel.Application> applications, List<DbModel.Environment> environments)
    {
        return ConfigurationMapper.ToApi(configuration, applications, environments);
    }
    
    public static DbModel.Configuration ToDb(this Configuration configuration)
    {
        return ConfigurationMapper.ToDb(configuration);
    }
}