using System.Text.Json;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Mappers;

public class ConfigurationMapper
{
    public static ConfigurationHeader ToClient(Api.Model.ConfigurationHeader header)
    {
        return new ConfigurationHeader
        {
            Id = header.Id,
            Name = header.Name,
            CreatedUtc = header.CreatedUtc,
            UpdateUtc = header.UpdateUtc,
            Deleted = header.Deleted,
            IsActive = header.IsActive,
            Configurations = ToClient(header.Configurations)
        };
    }

    public static Api.Model.ConfigurationHeader ToApi(ConfigurationHeader header)
    {
        return new Api.Model.ConfigurationHeader
        {
            Id = header.Id,
            Name = header.Name,
            CreatedUtc = header.CreatedUtc,
            UpdateUtc = header.UpdateUtc,
            Deleted = header.Deleted,
            IsActive = header.IsActive,
            Configurations = ToApi(header.Configurations)
        };
    }

    public static Configuration ToClient(Api.Model.Configuration configuration)
    {
        return new Configuration
        {
            HeaderId = configuration.HeaderId,
            Id = configuration.Id,
            CreatedUtc = configuration.CreatedUtc,
            Json = configuration.Json,
            Deleted = configuration.Deleted,
            IsActive = configuration.IsActive,
            Systems = StringToList<ConfigSystem>(configuration.Systems),
            Environments = StringToList<ConfigEnvironment>(configuration.Environments),
            History = ToClient(configuration.History)
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
            HeaderId = configuration.HeaderId,
            Id = configuration.Id,
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

    public static List<ConfigurationHeader> ToClient(List<Api.Model.ConfigurationHeader> headers)
    {
        return headers.Select(ToClient).ToList();
    }

    public static List<Api.Model.ConfigurationHeader> ToApi(List<ConfigurationHeader> headers)
    {
        return headers.Select(ToApi).ToList();
    }

    private static Configuration Copy(Configuration configuration)
    {
        var newConfig = new Configuration
        {
            Id = configuration.Id,
            CreatedUtc = DateTime.UtcNow,
            Json = configuration.Json,
            Deleted = false,
            IsActive = configuration.IsActive,
            Systems = StringToList<ConfigSystem>(ListToString(configuration.Systems)),
            Environments = StringToList<ConfigEnvironment>(ListToString(configuration.Environments))
        };
        return newConfig;
    }

    public static ConfigurationHeader Copy(ConfigurationHeader header)
    {
        return new ConfigurationHeader
        {
            Id = header.Id,
            Name = header.Name,
            CreatedUtc = header.CreatedUtc,
            UpdateUtc = header.UpdateUtc,
            Deleted = header.Deleted,
            IsActive = header.IsActive,
            Configurations = header.Configurations.Select(Copy).ToList()
        };
    }
}