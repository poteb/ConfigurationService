using System.Text.Json;
using pote.Config.Admin.Api.Model;

namespace pote.Config.Admin.Api.Mappers;

public static class SecretMapper
{
    public static DbModel.SecretHeader ToDb(SecretHeader apiHeader)
    {
        return new DbModel.SecretHeader
        {
            Id = apiHeader.Id,
            Name = apiHeader.Name,
            CreatedUtc = apiHeader.CreatedUtc,
            UpdateUtc = apiHeader.UpdateUtc,
            Deleted = apiHeader.Deleted,
            IsActive = apiHeader.IsActive,
            Secrets = ToDb(apiHeader.Secrets)
        };
    }

    public static SecretHeader ToApi(DbModel.SecretHeader dbHeader, List<DbModel.Application> applications, List<DbModel.Environment> environments)
    {
        return new SecretHeader
        {
            Id = dbHeader.Id,
            Name = dbHeader.Name,
            CreatedUtc = dbHeader.CreatedUtc,
            UpdateUtc = dbHeader.UpdateUtc,
            Deleted = dbHeader.Deleted,
            IsActive = dbHeader.IsActive,
            Secrets = ToApi(dbHeader.Secrets, applications, environments)
        };
    }

    public static DbModel.Secret ToDb(Secret apiSecret)
    {
        return new DbModel.Secret
        {
            HeaderId = apiSecret.HeaderId,
            Id = apiSecret.Id,
            CreatedUtc = apiSecret.CreatedUtc,
            Value = apiSecret.Value,
            ValueType = apiSecret.ValueType,
            Applications = JsonSerializer.Deserialize<List<Application>>(apiSecret.Applications)?.Select(s => s.Id).ToList() ?? new List<string>(),
            Environments = JsonSerializer.Deserialize<List<Model.Environment>>(apiSecret.Environments)?.Select(s => s.Id).ToList() ?? new List<string>(),
            Deleted = apiSecret.Deleted,
            IsActive = apiSecret.IsActive
        };
    }

    public static Secret ToApi(DbModel.Secret dbSecret, List<DbModel.Application> applications, List<DbModel.Environment> environments)
    {
        return new Secret
        {
            HeaderId = dbSecret.HeaderId,
            Id = dbSecret.Id,
            CreatedUtc = dbSecret.CreatedUtc,
            Value = dbSecret.Value,
            ValueType = dbSecret.ValueType,
            Applications = GetFullApplications(dbSecret, applications),
            Environments = GetFullEnvironments(dbSecret, environments),
            Deleted = dbSecret.Deleted,
            IsActive = dbSecret.IsActive
        };
    }

    private static string GetFullApplications(DbModel.Secret dbSecret, List<DbModel.Application> applications)
    {
        return !dbSecret.Applications.Any() 
            ? JsonSerializer.Serialize(dbSecret.Applications) 
            : JsonSerializer.Serialize(applications.Where(x => dbSecret.Applications.Any(y => y == x.Id)));
    }

    private static string GetFullEnvironments(DbModel.Secret dbSecret, List<DbModel.Environment> environments)
    {
        return !dbSecret.Environments.Any()
            ? JsonSerializer.Serialize(dbSecret.Environments) 
            : JsonSerializer.Serialize(environments.Where(x => dbSecret.Environments.Any(y => y == x.Id)));
    }

    public static List<DbModel.Secret> ToDb(List<Secret> secrets)
    {
        return secrets.Select(ToDb).ToList();
    }

    public static List<Secret> ToApi(List<DbModel.Secret> secrets, List<DbModel.Application> applications, List<DbModel.Environment> environments)
    {
        return secrets.Select(c => ToApi(c, applications, environments)).ToList();
    }

    public static List<DbModel.SecretHeader> ToDb(List<SecretHeader> headers)
    {
        return headers.Select(ToDb).ToList();
    }

    public static List<SecretHeader> ToApi(List<DbModel.SecretHeader> headers, List<DbModel.Application> applications, List<DbModel.Environment> environments)
    {
        return headers.Select(h => ToApi(h, applications, environments)).ToList();
    }
}

// public static class secretsMapperExtensions
// {
//     public static Model.Secret ToApi(this DbModel.Secret secrets)
//     {
//         return SecretMapper.ToApi(secrets);
//     }
//     
//     public static DbModel.Secret ToDb(this Model.Secret secrets)
//     {
//         return SecretMapper.ToDb(secrets);
//     }
// }