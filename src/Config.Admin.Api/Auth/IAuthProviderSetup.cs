using Microsoft.AspNetCore.Authentication;

namespace pote.Config.Admin.Api.Auth;

/// <summary>
/// Registration seam for pluggable auth providers (ADR-0002). A provider
/// contributes its services and its authentication handler; everything outside
/// the provider authorizes on claims only. Add a provider by implementing this
/// interface and registering it in Program.cs for its Auth:Provider value.
/// </summary>
public interface IAuthProviderSetup
{
    /// <summary>Value matched against the Auth:Provider config setting (e.g. "Local").</summary>
    string Type { get; }

    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Register the authentication scheme(s) that turn requests into claims.</summary>
    void ConfigureAuthentication(AuthenticationBuilder builder);

    /// <summary>Anonymous metadata served by GET api/auth/provider so the SPA can adapt its login UI.</summary>
    object ProviderMetadata { get; }
}
