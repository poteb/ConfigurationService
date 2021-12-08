namespace pote.Config.Admin.WebClient.Mappers;

public class ConfigurationMapper
{
    public static Model.Configuration ToClient(Api.Model.Configuration configuration)
    {
        return new Model.Configuration
        {
            Gid = configuration.Gid,
            Name = configuration.Name,
            CreatedUtc = configuration.CreatedUtc,
            Json = configuration.Json,
            Integrations = configuration.Integrations,
            Deleted = configuration.Deleted,
            IsActive = configuration.IsActive
        };
    }

    public static Api.Model.Configuration ToApi(Model.Configuration configuration)
    {
        return new Api.Model.Configuration
        {
            Gid = configuration.Gid,
            Name = configuration.Name,
            CreatedUtc = configuration.CreatedUtc,
            Json = configuration.Json,
            Integrations = configuration.Integrations,
            Deleted = configuration.Deleted,
            IsActive = configuration.IsActive
        };
    }

    public static List<Model.Configuration> ToClient(List<Api.Model.Configuration> configurations)
    {
        return configurations.Select(ToClient).ToList();
    }

    public static List<Api.Model.Configuration> ToApi(List<Model.Configuration> configurations)
    {
        return configurations.Select(ToApi).ToList();
    }
}