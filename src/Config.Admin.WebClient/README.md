# Config.Admin.WebClient

Svelte (SvelteKit) admin frontend for ConfigurationService. Builds to plain static files (`adapter-static`, SPA mode) — no server runtime.

## Develop

```bash
npm install
npm run dev        # http://localhost:5071 (strict port)
```

The app reads `static/config.json` at boot: set `adminApiUrl` (default `http://localhost:34246`) and a valid `apiKey` for your local Admin API. The committed file holds a placeholder key.

## Test

```bash
npm test           # Vitest unit tests
npm run test:e2e   # Playwright smoke tests (mocked Admin API)
npm run check      # svelte-check
```

## Build & deploy

```bash
npm run build      # static site in build/
```

Deploy `build/` to any static server at the site root. `web.config` (IIS SPA fallback + cache rules) ships in the output; other servers need an equivalent rewrite of unknown paths to `index.html`. Deployment overwrites `config.json` per environment. Repo-root `build/` has convenience scripts (`build-admin-client.cmd`, `run-admin-client.cmd`, `build-and-run-admin.cmd`, `pack-admin-client.cmd`).

## API types

TypeScript DTO types are generated from the Admin API's OpenAPI spec and committed:

```bash
npm run gen:api    # requires the Admin API running on localhost:34246
```

Regenerate whenever the Admin API's models change.

## Notes

- Vite config keeps Svelte component libraries and CodeMirror packages out of `optimizeDeps` — removing those exclusions breaks reactivity/editor in dev (duplicate runtime instances).
- bits-ui tooltips require the `Tooltip.Provider` that lives in `AppShell.svelte`.
