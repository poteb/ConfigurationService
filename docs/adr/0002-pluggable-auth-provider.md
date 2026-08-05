# 0002 — Pluggable auth provider; the app consumes claims, never issuance details

**Status:** Accepted (2026-08-04)

## Context

The admin page gets database-backed login now, but ADFS/IdentityServer/OIDC login may come
later, and as an open-source project the auth mechanism must be extendable by others.

## Decision

Authentication is selected by config (`Auth:Provider`). Each provider implements a
registration seam (`IAuthProviderSetup`) contributing its service registrations, its
token-validation setup, and optionally its own endpoints. Everything outside the provider
authorizes on claims (`name`, plus a `guest` claim only the Local provider ever issues) and
never on how the token was minted. The SPA asks `GET /api/auth/provider` (anonymous) what
provider is active and adapts its login UI; otherwise it only ever forwards an opaque token.

Invite/reset/guest/user-management are features of the Local provider, not of the app — they
drop out of the request path entirely when another provider is active.

## Consequences

- A future OIDC provider is validation-only (JwtBearer against an external authority): no user
  table, no guest, no local endpoints.
- Mixed mode (two providers at once) and mapping external identities to local user records are
  explicitly out of scope until needed.
