namespace pote.Config.Admin.Api.Mappers;

public static class ApiKeysMapper
{
    public static Model.ApiKeys ToApi(DbModel.ApiKeys apiKeys)
    {
        return new Model.ApiKeys
        {
            Keys = apiKeys.Keys.Select(k => new Model.ApiKeyEntry { Name = k.Name, Key = k.Key }).ToList()
        };
    }

    public static DbModel.ApiKeys ToDb(Model.ApiKeys apiKeys)
    {
        return new DbModel.ApiKeys
        {
            Keys = apiKeys.Keys.Select(k => new DbModel.ApiKeyEntry { Name = k.Name, Key = k.Key }).ToList()
        };
    }
}