# Svelte Admin Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Blazor WASM admin client with a SvelteKit static SPA at `src/Config.Admin.WebClient/`, feature-parity per the spec.

**Architecture:** SvelteKit adapter-static SPA (ssr off, fallback index.html). Unified editor core: configs and secrets share `HeaderListPage`/`HeaderEditorPage`/`SectionPanel`, parameterized by an entity descriptor. One typed API client over generated OpenAPI types. CodeMirror 6 JSON editor with $ref links/autocomplete.

**Tech Stack:** Svelte 5 (runes) + TypeScript, SvelteKit + adapter-static, Tailwind v4 + shadcn-svelte (bits-ui), CodeMirror 6, svelte-dnd-action, openapi-typescript, Vitest, Playwright.

**Spec:** `docs/plans/2026-08-04-svelte-admin-frontend-design.md` — its Global rules apply to every task. **Porting reference:** the Blazor client, renamed to `src/Config.Admin.WebClient.Blazor/` in Task 1, is the behavioral source of truth; each task names the files to port from.

## Global Constraints

- Package manager: npm; commit `package-lock.json`; `npm ci` in CI/pack scripts.
- Dev server port **5071**, `strictPort: true`.
- Routes keep exact current casing: `/`, `/EditConfiguration[/gid]`, `/secrets`, `/EditSecret[/gid]`, `/applications`, `/environments`, `/ApiKeys`, `/Settings`.
- Root-only hosting; `ssr = false`, `prerender = false`, adapter-static `fallback: 'index.html'`.
- Committed `static/config.json` holds dev values only (no real key).
- All API calls return `ApiResult<T>`; nothing in `lib/api` throws.
- Namespace/branding: page title "Configuration Admin".
- Commit after every task (small commits within tasks welcome).

---

### Task 1: Scaffold + boot config + shell hosting files

**Files:**
- Rename: `src/Config.Admin.WebClient/` → `src/Config.Admin.WebClient.Blazor/` (git mv; also move `Config.Admin.WebClient.sln`, fix its project paths)
- Create: SvelteKit project at `src/Config.Admin.WebClient/` (`package.json`, `svelte.config.js`, `vite.config.ts`, `tsconfig.json`, `src/app.html`, `src/app.css`, `src/routes/+layout.ts`, `src/routes/+layout.svelte` placeholder, `src/routes/+page.svelte` placeholder)
- Create: `static/config.json`, `static/web.config`, `src/lib/runtime-config.ts`, `src/lib/runtime-config.test.ts`

**Interfaces (produces):**
- `loadRuntimeConfig(fetchFn?): Promise<RuntimeConfig>` where `type RuntimeConfig = { adminApiUrl: string; apiKey: string }` — throws `RuntimeConfigError` with a human message on fetch failure/missing fields; app boot catches it and renders the full-page error.
- `getRuntimeConfig(): RuntimeConfig` — returns the loaded config (set once at boot).

Key content:

`svelte.config.js`: adapter-static `{ fallback: 'index.html' }`.
`src/routes/+layout.ts`: `export const ssr = false; export const prerender = false;`
`vite.config.ts`: `server: { port: 5071, strictPort: true }`.
`static/config.json`: `{ "adminApiUrl": "http://localhost:34246", "apiKey": "dev-placeholder-key" }`
`static/web.config`: IIS rewrite → `index.html` with `{REQUEST_FILENAME}` isFile/isDirectory negations; no-cache for `index.html` + `config.json` (outbound rule or location headers); `_app/immutable/*` long-cache.
`src/app.html`: inline pre-paint theme script reading `localStorage['theme-preference']` (System|Light|Dark, matchMedia fallback) setting `document.documentElement.classList.toggle('dark', …)`.

- [ ] Step 1: `git mv` Blazor project + sln; fix sln project path; commit "Rename Blazor admin client to Config.Admin.WebClient.Blazor".
- [ ] Step 2: Scaffold SvelteKit app (`npx sv create` or manual files), add tailwind v4 + shadcn-svelte init, adapter-static. Verify `npm run build` produces `build/index.html`.
- [ ] Step 3: Write failing Vitest `runtime-config.test.ts`: valid config parses; missing field → `RuntimeConfigError`; fetch rejection → `RuntimeConfigError`. Run (fails).
- [ ] Step 4: Implement `runtime-config.ts`; tests pass.
- [ ] Step 5: Wire boot in `+layout.svelte`: await load before rendering slot; on error render full-page error naming `config.json`.
- [ ] Step 6: Commit "Scaffold SvelteKit admin client with runtime config boot".

