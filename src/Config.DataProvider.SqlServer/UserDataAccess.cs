using Dapper;
using Microsoft.Data.SqlClient;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

namespace pote.Config.DataProvider.SqlServer;

public class UserDataAccess : IUserDataAccess
{
    private readonly SqlConnectionFactory _connectionFactory;

    public UserDataAccess(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CountUsers(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM [Users]", cancellationToken: cancellationToken));
    }

    public async Task<List<User>> GetUsers(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var users = await conn.QueryAsync<User>(new CommandDefinition(
            "SELECT * FROM [Users] ORDER BY [Username]", cancellationToken: cancellationToken));
        return users.ToList();
    }

    public async Task<User?> GetUserByUsername(string username, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(
            "SELECT * FROM [Users] WHERE [Username] = @username", new { username }, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(
            "SELECT * FROM [Users] WHERE [Id] = @id", new { id }, cancellationToken: cancellationToken));
    }

    public async Task InsertUser(User user, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO [Users] ([Id], [Username], [PasswordHash], [Role], [IsGuest], [Deleted], [CreatedUtc], [LastLoginUtc])
            VALUES (@Id, @Username, @PasswordHash, @Role, @IsGuest, @Deleted, @CreatedUtc, @LastLoginUtc)
            """;
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(sql, user, cancellationToken: cancellationToken));
        }
        catch (SqlException ex) when (user.IsGuest && ex.Number is 2601 or 2627)
        {
            // Multi-instance guest bootstrap: losing the duplicate-key race is success.
        }
    }

    public async Task<bool> SoftDeleteUser(Guid id, CancellationToken cancellationToken)
    {
        // The last-active-admin invariant is re-checked inside the statement so
        // concurrent deletions/demotions cannot strand the system without an admin.
        const string sql = """
            UPDATE [Users] SET [Deleted] = 1
            WHERE [Id] = @id AND [Deleted] = 0
              AND ([Role] <> N'Admin' OR EXISTS (
                    SELECT 1 FROM [Users] u2
                    WHERE u2.[Role] = N'Admin' AND u2.[Deleted] = 0 AND u2.[Id] <> @id))
            """;
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { id }, tx, cancellationToken: cancellationToken));
        if (rows == 1)
        {
            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [Sessions] WHERE [UserId] = @id", new { id }, tx, cancellationToken: cancellationToken));
            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM [PasswordResets] WHERE [UserId] = @id", new { id }, tx, cancellationToken: cancellationToken));
        }
        await tx.CommitAsync(cancellationToken);
        return rows == 1;
    }

    public async Task RestoreUser(Guid id, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE [Users] SET [Deleted] = 0 WHERE [Id] = @id AND [Deleted] = 1", new { id }, cancellationToken: cancellationToken));
    }

    public async Task PermanentlyDeleteUser(Guid id, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [Users] WHERE [Id] = @id AND [Deleted] = 1", new { id }, cancellationToken: cancellationToken));
    }

    public async Task HardDeleteGuest(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [Users] WHERE [IsGuest] = 1", cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateRole(Guid id, string role, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE [Users] SET [Role] = @role
            WHERE [Id] = @id AND [Deleted] = 0
              AND (@role = N'Admin' OR [Role] <> N'Admin' OR EXISTS (
                    SELECT 1 FROM [Users] u2
                    WHERE u2.[Role] = N'Admin' AND u2.[Deleted] = 0 AND u2.[Id] <> @id))
            """;
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { id, role }, cancellationToken: cancellationToken));
        return rows == 1;
    }

    public async Task UpdateLastLogin(Guid id, DateTime utc, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE [Users] SET [LastLoginUtc] = @utc WHERE [Id] = @id", new { id, utc }, cancellationToken: cancellationToken));
    }

    public async Task UpdatePasswordHash(Guid id, string newHash, string? expectedOldHash, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE [Users] SET [PasswordHash] = @newHash
            WHERE [Id] = @id AND (@expectedOldHash IS NULL OR [PasswordHash] = @expectedOldHash)
            """;
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { id, newHash, expectedOldHash }, cancellationToken: cancellationToken));
    }

    public async Task<List<UserInvite>> GetInvites(CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        var invites = await conn.QueryAsync<UserInvite>(new CommandDefinition(
            "SELECT * FROM [UserInvites] WHERE [ExpiresUtc] > GETUTCDATE() ORDER BY [Username]", cancellationToken: cancellationToken));
        return invites.ToList();
    }

    public async Task UpsertInvite(UserInvite invite, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM [UserInvites] WHERE [Username] = @Username;
            INSERT INTO [UserInvites] ([Id], [Token], [Username], [Role], [CreatedBy], [ExpiresUtc])
            VALUES (@Id, @Token, @Username, @Role, @CreatedBy, @ExpiresUtc);
            """;
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(sql, invite, tx, cancellationToken: cancellationToken));
        await tx.CommitAsync(cancellationToken);
    }

    public async Task DeleteInvite(string username, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [UserInvites] WHERE [Username] = @username", new { username }, cancellationToken: cancellationToken));
    }

    public async Task<UserInvite?> ConsumeInvite(string token, CancellationToken cancellationToken)
    {
        // Atomic single-use consumption: concurrent redemptions cannot both get a row.
        const string sql = """
            DELETE FROM [UserInvites]
            OUTPUT DELETED.[Id], DELETED.[Token], DELETED.[Username], DELETED.[Role], DELETED.[CreatedBy], DELETED.[ExpiresUtc]
            WHERE [Token] = @token AND [ExpiresUtc] > GETUTCDATE()
            """;
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<UserInvite>(new CommandDefinition(sql, new { token }, cancellationToken: cancellationToken));
    }

    public async Task UpsertReset(PasswordReset reset, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM [PasswordResets] WHERE [UserId] = @UserId;
            INSERT INTO [PasswordResets] ([Id], [Token], [UserId], [ExpiresUtc])
            VALUES (@Id, @Token, @UserId, @ExpiresUtc);
            """;
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(sql, reset, tx, cancellationToken: cancellationToken));
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<PasswordReset?> ConsumeReset(string token, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM [PasswordResets]
            OUTPUT DELETED.[Id], DELETED.[Token], DELETED.[UserId], DELETED.[ExpiresUtc]
            WHERE [Token] = @token AND [ExpiresUtc] > GETUTCDATE()
            """;
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<PasswordReset>(new CommandDefinition(sql, new { token }, cancellationToken: cancellationToken));
    }

    public async Task DeleteResetsForUser(Guid userId, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [PasswordResets] WHERE [UserId] = @userId", new { userId }, cancellationToken: cancellationToken));
    }

    public async Task InsertSession(Session session, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO [Sessions] ([Token], [UserId], [CreatedUtc], [ExpiresUtc]) VALUES (@Token, @UserId, @CreatedUtc, @ExpiresUtc)",
            session, cancellationToken: cancellationToken));
    }

    public async Task<SessionUser?> GetSessionUser(string token, CancellationToken cancellationToken)
    {
        // Joined to the live user row: role changes, soft deletes, and hard deletes
        // take effect on the next request. Expiry is authoritative here regardless of cleanup.
        const string sql = """
            SELECT u.[Id] AS UserId, u.[Username], u.[Role], u.[IsGuest], s.[ExpiresUtc]
            FROM [Sessions] s
            INNER JOIN [Users] u ON u.[Id] = s.[UserId]
            WHERE s.[Token] = @token AND s.[ExpiresUtc] > GETUTCDATE() AND u.[Deleted] = 0
            """;
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<SessionUser>(new CommandDefinition(sql, new { token }, cancellationToken: cancellationToken));
    }

    public async Task DeleteSession(string token, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [Sessions] WHERE [Token] = @token", new { token }, cancellationToken: cancellationToken));
    }

    public async Task DeleteSessionsForUser(Guid userId, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [Sessions] WHERE [UserId] = @userId", new { userId }, cancellationToken: cancellationToken));
    }

    public async Task DeleteOtherSessionsForUser(Guid userId, string keepToken, CancellationToken cancellationToken)
    {
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [Sessions] WHERE [UserId] = @userId AND [Token] <> @keepToken",
            new { userId, keepToken }, cancellationToken: cancellationToken));
    }

    public async Task CleanupExpired(CancellationToken cancellationToken)
    {
        // Bounded batches so a large backlog on a quiet system cannot block the login path.
        const string sql = """
            DELETE TOP (1000) FROM [Sessions] WHERE [ExpiresUtc] < GETUTCDATE();
            DELETE TOP (1000) FROM [UserInvites] WHERE [ExpiresUtc] < GETUTCDATE();
            DELETE TOP (1000) FROM [PasswordResets] WHERE [ExpiresUtc] < GETUTCDATE();
            """;
        await using var conn = await _connectionFactory.CreateOpenConnection(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
