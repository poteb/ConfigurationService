# Admin login — design

Date: 2026-08-04
Status: Approved (brainstorm phase)

## Goal

Add user login to the ConfigurationService admin page. Simple database-backed authentication —
no IdentityServer/ADFS/Google for now — but architected so the identity provider is swappable
later (this is an open-source project; the auth mechanism must fit anyone and be extendable).

## Decisions (from brainstorm interview)

1. **Scope**: Login replaces the static `X-API-Key` for the Admin API. The SPA no longer ships
   an API key in `static/config.json`. Config.Api (middleware-facing) keeps API-key auth,
   untouched.
2. **Session mechanism**: JWT bearer token, 8-hour lifetime, no refresh tokens. The SPA sends
   `Authorization: Bearer <token>` via the existing seam in `src/lib/api/client.ts`.
3. **Password reset**: Admin-driven — any logged-in (non-guest) user can generate a one-time
   reset link for another user. No SMTP/email anywhere. A logged-in user can always change
   their own password.
4. **Invites**: New users only by invite. The inviter fixes the username; the invitee opens a
   single-use link (valid 7 days) and sets their password. Invite links and reset links are the
   same mechanism: a token that authorizes setting the password for a username.
5. **Guest user**:
   - A new instance is born with user `guest` / password `guest`.
   - Guest can do exactly one thing: directly create a real user (username + password form —
     no invite-link ceremony, since the person at the keyboard is the new user).
   - Guest is **deleted** (not disabled) the first time a real (non-guest) user logs in.
     Redeeming an invite auto-logs-in and counts as a login.
   - Until a real user has logged in, guest keeps working and can create more users
     (protects against typo'd usernames / lost invites).
   - **Empty user store ⇒ guest is (re)created at startup.** Bootstrap and disaster recovery
     ("last admin locked out": delete all user rows, restart) are the same mechanism.
6. **Roles**: None. Every real user is a full admin. Guest is the only special case.
7. **Storage**: SQL Server provider is the focus (file provider is likely end-of-life). File
   provider gets a stub that throws `NotSupportedException` with a clear message.

## Architecture

### Swappable identity provider (the load-bearing seam)

**The app consumes standard JWT bearer identities; how tokens are issued is the pluggable part.**

- Config setting `Auth:Provider` selects the provider. This feature implements `"Local"`.
  A future `"Oidc"` provider covers ADFS / IdentityServer / Entra / any OIDC authority.
- Backend seam: an `IAuthProviderSetup` registration interface. Each provider contributes:
  - service registrations,
  - JWT-validation configuration,
  - optionally its own endpoints.

  `Local` contributes the `AuthController` (login/redeem/change-password), user management,
  the user store (`IUserDataAccess`), guest bootstrap, and validation of its self-signed JWTs.
  A future `Oidc` provider contributes only JwtBearer validation against the external
  authority — no local endpoints, no user table, no guest user.
- Everything outside the provider is provider-agnostic: controllers authorize on **claims**
  (`name`, plus a `guest` claim only Local ever issues), never on how the token was minted.
  Invite/reset/guest/user-management are Local-only features and drop out of the request path
  when another provider is active.
- Frontend seam: anonymous `GET /api/auth/provider` returns provider metadata.
  `{type: "local"}` → SPA shows the login form. Future `{type: "oidc", authority, clientId}` →
  redirect-based flow. The SPA auth store and `client.ts` deal only in "current token".
- Docs: add a short "adding an auth provider" section (public extension point).

Out of scope now: implementing OIDC, mixed mode (two providers at once), mapping external
identities to local user records.

### Data model (SqlServer `CreateScripts`)

`15_Users.sql`:

| Column       | Type                | Notes                        |
|--------------|---------------------|------------------------------|
| Id           | UNIQUEIDENTIFIER PK |                              |
| Username     | NVARCHAR(100)       | unique                       |
| PasswordHash | NVARCHAR(500)       | ASP.NET Core PasswordHasher  |
| IsGuest      | BIT                 |                              |
| CreatedUtc   | DATETIME2           |                              |

`16_UserTokens.sql`:

| Column     | Type           | Notes                                  |
|------------|----------------|----------------------------------------|
| Token      | NVARCHAR(100) PK | random 256-bit, url-safe             |
| Username   | NVARCHAR(100)  | target user                            |
| Purpose    | NVARCHAR(20)   | `invite` \| `reset`                    |
| ExpiresUtc | DATETIME2      | now + 7 days                           |

Tokens are single-use: deleted on redemption. Username `guest` is reserved (cannot be invited
or created).

New `IUserDataAccess` in `Config.DataProvider.Interfaces`; Dapper implementation in
`Config.DataProvider.SqlServer`; stub in `Config.DataProvider.File` (throws
`NotSupportedException`: "user login requires the SqlServer provider").

### Endpoints (Admin API)

`AuthController` (anonymous):

- `POST /api/auth/login` `{username, password}` → `{token, expiresUtc, username, isGuest}` or
  uniform 401 (same response for unknown user / wrong password). ~500 ms delay on failure to
  blunt brute force; no lockout machinery.
  **Side effect: successful non-guest login deletes the guest user.**
- `POST /api/auth/redeem` `{token, password}` → creates the user (invite) or sets the password
  (reset), then auto-logs-in and returns the same shape as login. Expired/unknown/used token →
  400 with a generic message.
- `POST /api/auth/change-password` `{currentPassword, newPassword}` — authenticated, non-guest.
- `GET /api/auth/provider` — anonymous provider metadata (see seam above).

`UsersController` (JWT required):

- `GET /api/users` — users + pending invites (with expiry). Non-guest only.
- `POST /api/users/invites` `{username}` → `{token, expiresUtc}`. Non-guest only. The SPA
  composes the link as `<its own origin>/redeem?token=...` — the backend never needs to know
  the SPA's URL.
- `POST /api/users/{username}/reset` → `{token, expiresUtc}`. Non-guest only.
- `DELETE /api/users/{username}` — non-guest only; cannot delete yourself. (Deleting the last
  user directly in the DB is the designed recovery path.)
- `POST /api/users` `{username, password}` — **guest-only** direct create (real users are
  invite-only; guest gets the direct form).

Guest policy: a guest JWT may only call `POST /api/users` (plus the anonymous endpoints).
Everything else → 403.

All existing Admin API controllers switch from `ApiKeyAuthenticationFilter` to `[Authorize]`
with a deny-guest policy. Config.Api is untouched.

### Bootstrap & configuration

- Startup (Local provider only): if the `Users` table is empty, insert `guest` with hashed
  password `guest`. Runs on every startup.
- New `AuthSettings`:
  - `JwtSigningKey` — if empty, auto-generate at startup and log a warning (restart then
    invalidates sessions).
  - `TokenLifetimeHours` = 8
  - `InviteLifetimeDays` = 7
- Password hashing: ASP.NET Core standalone `PasswordHasher` (PBKDF2, format-versioned)
  from `Microsoft.Extensions.Identity.Core` — no EF, no Identity schema.

### Frontend (SvelteKit SPA)

- `/login` page. Auth store holds `{token, expiresUtc, username, isGuest}` in localStorage.
- `client.ts` seam sends `Authorization: Bearer`; any 401 clears the session and redirects to
  `/login`. The static `apiKey` in `config.json` is removed.
- Layout guard: no token → `/login`. Guest token → locked to a single "create your first real
  user" screen.
- `/redeem?token=...` page: set password (×2), auto-login, navigate home.
- Users admin page: list users, create invite (copyable link), generate reset link (copyable),
  delete user, pending invites with expiry.
- Change-password in the header menu.

### Audit & testing

- User actions (login, user created/deleted, invite/reset created, password changed) go
  through the existing audit-log handler, stamped with the acting username from the JWT.
- NUnit: auth service (guest lifecycle, redemption paths, expiry/reuse rejection, hashing,
  login side effects) with NSubstitute'd `IUserDataAccess`.
- Vitest: auth store, client seam (401 handling, bearer header).
- Playwright: one e2e for the login flow.

## Security posture (accepted for v1)

- No rate limiting beyond the fixed failure delay; no account lockout.
- No refresh tokens; JWTs are irrevocable until expiry (8 h).
- Raw (unhashed) one-time tokens stored in the DB — an attacker with DB write access could
  mint credentials anyway.
- HTTPS is assumed to be handled by deployment/reverse proxy.