### Task 2: OpenAPI types + API client (`ApiResult`)

**Files:**
- Create: `src/lib/api/generated.d.ts` (via `npm run gen:api` against running Admin API), `src/lib/api/client.ts`, `src/lib/api/client.test.ts`, `src/lib/api/adminApi.ts`
- Modify: `package.json` (script `"gen:api": "openapi-typescript http://localhost:34246/swagger/v1/swagger.json -o src/lib/api/generated.d.ts"`)

**Interfaces (produces):**
```ts
export type ApiError = { kind: 'network'|'abort'|'http'|'invalid-json'; status?: number; message: string; errors?: string[] };
export type ApiResult<T> = { ok: true; value: T } | { ok: false; error: ApiError };
// client.ts
export async function apiFetch<T>(path: string, init?: RequestInit & { signal?: AbortSignal }): Promise<ApiResult<T>>;
// adminApi.ts — one function per endpoint, e.g.:
export function getConfigurations(): Promise<ApiResult<ConfigurationHeaderApi[]>>;
export function getConfiguration(gid: string): Promise<ApiResult<ConfigurationHeaderApi>>;
export function saveConfiguration(h: ConfigurationHeaderApi): Promise<ApiResult<ConfigurationHeaderApi>>;
export function deleteConfiguration(id: string, permanent: boolean): Promise<ApiResult<void>>;
// … same pattern for secrets, applications, environments, settings, apiKeys, headerHistory, sectionHistory, dependencyGraph, parse (test)
```
Endpoint list + verbs: port exactly from `src/Config.Admin.WebClient.Blazor/Config.Admin.WebClient/Services/AdminApiService.cs` (315 lines), `ApiService.cs`, `SettingsService.cs`, `ApiKeysService.cs`, `DependencyGraphApiService.cs`. Save-error parsing: non-2xx with JSON body containing `errors` array → `ApiError.errors`.

- [ ] Step 1: Add gen:api script; run Admin API locally; generate `generated.d.ts`; commit.
- [ ] Step 2: Failing tests for `apiFetch`: 2xx JSON → ok; 204 → ok undefined; non-2xx JSON `{errors:[…]}` → http error with errors; non-JSON body → invalid-json; network reject → network; AbortError → abort. Use injected `fetch` stub.
- [ ] Step 3: Implement `client.ts` (URL join via `new URL`, `X-API-Key` header, encodeURIComponent for path params helper `p(...)`). Tests pass.
- [ ] Step 4: Implement `adminApi.ts` covering every endpoint the five C# services expose.
- [ ] Step 5: Commit "Add typed API client with ApiResult normalization".

### Task 3: Domain models, mappers, dirty tracking

**Files:**
- Create: `src/lib/model/types.ts`, `src/lib/model/mappers.ts`, `src/lib/model/mappers.test.ts`, `src/lib/model/deepEqual.ts`, `src/lib/model/deepEqual.test.ts`

**Interfaces (produces):**
```ts
// UI models (port from Blazor Model/ + Mappers/)
export type Header = { id: string; name: string; created: string; isActive: boolean; isJsonEncrypted: boolean; sections: Section[]; kind: 'configuration'|'secret' };
export type Section = { id: string; index: number; json: string; value: string; deleted: boolean; isJsonEncrypted: boolean; isNew: boolean; environments: NamedRef[]; applications: NamedRef[] };
export type NamedRef = { id: string; name: string };
export function apiToHeader(api: ConfigurationHeaderApi): Header;
export function headerToApi(h: Header): ConfigurationHeaderApi;
export function cloneHeader(h: Header): Header;               // deep copy (dirty baseline, duplicate)
export function duplicateSection(s: Section, atIndex: number): Section; // new id, isNew, index+1 semantics
export function deepEqual(a: unknown, b: unknown): boolean;
```
Port mapping + Copy semantics from `Mappers/ConfigurationMapper.cs`, `SecretMapper.cs` (source of truth for what copies/resets). Port `ConfigurationMapperTests.cs` cases into `mappers.test.ts`.

