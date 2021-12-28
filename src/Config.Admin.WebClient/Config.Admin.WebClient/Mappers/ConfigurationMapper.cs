using System.Text.Json;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Mappers;

public class ConfigurationMapper
{
    public static Configuration ToClient(Api.Model.Configuration configuration)
    {
        return new Configuration
        {
            Gid = configuration.Gid,
            Name = configuration.Name,
            CreatedUtc = configuration.CreatedUtc,
            Json = configuration.Json,
            Deleted = configuration.Deleted,
            IsActive = configuration.IsActive,
            Systems = StringToList<ConfigSystem>(configuration.Systems),
            Environments = StringToList<ConfigEnvironment>(configuration.Environments)
        };
    }

    private static List<T> StringToList<T>(string systemsJson)
    {
        var list = JsonSerializer.Deserialize<List<T>>(systemsJson) ?? new List<T>();
        return list;
    }

    public static Api.Model.Configuration ToApi(Configuration configuration)
    {
        return new Api.Model.Configuration
        {
            Gid = configuration.Gid,
            Name = configuration.Name,
            CreatedUtc = configuration.CreatedUtc,
            Json = configuration.Json,
            Deleted = configuration.Deleted,
            IsActive = configuration.IsActive,
            Systems = ListToString(configuration.Systems),
            Environments = ListToString(configuration.Environments)
        };
    }

    private static string ListToString<T>(List<T> systems)
    {
        return JsonSerializer.Serialize(systems);
    }

    public static List<Configuration> ToClient(List<Api.Model.Configuration> configurations)
    {
        return configurations.Select(ToClient).ToList();
    }

    public static List<Api.Model.Configuration> ToApi(List<Configuration> configurations)
    {
        return configurations.Select(ToApi).ToList();
    }

    public static Configuration Copy(Configuration configuration)
    {
        var newConfig = new Configuration
        {
            Gid = Guid.NewGuid().ToString(),
            Name = $"{configuration.Name} COPY",
            CreatedUtc = DateTime.UtcNow,
            Json = configuration.Json,
            Deleted = false,
            IsActive = configuration.IsActive,
            Systems = StringToList<ConfigSystem>(ListToString(configuration.Systems)),
            Environments = StringToList<ConfigEnvironment>(ListToString(configuration.Environments))
        };
        return newConfig;
    }
}