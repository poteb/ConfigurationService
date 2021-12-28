using System.Text.Json;
using pote.Config.Admin.Api.Model;

namespace pote.Config.Admin.Api.Mappers;

public class ConfigurationMapper
{
    public static DbModel.Configuration ToDb(Configuration apiConfiguration)
    {
        return new DbModel.Configuration
        {
            Id = string.Empty,
            Gid = apiConfiguration.Gid,
            Name = apiConfiguration.Name,
            CreatedUtc = apiConfiguration.CreatedUtc,
            Json = apiConfiguration.Json,
            Systems = JsonSerializer.Serialize(JsonSerializer.Deserialize<List<Model.System>>(apiConfiguration.Systems)?.Select(s => s.Id) ?? new List<string>()),
            Environments = JsonSerializer.Serialize(JsonSerializer.Deserialize<List<Model.Environment>>(apiConfiguration.Environments)?.Select(s => s.Id) ?? new List<string>()),
            Deleted = apiConfiguration.Deleted,
            IsActive = apiConfiguration.IsActive
        };
    }

    public static Configuration ToApi(DbModel.Configuration dbConfiguration, List<DbModel.System> systems, List<DbModel.Environment> environments)
    {
        return new Configuration
        {
            Gid = dbConfiguration.Gid,
            Name = dbConfiguration.Name,
            CreatedUtc = dbConfiguration.CreatedUtc,
            Json = dbConfiguration.Json,
            Systems = GetFullSystems(dbConfiguration, systems),
            Environments = GetFullEnvironments(dbConfiguration, environments),
            Deleted = dbConfiguration.Deleted,
            IsActive = dbConfiguration.IsActive
        };
    }

    private static string GetFullSystems(DbModel.Configuration dbConfiguration, List<DbModel.System> systems)
    {
        var systemIds = JsonSerializer.Deserialize<List<string>>(dbConfiguration.Systems)?.ToList() ?? new List<string>();
        if (!systemIds.Any()) return JsonSerializer.Serialize(systemIds);
        return JsonSerializer.Serialize(systems.Where(x => systemIds.Any(y => y == x.Id)));
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

    public static List<Configuration> ToApi(List<DbModel.Configuration> configurations, List<DbModel.System> systems, List<DbModel.Environment> environments)
    {
        return configurations.Select(c => ToApi(c, systems, environments)).ToList();
    }
}