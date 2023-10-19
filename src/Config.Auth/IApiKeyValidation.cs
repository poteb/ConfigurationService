namespace pote.Config.Auth;

public interface IApiKeyValidation
{
    Task<bool> IsValidApiKey(string userApiKey);
}