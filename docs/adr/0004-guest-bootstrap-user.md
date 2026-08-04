# 0004 — Guest bootstrap user; empty user store resurrects it

**Status:** Accepted (2026-08-04)

## Context

A fresh instance needs a way to create the first real user without a setup wizard, CLI tool,
or email infrastructure. Locked-out-last-admin recovery needs an answer too.

## Decision

Whenever the user store is empty at startup, the instance seeds user `guest` / password
`guest`, which can only create a real user (direct username+password form). Guest is deleted —
not disabled — the first time a real user logs in (redeeming an invite counts). Until then
guest keeps working, protecting against typo'd usernames and lost invites. Disaster recovery
is the same mechanism: delete all user rows, restart, log in as guest.

Alternatives rejected: first-run setup wizard (more code, and an unauthenticated setup page is
its own attack surface); CLI user creation (hostile to container deployments); config-file
admin credentials (secrets in config, no rotation story).

## Consequences

- The well-known guest/guest credential is live until the first real login — acceptable
  because the instance is unusable-by-design until then (guest can only create users), and
  operators are expected to claim the instance immediately after deployment.
- Username `guest` is reserved forever.
