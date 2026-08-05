# Admin Login Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Database-backed user login for the admin page per `docs/plans/2026-08-04-admin-login-design.md` (the spec — authoritative for all behavior).

**Architecture:** Local auth provider (DB sessions, opaque bearer tokens) behind a pluggable `IAuthProviderSetup` seam. SQL Server storage via Dapper. SvelteKit SPA gets login/redeem/users pages. Claims contract: `name`, `role` (`Admin`|`User`), `guest`.

**Tech Stack:** ASP.NET Core (net10.0), Dapper, `Microsoft.Extensions.Identity.Core` (PasswordHasher), NUnit + NSubstitute, SvelteKit/Svelte 5 + TS, Vitest, Playwright.

## Global Constraints

- Root namespace prefix `pote.` (e.g. `pote.Config.Admin.Api`).
- Nullable enabled, implicit usings, net10.0 (interfaces lib: follows existing csproj settings).
- Password policy: 16–128 chars, ≥1 lower, ≥1 upper, ≥1 digit, ≥1 special.
- Username: trimmed, 1–100, chars `[A-Za-z0-9.\-_@]`, case-insensitive unique; `guest` reserved.
- Tokens: 256-bit random, url-safe base64 (no padding); single-use; invites/resets 7 days, sessions 8 h absolute.
- All SQL usernames columns: `COLLATE Latin1_General_100_CI_AS`.
- Uniform errors: login → plain 401; redeem failure → generic 400. Minimum ~500 ms response time on failed login, dummy-hash for unknown users.
- Frontend: existing patterns — `ApiResult<T>` from `client.ts`, shadcn-svelte components, Svelte 5 runes.
- Commit after each task (conventional messages, `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`).

---

### Task 1: DbModel entities

**Files:**
- Create: `src/Config.DbModel/User.cs`, `src/Config.DbModel/UserInvite.cs`, `src/Config.DbModel/PasswordReset.cs`, `src/Config.DbModel/Session.cs`

**Produces (later tasks rely on):**

```csharp
namespace pote.Config.DbModel;
public class User {
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.User;   // "Admin" | "User"
    public bool IsGuest { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }
}
public static class UserRoles { public const string Admin = "Admin"; public const string User = "User"; }

public class UserInvite {
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.User;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
}
public class PasswordReset {
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresUtc { get; set; }
}
public class Session {
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
}
```

- [ ] Create the four files; build `dotnet build src/Config.DbModel/Config.DbModel.csproj`; commit.

### Task 2: IUserDataAccess interface

**Files:**
- Create: `src/Config.DataProvider.Interfaces/IUserDataAccess.cs`

**Produces:**

