using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace pote.Config.Auth;

public class ApiKeyAuthenticationFilter : IAsyncAuthorizationFilter
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly IApiKeyValidation _apiKeyValidation;

    public ApiKeyAuthenticationFilter(IApiKeyValidation apiKeyValidation)
    {
        _apiKeyValidation = apiKeyValidation;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (HttpMethods.IsOptions(context.HttpContext.Request.Method))
            return;

        var userApiKey = context.HttpContext.Request.Headers[ApiKeyHeaderName].ToString();

        if (string.IsNullOrWhiteSpace(userApiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!await _apiKeyValidation.IsValidApiKey(userApiKey))
            context.Result = new UnauthorizedResult();
    }
}