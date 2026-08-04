# Svelte Admin Frontend — Design

**Date:** 2026-08-04
**Status:** Approved (brainstorm + grilling complete; pending external review)
**Related:** [ADR 0001](../adr/0001-svelte-admin-frontend.md), glossary in [CONTEXT.md](../../CONTEXT.md)

## Goal

Replace the Blazor WASM admin client (`Config.Admin.WebClient`, MudBlazor) with a Svelte SPA. Drivers: Blazor WASM payload/startup/debugging pain, preference for a mainstream JS/TS stack, easier future development, and visual modernization. Functional scope is a 1:1 port of current behavior with agreed small wins; workflows and page structure are unchanged.

## Decisions (from brainstorm)

| Topic | Decision |
|---|---|
| Strategy | Full replacement in one PR; no dual maintenance |
| Stack | SvelteKit, Svelte 5 (runes), TypeScript, adapter-static SPA mode (`ssr = false`, fallback `index.html`) |
| UI | Tailwind v4 + shadcn-svelte (copy-in components on bits-ui); drag-drop via `svelte-dnd-action` |
| JSON editor | CodeMirror 6 (replaces `pote.BlazorJsonEditor`) |
| API types | Generated from Admin API OpenAPI (`openapi-typescript`); generated file committed |
| Auth | Unchanged: static `X-API-Key` from a runtime config file (documented trust model: internal/network-isolated). User login is a planned follow-up feature; this rewrite is built login-ready (see §5) |
| UX scope | Same structure, better polish; filters move to URL; reactive dirty tracking |
| Structure | Unified editor core: configs and secrets share components via an entity descriptor |
| Testing | Vitest (logic) + Playwright smoke (mocked API) |

## 1. Layout, build & hosting

