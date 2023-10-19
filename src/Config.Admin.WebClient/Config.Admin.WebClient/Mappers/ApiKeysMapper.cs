using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Mappers;

public class ApiKeysMapper
{
    public static Model.ApiKeys ToClient(Api.Model.ApiKeys apiKeys)
    {
        return new Model.ApiKeys
        {
            Keys = apiKeys.Keys.Select(k => new ApiKey {Key = k}).ToList()
        };
    }
    
    public static Api.Model.ApiKeys ToApi(Model.ApiKeys apiKeys)
    {
        return new Api.Model.ApiKeys
        {
            Keys = apiKeys.Keys.Select(k => k.Key).ToList()
        };
    }
}