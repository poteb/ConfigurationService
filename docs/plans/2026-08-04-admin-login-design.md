# Admin login — design

Date: 2026-08-04
Status: Approved (brainstorm + grilling + external review complete)
Related: ADR-0002 (pluggable auth provider), ADR-0003 (DB-backed sessions),
ADR-0004 (guest bootstrap user), ADR-0005 (SQL Server primary provider), CONTEXT.md glossary,
spec-review triage in `2026-08-04-admin-login-spec-review.md`.

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
   via the existing seam in `src/lib/api/client.ts`; validation is a DB lookup per request
   that joins the user row — so role changes, soft deletes, and hard deletes take effect on
   the next request. Fixed 8-hour absolute lifetime, no refresh. Logout deletes the session
   row. No signing keys — restarts and load-balanced instances need zero coordination.
3. **Password reset**: Admin-driven — an **admin** can generate a one-time reset link for
   another user. No SMTP/email anywhere. Redeeming a reset link revokes all of that user's
   existing sessions before issuing the new one (resets are used after compromise; old
   sessions must die). A logged-in real user can always change their own password
   (current-password verified); doing so revokes their *other* sessions.
4. **Invites**: New users only by invite. The inviting admin fixes the username **and role**;
   the invitee opens a single-use link (valid 7 days) and sets their password. Invites and
   reset links are the same concept (a token authorizing a password set) but live in separate
   tables because invites target a username+role that has no user row yet, while resets
   target an existing user.
