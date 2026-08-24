# Glossary

Ubiquitous language for ConfigurationService. Terms here are domain concepts, not implementation details.

- **Configuration Header** — A named configuration entity. Contains an ordered list of Configuration Sections. Has name (unique), active flag, and an encryption flag that cascades to its sections.
- **Configuration Section** — One JSON document inside a Header, applying to a set of Applications × Environments. Sections have an explicit order that affects resolution; duplicating a section inserts the copy directly after the original.
- **Secret** — A named value with the same header/section structure as a configuration, but each section holds a single plain-text value instead of JSON. Secrets have no tests and no history.
- **Application** — A consuming program. One of the two scoping dimensions for sections.
- **Environment** — A deployment context (e.g. test, production). The other scoping dimension.
- **$ref** — Reference syntax `$ref:ConfigName#Property/Path` inside configuration JSON. Resolves to the referenced configuration's value at that path; an empty path after `#` takes the entire configuration.
- **$refs** — Secret reference syntax `$refs:SecretName#` inside configuration JSON. Never resolved server-side; the literal string passes through to the client, where the middleware's `ISecretResolver` fetches the value from Config.Api's `/Secrets` endpoint on first use (lazy, cached per process).
- **Base convention** — A property named `base`/`Base` inside an object: its resolved value replaces the parent object entirely.
- **Soft delete** — Default deletion: the entity is flagged deleted but recoverable. Contrast **permanent delete**, an explicit opt-in.
- **Unhandled application / environment** — An application or environment not covered by any active section of a header. Shown on the editor as a coverage gap.
- **Test** — Resolving one section's JSON through the parser for one Application × Environment combination. A section's test runs all combinations; a header's test runs all its sections.
- **Usage** — An occurrence of a configuration referencing an entity (configuration, application, or environment), derived from the dependency graph. Always loaded on demand.
- **API key** — The `X-API-Key` credential used by middleware clients calling the public Config API. Admin-client keys use the `csk_` prefix. Not a login mechanism for humans; the Admin API uses Sessions instead.

## Admin identity & access

- **Real User** — A person who can log in to the admin page. Has a Role. Avoid: account, member.
- **Role** — What a real user may do: `Admin` (user management plus everything else) or `User` (everything except user management).
- **Guest User** — The bootstrap user (`guest`/`guest`) that exists whenever the user store is empty. Can do exactly one thing: create the first Admin. Deleted — not disabled (and not soft-deleted) — the first time a real user logs in. Avoid: default user, setup user.
- **Restore** — Reactivating a soft-deleted real user with their old password and role. The counterpart of the repo-wide soft delete, applied to users.
- **Invite** — Permission for a named person to become a real user by setting their own password, delivered as a link. Single-use, expires. Real users are created only by invite (or by the guest user). Avoid: registration, signup.
- **Reset Link** — The same mechanism as an invite, but targeting an existing real user who needs a new password. Avoid: forgot-password flow.
- **Redemption** — Consuming an invite or reset link: the holder sets a password and is logged in.
- **Session** — A revocable grant of admin-page access created by logging in. Expires after a fixed lifetime; deleting it ends access immediately. Avoid: JWT, bearer token (implementation terms).
- **Auth Provider** — The pluggable source of identity for the admin page. `Local` (database users) is the first; OIDC-based providers (ADFS, IdentityServer, Entra) can be added later.
