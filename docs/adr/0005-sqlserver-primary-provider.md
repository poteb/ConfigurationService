# 0005 — SQL Server is the primary data provider; File is legacy

**Status:** Accepted (2026-08-04)

## Context

The repo historically treated the file-based data provider as production-ready and SQL Server
as incomplete. Direction has reversed: the file provider is likely end-of-life.

## Decision

New persistence features (starting with admin login's user/token/session storage) are
implemented in the SQL Server provider only; the File provider gets `NotSupportedException`
stubs. The default `DataProvider` setting flips from `File` to `SqlServer`. The Admin API
fails fast at startup if the active auth provider requires user storage the data provider
doesn't support.

## Consequences

- Fresh clones must configure a SQL Server connection string before the Admin API starts.
- Existing file-based deployments cannot use admin login; upgrading them means migrating to
  SQL Server (loud release note required).
