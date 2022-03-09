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
            Configurations = ToDb(apiHeader.Configurations)
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
            Configurations = ToApi(dbHeader.Configurations, applications, environments)
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
            Applications = JsonSerializer.Serialize(JsonSerializer.Deserialize<List<Model.Application>>(apiConfiguration.Applications)?.Select(s => s.Id) ?? new List<string>()),
            Environments = JsonSerializer.Serialize(JsonSerializer.Deserialize<List<Model.Environment>>(apiConfiguration.Environments)?.Select(s => s.Id) ?? new List<string>()),
            Deleted = apiConfiguration.Deleted,
            IsActive = apiConfiguration.IsActive,
            History = ToDb(apiConfiguration.History)
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
            History = ToApi(dbConfiguration.History, applications, environments)
        };
    }

    private static string GetFullApplications(DbModel.Configuration dbConfiguration, List<DbModel.Application> applications)
    {
        var applicationIds = JsonSerializer.Deserialize<List<string>>(dbConfiguration.Applications)?.ToList() ?? new List<string>();
        if (!applicationIds.Any()) return JsonSerializer.Serialize(applicationIds);
        return JsonSerializer.Serialize(applications.Where(x => applicationIds.Any(y => y == x.Id)));
    }

    private static string GetFullEnvironments(DbModel.Configuration dbConfiguration, List<DbModel.Environment> environments)
    {
        var list = JsonSerializer.Deserialize<List<string>>(dbConfiguration.Environments)?.ToList() ?? new List<string>();
        if (!list.Any()) return JsonSerializer.Serialize(list);
        return JsonSerializer.Serialize(environments.Where(x => list.Any(y => y == x.Id)));
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