using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

namespace pote.Config.DataProvider.File;

/// <summary>
/// The file provider has no user storage (ADR-0005: SQL Server is the primary provider).
/// Every member throws so a misconfigured instance fails fast at startup.
/// </summary>
public class UserDataAccess : IUserDataAccess
{
    public const string NotSupportedMessage = "User login requires the SqlServer data provider. Set DataProvider=SqlServer and configure SqlServer:ConnectionString.";

    private static NotSupportedException NotSupported() => new(NotSupportedMessage);

    public Task<int> CountUsers(CancellationToken cancellationToken) => throw NotSupported();
    public Task<List<User>> GetUsers(CancellationToken cancellationToken) => throw NotSupported();
    public Task<User?> GetUserByUsername(string username, CancellationToken cancellationToken) => throw NotSupported();
    public Task<User?> GetUserById(Guid id, CancellationToken cancellationToken) => throw NotSupported();
    public Task InsertUser(User user, CancellationToken cancellationToken) => throw NotSupported();
    public Task<bool> SoftDeleteUser(Guid id, CancellationToken cancellationToken) => throw NotSupported();
    public Task RestoreUser(Guid id, CancellationToken cancellationToken) => throw NotSupported();
    public Task PermanentlyDeleteUser(Guid id, CancellationToken cancellationToken) => throw NotSupported();
    public Task HardDeleteGuest(CancellationToken cancellationToken) => throw NotSupported();
    public Task<bool> UpdateRole(Guid id, string role, CancellationToken cancellationToken) => throw NotSupported();
    public Task UpdateLastLogin(Guid id, DateTime utc, CancellationToken cancellationToken) => throw NotSupported();
    public Task<bool> UpdatePasswordHash(Guid id, string newHash, string? expectedOldHash, CancellationToken cancellationToken) => throw NotSupported();
    public Task<List<UserInvite>> GetInvites(CancellationToken cancellationToken) => throw NotSupported();
    public Task UpsertInvite(UserInvite invite, CancellationToken cancellationToken) => throw NotSupported();
    public Task<Guid?> DeleteInvite(string username, CancellationToken cancellationToken) => throw NotSupported();
    public Task<UserInvite?> ConsumeInvite(string token, CancellationToken cancellationToken) => throw NotSupported();
    public Task UpsertReset(PasswordReset reset, CancellationToken cancellationToken) => throw NotSupported();
    public Task<PasswordReset?> ConsumeReset(string token, CancellationToken cancellationToken) => throw NotSupported();
    public Task DeleteResetsForUser(Guid userId, CancellationToken cancellationToken) => throw NotSupported();
    public Task InsertSession(Session session, CancellationToken cancellationToken) => throw NotSupported();
    public Task<SessionUser?> GetSessionUser(string token, CancellationToken cancellationToken) => throw NotSupported();
    public Task DeleteSession(string token, CancellationToken cancellationToken) => throw NotSupported();
    public Task DeleteSessionsForUser(Guid userId, CancellationToken cancellationToken) => throw NotSupported();
    public Task DeleteOtherSessionsForUser(Guid userId, string keepToken, CancellationToken cancellationToken) => throw NotSupported();
    public Task CleanupExpired(CancellationToken cancellationToken) => throw NotSupported();
}
