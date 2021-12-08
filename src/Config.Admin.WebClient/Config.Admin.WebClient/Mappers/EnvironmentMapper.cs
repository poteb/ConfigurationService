namespace pote.Config.Admin.WebClient.Mappers;

public class EnvironmentMapper
{
    public static Model.Environment ToClient(Api.Model.Environment environment)
    {
        return new Model.Environment
        {
            Id = environment.Id,
            Name = environment.Name
        };
    }

    public static Api.Model.Environment ToApi(Model.Environment environment)
    {
        return new Api.Model.Environment
        {
            Id = environment.Id,
            Name = environment.Name
        };
    }

    public static List<Model.Environment> ToClient(List<Api.Model.Environment> environments)
    {
        return environments.Select(ToClient).ToList();
    }

    public static List<Api.Model.Environment> ToApi(List<Model.Environment> environments)
    {
        return environments.Select(ToApi).ToList();
    }
}