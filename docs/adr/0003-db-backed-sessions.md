# 0003 — DB-backed sessions instead of self-signed JWTs for local auth

**Status:** Accepted (2026-08-04)

## Context

The Local auth provider needs a way to keep users logged in. The obvious path is self-signed
JWTs, but those require signing-key management (config burden, multi-instance key sync,
restart invalidation when auto-generated) and are irrevocable until expiry — which conflicts
with hard guarantees we want (guest deletion is terminal; deleting a user ends their access).

## Decision

Local sessions are opaque random tokens stored in a `Sessions` table and validated by DB
lookup per request. Deleting the row revokes access immediately; deleting a user (or the
guest) deletes their sessions. No signing key exists, so load-balanced instances and restarts
need zero coordination. Fixed 8-hour absolute lifetime; expired rows cleaned up
opportunistically.

The per-request DB lookup is irrelevant at admin-UI traffic levels. External OIDC providers
(ADR-0002) still use stateless JWT validation — this decision is Local-provider-only.
