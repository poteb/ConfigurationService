using pote.Config.DataProvider.Interfaces;

namespace pote.Config.Api.Authentication;

public class ApiKeyValidation : IApiKeyValidation
{
    private readonly IConfiguration _configuration;
    private readonly IDataProvider _dataProvider;

    public ApiKeyValidation(IConfiguration configuration, IDataProvider dataProvider)
    {
        _configuration = configuration;
        _dataProvider = dataProvider;
    }
    public async Task<bool> IsValidApiKey(string userApiKey)
    {
        if (string.IsNullOrWhiteSpace(userApiKey))
            return false;
        var apiKeys = _dataProvider.GetApiKeys(CancellationToken.None);
        // string? apiKey = _configuration.GetValue<string>(Constants.ApiKeyName);
        // if (apiKey == null || apiKey != userApiKey)
        //     return false;
        return true;
    }
}