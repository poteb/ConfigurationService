# Svelte Admin Frontend — Design

**Date:** 2026-08-04
**Status:** Approved (brainstorm complete; pending grilling + external review)

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

- Svelte project lives at **`src/Config.Admin.WebClient/`** (takes over the Blazor project's path). During branch work the Blazor project is renamed to `src/Config.Admin.WebClient.Blazor/` for reference; it is deleted before the PR opens, together with its `.sln`, `.nuspec`, and `Config.Admin.WebClient.Tests` (its single mapper test is ported to Vitest).
- Build output is plain static files; no Node in production. Deployable to any static server, same model as the published WASM output.
- **Runtime config:** `static/config.json` → `{ "adminApiUrl": "...", "apiKey": "..." }`, fetched at boot before first render (same deploy-anywhere model as `wwwroot/appsettings.json`). Admin API and CORS unchanged.
- **Dev:** `npm run dev` on port 5071 (matches today). `npm run gen:api` regenerates TS types from `http://localhost:34246/swagger/v1/swagger.json`.
- **Scripts in `build/`** (Spool style: `@echo off`, `npm install` when `node_modules` missing, `errorlevel` checks):
  - `build-admin-client.cmd`, `build-admin-api.cmd`
  - `run-admin-client.cmd` (Vite dev, 5071), `run-admin-api.cmd` (Admin API, 34246)
  - `build-and-run-admin.cmd` — builds both, starts Admin API in a new window, runs the client dev server in the current one
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
- List filters move to the URL (`/?app=…&env=…&search=…`), replacing the in-memory `SearchCriteria` singleton. Shareable, survives refresh.
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
- **Dialogs**: `ConfirmDialog`, `DeleteDialog` (soft/permanent checkbox, default soft → `POST .../delete/{id}/{permanent}`), `DuplicateNameDialog` (prefilled `"<name> COPY"`), `ReorderDialog` (drag-drop, `svelte-dnd-action`).
- **`UsagesPanel`**: shared by applications, environments, and both editors; lazily loads the dependency graph on demand and renders links to referencing configurations. Never eager.
- **`SectionTestPanel`**: runs the section JSON through the parse endpoint for every application×environment combination; progress bar while running; one sub-panel per result (auto-expanded on problems) with pass/fail indicator, problem list, and a read-only editor showing the resolved JSON, height sized to content.
- **Test state store**: keyed by header id — NotStarted / InProgress / Complete / Failed(+problems). Drives per-row icons on `/` and "Test all" (all headers in parallel); cleared when the edited configuration id changes.
- **Dirty tracking**: snapshot on load + `$derived` deep-equal (replaces the 1 s polling timer). Drives Save enablement, `beforeNavigate` confirm, and `beforeunload`.
- **Mappers**: API DTO ↔ UI model as plain TS functions, incl. deep copy for dirty-checking and duplication. Existing `ConfigurationMapperTests` ported to Vitest.

## 4. Data layer & JSON editor

**Data layer** (`src/lib/api/`), replacing five C# services:
- `client.ts`: `fetch` wrapper — base URL + `X-API-Key` from runtime config; never throws: `ApiResult<T> = { ok: true, value } | { ok: false, error }`; parses save-error details out of the API's `errors` array.
- `adminApi.ts`: endpoint functions (configurations, secrets, applications, environments, settings, api-keys, header/section history, dependency graph, parse-for-test), typed against generated OpenAPI types. The Blazor app's unused `"Api"` HttpClient registration is dropped — everything calls the Admin API, matching actual current behavior.

**JSON editor** (`src/lib/editor/`) — CodeMirror 6 in a Svelte wrapper, feature parity with BlazorJsonEditor:
- JSON highlighting, line numbers, debounced lint with Ln/Col error panel, Valid/Invalid/Empty status, Format (pretty-print) button, auto-close brackets, Tab/Shift-Tab indent, configurable height/read-only.
- **`$ref` links**: decoration plugin marks `$ref:Name#Path` string values. Ctrl/Cmd+Click navigates to the referenced configuration; Ctrl+Shift+Click opens a new browser tab; link affordance (cursor/underline) while Ctrl held.
- **`$ref` autocomplete**: configuration names after `$ref:`, property paths after `#` (recursive path extraction from the referenced config's JSON — pure, unit-tested TS function; paths `/`-separated as today).
- Read-only instances in test-result and history panels; theme follows the app's light/dark state.

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

- **Vitest**: $ref parsing + suggestion providers, mappers (round-trip + deep copy), dirty deep-equal, `ApiResult` error parsing, ported mapper test.
- **Playwright smoke** against a mocked Admin API: configurations list → open → edit → save round-trip; secret round-trip; Ctrl+Click ref navigation; unsaved-changes guard.

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

## Out of scope

- Any Admin API change (endpoints, auth, CORS all stay as-is).
- Real authentication/login — planned as a separate follow-up feature; this rewrite only prepares the seams (§5).
- Workflow redesigns (side-by-side test results, command palette, etc.).
- SQL Server data provider, middleware packages, Config.Api — untouched.