- Svelte project lives at **`src/Config.Admin.WebClient/`** (takes over the Blazor project's path). During branch work the Blazor project is renamed to `src/Config.Admin.WebClient.Blazor/` for reference; it is deleted before the PR opens, together with its `.sln` and `Config.Admin.WebClient.Tests` (its single mapper test is ported to Vitest).
- **NuGet packaging stays** (grilling outcome): the TeamCity/Octopus pipeline may consume `pote.Config.Admin.Client`, so a nuspec keeps that package id, now packing the Svelte `build/` output (static files). A `build/pack-admin-client.cmd` script wraps the pack step. Dropping the package is a possible follow-up once the pipeline is confirmed not to reference it.
- **IIS hosting** (grilling outcome): production host is IIS. The build ships its own SPA fallback — a `web.config` in `static/` so every build/package is self-contained. Deep links (`/EditConfiguration/{gid}`) depend on it. The rewrite rule excludes existing files and directories (`REQUEST_FILENAME` checks) so assets are never rewritten to HTML. Caching: `index.html` and `config.json` are served no-cache; hashed assets under `_app/immutable/` get long-lived immutable caching.
- Build output is plain static files; no Node in production. Deployable to any static server, same model as the published WASM output.
- **Runtime config:** `static/config.json` → `{ "adminApiUrl": "...", "apiKey": "..." }`, fetched at boot before first render (same deploy-anywhere model as `wwwroot/appsettings.json`). Admin API and CORS unchanged. Confirmed in grilling: Octopus JSON variable substitution only targets the two APIs' appsettings, not the client's config file, so the rename from `appsettings.json` is safe.
  - The **committed** `config.json` contains dev values only (localhost URL, placeholder key) — no real key ever enters the repo or the package. Deployment overwrites the file per environment (manual or future Octopus substitution).
  - Boot validates both fields; a missing/unfetchable/invalid `config.json` renders a full-page error naming the file (misdeployment signal).
- **SvelteKit config is pinned**: `adapter-static` with `fallback: 'index.html'`, root layout exports `ssr = false`, `prerender = false`. Root-only hosting is declared — the app is not designed for an IIS virtual directory subpath (same as today's `<base href="/">`).
- **Deployment risk (must verify before merge):** the TeamCity build config for this repo must be checked — if it is what packs/publishes the admin client, it needs Node and the new pack step. The Blazor folder is deleted only after that check. The package layout is documented: static site at package root, `index.html` top-level (same shape a wwwroot-purging Octopus deploy expects).
- **Dev:** `npm run dev` on port 5071 (matches today), `strictPort` so the port never silently drifts. `npm run gen:api` regenerates TS types from `http://localhost:34246/swagger/v1/swagger.json`; `openapi-typescript` is version-pinned via the committed `package-lock.json`. CI and the pack script use `npm ci`; the convenience scripts use `npm install` only when `node_modules` is missing.
- **Scripts in `build/`** (Spool style: `@echo off`, `npm install` when `node_modules` missing, `errorlevel` checks):
  - `build-admin-client.cmd`, `build-admin-api.cmd`
  - `run-admin-client.cmd` (Vite dev, 5071), `run-admin-api.cmd` (Admin API, 34246)
  - `build-and-run-admin.cmd` — builds the Admin API, starts it in a new window, runs the client dev server in the current one (no redundant client production build)
  - `pack-admin-client.cmd` — builds the client and packs `pote.Config.Admin.Client` from the build output
- **CI:** add a Node build+test job to `azure-pipelines.yml`; the existing middleware build is untouched.

## 2. Routing & pages

Canonical paths match today's exactly (SvelteKit routes are case-sensitive; keep current casing so bookmarks work):

| Route | Page |
|---|---|
| `/` | Configurations list |
| `/EditConfiguration`, `/EditConfiguration/[gid]` | Configuration editor (new / existing) |
| `/secrets` | Secrets list |
| `/EditSecret`, `/EditSecret/[gid]` | Secret editor |
| `/applications`, `/environments` | Inline-editable name tables, lazy "load usages" |
| `/ApiKeys` | API keys: name/key table, generate `csk_` + base64(32 random bytes, stripped of `+/=`), copy to clipboard |
| `/Settings` | Global settings (`EncryptAllJson`) |

Shared layout: sidebar nav (permanent desktop / collapsible mobile), Light/Dark/System theme menu persisted in localStorage (`theme-preference`), toast area, error banner region (replaces cascading `PageError`).

Behavioral changes:
- List filters move to the URL (`/?app=…&env=…&search=…`), replacing the in-memory `SearchCriteria` singleton. Shareable, survives refresh. Semantics: values are **names** (URL-encoded), absent/empty means no filter, unknown values are ignored (shown as no selection), and `/secrets` uses the same query model.
- After saving a new configuration: navigate to `/EditConfiguration/{gid}` with `replaceState` (parity — Back must not return to the empty form).

## 3. Unified editor core & components

Configs and secrets share one component set, parameterized by an **entity descriptor**:

```ts
{ kind: 'configuration' | 'secret',
  labels, routes, api,
  capabilities: { jsonEditor, tests, history, encryption } }
```

Configs enable all capabilities; secrets disable all four (plain text value instead of the editor).

- **`HeaderListPage`** (`/`, `/secrets`): filter bar (application, environment, name search, reset), sortable table, New/Refresh; test-status column and "Test all" only when `capabilities.tests`. Row click → editor route.
- **`HeaderEditorPage`**: header form — name with uniqueness validation, created timestamp, active checkbox, encryption checkbox (when enabled; force-locked on when global `EncryptAllJson` is set, cascades to sections) — unhandled apps/envs summary, lazy usages, sections accordion with alternating panel backgrounds, duplicated top/bottom toolbars: Save (enabled only when dirty+valid), Duplicate (dialog → toast with click-to-open), Delete (dialog, soft/permanent), Add section, Reorder, Expand/Collapse all; Test all and Load history (header level, page 1 size 10) for configs only.
- **`SectionPanel`**: env/app multi-selects with select-all, per-section encryption checkbox (configs), duplicate / soft-delete-with-undo / undo-changes buttons; body is the JSON editor stack (editor + `SectionTestPanel` + `SectionHistoryPanel`) or the secret text field. Explicit `index` ordering persisted; duplicate inserts at `index+1` and shifts the rest.
- **Dialogs**: `ConfirmDialog`, `DeleteDialog` (soft/permanent checkbox, default soft → `POST .../delete/{id}/{permanent}`), `DuplicateNameDialog` (prefilled `"<name> COPY"`), `ReorderDialog` (drag-drop, `svelte-dnd-action`, which also provides keyboard reorder for accessibility; dialogs/accordions get their a11y from bits-ui primitives).
- **Ordering/duplication edge rules** (from external review): soft-deleted sections keep their index and are excluded from the reorder dialog; indices are normalized sequentially on save; duplication's field-level copy/reset semantics (new ids, reset history/test state, copied flags) are ported from the existing mapper `Copy` code, which is the source of truth. Header/section history pagination matches current behavior exactly.
- **Name uniqueness**: client-side case-insensitive check against loaded names excluding the entity itself; the server remains authoritative. Save validates synchronously on click, so debounced lint state can never let an invalid document through.
- **Encryption edge case**: if the global settings fetch fails, the error banner shows and the checkbox behaves as not-forced — server-side encryption enforcement is unaffected by the client's view of the flag.
- **API keys**: generation uses `crypto.getRandomValues`; clipboard-copy failures surface a toast instead of silently reporting success.
- **`UsagesPanel`**: shared by applications, environments, and both editors; lazily loads the dependency graph on demand and renders links to referencing configurations. Never eager.
- **`SectionTestPanel`**: runs the section JSON through the parse endpoint for every application×environment combination; progress bar while running; one sub-panel per result (auto-expanded on problems) with pass/fail indicator, problem list, and a read-only editor showing the resolved JSON, height sized to content.
- **Test state store**: keyed by header id — NotStarted / InProgress / Complete / Failed(+problems). Drives per-row icons on `/` and "Test all". Refinements from external review: "Test all" runs with a small concurrency cap (~4 headers at a time) with progress feedback instead of unbounded parallel fan-out; any edit to a header invalidates its cached test results (not just an id change); leaving the page cancels in-flight runs. The combination algorithm (which app×env pairs a section tests) is ported exactly from `ConfigurationTestService` — including empty-selection behavior — not re-derived.
- **Dirty tracking**: snapshot on load + `$derived` deep-equal (replaces the 1 s polling timer); section JSON compares as raw text, so formatting-only changes count as dirty (parity — formatting changes the stored text). Drives Save enablement, the unsaved-changes guard (in-app navigation uses a proper dialog instead of native `confirm()`; tab close uses the browser's `beforeunload`), and Save-button state.
- **Secret value field**: single-line text input (parity with today's `MudTextField`).
- **Mappers**: API DTO ↔ UI model as plain TS functions, incl. deep copy for dirty-checking and duplication. Existing `ConfigurationMapperTests` ported to Vitest.

## 4. Data layer & JSON editor

**Data layer** (`src/lib/api/`), replacing five C# services:
- `client.ts`: `fetch` wrapper — base URL + `X-API-Key` from runtime config; never throws: `ApiResult<T> = { ok: true, value } | { ok: false, error }`; parses save-error details out of the API's `errors` array. The normalization boundary is total and unit-tested per class: network failure, abort, non-2xx status, non-JSON/malformed body, and empty (204) responses all map to a typed `ApiResult`. URLs are built with the `URL` API and path parameters are encoded.
- `adminApi.ts`: endpoint functions (configurations, secrets, applications, environments, settings, api-keys, header/section history, dependency graph, parse-for-test), typed against generated OpenAPI types. The Blazor app's unused `"Api"` HttpClient registration is dropped — everything calls the Admin API, matching actual current behavior.

**JSON editor** (`src/lib/editor/`) — CodeMirror 6 in a Svelte wrapper, feature parity with BlazorJsonEditor:
- JSON highlighting, line numbers, debounced lint with Ln/Col error panel, Valid/Invalid/Empty status, Format (pretty-print) button, auto-close brackets, Tab/Shift-Tab indent, configurable height/read-only.
- **`$ref` links**: decoration plugin marks `$ref:Name#Path` string values, detected on CodeMirror's syntax tree (string tokens), not raw regex over text. Ctrl+Click (Cmd+Click on macOS) navigates to the referenced configuration; Ctrl+Shift+Click (Cmd+Shift+Click) opens a new browser tab; link affordance (cursor/underline) while the modifier is held. The `$ref` grammar is taken from `Config.Parser` (in-repo source of truth), with shared test fixtures covering the empty-path whole-config form and names matched case-insensitively.
- **`$ref` autocomplete**: configuration names after `$ref:`, property paths after `#` (recursive path extraction from the referenced config's JSON — pure, unit-tested TS function; paths `/`-separated as today). Data source (parity, confirmed in code): the editor page loads the full configurations list once on mount (`GET Configurations` is unpaginated); name suggestions filter that list (prefix matches first), and path suggestions come from the **lowest-index non-deleted section** of the referenced header, matched case-insensitively — invalid JSON in that section yields no suggestions. `$ref:Name#` with an empty path (whole-config reference) must be handled by link parsing and autocomplete.
- Read-only instances in test-result and history panels; content-sized height is capped with internal scrolling beyond the cap. Theme follows the app's light/dark state; an inline pre-paint script in `app.html` applies the persisted/system theme before first render to avoid a flash.

## 5. Login readiness

User login will be added later as its own feature (it primarily requires Admin API work — identity story, token issuance, authorization). This rewrite stays login-free but reserves the seams so login lands without restructuring:

- **Single credential point**: every request's credentials attach in `client.ts` only. Swapping the static `X-API-Key` for a bearer token (or adding one) is a one-file change.
- **Boot auth gate**: the boot sequence is explicitly `load config.json → auth gate → render app`. The auth gate is a no-op today; a login screen slots into it later.
- **Layout & guard seams**: the shared layout reserves a slot for a user menu; SvelteKit's root `+layout.ts` is the designated place for a future route guard.

## 6. Error handling

- All API calls return `ApiResult`; failures surface in the page error banner (fetch/network) or as field/section-level messages (save validation errors from the API's `errors` array).
- The boot config fetch failing renders a clear full-page error (misdeployment signal), not a blank app.
- JSON parse errors are editor-local (lint panel), never banners.

## 7. Testing

- **Vitest**: $ref parsing + suggestion providers (shared fixtures with the grammar), mappers (round-trip + deep copy), dirty deep-equal, `ApiResult` normalization per failure class (network, abort, non-JSON, status codes, 204), editor helpers (format, lint positions, link detection).
- **Playwright smoke** against a mocked Admin API: configurations list → open → edit → save round-trip; secret round-trip; Ctrl+Click ref navigation; unsaved-changes guard (Back, sidebar link, save-then-leave); boot-config failure page; save validation errors surfaced.
- **Manual pre-merge checklist**: run against the real Admin API side by side with the Blazor client; one IIS deploy test exercising deep links and the web.config fallback. (Automated real-API/IIS suites were considered and rejected — see review triage.)

## 8. Parity checklist (must not miss)

1. Ctrl/Cmd+Click and Ctrl+Shift+Click `$ref` navigation; editor Tab handling and auto-close.
2. Deep links `/EditConfiguration/{gid}`, `/EditSecret/{gid}` unchanged.
3. Unsaved-changes guard on both editors (in-app + `beforeunload`).
4. Soft vs permanent delete semantics; soft-deleted sections restorable via undo before save.
5. `EncryptAllJson` force-locks encryption checkboxes; header checkbox cascades to sections.
6. Explicit section ordering; duplicate-at-index+1 semantics (order affects resolution).
7. Test = parse per app×env per section; index icons aggregate across a header's sections.
8. Lazy-only dependency-graph loading.
9. Runtime (not build-time) client configuration.
10. API-key generation format `csk_…` and clipboard copy.

## External review triage (ChatGPT, 2026-08-04)

The spec was reviewed by gpt-5.6-sol; 39 findings. Accepted items are folded into the sections above (runtime-config placeholder + boot validation, pinned SvelteKit config, root-only hosting declaration, IIS rewrite exclusions + caching, deployment-risk verification step, lockfile/`npm ci`/`strictPort`, URL filter semantics, test-run concurrency cap + invalidation-on-edit, ordering/duplication/uniqueness/encryption edge rules, crypto RNG, error-normalization contract, syntax-tree `$ref` detection + macOS modifiers, height caps, theme pre-paint, failure-path tests).

**Rejected, with reasons:**
- *NuGet package-contract test* — the consumer is unknown (possibly none); a contract test against an unknown contract tests nothing. Mitigated instead by documenting the package layout and the pre-merge TeamCity verification step.
- *Formal executable parity baseline (screenshots, acceptance doc per page)* — the renamed Blazor app running side by side **is** the baseline; a parallel document would duplicate the source of truth and immediately go stale.
- *Fully specifying every secondary page's states in the spec* — the Blazor source is the porting reference; the spec names the pages and their operations, implementation ports behavior from code.
- *CI check that regenerates OpenAPI types and fails on drift* — requires booting the .NET Admin API inside the Node CI job; cost outweighs benefit for a single-developer repo. Pinned versions + committed types + regen-on-API-change convention instead.
- *Automated contract/integration suite against a real Admin API + automated IIS smoke test* — replaced by the manual pre-merge checklist; this is an internal tool with one developer, and the mocked Playwright suite covers the client's logic.
- *Full CodeMirror browser-test matrix* — editor helpers are unit-tested and the smoke suite covers the critical paths (editing, ref click); a per-behavior browser matrix is disproportionate.
- *Automated accessibility audits* — a11y comes from accessible primitives (bits-ui, svelte-dnd-action keyboard support); audits are a worthwhile follow-up, not a rewrite gate.
- *Request cancellation/stale-response handling as a general data-layer feature* — applied only where staleness is real (test runs, filter-driven list fetches); a blanket AbortSignal architecture is YAGNI for this CRUD surface.

## Out of scope

- Any Admin API change (endpoints, auth, CORS all stay as-is).
- Real authentication/login — planned as a separate follow-up feature; this rewrite only prepares the seams (§5).
- Workflow redesigns (side-by-side test results, command palette, etc.).
- SQL Server data provider, middleware packages, Config.Api — untouched.
