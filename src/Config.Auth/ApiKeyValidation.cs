using pote.Config.DataProvider.Interfaces;

namespace pote.Config.Auth;

public class ApiKeyValidation : IApiKeyValidation
{
    private readonly IDataProvider _dataProvider;

    public ApiKeyValidation(IDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }
    public async Task<bool> IsValidApiKey(string userApiKey)
    {
        if (string.IsNullOrWhiteSpace(userApiKey)) return false;
        var apiKeys = await _dataProvider.GetApiKeys(CancellationToken.None);
        return apiKeys.Keys.Any(a => a.Equals(userApiKey));
    }
}