- [ ] Steps: TDD each function (failing test → implement → pass), then commit "Add domain models, mappers, deep-equal".

### Task 4: $ref grammar + suggestions

**Files:**
- Create: `src/lib/refs/refGrammar.ts`, `src/lib/refs/refGrammar.test.ts`, `src/lib/refs/pathSuggestions.ts`, `src/lib/refs/pathSuggestions.test.ts`

**Interfaces (produces):**
```ts
export type ParsedRef = { name: string; path: string };       // path '' = whole config
export function parseRef(value: string): ParsedRef | null;    // grammar per src/Config.Parser (see Parser.cs $ref handling)
export function extractPropertyPaths(json: string): string[]; // recursive, '/'-separated (port GetConfigurationPropertyPaths body)
export function nameSuggestions(all: string[], filter: string): string[];   // contains-filter, prefix-first ordering
export function pathSuggestions(paths: string[], filter: string): string[]; // same ordering
```
Check `src/Config.Parser/Parser.cs` for the authoritative `$ref:` syntax (prefix, separator `#`, empty path). Fixtures: nested objects, arrays, empty path, case-insensitive name match.

- [ ] Steps: TDD, commit "Add \$ref grammar and suggestion providers".

### Task 5: Layout shell, theme, nav, error banner, toasts

**Files:**
- Create: `src/lib/theme.ts`, `src/lib/components/AppShell.svelte`, `src/lib/components/NavMenu.svelte`, `src/lib/components/ThemeMenu.svelte`, `src/lib/components/ErrorBanner.svelte`, `src/lib/stores/pageError.svelte.ts`; shadcn components as needed (button, dialog, table, accordion, select, checkbox, tooltip, sonner toast)
- Modify: `src/routes/+layout.svelte`

**Interfaces (produces):**
- `pageError.svelte.ts`: `export const pageError = $state({ message: '' }); export function setPageError(m: string): void; export function clearPageError(): void` (module-level runes store).
- `theme.ts`: `getTheme(): 'System'|'Light'|'Dark'`, `setTheme(t): void` (persists `theme-preference`, toggles `dark` class, tracks system changes).
- Nav entries (6): Configurations `/`, Secrets `/secrets`, Applications `/applications`, Environments `/environments`, Api keys `/ApiKeys`, Settings `/Settings` — port from `Shared/NavMenu.razor`.

- [ ] Steps: build shell (desktop sidebar + mobile sheet), theme menu pinned bottom; error banner above content cleared on navigation; verify visually via dev server; commit "Add app shell, theme, navigation".

### Task 6: Entity descriptors + list pages (`/`, `/secrets`) with URL filters

**Files:**
- Create: `src/lib/entities/descriptor.ts`, `src/lib/components/HeaderListPage.svelte`, `src/lib/stores/testState.svelte.ts` (icon states only; runner in Task 9), `src/lib/components/TestStatusIcon.svelte`
- Modify: `src/routes/+page.svelte`, create `src/routes/secrets/+page.svelte`

**Interfaces (produces):**
```ts
export type EntityDescriptor = {
  kind: 'configuration'|'secret';
  labels: { singular: string; plural: string };
  editRoute: (gid?: string) => string;          // '/EditConfiguration/…' | '/EditSecret/…'
  api: { list(): Promise<ApiResult<Header[]>>; get(gid): …; save(h): …; delete(id, permanent): … };
  capabilities: { jsonEditor: boolean; tests: boolean; history: boolean; encryption: boolean };
};
export const configurationDescriptor: EntityDescriptor;
export const secretDescriptor: EntityDescriptor;
// testState.svelte.ts
export type TestStatus = 'NotStarted'|'InProgress'|'Complete'|'Failed';
export function getTestState(headerId: string): { status: TestStatus; problems: string[] };
export function setTestState(headerId: string, s: {status: TestStatus; problems: string[]}): void;
export function clearTestState(headerId?: string): void;
```
List behavior port source: `Pages/Index.razor(.cs)`, `Pages/Secrets.razor(.cs)`. Filters: URL query `app`, `env`, `search` (names, encoded; absent = all; unknown ignored). Sortable name column; applications/environments comma columns; inactive icon; test icon column + "Test all" only for configurations.

