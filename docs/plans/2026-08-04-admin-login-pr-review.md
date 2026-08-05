# Admin login — external PR review triage

Date: 2026-08-04. Reviewer: ChatGPT (gpt-5.6-sol) via OpenAI API, reviewing the full PR #203
diff plus description. 8 findings: 2 blocker, 6 should-fix.

## Accepted (fixed in the PR)

1. **[blocker] Last-admin rail not concurrency-safe under READ COMMITTED** — two concurrent
   demotions/deletes could each observe the other admin and both succeed. Fixed:
   `SoftDeleteUser` and `UpdateRole` now serialize on a transaction-scoped
   `sp_getapplock` (`ConfigService.AdminRail`) before the guarded UPDATE.
2. **[blocker, partial] Concurrent password changes could both report success** — fixed:
   `ChangePassword` now uses a guarded update (`WHERE PasswordHash = @expectedOldHash`,
   rowcount-checked); the loser of the race gets `false`. See Rejected #1 for the rest of
   this finding.
3. **Audit rows on anonymous endpoints had `Username = NULL`** — fixed: the audit helper
   accepts an explicit acting username; login/redeem stamp the just-authenticated user.
4. **Invite redemption only audited `Login`; reset redemption missed `PasswordChanged`;
   invite revocation used `Guid.Empty`** — fixed: `LoginResult.Redemption` drives extra
   `UserCreated`/`PasswordChanged` audit events; `DeleteInvite` returns the deleted
   invite's Id (`OUTPUT DELETED`) for the audit row.
5. **Audit failure turned completed operations into API errors** — fixed: audit writes are
   wrapped; a failed audit is logged (Serilog) but never changes the API result.
6. **`Problem(ex.Message)` leaked SQL/provider details** — fixed in the new
   auth/user controllers (generic messages, full exception logged server-side). The same
   pattern in pre-existing controllers is untouched: changing repo-wide error behavior is
   out of scope for this feature.
7. **Token columns inherited case-insensitive collation** — fixed: `Token` columns in
   `16`–`18` are `Latin1_General_100_BIN2` (opaque-token semantics). No alter script
   needed — these tables are new in this PR.
8. **Role CHECK constraints case-insensitive (`'admin'` would pass)** — fixed: `Role`
   columns in `15`/`16` are `Latin1_General_100_BIN2`, so only canonical values pass.
9. **Frontend/backend password policy divergence (Unicode vs ASCII classes)** — fixed:
   backend now uses explicit ASCII checks (`IsAsciiLetterLower/Upper`, `IsAsciiDigit`;
   special = anything else), matching the client regexes exactly.

## Rejected / deferred (with reasons)

1. **Full unit-of-work refactor (one connection/transaction per workflow: redeem,
   first-user, guest deletion + session issuance)** — Deferred. The remaining windows are
   narrow (e.g. invite consumed but user insert fails; guest finishing a create while a
   concurrent first login deletes guest) and low-impact for an admin tool with a handful
   of users. The single-use token consume, the admin rail, and the password-change guard —
   the invariants that actually protect security — are now atomic. A UoW abstraction over
   `IUserDataAccess` is honest future work if the tool grows multi-tenant traffic.
2. **Concurrent SQL integration tests** — Rejected for this feature, consistent with the
   spec-review triage: no SQL Server integration-test infrastructure in the repo.
