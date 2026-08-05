using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.Admin.Api.Auth;

/// <summary>
/// Validates opaque bearer tokens against the Sessions table (joined to the live
/// user row, so role changes and deletions take effect on the next request).
/// </summary>
public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IUserDataAccess _users;

    public SessionAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, IUserDataAccess users)
        : base(options, logger, encoder)
    {
        _users = users;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header["Bearer ".Length..].Trim();
        if (token.Length == 0)
            return AuthenticateResult.NoResult();

        var sessionUser = await _users.GetSessionUser(token, Context.RequestAborted);
        if (sessionUser == null)
            return AuthenticateResult.Fail("Invalid or expired session");

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, sessionUser.Username),
            new(ClaimTypes.Role, sessionUser.Role),
            new(AuthPolicies.UserIdClaim, sessionUser.UserId.ToString()),
            new("sessionToken", token)
        };
        if (sessionUser.IsGuest)
            claims.Add(new Claim(AuthPolicies.GuestClaim, "true"));

        var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
