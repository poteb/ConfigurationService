using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace pote.Config.Api.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal class ApiKeyAttribute : ServiceFilterAttribute
{
    public ApiKeyAttribute()
        : base(typeof(ApiKeyAuthenticationFilter))
    {
    }
}
// {
//     private const string ApiKeyHeaderName = "X-Api-Key";
//     
//     public void OnAuthorization(AuthorizationFilterContext context)
//     {
//         if (!IsApiKeyValid(context.HttpContext))
//         {
//             context.Result = new UnauthorizedResult();
//         }
//     }
//
//     private static bool IsApiKeyValid(HttpContext context)
//     {
//         if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
//             return false;
//
//         var apiKey = extractedApiKey.ToString();
//
//         return apiKey.Equals("1234567890");
//     }
// }