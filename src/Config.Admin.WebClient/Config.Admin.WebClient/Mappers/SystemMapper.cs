namespace pote.Config.Admin.WebClient.Mappers;

public class SystemMapper
{
    public static Model.System ToClient(Api.Model.System system)
    {
        return new Model.System
        {
            Id = system.Id,
            Name = system.Name
        };
    }

    public static Api.Model.System ToApi(Model.System system)
    {
        return new Api.Model.System
        {
            Id = system.Id,
            Name = system.Name
        };
    }

    public static List<Model.System> ToClient(List<Api.Model.System> systems)
    {
        return systems.Select(ToClient).ToList();
    }

    public static List<Api.Model.System> ToApi(List<Model.System> systems)
    {
        return systems.Select(ToApi).ToList();
    }
}