using Microsoft.AspNetCore.Mvc;

namespace pote.Config.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : ServiceFilterAttribute
{
    public ApiKeyAttribute()
        : base(typeof(ApiKeyAuthenticationFilter))
    {
    }
}