```csharp
namespace pote.Config.DataProvider.Interfaces;
using pote.Config.DbModel;

/// <summary>Result of validating a session token: the session joined with its live user row.</summary>
public class SessionUser {
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsGuest { get; set; }
    public Guid UserId { get; set; }
    public DateTime ExpiresUtc { get; set; }
}

public interface IUserDataAccess {
    // Users
    Task<int> CountUsers(CancellationToken ct);                       // all rows incl. soft-deleted
    Task<List<User>> GetUsers(CancellationToken ct);                  // incl. soft-deleted
    Task<User?> GetUserByUsername(string username, CancellationToken ct); // incl. soft-deleted
    Task<User?> GetUserById(Guid id, CancellationToken ct);
    Task InsertUser(User user, CancellationToken ct);                 // idempotent for guest: swallow duplicate-key when IsGuest
    /// <summary>Set Deleted=1 unless user is the last active admin. Returns false if blocked.</summary>
    Task<bool> SoftDeleteUser(Guid id, CancellationToken ct);
    Task RestoreUser(Guid id, CancellationToken ct);
    Task PermanentlyDeleteUser(Guid id, CancellationToken ct);        // only call on soft-deleted rows
    Task HardDeleteGuest(CancellationToken ct);                       // deletes IsGuest row; FK cascades sessions
    /// <summary>Change role unless demoting the last active admin. Returns false if blocked.</summary>
    Task<bool> UpdateRole(Guid id, string role, CancellationToken ct);
    Task UpdateLastLogin(Guid id, DateTime utc, CancellationToken ct);
    /// <summary>Update hash only if current hash matches expectedOldHash (rehash race guard).</summary>
    Task UpdatePasswordHash(Guid id, string newHash, string? expectedOldHash, CancellationToken ct);

    // Invites
    Task<List<UserInvite>> GetInvites(CancellationToken ct);
    Task UpsertInvite(UserInvite invite, CancellationToken ct);       // replaces existing invite for same username
    Task DeleteInvite(string username, CancellationToken ct);
    /// <summary>Atomically consume (delete+return) an unexpired invite. Null if missing/expired.</summary>
    Task<UserInvite?> ConsumeInvite(string token, CancellationToken ct);

    // Password resets
    Task UpsertReset(PasswordReset reset, CancellationToken ct);      // replaces existing reset for same user
    Task<PasswordReset?> ConsumeReset(string token, CancellationToken ct);
    Task DeleteResetsForUser(Guid userId, CancellationToken ct);

    // Sessions
    Task InsertSession(Session session, CancellationToken ct);
    /// <summary>Join session→user; null when missing, expired, or user soft-deleted.</summary>
    Task<SessionUser?> GetSessionUser(string token, CancellationToken ct);
    Task DeleteSession(string token, CancellationToken ct);
    Task DeleteSessionsForUser(Guid userId, CancellationToken ct);
    Task DeleteOtherSessionsForUser(Guid userId, string keepToken, CancellationToken ct);

    Task CleanupExpired(CancellationToken ct);                        // bounded batches
}
```

- [ ] Create file; build interfaces project; commit.

### Task 3: SQL CreateScripts

**Files:**
- Create: `src/Config.DataProvider.SqlServer/CreateScripts/15_Users.sql`, `16_UserInvites.sql`, `17_PasswordResets.sql`, `18_Sessions.sql`, `19_AlterAuditLog_AddUsernameAndAction.sql`
- Modify: `src/Config.DataProvider.SqlServer/CreateScripts/13_AuditLog.sql` (add `Username NVARCHAR(100) NULL`, `Action NVARCHAR(50) NULL`)

Schemas exactly per spec "Data model" section (CHECK constraints on Role; unique indexes: `Users.Username`, `UserInvites.Username`, `PasswordResets.UserId`; FKs `ON DELETE CASCADE` from PasswordResets/Sessions to Users; indexes on every ExpiresUtc; CI collation on username columns). Script 19 idempotent (`IF COL_LENGTH(...) IS NULL`) and backfills `Action = Content` for legacy rows (`WHERE [Action] IS NULL AND LEN([Content]) <= 50`).

- [ ] Write scripts; commit. (No automated verification — scripts are deployed manually per repo convention.)

### Task 4: SqlServer UserDataAccess (Dapper)

**Files:**
- Create: `src/Config.DataProvider.SqlServer/UserDataAccess.cs`

**Consumes:** `SqlConnectionFactory` (existing), Task 2 interface.

Key requirements (per spec Concurrency section):
- `ConsumeInvite`/`ConsumeReset`: `DELETE ... OUTPUT DELETED.* WHERE Token=@token AND ExpiresUtc > GETUTCDATE()` — atomic single statement.
- `SoftDeleteUser`: `UPDATE Users SET Deleted=1 WHERE Id=@id AND Deleted=0 AND (Role<>'Admin' OR EXISTS(SELECT 1 FROM Users u2 WHERE u2.Role='Admin' AND u2.Deleted=0 AND u2.Id<>@id))`; rowcount 1 → also delete sessions+resets for user (same transaction); return rowcount=1.
- `UpdateRole`: same EXISTS-guard pattern for demotion from Admin.
- `InsertUser` guest idempotency: `IF NOT EXISTS (SELECT 1 FROM Users) INSERT ...` inside try/catch treating error 2601/2627 as success when `IsGuest`.
- `GetSessionUser`: `SELECT u.Username, u.Role, u.IsGuest, u.Id AS UserId, s.ExpiresUtc FROM Sessions s JOIN Users u ON u.Id=s.UserId WHERE s.Token=@token AND s.ExpiresUtc > GETUTCDATE() AND u.Deleted=0`.
- `UpdatePasswordHash` with `expectedOldHash`: `WHERE Id=@id AND (@expectedOldHash IS NULL OR PasswordHash=@expectedOldHash)`.
- `CleanupExpired`: `DELETE TOP (1000) FROM Sessions WHERE ExpiresUtc < GETUTCDATE()` (same for invites/resets).