- [ ] Steps: implement, port filter logic, wire URL params via `page.url.searchParams` + `goto(…, { replaceState: true, keepFocus: true })`; commit "Add list pages with URL filters".

### Task 7: CodeMirror JSON editor component

**Files:**
- Create: `src/lib/editor/JsonEditor.svelte`, `src/lib/editor/format.ts`, `src/lib/editor/refLinks.ts` (decoration + click plugin), `src/lib/editor/refAutocomplete.ts`, `src/lib/editor/editor.test.ts` (helpers only)

**Interfaces (produces):**
```ts
// JsonEditor.svelte props
{ value: string; onChange?: (v: string) => void; readOnly?: boolean; height?: string; maxHeight?: string;
  onRefClick?: (ref: ParsedRef, newTab: boolean) => void;
  getNameSuggestions?: (filter: string) => string[];
  getPathSuggestions?: (configName: string, filter: string) => string[] }
```
Features: json language + lint (Ln/Col panel, debounced), status chip (Valid/Invalid/Empty), Format button using `format.ts` (2-space, port `FormatJsonHelper` behavior incl. returning input on failure), auto-close brackets, Tab indent, dark theme via CSS variables synced to `.dark`, ref decorations from syntax tree string tokens via `parseRef`, modifier-held link affordance (Ctrl, Cmd on mac), click → `onRefClick(ref, shiftKey)`.

- [ ] Steps: TDD helpers (`format.ts`, link ranges from a doc string), build component, manual check in dev route; commit "Add CodeMirror 6 JSON editor with \$ref links and autocomplete".

### Task 8: Header editor (`/EditConfiguration`) — sections, dialogs, dirty guard

**Files:**
- Create: `src/lib/components/HeaderEditorPage.svelte`, `src/lib/components/SectionPanel.svelte`, `src/lib/components/UsagesPanel.svelte`, `src/lib/components/dialogs/{ConfirmDialog,DeleteDialog,DuplicateNameDialog,ReorderDialog}.svelte`, `src/lib/dirty.svelte.ts`
- Create: `src/routes/EditConfiguration/+page.svelte`, `src/routes/EditConfiguration/[gid]/+page.svelte`

**Interfaces (produces):**
- `dirty.svelte.ts`: `createDirtyTracker<T>(getCurrent: () => T): { baseline: T; isDirty: boolean (getter, deepEqual-based); reset(): void }`
- `HeaderEditorPage` props: `{ descriptor: EntityDescriptor; gid?: string }` — everything else internal.

Port source: `Pages/EditConfiguration.razor(.cs)` (435+106 lines), `Components/ConfigurationContent.razor`. All behaviors per spec §3 (uniqueness, encryption forcing+cascade, unhandled apps/envs, duplicate→toast with click-to-open, soft-delete undo, reorder normalization on save, add-section scroll-to-bottom, save→replaceState for new, guard via `beforeNavigate` dialog + `beforeunload`).

- [ ] Steps: build page against configurationDescriptor; Vitest for `createDirtyTracker`; manual parity pass against Blazor app; commit "Add configuration editor".

### Task 9: Tests + history panels, test runner, Test all

**Files:**
- Create: `src/lib/components/SectionTestPanel.svelte`, `src/lib/components/SectionHistoryPanel.svelte`, `src/lib/tests/testRunner.ts`, `src/lib/tests/testRunner.test.ts`
- Modify: `src/lib/components/{SectionPanel,HeaderEditorPage,HeaderListPage}.svelte`

