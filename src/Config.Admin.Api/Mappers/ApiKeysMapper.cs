namespace pote.Config.Admin.Api.Mappers;

public static class ApiKeysMapper
{
    public static Model.ApiKeys ToApi(DbModel.ApiKeys apiKeys)
    {
        return new Model.ApiKeys
        {
            Keys = apiKeys.Keys
        };
    }
    
    public static DbModel.ApiKeys ToDb(Model.ApiKeys apiKeys)
    {
        return new DbModel.ApiKeys
        {
            Keys = apiKeys.Keys
        };
    }
}