- [ ] Implement; build; commit.

### Task 5: File provider stub + DI wiring + fail-fast

**Files:**
- Create: `src/Config.DataProvider.File/UserDataAccess.cs` (every method `throw new NotSupportedException("User login requires the SqlServer data provider.");` — expose `public const string NotSupportedMessage`)
- Modify: `src/Config.Admin.Api/Program.cs` (register `IUserDataAccess` in both provider branches; default `dataProviderType` → `"SqlServer"`)
- Modify: `src/Config.Api/Program.cs` (default `"SqlServer"` only — no user store needed there)

Fail-fast (Program.cs, after `builder.Build()`, Local provider only): resolve `IUserDataAccess`, call `CountUsers`; catch `NotSupportedException` → log fatal + throw. (Folded into Task 8 startup wiring — here just registration + default flip.)

- [ ] Implement; build both APIs; commit.

### Task 6: Audit logging — Username + Action columns

**Files:**
- Modify: `src/Config.DataProvider.Interfaces/IAuditLogHandler.cs` — every method gains `string? username` and `string action` parameters; add `Task AuditLogUser(string entityId, string callerIp, string? username, string action, string content)`.
- Modify: `src/Config.DataProvider.SqlServer/AuditLogHandler.cs` — write `Username`/`Action` columns; `EntityType="User"` for the new method.
- Modify: `src/Config.DataProvider.File/AuditLogHandler.cs` (check existing shape first) — include username/action in the written content string.
- Modify: `src/Config.Admin.Api/Helpers/AuditLogger.cs`:

```csharp
public static async Task AuditLog(this ControllerBase c, string id, string action,
    Func<string, string, string?, string, string, Task> func, string content = "")
{
    var ip = c.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var username = c.User.Identity?.IsAuthenticated == true ? c.User.Identity!.Name : null;
    await func(id, ip, username, action, content);
}
```

- Modify: all existing controllers' `AuditLog` call sites (signature ripple only).

**Interfaces produced:** `IAuditLogHandler.AuditLogUser(entityId, callerIp, username, action, content)` — used by Task 9/10 for events `Login`, `LoginFailed`, `InviteCreated`, `InviteRevoked`, `ResetLinkCreated`, `PasswordChanged`, `RoleChanged`, `UserCreated`, `UserDeleted`, `UserRestored`, `UserPermanentlyDeleted`.

- [ ] Implement; build solution; fix ripples; commit.

### Task 7: Auth core — password policy, token generator, AuthService (TDD)

**Files:**
- Create: `src/Config.Admin.Api/Auth/PasswordPolicy.cs`, `src/Config.Admin.Api/Auth/TokenGenerator.cs`, `src/Config.Admin.Api/Auth/AuthService.cs`, `src/Config.Admin.Api/Auth/AuthSettings.cs`, `src/Config.Admin.Api/Auth/UsernameRules.cs`
- Test: `src/Config.UnitTests/Auth/PasswordPolicyTests.cs`, `UsernameRulesTests.cs`, `AuthServiceTests.cs`
- Modify: `src/Config.Admin.Api/Config.Admin.Api.csproj` (add `Microsoft.Extensions.Identity.Core`)

**Produces:**

