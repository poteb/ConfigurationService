using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using pote.Config.Admin.Api.Auth;
using pote.Config.Admin.Api.Helpers;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    /// <summary>Failed logins take at least this long, uniformly, regardless of failure cause.</summary>
    public static readonly TimeSpan MinimumFailureDuration = TimeSpan.FromMilliseconds(500);

    private readonly ILogger<AuthController> _logger;
    private readonly AuthService _authService;
    private readonly IUserDataAccess _users;
    private readonly IAuthProviderSetup _provider;
    private readonly IAuditLogHandler _auditLogHandler;

    public AuthController(ILogger<AuthController> logger, AuthService authService, IUserDataAccess users,
        IAuthProviderSetup provider, IAuditLogHandler auditLogHandler)
    {
        _logger = logger;
        _authService = authService;
        _users = users;
        _provider = provider;
        _auditLogHandler = auditLogHandler;
    }

    [HttpGet("provider")]
    [AllowAnonymous]
    public ActionResult<object> GetProvider() => Ok(_provider.ProviderMetadata);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var started = DateTime.UtcNow;
        try
        {
            var result = await _authService.Login(request.Username, request.Password, cancellationToken);
            if (result == null)
            {
                var attempted = (request.Username ?? string.Empty).Trim();
                if (attempted.Length > 100) attempted = attempted[..100];
                await this.AuditLog(Guid.Empty.ToString(), "LoginFailed", _auditLogHandler.AuditLogUser, attempted);
                await DelayToUniformDuration(started, cancellationToken);
                return Unauthorized();
            }
            await this.AuditLog(result.UserId.ToString(), "Login", _auditLogHandler.AuditLogUser, result.Username, actingUsername: result.Username);
            return Ok(ToResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed unexpectedly");
            await DelayToUniformDuration(started, cancellationToken);
            return Unauthorized();
        }
    }

    [HttpPost("redeem")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Redeem([FromBody] RedeemRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var result = await _authService.Redeem(request.Token, request.Password, cancellationToken);
            if (result == null)
                return BadRequest(new { errors = new[] { "The link is invalid or has expired." } });
            if (result.Redemption == "invite")
                await this.AuditLog(result.UserId.ToString(), "UserCreated", _auditLogHandler.AuditLogUser, $"{result.Username} ({result.Role}, via invite)", actingUsername: result.Username);
            else if (result.Redemption == "reset")
                await this.AuditLog(result.UserId.ToString(), "PasswordChanged", _auditLogHandler.AuditLogUser, $"{result.Username} (via reset link)", actingUsername: result.Username);
            await this.AuditLog(result.UserId.ToString(), "Login", _auditLogHandler.AuditLogUser, result.Username, actingUsername: result.Username);
            return Ok(ToResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redeem failed unexpectedly");
            return BadRequest(new { errors = new[] { "The link is invalid or has expired." } });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        var token = User.FindFirstValue("sessionToken");
        if (!string.IsNullOrEmpty(token))
            await _users.DeleteSession(token, cancellationToken);
        return Ok();
    }

    [HttpPost("change-password")]
    [Authorize(Policy = AuthPolicies.RealUser)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(AuthPolicies.UserIdClaim)!);
        var keepToken = User.FindFirstValue("sessionToken") ?? string.Empty;
        var passwordError = PasswordPolicy.Validate(request.NewPassword);
        if (passwordError != null)
            return BadRequest(new { errors = new[] { passwordError } });

        var ok = await _authService.ChangePassword(userId, request.CurrentPassword, request.NewPassword, keepToken, cancellationToken);
        if (!ok)
            return BadRequest(new { errors = new[] { "The current password is incorrect." } });

        await this.AuditLog(userId.ToString(), "PasswordChanged", _auditLogHandler.AuditLogUser);
        return Ok();
    }

    private static async Task DelayToUniformDuration(DateTime started, CancellationToken cancellationToken)
    {
        var elapsed = DateTime.UtcNow - started;
        if (elapsed < MinimumFailureDuration)
            await Task.Delay(MinimumFailureDuration - elapsed, cancellationToken);
    }

    private static LoginResponse ToResponse(LoginResult result) => new()
    {
        Token = result.Token,
        ExpiresUtc = result.ExpiresUtc,
        Username = result.Username,
        Role = result.Role,
        IsGuest = result.IsGuest
    };
}
