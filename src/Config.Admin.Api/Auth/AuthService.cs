using Microsoft.AspNetCore.Identity;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

namespace pote.Config.Admin.Api.Auth;

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsGuest { get; set; }
}

/// <summary>
/// Core of the Local auth provider: credential verification, session issuance,
/// invite/reset redemption, and the guest bootstrap lifecycle.
/// </summary>
public class AuthService
{
    private readonly IUserDataAccess _users;
    private readonly IPasswordHasher<User> _hasher;
    private readonly AuthSettings _settings;
    private readonly ILogger<AuthService> _logger;

    // A fixed hash used to burn comparable CPU time for unknown usernames so
    // response timing does not reveal whether a username exists.
    private static readonly User DummyUser = new() { Username = "dummy" };
    private readonly string _dummyHash;

    public AuthService(IUserDataAccess users, IPasswordHasher<User> hasher, AuthSettings settings, ILogger<AuthService> logger)
    {
        _users = users;
        _hasher = hasher;
        _settings = settings;
        _logger = logger;
        _dummyHash = _hasher.HashPassword(DummyUser, TokenGenerator.NewToken());
    }

    public async Task<LoginResult?> Login(string username, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(password) || password.Length > PasswordPolicy.MaxLength)
            return null;

        var user = await _users.GetUserByUsername(username?.Trim() ?? string.Empty, cancellationToken);
        if (user == null || user.Deleted)
        {
            _hasher.VerifyHashedPassword(DummyUser, _dummyHash, password);
            return null;
        }

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
            return null;

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Guarded by the old hash so a concurrent password change wins.
            var newHash = _hasher.HashPassword(user, password);
            await _users.UpdatePasswordHash(user.Id, newHash, user.PasswordHash, cancellationToken);
        }

        await _users.UpdateLastLogin(user.Id, DateTime.UtcNow, cancellationToken);
        await _users.CleanupExpired(cancellationToken);
        return await CreateSession(user, cancellationToken);
    }

    public async Task<LoginResult?> Redeem(string token, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;
        // Validate the password before consuming the single-use token, so a
        // rejected password does not burn the link.
        if (PasswordPolicy.Validate(password) != null)
            return null;

        var invite = await _users.ConsumeInvite(token, cancellationToken);
        if (invite != null)
            return await RedeemInvite(invite, password, cancellationToken);

        var reset = await _users.ConsumeReset(token, cancellationToken);
        if (reset != null)
            return await RedeemReset(reset, password, cancellationToken);

        return null;
    }

    private async Task<LoginResult?> RedeemInvite(UserInvite invite, string password, CancellationToken cancellationToken)
    {
        var existing = await _users.GetUserByUsername(invite.Username, cancellationToken);
        if (existing != null)
        {
            _logger.LogWarning("Invite for {Username} redeemed but the username is taken", invite.Username);
            return null;
        }

        var user = new User
        {
            Username = invite.Username,
            Role = invite.Role,
            IsGuest = false,
            CreatedUtc = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, password);
        await _users.InsertUser(user, cancellationToken);
        await _users.UpdateLastLogin(user.Id, DateTime.UtcNow, cancellationToken);
        return await CreateSession(user, cancellationToken);
    }

    private async Task<LoginResult?> RedeemReset(PasswordReset reset, string password, CancellationToken cancellationToken)
    {
        var user = await _users.GetUserById(reset.UserId, cancellationToken);
        if (user == null || user.Deleted || user.IsGuest)
            return null;

        var newHash = _hasher.HashPassword(user, password);
        await _users.UpdatePasswordHash(user.Id, newHash, null, cancellationToken);
        // A reset is typically used after compromise: every prior session dies.
        await _users.DeleteSessionsForUser(user.Id, cancellationToken);
        await _users.UpdateLastLogin(user.Id, DateTime.UtcNow, cancellationToken);
        return await CreateSession(user, cancellationToken);
    }

    public async Task<bool> ChangePassword(Guid userId, string currentPassword, string newPassword, string keepToken, CancellationToken cancellationToken)
    {
        if (PasswordPolicy.Validate(newPassword) != null)
            return false;

        var user = await _users.GetUserById(userId, cancellationToken);
        if (user == null || user.Deleted || user.IsGuest)
            return false;

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            return false;

        var newHash = _hasher.HashPassword(user, newPassword);
        await _users.UpdatePasswordHash(user.Id, newHash, null, cancellationToken);
        await _users.DeleteOtherSessionsForUser(user.Id, keepToken, cancellationToken);
        return true;
    }

    /// <summary>Guest-only path: directly create the first Admin and log them in.</summary>
    public async Task<LoginResult> CreateFirstUser(string username, string password, CancellationToken cancellationToken)
    {
        var usernameError = UsernameRules.Validate(username, out var trimmed);
        if (usernameError != null)
            throw new InvalidOperationException(usernameError);
        var passwordError = PasswordPolicy.Validate(password);
        if (passwordError != null)
            throw new InvalidOperationException(passwordError);

        var existing = await _users.GetUserByUsername(trimmed, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException(existing.Deleted
                ? "That username belongs to a deleted user. Restore it instead."
                : "That username is already taken.");

        var user = new User
        {
            Username = trimmed,
            Role = UserRoles.Admin,
            IsGuest = false,
            CreatedUtc = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, password);
        await _users.InsertUser(user, cancellationToken);
        await _users.UpdateLastLogin(user.Id, DateTime.UtcNow, cancellationToken);
        // The auto-login is the first real login: guest dies here.
        return (await CreateSession(user, cancellationToken))!;
    }

    /// <summary>Empty user store ⇒ (re)seed guest/guest. Idempotent and race-safe (see InsertUser).</summary>
    public async Task EnsureGuestSeeded(CancellationToken cancellationToken)
    {
        if (await _users.CountUsers(cancellationToken) > 0)
            return;
        var guest = new User
        {
            Username = UsernameRules.GuestUsername,
            Role = UserRoles.Admin,
            IsGuest = true,
            CreatedUtc = DateTime.UtcNow
        };
        guest.PasswordHash = _hasher.HashPassword(guest, UsernameRules.GuestUsername);
        await _users.InsertUser(guest, cancellationToken);
        _logger.LogWarning("User store was empty: seeded the guest/guest bootstrap user. Log in as guest and create a real user to claim this instance.");
    }

    private async Task<LoginResult?> CreateSession(User user, CancellationToken cancellationToken)
    {
        if (!user.IsGuest)
            await _users.HardDeleteGuest(cancellationToken);

        var session = new Session
        {
            Token = TokenGenerator.NewToken(),
            UserId = user.Id,
            CreatedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddHours(_settings.SessionLifetimeHours)
        };
        await _users.InsertSession(session, cancellationToken);
        return new LoginResult
        {
            Token = session.Token,
            ExpiresUtc = session.ExpiresUtc,
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            IsGuest = user.IsGuest
        };
    }
}
