namespace pote.Config.Api.Authentication;

public interface IApiKeyValidation
{
    Task<bool> IsValidApiKey(string userApiKey);
}