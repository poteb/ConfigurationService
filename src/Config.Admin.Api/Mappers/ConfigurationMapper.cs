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
            Integrations = apiConfiguration.Integrations,
            Deleted = apiConfiguration.Deleted,
            IsActive = apiConfiguration.IsActive
        };
    }

    public static Configuration ToApi(DbModel.Configuration dbConfiguration)
    {
        return new Configuration
        {
            Gid = dbConfiguration.Gid,
            Name = dbConfiguration.Name,
            CreatedUtc = dbConfiguration.CreatedUtc,
            Json = dbConfiguration.Json,
            Integrations = dbConfiguration.Integrations,
            Deleted = dbConfiguration.Deleted,
            IsActive = dbConfiguration.IsActive
        };
    }

    public static List<DbModel.Configuration> ToDb(List<Configuration> configurations)
    {
        return configurations.Select(ToDb).ToList();
    }

    public static List<Configuration> ToApi(List<DbModel.Configuration> configurations)
    {
        return configurations.Select(ToApi).ToList();
    }
}