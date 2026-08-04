using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using pote.Config.DbModel;

namespace pote.Config.Admin.Api.Auth;

/// <summary>
/// The Local (database users) auth provider: session-token authentication,
/// AuthService, guest bootstrap. A future Oidc provider would instead register
/// JwtBearer validation against an external authority and no local services.
/// </summary>
public class LocalAuthProviderSetup : IAuthProviderSetup
{
    public string Type => "Local";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<AuthService>();
    }

    public void ConfigureAuthentication(AuthenticationBuilder builder)
    {
        builder.AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(AuthPolicies.SchemeName, null);
    }

    public object ProviderMetadata => new { type = "local" };
}
