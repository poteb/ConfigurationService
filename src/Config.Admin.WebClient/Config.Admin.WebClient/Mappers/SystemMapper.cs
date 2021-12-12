namespace pote.Config.Admin.WebClient.Mappers;

public class SystemMapper
{
    public static Model.ConfigSystem ToClient(Api.Model.System system)
    {
        return new Model.ConfigSystem
        {
            Id = system.Id,
            Name = system.Name
        };
    }

    public static Api.Model.System ToApi(Model.ConfigSystem system)
    {
        return new Api.Model.System
        {
            Id = system.Id,
            Name = system.Name
        };
    }

    public static List<Model.ConfigSystem> ToClient(List<Api.Model.System> systems)
    {
        return systems.Select(ToClient).ToList();
    }

    public static List<Api.Model.System> ToApi(List<Model.ConfigSystem> systems)
    {
        return systems.Select(ToApi).ToList();
    }
}