```csharp
public class AuthSettings { public int SessionLifetimeHours { get; set; } = 8; public int InviteLifetimeDays { get; set; } = 7; }

public static class PasswordPolicy {
    /// <summary>Null when valid, else human-readable reason. 16–128, lower+upper+digit+special.</summary>
    public static string? Validate(string password);
}
public static class UsernameRules {
    /// <summary>Trims; null+normalized out-param when valid. Rejects "guest" (case-insensitive), bad chars, bad length.</summary>
    public static string? Validate(string raw, out string trimmed);
    public const string GuestUsername = "guest";
}
public static class TokenGenerator { public static string NewToken(); } // 32 random bytes, base64url

public class LoginResult { public string Token=""; public DateTime ExpiresUtc; public string Username=""; public string Role=""; public bool IsGuest; }

public class AuthService {   // ctor: IUserDataAccess, IPasswordHasher<User>, AuthSettings, ILogger<AuthService>
    Task<LoginResult?> Login(string username, string password, CancellationToken ct);       // null = uniform failure
    Task<LoginResult?> Redeem(string token, string password, CancellationToken ct);         // invites + resets
    Task<bool> ChangePassword(Guid userId, string current, string @new, string keepToken, CancellationToken ct);
    Task<LoginResult> CreateFirstUser(string username, string password, CancellationToken ct); // guest path; role=Admin — throws InvalidOperationException on taken username
    Task EnsureGuestSeeded(CancellationToken ct);   // CountUsers==0 → InsertUser(guest, hashed "guest", Role=Admin, IsGuest=true)
}
```

