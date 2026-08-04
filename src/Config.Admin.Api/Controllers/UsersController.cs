using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pote.Config.Admin.Api.Auth;
using pote.Config.Admin.Api.Helpers;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IUserDataAccess _users;
    private readonly AuthService _authService;
    private readonly AuthSettings _settings;
    private readonly IAuditLogHandler _auditLogHandler;

    public UsersController(ILogger<UsersController> logger, IUserDataAccess users, AuthService authService,
        AuthSettings settings, IAuditLogHandler auditLogHandler)
    {
        _logger = logger;
        _users = users;
        _authService = authService;
        _settings = settings;
        _auditLogHandler = auditLogHandler;
    }

    [HttpGet]
    public async Task<ActionResult<UserListResponse>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _users.GetUsers(cancellationToken);
            var invites = await _users.GetInvites(cancellationToken);
            return Ok(new UserListResponse
            {
                Users = users.Select(u => new UserInfo
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role,
                    Deleted = u.Deleted,
                    IsGuest = u.IsGuest,
                    CreatedUtc = u.CreatedUtc,
                    LastLoginUtc = u.LastLoginUtc
                }).ToList(),
                Invites = invites.Select(i => new InviteInfo
                {
                    Username = i.Username,
                    Role = i.Role,
                    CreatedBy = i.CreatedBy,
                    ExpiresUtc = i.ExpiresUtc
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users");
            return Problem(ex.Message);
        }
    }

    [HttpPost("invites")]
    public async Task<ActionResult<TokenResponse>> CreateInvite([FromBody] CreateInviteRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var usernameError = UsernameRules.Validate(request.Username, out var username);
            if (usernameError != null)
                return BadRequest(new { errors = new[] { usernameError } });
            if (!UserRoles.IsValid(request.Role))
                return BadRequest(new { errors = new[] { "Role must be Admin or User." } });

            var existing = await _users.GetUserByUsername(username, cancellationToken);
            if (existing != null)
                return BadRequest(new { errors = new[] { existing.Deleted
                    ? "That username belongs to a deleted user. Restore it instead."
                    : "That username is already taken." } });

            var invite = new UserInvite
            {
                Token = TokenGenerator.NewToken(),
                Username = username,
                Role = request.Role,
                CreatedBy = User.Identity!.Name!,
                ExpiresUtc = DateTime.UtcNow.AddDays(_settings.InviteLifetimeDays)
            };
            await _users.UpsertInvite(invite, cancellationToken);
            await this.AuditLog(invite.Id.ToString(), "InviteCreated", _auditLogHandler.AuditLogUser, $"{username} ({request.Role})");
            return Ok(new TokenResponse { Token = invite.Token, ExpiresUtc = invite.ExpiresUtc });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invite");
            return Problem(ex.Message);
        }
    }

    [HttpDelete("invites/{username}")]
    public async Task<ActionResult> RevokeInvite(string username, CancellationToken cancellationToken)
    {
        try
        {
            await _users.DeleteInvite(username, cancellationToken);
            await this.AuditLog(Guid.Empty.ToString(), "InviteRevoked", _auditLogHandler.AuditLogUser, username);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke invite");
            return Problem(ex.Message);
        }
    }

    [HttpPost("{username}/reset")]
    public async Task<ActionResult<TokenResponse>> CreateResetLink(string username, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var user = await _users.GetUserByUsername(username, cancellationToken);
            if (user == null || user.Deleted || user.IsGuest)
                return BadRequest(new { errors = new[] { "No active user with that username." } });

            var reset = new PasswordReset
            {
                Token = TokenGenerator.NewToken(),
                UserId = user.Id,
                ExpiresUtc = DateTime.UtcNow.AddDays(_settings.InviteLifetimeDays)
            };
            await _users.UpsertReset(reset, cancellationToken);
            await this.AuditLog(user.Id.ToString(), "ResetLinkCreated", _auditLogHandler.AuditLogUser, user.Username);
            return Ok(new TokenResponse { Token = reset.Token, ExpiresUtc = reset.ExpiresUtc });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create reset link");
            return Problem(ex.Message);
        }
    }

    [HttpPut("{username}/role")]
    public async Task<ActionResult> ChangeRole(string username, [FromBody] ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!UserRoles.IsValid(request.Role))
                return BadRequest(new { errors = new[] { "Role must be Admin or User." } });

            var user = await _users.GetUserByUsername(username, cancellationToken);
            if (user == null || user.Deleted || user.IsGuest)
                return BadRequest(new { errors = new[] { "No active user with that username." } });

            if (!await _users.UpdateRole(user.Id, request.Role, cancellationToken))
                return BadRequest(new { errors = new[] { "Cannot demote the last admin." } });

            await this.AuditLog(user.Id.ToString(), "RoleChanged", _auditLogHandler.AuditLogUser, $"{user.Username} -> {request.Role}");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change role");
            return Problem(ex.Message);
        }
    }

    [HttpDelete("{username}")]
    public async Task<ActionResult> Delete(string username, [FromQuery] bool permanent, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _users.GetUserByUsername(username, cancellationToken);
            if (user == null || user.IsGuest)
                return BadRequest(new { errors = new[] { "No user with that username." } });

            if (permanent)
            {
                if (!user.Deleted)
                    return BadRequest(new { errors = new[] { "Only an already deleted user can be permanently deleted." } });
                await _users.PermanentlyDeleteUser(user.Id, cancellationToken);
                await this.AuditLog(user.Id.ToString(), "UserPermanentlyDeleted", _auditLogHandler.AuditLogUser, user.Username);
                return Ok();
            }

            if (string.Equals(user.Username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { errors = new[] { "You cannot delete yourself." } });
            if (user.Deleted)
                return BadRequest(new { errors = new[] { "The user is already deleted." } });

            if (!await _users.SoftDeleteUser(user.Id, cancellationToken))
                return BadRequest(new { errors = new[] { "Cannot delete the last admin." } });

            await this.AuditLog(user.Id.ToString(), "UserDeleted", _auditLogHandler.AuditLogUser, user.Username);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user");
            return Problem(ex.Message);
        }
    }

    [HttpPost("{username}/restore")]
    public async Task<ActionResult> Restore(string username, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _users.GetUserByUsername(username, cancellationToken);
            if (user == null || !user.Deleted)
                return BadRequest(new { errors = new[] { "No deleted user with that username." } });

            await _users.RestoreUser(user.Id, cancellationToken);
            await this.AuditLog(user.Id.ToString(), "UserRestored", _auditLogHandler.AuditLogUser, user.Username);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore user");
            return Problem(ex.Message);
        }
    }

    /// <summary>Guest-only: create the first Admin directly (real users are invite-only).</summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.GuestOnly)]
    public async Task<ActionResult<LoginResponse>> CreateFirstUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var result = await _authService.CreateFirstUser(request.Username, request.Password, cancellationToken);
            await this.AuditLog(Guid.Empty.ToString(), "UserCreated", _auditLogHandler.AuditLogUser, $"{result.Username} (Admin, by guest)");
            return Ok(new LoginResponse
            {
                Token = result.Token,
                ExpiresUtc = result.ExpiresUtc,
                Username = result.Username,
                Role = result.Role,
                IsGuest = result.IsGuest
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user");
            return Problem(ex.Message);
        }
    }
}
