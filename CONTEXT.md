# Glossary

Ubiquitous language for ConfigurationService. Terms here are domain concepts, not implementation details.

- **Configuration Header** — A named configuration entity. Contains an ordered list of Configuration Sections. Has name (unique), active flag, and an encryption flag that cascades to its sections.
- **Configuration Section** — One JSON document inside a Header, applying to a set of Applications × Environments. Sections have an explicit order that affects resolution; duplicating a section inserts the copy directly after the original.
- **Secret** — A named value with the same header/section structure as a configuration, but each section holds a single plain-text value instead of JSON. Secrets have no tests and no history.
- **Application** — A consuming program. One of the two scoping dimensions for sections.
- **Environment** — A deployment context (e.g. test, production). The other scoping dimension.
- **$ref** — Reference syntax `$ref:ConfigName#Property/Path` inside configuration JSON. Resolves to the referenced configuration's value at that path; an empty path after `#` takes the entire configuration.
- **Base convention** — A property named `base`/`Base` inside an object: its resolved value replaces the parent object entirely.
- **Soft delete** — Default deletion: the entity is flagged deleted but recoverable. Contrast **permanent delete**, an explicit opt-in.
- **Unhandled application / environment** — An application or environment not covered by any active section of a header. Shown on the editor as a coverage gap.
- **Test** — Resolving one section's JSON through the parser for one Application × Environment combination. A section's test runs all combinations; a header's test runs all its sections.
- **Usage** — An occurrence of a configuration referencing an entity (configuration, application, or environment), derived from the dependency graph. Always loaded on demand.
- **API key** — The `X-API-Key` credential required by both APIs. Admin-client keys use the `csk_` prefix.
