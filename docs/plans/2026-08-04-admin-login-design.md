# Admin login — design

Date: 2026-08-04
Status: Approved (brainstorm + grilling complete)
Related: ADR-0002 (pluggable auth provider), ADR-0003 (DB-backed sessions),
ADR-0004 (guest bootstrap user), ADR-0005 (SQL Server primary provider), CONTEXT.md glossary.

## Goal

Add user login to the ConfigurationService admin page. Simple database-backed authentication —
no IdentityServer/ADFS/Google for now — but architected so the identity provider is swappable
later (this is an open-source project; the auth mechanism must fit anyone and be extendable).

## Decisions

1. **Scope**: Login replaces the static `X-API-Key` for the Admin API — a clean break, no
   grace period (the key shipped to every browser and was never a real boundary; scripts can
   `POST /api/auth/login` instead). The SPA no longer ships an API key in `static/config.json`.
   Config.Api (middleware-facing) keeps API-key auth, untouched.
2. **Session mechanism**: DB-backed sessions (ADR-0003). Login stores an opaque random
   256-bit token in a `Sessions` table; the SPA sends it as `Authorization: Bearer <token>`
   via the existing seam in `src/lib/api/client.ts`; validation is a DB lookup per request.
   Fixed 8-hour absolute lifetime, no refresh. Sessions are revocable: deleting a user (or
   guest) deletes their sessions and ends access immediately. Logout deletes the session row.
   No signing keys — restarts and load-balanced instances need zero coordination.
3. **Password reset**: Admin-driven — any real user can generate a one-time reset link for
   another user. No SMTP/email anywhere. A logged-in real user can always change their own
   password.
4. **Invites**: New users only by invite. The inviter fixes the username; the invitee opens a
   single-use link (valid 7 days) and sets their password. Invite links and reset links are
   the same mechanism: a token that authorizes setting the password for a username.
