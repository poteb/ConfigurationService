namespace pote.Config.Admin.Api.Auth;

/// <summary>
/// The provider-agnostic claims contract: authorization is built exclusively on
/// name, role, and the guest claim (which only the Local provider ever issues).
/// </summary>
public static class AuthPolicies
{
    public const string SchemeName = "LocalSession";

    /// <summary>Authenticated, not guest. The fallback policy for all endpoints.</summary>
    public const string RealUser = "RealUser";
    /// <summary>Role claim == Admin (user management).</summary>
    public const string AdminOnly = "AdminOnly";
    /// <summary>Guest claim present (only the create-first-user endpoint).</summary>
    public const string GuestOnly = "GuestOnly";

    public const string GuestClaim = "guest";
    public const string UserIdClaim = "userId";
}