AuthService behavior (all spec'd): Login does dummy-hash verify for unknown/deleted users; on success updates LastLoginUtc, hard-deletes guest when non-guest logs in, creates session; rehash-on-`SuccessRehashNeeded` via `UpdatePasswordHash(expectedOldHash: oldHash)`. Redeem: try `ConsumeInvite` → validate username still free → `InsertUser` (invite role); else `ConsumeReset` → user must exist non-deleted → update hash + `DeleteSessionsForUser`; both → auto-login result (which triggers guest deletion path). ChangePassword verifies current, validates policy, updates hash, `DeleteOtherSessionsForUser`. **Minimum-response-time lives in the controller, not here.**

- [ ] Write failing tests (guest lifecycle: seed-on-empty, guest deleted on real login, not deleted on guest login; login unknown/wrong-password/soft-deleted → null; redemption: invite happy, invite username-taken, reset happy revokes sessions, expired/consumed → null; change password wrong-current → false; policy matrix; username matrix incl. `guest` reserved)
- [ ] Run `dotnet test src/Config.UnitTests/Config.UnitTests.csproj --filter "FullyQualifiedName~Auth"` → all fail
- [ ] Implement; tests green; commit.

### Task 8: Provider seam, session auth handler, policies, bootstrap, rate limiting

**Files:**
- Create: `src/Config.Admin.Api/Auth/IAuthProviderSetup.cs`, `src/Config.Admin.Api/Auth/LocalAuthProviderSetup.cs`, `src/Config.Admin.Api/Auth/SessionAuthenticationHandler.cs`, `src/Config.Admin.Api/Auth/AuthPolicies.cs`
- Modify: `src/Config.Admin.Api/Program.cs`, `src/Config.Admin.Api/appsettings.json` (`"Auth": { "Provider": "Local" }`)
- Test: `src/Config.UnitTests/Auth/SessionAuthenticationHandlerTests.cs`

**Produces:**

```csharp
public interface IAuthProviderSetup {
    string Type { get; }                                  // "local"
    void ConfigureServices(IServiceCollection services, IConfiguration config);
    void ConfigureAuthentication(AuthenticationBuilder builder);
    object ProviderMetadata { get; }                      // serialized by GET /api/auth/provider
}
public static class AuthPolicies {
    public const string RealUser = "RealUser";   // authenticated && !guest claim
    public const string AdminOnly = "AdminOnly"; // role claim == Admin
    public const string GuestOnly = "GuestOnly"; // guest claim present
    public const string GuestClaim = "guest";
    public const string SchemeName = "LocalSession";
}
```

`SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>`: reads `Authorization: Bearer <token>`, calls `IUserDataAccess.GetSessionUser`, builds `ClaimsPrincipal` with `ClaimTypes.Name`, `ClaimTypes.Role`, `guest` claim (+ `userId` claim with the GUID). No token/expired/deleted → `AuthenticateResult.NoResult()/Fail`.

Program.cs wiring: `AddAuthentication(SchemeName).AddScheme<...>`; `AddAuthorization` with the three policies + FallbackPolicy = RealUser (so unattributed endpoints default to real-user); rate limiter policy `"auth"` (fixed window per IP, 10/min, on login+redeem); startup: fail-fast check + `AuthService.EnsureGuestSeeded` + warning log while guest exists; `app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();`. Remove `ApiKeyValidation`/`ApiKeyAuthenticationFilter` registrations. Swagger: bearer security definition replacing X-API-Key.

- [ ] TDD the handler (valid token → claims incl. role; guest claim only when IsGuest; missing/expired → fail); implement wiring; build; commit.

### Task 9: AuthController

**Files:**
- Create: `src/Config.Admin.Api/Controllers/AuthController.cs`
- Create (DTOs): `src/Config.Admin.Api.Model/RequestResponse/Auth/*` following existing Model project layout — `LoginRequest {Username, Password}`, `RedeemRequest {Token, Password}`, `ChangePasswordRequest {CurrentPassword, NewPassword}`, `LoginResponse {Token, ExpiresUtc, Username, Role, IsGuest}`, `ProviderResponse {Type}`
- Test: `src/Config.UnitTests/Controllers/AuthControllerTests.cs`

Endpoints per spec: `POST auth/login` (AllowAnonymous, rate-limited, min-500ms-on-failure via `Task.Delay` remainder, audit Login/LoginFailed, `Cache-Control: no-store`), `POST auth/redeem` (same anon/limits, generic 400), `POST auth/logout` (any authenticated incl. guest → `DeleteSession`), `POST auth/change-password` (RealUser policy), `GET auth/provider` (anon → active `IAuthProviderSetup.ProviderMetadata`). Route prefix: `[Route("api/auth")]` — **note** existing controllers use bare `[Route("[controller]")]`; match SPA client paths accordingly (use `api/auth` + update below).

- [ ] TDD controller logic (login 401 shape uniform, no-store header set, logout deletes session, redeem 400 on null); implement; commit.

### Task 10: UsersController

**Files:**
- Create: `src/Config.Admin.Api/Controllers/UsersController.cs`
- Create DTOs: `UserListResponse { Users: List<UserInfo>, Invites: List<InviteInfo> }`, `UserInfo {Id, Username, Role, Deleted, IsGuest, CreatedUtc, LastLoginUtc}`, `InviteInfo {Username, Role, CreatedBy, ExpiresUtc}`, `CreateInviteRequest {Username, Role}`, `TokenResponse {Token, ExpiresUtc}`, `CreateUserRequest {Username, Password}`, `ChangeRoleRequest {Role}`
- Test: `src/Config.UnitTests/Controllers/UsersControllerTests.cs`

Endpoints per spec (AdminOnly unless noted): `GET api/users`; `POST api/users/invites` (validate username rules + not-existing/not-soft-deleted → 400 messages, `UpsertInvite`, audit); `DELETE api/users/invites/{username}`; `POST api/users/{username}/reset` (`UpsertReset`); `PUT api/users/{username}/role` (400 when `UpdateRole` returns false); `DELETE api/users/{username}` (`?permanent=` param; self-delete → 400; last-admin block → 400; permanent only when already deleted); `POST api/users/{username}/restore`; `POST api/users` (**GuestOnly**, `AuthService.CreateFirstUser`). All mutations audited with the acting username.

- [ ] TDD (guest-only create enforced by policy attribute presence; last-admin 400 path; self-delete 400; invite→existing-user 400 vs soft-deleted "restore instead" message); implement; commit.

### Task 11: Convert existing controllers to session auth

**Files:**
- Modify: `ApiKeysController.cs`, `ApplicationsController.cs`, `ConfigParseController.cs`, `DependencyGraphController.cs`, `EnvironmentsController.cs`, `SecretsController.cs`, `SettingsController.cs`, `ConfigurationsController.cs` — replace `[ApiKey]` with `[Authorize(Policy = AuthPolicies.RealUser)]`.
- Modify: `src/Config.Auth/*` stays (Config.Api still uses it); only Admin API references dropped.

- [ ] Sweep, build, run full test suite `dotnet test src/Config.UnitTests/Config.UnitTests.csproj`; commit.

### Task 12: Frontend auth store + client seam

**Files:**
- Create: `src/Config.Admin.WebClient/src/lib/auth/session.svelte.ts` (auth store: `{token, expiresUtc, username, role, isGuest}`, localStorage persistence key `configservice.session`, `login/logout/load` functions, `$state` rune)
- Modify: `src/lib/runtime-config.ts` (drop `apiKey` — remove validation + type field)
- Modify: `src/lib/api/client.ts` (bearer header from store; on 401 → clear session + `goto('/login')`; skip redirect for the login/redeem calls themselves)
- Modify: `static/config.json` (remove apiKey)
- Test: extend `src/lib/api/client.test.ts`, create `src/lib/auth/session.test.ts`

- [ ] TDD store + client changes (`npm test`); commit.

### Task 13: Login, redeem, guest pages + layout guard

**Files:**
- Create: `src/routes/login/+page.svelte`, `src/routes/redeem/+page.svelte`, `src/routes/first-user/+page.svelte`
- Modify: `src/routes/+layout.svelte` (guard: no session → `/login` except `/login`,`/redeem`; guest → forced to `/first-user`; hide nav for guest), `src/app.html` (`<meta name="referrer" content="same-origin">`)
- Create: `src/lib/api/authApi.ts` (login/redeem/logout/changePassword/provider wrappers using `postJson`/`getJson`)

Redeem page: token from `location.hash` (`#token=...`), `history.replaceState` immediately, password ×2 with client-side policy check mirroring `PasswordPolicy`, submit → store session → `goto('/')`.

- [ ] Implement; `npm run check`; manual smoke via dev server; commit.

### Task 14: Users admin page + header menu

**Files:**
- Create: `src/routes/users/+page.svelte`, `src/lib/api/usersApi.ts`
- Modify: header/nav component (add Users link for role Admin, change-password dialog, logout button)

Users page per spec: table (username, role, last login, created, deleted badge), "show deleted" toggle, invite dialog (username+role → copyable `${location.origin}/redeem#token=...`), revoke invite, reset link dialog (copyable), role select (with last-admin 400 surfaced), soft delete (confirm), restore, permanent delete (confirm, only on deleted).

- [ ] Implement; `npm run check`; commit.

### Task 15: Regenerate API types + e2e tests

**Files:**
- Regenerate: `src/lib/api/generated.d.ts` (`npm run gen:api` with Admin API running)
- Create: `e2e/auth.spec.ts` (mocked routes per existing `e2e/mocks.ts` pattern): login happy path → main page; guest login → forced first-user screen → create → lands authenticated; redeem flow (`/redeem#token=x` mock 200 → home)
- Modify: `e2e/mocks.ts` (auth route mocks), `e2e/smoke.spec.ts` (inject session before navigation)

- [ ] `npm run test:e2e` green locally; commit.

### Task 16: Docs + rollout

**Files:**
- Modify: `README.md` (setup flow: SQL scripts, guest/guest claim, invites, reset; breaking-changes section; "adding an auth provider" extension section)
- Modify: `CLAUDE.md` (SQL Server primary provider, auth architecture summary, new test locations)

- [ ] Update; final full test pass (`dotnet test` + `npm test` + `npm run build`); commit.

## Self-Review Notes

- Spec coverage checked section-by-section: data model (T1–T4), provider seam (T8), endpoints (T9–T11), bootstrap/config (T5, T7, T8), frontend (T12–T15), audit (T6, T9, T10), testing (T7–T10, T12, T15), rollout (T16). Concurrency requirements live in T4 SQL patterns + T7 service semantics.
- Type consistency: `IUserDataAccess` signatures in T2 are the single source; T4/T7/T8 consume them verbatim.
- Deviation from writing-plans skill granularity: steps are task-level with TDD checkpoints rather than 2-minute micro-steps — executor is the planning session itself, in-session, immediately after planning.
