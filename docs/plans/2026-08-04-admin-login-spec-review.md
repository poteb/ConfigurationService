# Admin login — external spec review triage

Date: 2026-08-04. Reviewer: ChatGPT (gpt-5.6-sol) via OpenAI API. 24 findings:
4 blocker, 19 should-fix, 1 nitpick. Full reviewer output retained in the session; this file
records the disposition of each finding.

## Accepted (spec updated)

1. **[blocker] Invite role not persisted** — `UserTokens` split into `UserInvites`
   (username, role, createdBy) and `PasswordResets` (UserId FK); invites carry role in the DB.
2. **[blocker] Claims contract missing role** — canonical `role` claim added; login response
   and SPA auth store include `role`; session validation joins Users so claims always
   reflect the current row; OIDC role-mapping declared the provider's responsibility.
3. **[blocker] Non-null `Action` migration fails on existing rows** — `Action` is nullable;
   idempotent alter script backfills it from legacy `Content`; all new writes set it.
4. **Reset/change-password leaves sessions valid** — reset redemption revokes all prior
   sessions; change-password revokes other sessions; both transactional.
5. **Transactions/locking unspecified** — new "Concurrency & atomicity" section: atomic
   token consumption (affected-row check), in-transaction last-admin invariant, idempotent
   race-safe guest bootstrap, transactional password change.
6. **Reset authorization contradiction (any user vs admin-only)** — resolved to admin-only
   (Decision 3 fixed); ordinary users only change their own password. The contradiction was
   a leftover from before roles existed.
7. **Sessions/resets keyed by mutable username** — now keyed by immutable `UserId` with
   `ON DELETE CASCADE` FKs; invites remain username-keyed by necessity (no user row yet).
8. **Missing DB-enforced invariants** — NOT NULL, CHECK constraints on Role, explicit unique
   indexes (username; one active invite per username; one active reset per user), expiry
   indexes.
9. **Collation not guaranteed CI** — explicit `Latin1_General_100_CI_AS` on username columns
   instead of relying on server defaults.
10. **Route-breaking username characters** — charset restricted to letters, digits, `.-_@`.
11. **Role changes don't affect live sessions** — solved structurally: validation joins the
    user row per request, so demotion/soft-delete apply on the next request.
12. **Reset token leaks via query string** — redeem links use the URL fragment
    (`#token=...`), stripped with `history.replaceState`; restrictive `Referrer-Policy`.
13. **No cache/log hygiene for token responses** — `Cache-Control: no-store` on auth
    responses; tokens/passwords/Authorization never logged.
14. **500 ms sleep + no rate limit = resource exhaustion** — per-IP rate limiting on
    login/redeem (ASP.NET Core rate limiter); minimum-total-response-time instead of an
    unconditional sleep.
15. **Timing-based username enumeration** — dummy hash verification for unknown users;
    password length capped (128) and checked before hashing.
16. **Failed-login audit unbounded** — attempted username truncated to 100 chars; write
    volume bounded by the login rate limiter; forwarded-IP headers not trusted (unchanged
    existing behavior: direct connection IP).
17. **Cleanup unbounded / missing indexes** — bounded-batch deletes (`DELETE TOP (n)`),
    expiry indexes, expiry authoritative at validation regardless of cleanup.
18. **Audit `EntityId` undefined for invites / failed logins** — invites get a stable `Id`
    GUID used as EntityId; failed logins use `Guid.Empty` + attempted username in Content.
19. **Multi-instance bootstrap race** — idempotent insert; duplicate-key loss = success.
20. **[nitpick] Rehash-on-verify** — adopted, guarded against concurrent password change.

## Rejected (with reasons)

1. **[blocker] Replace guest/guest with a per-install random bootstrap secret / localhost
   setup mode** — Rejected as a redesign: the well-known guest/guest bootstrap is an explicit
   product decision (developer requirement, recorded with its trade-offs in ADR-0004). The
   reviewer's severity is understood; mitigations adopted instead: startup warning while
   guest exists, "claim immediately" rollout instruction, and guest's capability being
   limited to creating a user. Revisit if the project's threat model changes (e.g. instances
   exposed to untrusted networks at first boot).
2. **Force a password reset on restore (or block restore-with-old-hash)** — Rejected:
   restore-with-old-credentials is the developer-approved semantic; the admin performing the
   restore decides whether to also hand over a reset link. Sessions/tokens were already
   revoked at soft-delete time, so nothing stale survives restore.
3. **SQL Server integration tests (containerized) for migrations/races/collation** —
   Rejected for this feature: the repo has no integration-test infrastructure and standing
   one up is its own project. Recorded as accepted risk; the concurrency-sensitive SQL uses
   affected-row patterns and is hand-reviewed. Reasonable future work.
4. **Unicode normalization (NFC/NFKC) of usernames** — Rejected as overkill: the adopted
   charset restriction (letters, digits, `.-_@`) plus explicit CI collation removes the
   attack surface the normalization advice targets.
5. **Audit retention/rotation policy** — Rejected as out of scope: consistent with the
   existing AuditLog table, which has never had retention. Unchanged behavior, not a
   regression introduced by this feature.
