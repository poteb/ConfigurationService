using System.Text.Json;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Mappers;

public static class ConfigurationMapper
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
            Applications = StringToList<Application>(configuration.Applications),
            Environments = StringToList<ConfigEnvironment>(configuration.Environments),
            History = ToClient(configuration.History)
        };
    }

    private static List<T> StringToList<T>(string itemsJson)
    {
        return JsonSerializer.Deserialize<List<T>>(itemsJson) ?? new List<T>();
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
            Applications = ListToString(configuration.Applications),
            Environments = ListToString(configuration.Environments)
        };
    }

    private static string ListToString<T>(List<T> items)
    {
        return JsonSerializer.Serialize(items);
    }

    public static List<Configuration> ToClient(IReadOnlyList<Api.Model.Configuration> configurations)
    {
        return configurations.Select(ToClient).ToList();
    }

    public static List<Api.Model.Configuration> ToApi(IReadOnlyList<Configuration> configurations)
    {
        return configurations.Select(ToApi).ToList();
    }

    public static List<ConfigurationHeader> ToClient(IReadOnlyList<Api.Model.ConfigurationHeader> headers)
    {
        return headers.Select(ToClient).ToList();
    }

    public static List<Api.Model.ConfigurationHeader> ToApi(IReadOnlyList<ConfigurationHeader> headers)
    {
        return headers.Select(ToApi).ToList();
    }

    /// <summary>Creates a copy of given <see cref="Configuration"/> except the HeaderId property</summary>
    public static Configuration Copy(Configuration configuration)
    {
        var newConfig = new Configuration
        {
            Id = configuration.Id,
            CreatedUtc = configuration.CreatedUtc,
            Json = configuration.Json,
            Deleted = false,
            IsActive = configuration.IsActive,
            Applications = StringToList<Application>(ListToString(configuration.Applications)),
            Environments = StringToList<ConfigEnvironment>(ListToString(configuration.Environments)), 
            Index =configuration.Index 
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

    public static void CopyTo(this Configuration from, Configuration to)
    {

        to.Id = from.Id;
        to.CreatedUtc = from.CreatedUtc;
        to.Json = from.Json;
        to.Deleted = false;
        to.IsActive = from.IsActive;
        to.Applications = StringToList<Application>(ListToString(from.Applications));
        to.Environments = StringToList<ConfigEnvironment>(ListToString(from.Environments));
        to.Index = from.Index;
    }
}