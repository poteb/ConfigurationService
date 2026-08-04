# 0001 — Rewrite the admin frontend in Svelte, replacing Blazor WASM

**Status:** Accepted (2026-08-04)

## Context

The admin client was a Blazor WebAssembly app (MudBlazor, net10.0) — the only frontend in an otherwise .NET-only repository. Pain points: WASM payload size and startup time, poor debugging experience, and friction extending the UI. The custom JSON editor lived in a separate repo (pote.BlazorJsonEditor) as a Blazor wrapper around ~400 lines of vanilla JS.

## Decision

Replace the Blazor client wholesale with a SvelteKit static SPA (Svelte 5 + TypeScript, Tailwind + shadcn-svelte, CodeMirror 6), in one PR, at the same path (`src/Config.Admin.WebClient/`). The Admin API is untouched; the client remains a set of static files calling it with an `X-API-Key`. TypeScript DTO types are generated from the Admin API's OpenAPI spec instead of shared C# project references.

Alternatives considered:
- **Stay on Blazor, modernize incrementally** — keeps one language, but keeps the payload/startup/debugging costs that motivated the change.
- **Incremental page-by-page migration** — rejected: two frontends sharing auth/state for months of ceremony on a ~4,700-line app.
- **React/other frameworks** — Svelte chosen by developer preference and small-bundle fit for an internal tool.

## Consequences

- The repo becomes polyglot: Node toolchain (npm, Vite, Vitest, Playwright) alongside .NET. CI needs a Node job.
- Type safety across the API boundary now depends on regenerating types from swagger (`npm run gen:api`) instead of compile-time project references — drift is possible between regenerations.
- The C# DTO/type sharing with `Config.Admin.Api.Model` ends; that project serves only the API.
- pote.BlazorJsonEditor is no longer used by this repo; the editor is CodeMirror 6 in-repo.
- The client keeps shipping as NuGet package `pote.Config.Admin.Client` (now containing the static build output) because the TeamCity/Octopus pipeline may consume it; dropping it requires verifying the pipeline first.
