namespace pote.Config.Admin.Api.Mappers;

public class ApplicationMapper
{
    public static DbModel.Application ToDb(Model.Application application)
    {
        return new DbModel.Application
        {
            Id = application.Id,
            Name = application.Name
        };
    }

    public static Model.Application ToApi(DbModel.Application application)
    {
        return new Model.Application
        {
            Id = application.Id,
            Name = application.Name
        };
    }

    public static List<DbModel.Application> ToDb(List<Model.Application> applications)
    {
        return applications.Select(ToDb).ToList();
    }

    public static List<Model.Application> ToApi(List<DbModel.Application> applications)
    {
        return applications.Select(ToApi).ToList();
    }
}