5. **Guest user** (ADR-0004):
   - A new instance is born with user `guest` / password `guest`.
   - Guest can do exactly one thing: directly create a real user (username + password form —
     no invite-link ceremony, since the person at the keyboard is the new user).
   - Guest is **deleted** (not disabled) the first time a real user logs in. Redeeming an
     invite auto-logs-in and counts. Deletion cascades to guest's sessions, so a lingering
     guest login dies with it.
   - Until a real user has logged in, guest keeps working and can create more users
     (protects against typo'd usernames / lost invites).
   - **Empty user store ⇒ guest is (re)created at startup.** Bootstrap and disaster recovery
     ("last admin locked out": delete all user rows, restart) are the same mechanism.
6. **Roles**: Two — `Admin` and `User`. Admins do user management (invite, delete/restore,
   reset links, role assignment) plus everything else; Users do everything else (configs,
   secrets, environments, API keys) but nothing user-related except changing their own
   password. Invites carry the role, chosen by the inviting admin. The user guest creates is
   always an Admin. **The last active admin cannot be deleted or demoted.** `LastLoginUtc` is
   tracked and shown in the user list so admins can spot stale accounts (e.g. leavers).
7. **Storage** (ADR-0005): SQL Server provider only; the File provider gets
   `NotSupportedException` stubs. The default `DataProvider` becomes `SqlServer`. If the
   active auth provider needs user storage the data provider can't supply, the Admin API
   **fails fast at startup** with a clear message.
8. **Username rules**: trimmed, 1–100 chars, case-insensitive unique (SQL Server default
   collation) and case-insensitive at login; displayed as first entered. `guest` reserved
   case-insensitively.
9. **Password policy**: minimum 16 characters, at least one lowercase, one uppercase, one
   digit, one special character. Applies everywhere a password is chosen (redemption,
   change-password, guest's direct create). The seeded `guest`/`guest` credential is exempt
   (fixed bootstrap credential, not user-chosen).
10. **Uniform errors**: login always returns the same 401 regardless of what was wrong;
    redemption failures return one generic 400 ("invalid or expired link") without
    distinguishing expired/used/unknown. ~500 ms delay on failed login; no lockout.
11. **User deletion is soft** (repo convention): a `Deleted` flag; sessions revoked and
    outstanding tokens removed immediately; login refused. Username uniqueness spans live and
    soft-deleted users — inviting a soft-deleted username → 400 ("restore instead"). Restore
    (admin-only) reactivates the account with its old password hash + role; pair with a reset
    link if the password is forgotten. **Permanent delete** is the explicit second step,
    available only on already-soft-deleted users; it frees the username, and the audit log
    remains the historical record. Guest resurrection triggers only on a truly empty Users
    table (soft-deleted rows count as existing); DB-level recovery (delete all rows, restart)
    is unchanged. Guest itself is still hard-deleted on first real login.
12. **Audit log gets first-class columns**: `Username` (acting user, `NVARCHAR(100) NULL`)
    and `Action` (`NVARCHAR(50)`, e.g. `Insert`, `Delete`, `Login`, `LoginFailed`,
    `InviteCreated`). `Content` becomes optional free-form detail — today it misleadingly
    holds only the action verb.

## Architecture

### Swappable identity provider (ADR-0002)

**The app consumes claims-based identities; how they're established is the pluggable part.**

- Config setting `Auth:Provider` selects the provider. This feature implements `"Local"`.
  A future `"Oidc"` provider covers ADFS / IdentityServer / Entra / any OIDC authority.
- Backend seam: an `IAuthProviderSetup` registration interface. Each provider contributes:
  service registrations, authentication-handler configuration, and optionally its own
  endpoints.

  `Local` contributes: the `AuthController` (login/logout/redeem/change-password), user
  management, the stores (`IUserDataAccess`), guest bootstrap, and an authentication handler
  that validates opaque bearer tokens against the `Sessions` table.
  A future `Oidc` provider contributes only standard JwtBearer validation against the
  external authority — no local endpoints, no user table, no guest.
- Everything outside the provider is provider-agnostic: controllers authorize on **claims**
  (`name`, plus a `guest` claim only Local ever issues), never on how the identity was
  established.
- Frontend seam: anonymous `GET /api/auth/provider` returns provider metadata.
  `{type: "local"}` → SPA shows the login form. Future `{type: "oidc", authority, clientId}` →
  redirect-based flow. The SPA auth store and `client.ts` deal only in "current token" —
  opaque bytes either way.
- Docs: add a short "adding an auth provider" section (public extension point).

Out of scope now: implementing OIDC, mixed mode (two providers at once), mapping external
identities to local user records.

### Data model (SqlServer `CreateScripts`)

`15_Users.sql`:

| Column       | Type                | Notes                          |
|--------------|---------------------|--------------------------------|
| Id           | UNIQUEIDENTIFIER PK |                                |
| Username     | NVARCHAR(100)       | unique (case-insensitive), spans deleted users |
| PasswordHash | NVARCHAR(500)       | ASP.NET Core PasswordHasher    |
| Role         | NVARCHAR(20)        | `Admin` \| `User`              |
| IsGuest      | BIT                 |                                |
| Deleted      | BIT                 | soft delete                    |
| CreatedUtc   | DATETIME2           |                                |
| LastLoginUtc | DATETIME2 NULL      | updated on login               |

`16_UserTokens.sql`:

| Column     | Type             | Notes                                  |
|------------|------------------|----------------------------------------|
| Token      | NVARCHAR(100) PK | random 256-bit, url-safe               |
| Username   | NVARCHAR(100)    | target user                            |
| Purpose    | NVARCHAR(20)     | `invite` \| `reset`                    |
| ExpiresUtc | DATETIME2        | now + 7 days                           |

`17_Sessions.sql`:

| Column     | Type             | Notes                                  |
|------------|------------------|----------------------------------------|
| Token      | NVARCHAR(100) PK | random 256-bit, url-safe               |
| Username   | NVARCHAR(100)    |                                        |
| IsGuest    | BIT              |                                        |
| CreatedUtc | DATETIME2        |                                        |
| ExpiresUtc | DATETIME2        | now + 8 h (absolute)                   |

Token lifecycle rules:

- One active token per (username, purpose): creating a new invite/reset replaces the old one.
- Inviting a username that already exists as a user → 400.
- Tokens are single-use: deleted on redemption. Expired token/session rows are cleaned up
  opportunistically (on login and token creation).
- Pending invites can be revoked (`DELETE /api/users/invites/{username}`).
- A reset link whose user was deleted fails at redemption and is swept by cleanup.

New `IUserDataAccess` in `Config.DataProvider.Interfaces` (users, tokens, sessions); Dapper
implementation in `Config.DataProvider.SqlServer`; stub in `Config.DataProvider.File` (throws
`NotSupportedException`: "user login requires the SqlServer provider").

### Endpoints (Admin API)

`AuthController` (anonymous unless noted):

- `POST /api/auth/login` `{username, password}` → `{token, expiresUtc, username, isGuest}` or
  uniform 401 (~500 ms delay on failure).
  **Side effect: successful real-user login deletes the guest user and guest sessions.**
- `POST /api/auth/redeem` `{token, password}` → creates the user (invite) or sets the password
  (reset), then auto-logs-in and returns the same shape as login — and therefore also triggers
  guest deletion. Edge rules: invite redemption fails if the username was taken in the
  meantime; reset redemption fails if the user no longer exists. Failures → generic 400.
- `POST /api/auth/logout` (authenticated) — deletes the session row.
- `POST /api/auth/change-password` `{currentPassword, newPassword}` — authenticated, real
  users only.
- `GET /api/auth/provider` — anonymous provider metadata (see seam above).

`UsersController` (session required; user management is **admin-only**):

- `GET /api/users` — users (incl. soft-deleted, with `deleted`, `role`, `lastLoginUtc`) +
  pending invites (with expiry). Admins only.
- `POST /api/users/invites` `{username, role}` → `{token, expiresUtc}`. Admins only. The SPA
  composes the link as `<its own origin>/redeem?token=...` — the backend never needs to know
  the SPA's URL.
- `DELETE /api/users/invites/{username}` — revoke a pending invite. Admins only.
- `POST /api/users/{username}/reset` → `{token, expiresUtc}`. Admins only.
- `PUT /api/users/{username}/role` `{role}` — admins only; cannot demote the last active
  admin.
- `DELETE /api/users/{username}` — soft delete. Admins only; cannot delete yourself; cannot
  delete the last active admin. Revokes the user's sessions and outstanding tokens.
- `POST /api/users/{username}/restore` — admins only; reactivates a soft-deleted user with
  old password hash and role.
- `DELETE /api/users/{username}?permanent=true` — admins only; only valid on an already
  soft-deleted user; frees the username.
- `POST /api/users` `{username, password}` — **guest-only** direct create of the first Admin
  (real users are invite-only; guest gets the direct form).

Guest policy: a guest session may only call `POST /api/users` and `POST /api/auth/logout`
(plus the anonymous endpoints). Everything else → 403. Guest deletion revokes guest sessions,
so the policy needs no existence re-checks.

Authorization layers: anonymous (login/redeem/provider) → authenticated real user (all
config/secret/environment/API-key endpoints, own password change, logout) → admin (user
management) — with guest outside all of them except its two endpoints.

All existing Admin API controllers switch from `ApiKeyAuthenticationFilter` to `[Authorize]`
with a deny-guest policy. Swagger security definition switches from `X-API-Key` to bearer.
Config.Api is untouched.

### Bootstrap & configuration

- Startup (Local provider only): if the `Users` table is empty, insert `guest` with hashed
  password `guest`. Runs on every startup.
- Startup validation: `Auth:Provider=Local` + a data provider without user-storage support →
  fail fast with a clear message.
- New `AuthSettings`: `SessionLifetimeHours` = 8, `InviteLifetimeDays` = 7 (applies to reset
  links too).
- Password hashing: ASP.NET Core standalone `PasswordHasher` (PBKDF2, format-versioned)
  from `Microsoft.Extensions.Identity.Core` — no EF, no Identity schema.

### Frontend (SvelteKit SPA)

- `/login` page. Auth store holds `{token, expiresUtc, username, isGuest}` in localStorage.
- `client.ts` seam sends `Authorization: Bearer`; any 401 clears the session and redirects to
  `/login`. The static `apiKey` in `config.json` is removed.
- Layout guard: no token → `/login`. Guest token → locked to a single "create your first real
  user" screen.
- `/redeem?token=...` page: set password (×2, policy validated client- and server-side),
  auto-login, navigate home.
- Users admin page (admins only; hidden for role `User`): list users with role, last login,
  and deleted state ("show deleted" toggle); create invite with role (copyable link); revoke
  pending invite; generate reset link (copyable); change role; soft delete; restore;
  permanent delete (on deleted users, with confirmation).
- Change-password and logout in the header menu.
- Regenerate API types (`npm run gen:api`).

### Audit logging

The `AuditLog` table gains two first-class columns (fresh installs via updated
`13_AuditLog.sql`; existing databases via idempotent `18_AlterAuditLog_AddUsernameAndAction.sql`):

- **`Username`** `NVARCHAR(100) NULL` — the acting user (from the session). `NULL` for rows
  predating login or written by non-user paths. (`User` is a reserved word in T-SQL.)
- **`Action`** `NVARCHAR(50)` — `Insert`, `Delete`, `Save`, `Login`, `LoginFailed`,
  `InviteCreated`, `InviteRevoked`, `ResetLinkCreated`, `PasswordChanged`, `RoleChanged`,
  `UserRestored`, `UserPermanentlyDeleted`, ... Today the action verb hides in `Content`;
  after this, `Content` becomes optional free-form detail (e.g. the target username of an
  invite).
- User events: `EntityType="User"`, `EntityId` = the target user's GUID (usernames exceed the
  36-char column). The actor is the `Username` column.
- Logged events: user created (by whom), user soft/permanently deleted, restored, role
  changed, invite created/revoked, reset link created, password changed, login success, and
  **login failure** (attempted username + IP — the only brute-force visibility given no
  lockout).

### Testing

- **Backend (NUnit + NSubstitute)**: auth service logic — guest lifecycle, session
  issue/validate/revoke/expiry, redemption paths (incl. username-taken and user-deleted
  edges), token replace/revoke rules, password policy, role policies (admin-only user
  management, last-active-admin rail for delete and demote), soft delete/restore/permanent
  delete semantics, fail-fast provider check — against mocked `IUserDataAccess`. Dapper
  implementations stay unit-untested, consistent with the rest of the repo (no SQL Server
  integration-test infrastructure in this feature).
- **Frontend (Vitest)**: auth store (persistence, expiry), `client.ts` (bearer header,
  401 → clear session + redirect).
- **E2E (Playwright, mocked routes — no real backend, unchanged pattern)**: login happy path,
  guest → create-first-user flow, invite redemption.

## Rollout (breaking changes — release notes required)

- Admin API requires login; `X-API-Key` no longer accepted there. Scripts must log in first.
- Default `DataProvider` is now `SqlServer`; a connection string is required. File-based
  deployments cannot use admin login (startup fails fast) — migrating to SQL Server is the
  upgrade path.
- New `CreateScripts` `15_Users.sql`, `16_UserTokens.sql`, `17_Sessions.sql`, and
  `18_AlterAuditLog_AddUsernameAndAction.sql` must be run on existing databases.
- After upgrade the Users table is empty → guest/guest is live; the first operator to log in
  should immediately create their user (which kills guest on first real login).
- README + CLAUDE.md updated: setup flow, SQL Server as primary provider, auth documentation,
  "adding an auth provider" extension guide.

## Security posture (accepted for v1)

- No rate limiting beyond the fixed failure delay; no account lockout. Failed logins are
  audit-logged.
- Sessions are revocable (DB-backed); logout, user deletion, and guest deletion take effect
  immediately. Absolute 8 h lifetime, no refresh/sliding.
- Session token in localStorage: accepted XSS trade-off for a static SPA (no cookie
  infrastructure); the SPA has no third-party script includes.
- Raw (unhashed) one-time tokens and session tokens stored in the DB — an attacker with DB
  write access could mint credentials anyway.
- The well-known guest/guest credential is live until the first real login (ADR-0004) —
  acceptable because guest can only create users, and operators claim instances immediately.
- HTTPS is assumed to be handled by deployment/reverse proxy.
