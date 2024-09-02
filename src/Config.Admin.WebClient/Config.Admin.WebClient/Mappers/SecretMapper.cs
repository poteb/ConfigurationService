using System.Text.Json;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Mappers;

public static class SecretMapper
{
    public static SecretHeader ToClient(Api.Model.SecretHeader secret)
    {
        return new SecretHeader
        {
            Id = secret.Id,
            Name = secret.Name,
            CreatedUtc = secret.CreatedUtc,
            UpdateUtc = secret.UpdateUtc,
            Deleted = secret.Deleted,
            IsActive = secret.IsActive,
            Secrets = ToClient(secret.Secrets),
        };
    }

    public static Api.Model.SecretHeader ToApi(SecretHeader secret)
    {
        return new Api.Model.SecretHeader
        {
            Id = secret.Id,
            Name = secret.Name,
            CreatedUtc = secret.CreatedUtc,
            UpdateUtc = secret.UpdateUtc,
            Deleted = secret.Deleted,
            IsActive = secret.IsActive,
            Secrets = ToApi(secret.Secrets),
        };
    }

    public static Secret ToClient(Api.Model.Secret secret)
    {
        return new Secret
        {
            HeaderId = secret.HeaderId,
            Id = secret.Id,
            CreatedUtc = secret.CreatedUtc,
            Value = secret.Value,
            Deleted = secret.Deleted,
            IsActive = secret.IsActive,
            Applications = StringToList<Application>(secret.Applications),
            Environments = StringToList<ConfigEnvironment>(secret.Environments)
        };
    }
    
    private static List<T> StringToList<T>(string itemsJson)
    {
        return JsonSerializer.Deserialize<List<T>>(itemsJson) ?? new List<T>();
    }
    
    public static Api.Model.Secret ToApi(Secret secret)
    {
        return new Api.Model.Secret
        {
            HeaderId = secret.HeaderId,
            Id = secret.Id,
            CreatedUtc = secret.CreatedUtc,
            Value = secret.Value,
            ValueType = secret.ValueType,
            Deleted = secret.Deleted,
            IsActive = secret.IsActive,
            Applications = ListToString(secret.Applications),
            Environments = ListToString(secret.Environments)
        };
    }
    
    public static List<SecretHeader> ToClient(IReadOnlyList<Api.Model.SecretHeader> secrets)
    {
        return secrets.Select(ToClient).ToList();
    }

    public static List<Api.Model.SecretHeader> ToApi(List<SecretHeader> secrets)
    {
        return secrets.Select(ToApi).ToList();
    }
    
    private static string ListToString<T>(List<T> items)
    {
        return JsonSerializer.Serialize(items);
    }
    
    public static List<Model.Secret> ToClient(IReadOnlyList<Api.Model.Secret> secrets)
    {
        return secrets.Select(ToClient).ToList();
    }
    
    public static List<Api.Model.Secret> ToApi(IReadOnlyList<Model.Secret> secrets)
    {
        return secrets.Select(ToApi).ToList();
    }
    
    public static Secret Copy(Secret secret, bool generateNewId = false)
    {
        var newSecret = new Secret
        {
            Id = generateNewId ? Guid.NewGuid().ToString() : secret.Id,
            CreatedUtc = secret.CreatedUtc,
            Value = secret.Value,
            ValueType = secret.ValueType,
            Deleted = false,
            IsActive = secret.IsActive,
            Applications = StringToList<Application>(ListToString(secret.Applications)),
            Environments = StringToList<ConfigEnvironment>(ListToString(secret.Environments)), 
            Index = secret.Index 
        };
        return newSecret;
    }

    public static SecretHeader Copy(SecretHeader header, bool generateNewId = false)
    {
        return new SecretHeader
        {
            Id = generateNewId ? Guid.NewGuid().ToString() : header.Id,
            Name = header.Name,
            CreatedUtc = header.CreatedUtc,
            UpdateUtc = header.UpdateUtc,
            Deleted = header.Deleted,
            IsActive = header.IsActive,
            Secrets = header.Secrets.Select(c => Copy(c, generateNewId)).ToList(),
        };
    }

    public static void CopyTo(this Secret from, Secret to)
    {
        to.Id = from.Id;
        to.CreatedUtc = from.CreatedUtc;
        to.Value = from.Value;
        to.ValueType = from.ValueType;
        to.Deleted = false;
        to.IsActive = from.IsActive;
        to.Applications = StringToList<Application>(ListToString(from.Applications));
        to.Environments = StringToList<ConfigEnvironment>(ListToString(from.Environments));
        to.Index = from.Index;
    }
}