**Interfaces (produces):**
```ts
export function runSectionTests(section: Section, header: Header, opts: { signal?: AbortSignal; onProgress?: (done: number, total: number) => void }): Promise<SectionTestResult[]>;
export type SectionTestResult = { application: NamedRef; environment: NamedRef; ok: boolean; problems: string[]; resolvedJson: string };
export function runAllHeaders(headers: Header[], opts: { concurrency: 4; signal?: AbortSignal }): Promise<void>; // updates testState store
```
Port combination algorithm from `Services/ConfigurationTestService.cs` + `AllConfigurationsTestService.cs` exactly. Invalidation: any header edit → `clearTestState(headerId)`; page leave aborts.

- [ ] Steps: TDD runner with stubbed parse API; panels (progress bar, auto-expand problem results, read-only editors height-capped); history panel per `ConfigurationContentHistory.razor` (page 1 size 10 parity); commit "Add test and history panels".

### Task 10: Secret editor + remaining pages

**Files:**
- Create: `src/routes/EditSecret/+page.svelte`, `src/routes/EditSecret/[gid]/+page.svelte` (HeaderEditorPage + secretDescriptor)
- Create: `src/routes/applications/+page.svelte`, `src/routes/environments/+page.svelte`, shared `src/lib/components/NameTablePage.svelte`
- Create: `src/routes/ApiKeys/+page.svelte`, `src/routes/Settings/+page.svelte`, `src/lib/apikeys.ts` (+ test: `csk_` format via `crypto.getRandomValues`, strip `+/=`)

Port sources: `EditSecret.razor(.cs)`, `SecretContent.razor` (single-line value input), `Applications/Environments.razor(.cs)` (inline edit, mark-deleted+undo, save loop, load usages), `ApiKeysComponent.razor` (generate/copy/delete/save; clipboard failure toast), `SettingsComponent.razor` (EncryptAllJson).

- [ ] Steps: TDD `generateApiKey()`; build pages; manual parity pass; commit "Add secret editor and management pages".

### Task 11: Playwright smoke suite

**Files:**
- Create: `playwright.config.ts`, `e2e/mocks.ts` (route-intercepted Admin API fixtures), `e2e/smoke.spec.ts`

Cases (spec §7): list→open→edit→save round-trip; secret round-trip; Ctrl+Click ref navigation; unsaved guard (Back, sidebar, save-then-leave); boot-config failure page; save validation errors shown.

- [ ] Steps: mocks via `page.route('**/Configurations**', …)`; run headed locally + `npm run test:e2e`; commit "Add Playwright smoke suite".

### Task 12: Build scripts, packaging, CI, docs, Blazor deletion

**Files:**
- Create: `build/build-admin-client.cmd`, `build/build-admin-api.cmd`, `build/run-admin-client.cmd`, `build/run-admin-api.cmd`, `build/build-and-run-admin.cmd`, `build/pack-admin-client.cmd`, `src/Config.Admin.WebClient/Config.Admin.WebClient.nuspec` (id `pote.Config.Admin.Client`, files from `build/` output)
- Modify: `azure-pipelines.yml` (Node job: `npm ci`, `npm run build`, `npm test`), `README.md` (Admin Client section), `CLAUDE.md` (build commands)
- Delete: `src/Config.Admin.WebClient.Blazor/`, `src/Config.Admin.WebClient.Tests/`

Scripts follow Spool style (`@echo off`, `pushd "%~dp0.."`, npm install if missing, errorlevel checks).

- [ ] Steps: scripts; nuspec packs `build/**` → package root; pipeline job; docs; **manual pre-merge checklist from spec §7 (run vs real API side-by-side, IIS deep-link test) — run it before deletion**; delete Blazor folders; full `npm run build && npm test && npx playwright test` green; commit "Replace Blazor admin client with Svelte client".

---

## Self-review

- Spec coverage: §1→Tasks 1,2,12; §2→Tasks 5,6,8,10; §3→Tasks 3,6,8,9,10; §4→Tasks 2,4,7; §5 (login seams)→Tasks 1,2 (client.ts single credential point, boot auth-gate no-op comment, layout slot); §6→Tasks 2,5; §7→Tasks 3,4,9,11; §8 checklist→Tasks 7,8,9,11,12. No gaps.
- Types consistent across tasks (Header/Section/NamedRef, ApiResult, EntityDescriptor, ParsedRef).
- Porting references are exact file paths into the renamed Blazor folder; no TBDs.
