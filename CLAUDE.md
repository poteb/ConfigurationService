# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ConfigurationService is a centralized configuration management system for .NET applications. It resolves JSON configurations with a `$ref` reference syntax, supports per-application and per-environment overrides, and provides a Blazor WebAssembly admin UI.

## Build Commands

```bash
# Build entire solution
dotnet build src/Config.sln

# Build a specific project
dotnet build src/Config.Api/Config.Api.csproj

# Run tests
dotnet test src/Config.UnitTests/Config.UnitTests.csproj

# Run a single test by name
dotnet test src/Config.UnitTests/Config.UnitTests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run the Config API (port 5146)
dotnet run --project src/Config.Api/Config.Api.csproj

# Run the Admin API (port 34246)
dotnet run --project src/Config.Admin.Api/Config.Admin.Api.csproj

# Run the Admin WebClient dev server (port 5071)
cd src/Config.Admin.WebClient && npm run dev

# Admin WebClient: build, unit tests, e2e, type check, regen API types
cd src/Config.Admin.WebClient && npm run build
cd src/Config.Admin.WebClient && npm test
cd src/Config.Admin.WebClient && npm run test:e2e
cd src/Config.Admin.WebClient && npm run check
cd src/Config.Admin.WebClient && npm run gen:api   # requires Admin API running
```

## Architecture

### Three deployable components
- **Config.Api** — Public-facing API that middleware clients call to resolve configurations
- **Config.Admin.Api** — Backend API for managing configurations (CRUD)
- **Config.Admin.WebClient** — Svelte (SvelteKit static SPA, Tailwind + shadcn-svelte, CodeMirror 6) frontend, talks to Admin API. TS types generated from the Admin API OpenAPI spec (`npm run gen:api`, committed). Runtime config in `static/config.json`.

### Core libraries
- **Config.Parser** — JSON parsing engine that resolves `$ref:ConfigName#PropertyPath` references recursively
- **Config.Shared** — Shared models/DTOs (targets netstandard2.0)
- **Config.DbModel** — Entity models for configurations, applications, environments, secrets
- **Config.Encryption** — Encryption/decryption for sensitive configuration values
- **Config.Auth** — API key authentication (`X-API-Key` header)

### Data provider abstraction
- **Config.DataProvider.Interfaces** — `IDataProvider`, `IAdminDataProvider`, `IApplicationDataAccess`, `IEnvironmentDataAccess`, `ISecretDataAccess`, `IUserDataAccess`
- **Config.DataProvider.SqlServer** — SQL Server provider (primary and default; Dapper + numbered `CreateScripts/`, deployed manually in order)
- **Config.DataProvider.File** — File-based storage (legacy; no user storage — admin login requires SqlServer)

Both APIs must point to the same database (`SqlServer:ConnectionString`) and use the same `EncryptionSettings.JsonEncryptionKey`.

### Admin authentication
- Admin API + WebClient use DB-backed session login (see `docs/plans/2026-08-04-admin-login-design.md` and ADRs 0002–0005); Config.Api keeps `X-API-Key` for middleware clients.
- Auth code lives in `src/Config.Admin.Api/Auth/` behind the pluggable `IAuthProviderSetup` seam. Claims contract: `name`, `role` (`Admin`|`User`), `guest`.
- Bootstrap: empty `Users` table seeds `guest`/`guest`, which can only create the first admin and is hard-deleted on the first real login.

### Middleware (client-side NuGet packages)
Located in `src/Config.Middleware/`:
- **Config.Middleware.Net** — Multi-framework (net6.0–net10.0) extension on `IConfigurationBuilder`
- **Config.Middleware.NetStandard** — NetStandard 2.0 variant
- **Config.Middleware.Web.Net** — Web-specific middleware
- **Config.Middleware.Secrets** — Source generator for `[Secret]` attribute (lazy-resolved secrets)

### Key domain concepts
- **$ref syntax**: `$ref:ConfigName#PropertyPath` resolves references; empty path after `#` takes the entire config
- **"Base" convention**: A property named `base`/`Base` causes its resolved value to replace the parent object entirely
- **Application/Environment scoping**: Configurations can be scoped to specific apps and environments for per-context overrides
- **Glossary**: `CONTEXT.md` at the repo root is the ubiquitous language; ADRs live in `docs/adr/`

## Testing

- Framework: **NUnit** with **NSubstitute** for mocking
- Test project: `src/Config.UnitTests/`
- Coverage collector: coverlet

## Code Conventions

- Root namespace and assembly name: `pote.[ProjectName]` (e.g., `pote.Config.Api`)
- Nullable reference types: enabled
- Implicit usings: enabled
- Target framework: net10.0 (middleware targets multiple frameworks)
- Logging: Serilog (file + console sinks)

## CI/CD

Azure Pipelines (`azure-pipelines.yml`) triggers on main, builds the middleware solution with VSBuild, and runs VSTest.