5. **Guest user** (ADR-0004):
   - A new instance is born with user `guest` / password `guest`.
   - Guest can do exactly one thing: directly create the first Admin (username + password
     form — no invite-link ceremony, since the person at the keyboard is the new user).
   - Guest is **deleted** (hard, not soft) the first time a real user logs in. Redeeming an
     invite auto-logs-in and counts. Deletion cascades to guest's sessions.
   - Until a real user has logged in, guest keeps working and can create more users
     (protects against typo'd usernames / lost invites). While guest exists, the Admin API
     logs a prominent startup warning ("instance unclaimed — log in as guest and create a
     user now").
   - **Empty user store ⇒ guest is (re)created at startup**, as an idempotent insert that
     treats losing a duplicate-key race (multi-instance startup) as success. Bootstrap and
     disaster recovery ("last admin locked out": delete all user rows, restart) are the same
     mechanism.
6. **Roles**: Two — `Admin` and `User`. Admins do user management (invite, delete/restore,
   reset links, role assignment) plus everything else; Users do everything else (configs,
   secrets, environments, API keys) but nothing user-related except changing their own
   password. The user guest creates is always an Admin. **The last active admin cannot be
   deleted or demoted.** `LastLoginUtc` is tracked and shown in the user list so admins can
   spot stale accounts (e.g. leavers).
7. **Storage** (ADR-0005): SQL Server provider only; the File provider gets
   `NotSupportedException` stubs. The default `DataProvider` becomes `SqlServer`. If the
   active auth provider needs user storage the data provider can't supply, the Admin API
   **fails fast at startup** with a clear message.
8. **Username rules**: trimmed; 1–100 chars; allowed characters are letters, digits, and
   `. - _ @` (covers email-style names, excludes path separators, control characters, and
   bidi tricks); case-insensitive unique via an explicit case-insensitive collation on the
   column (never "whatever the server default is") and case-insensitive at login; displayed
   as first entered. `guest` reserved case-insensitively.
9. **Password policy**: 16–128 characters (length checked before any hashing), at least one
   lowercase, one uppercase, one digit, one special character. Applies everywhere a password
   is chosen. The seeded `guest`/`guest` credential is exempt (fixed bootstrap credential).
10. **Uniform errors**: login always returns the same 401 regardless of what was wrong;
    redemption failures return one generic 400 ("invalid or expired link"). Unknown-user
    logins run a dummy hash verification so response *timing* is uniform too, and failures
    complete no sooner than a minimum response time (~500 ms) rather than adding an
    unconditional sleep. Login and redeem endpoints are rate-limited per IP (ASP.NET Core
    rate limiter, e.g. 10/min) — brute-force *and* resource-exhaustion mitigation.
11. **User deletion is soft** (repo convention): a `Deleted` flag; sessions revoked and
    outstanding tokens removed immediately; login refused. Username uniqueness spans live and
    soft-deleted users — inviting a soft-deleted username → 400 ("restore instead"). Restore
    (admin-only) reactivates the account with its old password hash + role; pair with a reset
    link if the password is forgotten. **Permanent delete** is the explicit second step,
    available only on already-soft-deleted users; it frees the username (FKs cascade the
    remaining sessions/resets away), and the audit log remains the historical record. Guest
    resurrection triggers only on a truly empty Users table (soft-deleted rows count as
    existing); DB-level recovery (delete all rows, restart) is unchanged.
12. **Audit log gets first-class columns**: `Username` (acting user) and `Action`. See
    Audit logging below.

## Architecture

### Swappable identity provider (ADR-0002)

**The app consumes claims-based identities; how they're established is the pluggable part.**

- Config setting `Auth:Provider` selects the provider. This feature implements `"Local"`.
  A future `"Oidc"` provider covers ADFS / IdentityServer / Entra / any OIDC authority.
- **Claims contract** (the provider-agnostic boundary): `name` (username), `role`
  (`Admin` | `User`), and `guest` (only Local ever issues it). Authorization policies are
  built exclusively on these claims. A future OIDC provider must map external claims/groups
  into `role` as part of its setup — that mapping is the provider's job, not the app's.
- Backend seam: an `IAuthProviderSetup` registration interface. Each provider contributes:
  service registrations, authentication-handler configuration, and optionally its own
  endpoints.

  `Local` contributes: the `AuthController` (login/logout/redeem/change-password), user
  management, the stores (`IUserDataAccess`), guest bootstrap, and an authentication handler
  that validates opaque bearer tokens against the `Sessions` table (joined to `Users`, so
  claims always reflect the *current* row: current role, not-deleted).
  A future `Oidc` provider contributes only standard JwtBearer validation against the
  external authority — no local endpoints, no user table, no guest.
- Frontend seam: anonymous `GET /api/auth/provider` returns provider metadata.
  `{type: "local"}` → SPA shows the login form. Future `{type: "oidc", authority, clientId}` →
  redirect-based flow. The SPA auth store and `client.ts` deal only in "current token" —
  opaque bytes either way.
- Docs: add a short "adding an auth provider" section (public extension point).

Out of scope now: implementing OIDC, mixed mode (two providers at once), mapping external
identities to local user records.

### Data model (SqlServer `CreateScripts`)

All tables: `NOT NULL` everywhere except where noted; `CHECK` constraints on enum-like
columns; explicit case-insensitive collation (`Latin1_General_100_CI_AS`) on every username
column; indexes on every expiry column (cleanup + validation paths).

`15_Users.sql`:

| Column       | Type                | Notes                                        |
|--------------|---------------------|----------------------------------------------|
| Id           | UNIQUEIDENTIFIER PK |                                              |
| Username     | NVARCHAR(100)       | unique index, CI collation, spans deleted    |
| PasswordHash | NVARCHAR(500)       | ASP.NET Core PasswordHasher                  |
| Role         | NVARCHAR(20)        | CHECK: `Admin` \| `User`                     |
| IsGuest      | BIT                 | default 0                                    |
| Deleted      | BIT                 | default 0 (soft delete)                      |
| CreatedUtc   | DATETIME2           |                                              |
| LastLoginUtc | DATETIME2 NULL      | updated on login                             |

`16_UserInvites.sql` (no user row exists yet, so no FK):

| Column     | Type                | Notes                                          |
|------------|---------------------|------------------------------------------------|
| Id         | UNIQUEIDENTIFIER    | stable identity for audit EntityId             |
| Token      | NVARCHAR(100) PK    | random 256-bit, url-safe                       |
| Username   | NVARCHAR(100)       | unique index (one active invite per username)  |
| Role       | NVARCHAR(20)        | CHECK: `Admin` \| `User`                       |
| CreatedBy  | NVARCHAR(100)       | inviting admin's username                      |
| ExpiresUtc | DATETIME2           | now + 7 days, indexed                          |

`17_PasswordResets.sql`:

| Column     | Type                | Notes                                          |
|------------|---------------------|------------------------------------------------|
| Id         | UNIQUEIDENTIFIER    | audit EntityId                                 |
| Token      | NVARCHAR(100) PK    | random 256-bit, url-safe                       |
| UserId     | UNIQUEIDENTIFIER    | FK → Users ON DELETE CASCADE; unique (one active reset per user) |
| ExpiresUtc | DATETIME2           | now + 7 days, indexed                          |

`18_Sessions.sql`:

| Column     | Type                | Notes                                          |
|------------|---------------------|------------------------------------------------|
| Token      | NVARCHAR(100) PK    | random 256-bit, url-safe                       |
| UserId     | UNIQUEIDENTIFIER    | FK → Users ON DELETE CASCADE                   |
| CreatedUtc | DATETIME2           |                                                |
| ExpiresUtc | DATETIME2           | now + 8 h (absolute), indexed                  |

Sessions and resets are keyed to the immutable `UserId` (never the reusable username), so
hard deletes cascade in the database itself rather than relying on application deletes.
Invites are the one username-keyed table by necessity.

Token lifecycle rules:

- One active token per target (DB-enforced unique indexes above): creating a new invite/reset
  replaces the old one.
- Inviting a username that already exists as a user → 400 (live) / 400 "restore instead"
  (soft-deleted).
- Tokens are single-use: consumed with an atomic delete-and-check (`DELETE ... OUTPUT` /
  affected-row check inside the redemption transaction) so concurrent redemptions cannot
  both succeed. Expiry is authoritative at validation time regardless of cleanup.
- Expired rows are cleaned up opportunistically (on login and token creation) in bounded
  batches (`DELETE TOP (n)`), backed by the expiry indexes.
- Pending invites can be revoked (`DELETE /api/users/invites/{username}`).

New `IUserDataAccess` in `Config.DataProvider.Interfaces` (users, invites, resets, sessions);
Dapper implementation in `Config.DataProvider.SqlServer`; stub in `Config.DataProvider.File`
(throws `NotSupportedException`: "user login requires the SqlServer provider").

### Concurrency & atomicity (required, not optional)

- Token redemption: single transaction — consume token (affected-row check), create/update
  user, revoke prior sessions (resets), create session.
- Last-active-admin rail: the demote/soft-delete statement itself re-checks the invariant
  inside the transaction (`UPDATE ... WHERE (SELECT COUNT(*) FROM Users WHERE Role='Admin'
  AND Deleted=0 AND Id <> @id) > 0`-style), so concurrent demotions can't strand the system.
- Guest bootstrap: idempotent insert; duplicate-key loss of the race = success.
- Password change: update hash + revoke other sessions in one transaction.
- `PasswordHasher` rehash-on-verify: when verification returns `SuccessRehashNeeded`, update
  the stored hash concurrency-safely (guarded by the old hash value so a concurrent password
  change wins).

### Endpoints (Admin API)

`AuthController` (anonymous unless noted; login + redeem rate-limited per IP):

- `POST /api/auth/login` `{username, password}` →
  `{token, expiresUtc, username, role, isGuest}` or uniform 401 (uniform timing, minimum
  response duration). Updates `LastLoginUtc`.
  **Side effect: successful real-user login deletes the guest user (cascades guest
  sessions).**
- `POST /api/auth/redeem` `{token, password}` → creates the user (invite, with the invite's
  role) or sets the password (reset, revoking all prior sessions), then auto-logs-in and
  returns the same shape as login — and therefore also triggers guest deletion. Edge rules:
  invite redemption fails if the username was taken in the meantime; reset redemption fails
  if the user no longer exists or is soft-deleted. Failures → generic 400.
- `POST /api/auth/logout` (authenticated) — deletes the session row.
- `POST /api/auth/change-password` `{currentPassword, newPassword}` — authenticated, real
  users only. Revokes the user's other sessions.
- `GET /api/auth/provider` — anonymous provider metadata (see seam above).

Auth responses carrying tokens set `Cache-Control: no-store`. Session and one-time tokens,
passwords, and `Authorization` headers are never written to application logs.

`UsersController` (session required; user management is **admin-only**):

- `GET /api/users` — users (incl. soft-deleted, with `deleted`, `role`, `lastLoginUtc`) +
  pending invites (username, role, createdBy, expiry).
- `POST /api/users/invites` `{username, role}` → `{token, expiresUtc}`. The SPA composes the
  link as `<its own origin>/redeem#token=...` — the backend never needs to know the SPA's
  URL.
- `DELETE /api/users/invites/{username}` — revoke a pending invite.
- `POST /api/users/{username}/reset` → `{token, expiresUtc}`.
- `PUT /api/users/{username}/role` `{role}` — cannot demote the last active admin.
- `DELETE /api/users/{username}` — soft delete; cannot delete yourself; cannot delete the
  last active admin. Revokes the user's sessions and outstanding resets.
- `POST /api/users/{username}/restore` — reactivates a soft-deleted user with old password
  hash and role.
- `DELETE /api/users/{username}?permanent=true` — only valid on an already soft-deleted
  user; frees the username; FKs cascade remaining rows.
- `POST /api/users` `{username, password}` — **guest-only** direct create of the first Admin
  (real users are invite-only; guest gets the direct form).

Guest policy: a guest session may only call `POST /api/users` and `POST /api/auth/logout`
(plus the anonymous endpoints). Everything else → 403. Guest deletion cascades guest
sessions, so the policy needs no existence re-checks.

Authorization layers: anonymous (login/redeem/provider) → authenticated real user (all
config/secret/environment/API-key endpoints, own password change, logout) → admin (user
management) — with guest outside all of them except its two endpoints.

All existing Admin API controllers switch from `ApiKeyAuthenticationFilter` to `[Authorize]`
with a deny-guest policy. Swagger security definition switches from `X-API-Key` to bearer.
Config.Api is untouched.

### Bootstrap & configuration

- Startup (Local provider only): if the `Users` table is empty, insert `guest` (idempotent,
  race-safe). Runs on every startup. While guest exists, log a prominent warning.
- Startup validation: `Auth:Provider=Local` + a data provider without user-storage support →
  fail fast with a clear message.
- New `AuthSettings`: `SessionLifetimeHours` = 8, `InviteLifetimeDays` = 7 (applies to reset
  links too).
- Password hashing: ASP.NET Core standalone `PasswordHasher` (PBKDF2, format-versioned)
  from `Microsoft.Extensions.Identity.Core` — no EF, no Identity schema.

### Frontend (SvelteKit SPA)

- `/login` page. Auth store holds `{token, expiresUtc, username, role, isGuest}` in
  localStorage.
- `client.ts` seam sends `Authorization: Bearer`; any 401 clears the session and redirects to
  `/login`. The static `apiKey` in `config.json` is removed.
- Layout guard: no token → `/login`. Guest token → locked to a single "create your first real
  user" screen.
- `/redeem` page reads the token from the **URL fragment** (`#token=...`, never the query
  string — fragments don't reach server logs or `Referer` headers), immediately strips it
  with `history.replaceState`, then: set password (×2, policy validated client- and
  server-side), auto-login, navigate home. The app sets a restrictive `Referrer-Policy`.
- Users admin page (admins only; hidden for role `User`): list users with role, last login,
  and deleted state ("show deleted" toggle); create invite with role (copyable link); revoke
  pending invite; generate reset link (copyable); change role; soft delete; restore;
  permanent delete (on deleted users, with confirmation).
- Change-password and logout in the header menu.
- Regenerate API types (`npm run gen:api`).

### Audit logging

The `AuditLog` table gains two columns (fresh installs via updated `13_AuditLog.sql`;
existing databases via idempotent `19_AlterAuditLog_AddUsernameAndAction.sql`):

- **`Username`** `NVARCHAR(100) NULL` — the acting user (from the session). `NULL` for rows
  predating login or written by non-user paths. (`User` is a reserved word in T-SQL.)
- **`Action`** `NVARCHAR(50) NULL` — `Insert`, `Delete`, `Save`, `Login`, `LoginFailed`,
  `InviteCreated`, `InviteRevoked`, `ResetLinkCreated`, `PasswordChanged`, `RoleChanged`,
  `UserRestored`, `UserPermanentlyDeleted`, ... Nullable because historical rows predate it;
  the alter script backfills `Action` from the legacy `Content` value (which today holds
  only the action verb), after which `Content` is optional free-form detail. All new writes
  set `Action`.
- `EntityId` semantics (column is NVARCHAR(36), so always a GUID or empty): user events →
  the target user's GUID; invite events → the invite's `Id` GUID; failed logins →
  `Guid.Empty` with the attempted username (truncated to 100 chars) in `Content`. The actor
  is always the `Username` column. Caller IP remains the direct connection IP (existing
  behavior; no forwarded-header parsing).
- Logged events: user created (by whom), user soft/permanently deleted, restored, role
  changed, invite created/revoked, reset link created, password changed, login success, and
  **login failure** (the only brute-force fingerprint given no lockout; write volume is
  bounded by the login rate limiter).

### Testing

- **Backend (NUnit + NSubstitute)**: auth service logic — guest lifecycle, session
  issue/validate/revoke/expiry, redemption paths (incl. username-taken and user-deleted
  edges), token replace/revoke rules, password policy, role policies (admin-only user
  management, last-active-admin rail for delete and demote), soft delete/restore/permanent
  delete semantics, session revocation on reset/change-password, fail-fast provider check —
  against mocked `IUserDataAccess`. Dapper implementations stay unit-untested, consistent
  with the rest of the repo (no SQL Server integration-test infrastructure in this feature;
  recorded as accepted risk in the review triage — the concurrency-sensitive SQL uses
  affected-row patterns reviewed by hand).
- **Frontend (Vitest)**: auth store (persistence, expiry), `client.ts` (bearer header,
  401 → clear session + redirect).
- **E2E (Playwright, mocked routes — no real backend, unchanged pattern)**: login happy path,
  guest → create-first-user flow, invite redemption.

## Rollout (breaking changes — release notes required)

- Admin API requires login; `X-API-Key` no longer accepted there. Scripts must log in first.
- Default `DataProvider` is now `SqlServer`; a connection string is required. File-based
  deployments cannot use admin login (startup fails fast) — migrating to SQL Server is the
  upgrade path.
- New `CreateScripts` `15_Users.sql`–`18_Sessions.sql` and
  `19_AlterAuditLog_AddUsernameAndAction.sql` must be run on existing databases
  (`13_AuditLog.sql` is updated for fresh installs; 19 is idempotent either way).
- After upgrade the Users table is empty → guest/guest is live; **claim the instance
  immediately**: log in as guest, create your admin user (which kills guest on first real
  login). The startup warning nags until claimed.
- README + CLAUDE.md updated: setup flow, SQL Server as primary provider, auth documentation,
  "adding an auth provider" extension guide.

## Security posture (accepted for v1)

- Login/redeem rate-limited per IP; ~minimum-response-time on failures; no account lockout.
  Failed logins are audit-logged.
- Sessions are revocable (DB-backed, user-joined on every request); logout, user deletion,
  soft deletion, demotion, and guest deletion take effect on the next request. Absolute 8 h
  lifetime, no refresh/sliding.
- Session token in localStorage: accepted XSS trade-off for a static SPA (no cookie
  infrastructure); the SPA has no third-party script includes.
- Raw (unhashed) one-time tokens and session tokens stored in the DB — an attacker with DB
  write access could mint credentials anyway.
- The well-known guest/guest credential is live until the first real login (ADR-0004) —
  an explicit product decision; mitigations are the guest's minimal capability, the startup
  warning, and the "claim immediately" rollout instruction.
- Restore reactivates the old password hash by design (admin's judgment whether to pair it
  with a reset link).
- Audit-log retention/rotation is out of scope (matches the table's existing behavior).
- HTTPS is assumed to be handled by deployment/reverse proxy.
