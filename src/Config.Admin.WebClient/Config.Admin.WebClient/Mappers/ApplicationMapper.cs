namespace pote.Config.Admin.WebClient.Mappers;

public class ApplicationMapper
{
    public static Model.Application ToClient(Api.Model.Application application)
    {
        return new Model.Application
        {
            Id = application.Id,
            Name = application.Name
        };
    }

    public static Api.Model.Application ToApi(Model.Application application)
    {
        return new Api.Model.Application
        {
            Id = application.Id,
            Name = application.Name
        };
    }

    public static List<Model.Application> ToClient(List<Api.Model.Application> applications)
    {
        return applications.Select(ToClient).ToList();
    }

    public static List<Api.Model.Application> ToApi(List<Model.Application> applications)
    {
        return applications.Select(ToApi).ToList();
    }
}