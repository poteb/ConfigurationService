namespace pote.Config.Admin.Api.Mappers;

public class EnvironmentMapper
{
    public static DbModel.Environment ToDb(Model.Environment environment)
    {
        return new DbModel.Environment
        {
            Id = environment.Id,
            Name = environment.Name
        };
    }

    public static Model.Environment ToApi(DbModel.Environment environment)
    {
        return new Model.Environment
        {
            Id = environment.Id,
            Name = environment.Name
        };
    }

    public static List<DbModel.Environment> ToDb(List<Model.Environment> environments)
    {
        return environments.Select(ToDb).ToList();
    }

    public static List<Model.Environment> ToApi(List<DbModel.Environment> environments)
    {
        return environments.Select(ToApi).ToList();
    }
}

public static class EnvironmentMapperExtensions
{
    public static Model.Environment ToApi(this DbModel.Environment environment)
    {
        return EnvironmentMapper.ToApi(environment);
    }
    
    public static DbModel.Environment ToDb(this Model.Environment environment)
    {
        return EnvironmentMapper.ToDb(environment);
    }
}