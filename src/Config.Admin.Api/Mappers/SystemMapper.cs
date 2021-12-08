namespace pote.Config.Admin.Api.Mappers;

public class SystemMapper
{
    public static DbModel.System ToDb(Model.System system)
    {
        return new DbModel.System
        {
            Id = system.Id,
            Name = system.Name
        };
    }

    public static Model.System ToApi(DbModel.System system)
    {
        return new Model.System
        {
            Id = system.Id,
            Name = system.Name
        };
    }

    public static List<DbModel.System> ToDb(List<Model.System> systems)
    {
        return systems.Select(ToDb).ToList();
    }

    public static List<Model.System> ToApi(List<DbModel.System> systems)
    {
        return systems.Select(ToApi).ToList();
    }
}