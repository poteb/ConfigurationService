using pote.Config.DbModel;

namespace pote.Config.DataProvider.Interfaces;

/// <summary>Result of validating a session token: the session joined with its live user row.</summary>
public class SessionUser
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsGuest { get; set; }
    public DateTime ExpiresUtc { get; set; }
}

public interface IUserDataAccess
{
    // Users
    /// <summary>All rows including soft-deleted (guest resurrection triggers only on zero).</summary>
    Task<int> CountUsers(CancellationToken cancellationToken);
    /// <summary>All users including soft-deleted ones.</summary>
    Task<List<User>> GetUsers(CancellationToken cancellationToken);
    /// <summary>Case-insensitive lookup, including soft-deleted users.</summary>
    Task<User?> GetUserByUsername(string username, CancellationToken cancellationToken);
    Task<User?> GetUserById(Guid id, CancellationToken cancellationToken);
    /// <summary>For guest rows the insert is idempotent: losing a duplicate-key race counts as success.</summary>
    Task InsertUser(User user, CancellationToken cancellationToken);
    /// <summary>Soft delete plus session/reset revocation. Returns false when blocked (last active admin).</summary>
    Task<bool> SoftDeleteUser(Guid id, CancellationToken cancellationToken);
    Task RestoreUser(Guid id, CancellationToken cancellationToken);
    /// <summary>Only valid on an already soft-deleted row; FKs cascade sessions/resets.</summary>
    Task PermanentlyDeleteUser(Guid id, CancellationToken cancellationToken);
    /// <summary>Hard-deletes the guest row; FK cascade revokes guest sessions.</summary>
    Task HardDeleteGuest(CancellationToken cancellationToken);
    /// <summary>Returns false when blocked (demoting the last active admin).</summary>
    Task<bool> UpdateRole(Guid id, string role, CancellationToken cancellationToken);
    Task UpdateLastLogin(Guid id, DateTime utc, CancellationToken cancellationToken);
    /// <summary>When expectedOldHash is set, only updates if the stored hash still matches (concurrent-change guard). Returns whether a row was updated.</summary>
    Task<bool> UpdatePasswordHash(Guid id, string newHash, string? expectedOldHash, CancellationToken cancellationToken);

    // Invites
    Task<List<UserInvite>> GetInvites(CancellationToken cancellationToken);
    /// <summary>Replaces any existing invite for the same username.</summary>
    Task UpsertInvite(UserInvite invite, CancellationToken cancellationToken);
    /// <summary>Returns the deleted invite's Id, or null when no invite existed (used for audit).</summary>
    Task<Guid?> DeleteInvite(string username, CancellationToken cancellationToken);
    /// <summary>Atomically consumes (deletes and returns) an unexpired invite. Null when missing or expired.</summary>
    Task<UserInvite?> ConsumeInvite(string token, CancellationToken cancellationToken);

    // Password resets
    /// <summary>Replaces any existing reset for the same user.</summary>
    Task UpsertReset(PasswordReset reset, CancellationToken cancellationToken);
    /// <summary>Atomically consumes (deletes and returns) an unexpired reset. Null when missing or expired.</summary>
    Task<PasswordReset?> ConsumeReset(string token, CancellationToken cancellationToken);
    Task DeleteResetsForUser(Guid userId, CancellationToken cancellationToken);

    // Sessions
    Task InsertSession(Session session, CancellationToken cancellationToken);
    /// <summary>Joins session to user; null when missing, expired, or the user is soft-deleted.</summary>
    Task<SessionUser?> GetSessionUser(string token, CancellationToken cancellationToken);
    Task DeleteSession(string token, CancellationToken cancellationToken);
    Task DeleteSessionsForUser(Guid userId, CancellationToken cancellationToken);
    Task DeleteOtherSessionsForUser(Guid userId, string keepToken, CancellationToken cancellationToken);

    /// <summary>Removes expired sessions/invites/resets in bounded batches. Expiry stays authoritative at validation regardless.</summary>
    Task CleanupExpired(CancellationToken cancellationToken);
}
