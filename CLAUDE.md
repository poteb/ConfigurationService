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

# Run the Admin WebClient (port 5071)
dotnet run --project src/Config.Admin.WebClient/Config.Admin.WebClient/Config.Admin.WebClient.csproj
```

## Architecture

### Three deployable components
- **Config.Api** — Public-facing API that middleware clients call to resolve configurations
- **Config.Admin.Api** — Backend API for managing configurations (CRUD)
- **Config.Admin.WebClient** — Blazor WASM frontend (MudBlazor UI), talks to Admin API

### Core libraries
- **Config.Parser** — JSON parsing engine that resolves `$ref:ConfigName#PropertyPath` references recursively
- **Config.Shared** — Shared models/DTOs (targets netstandard2.0)
- **Config.DbModel** — Entity models for configurations, applications, environments, secrets
- **Config.Encryption** — Encryption/decryption for sensitive configuration values
- **Config.Auth** — API key authentication (`X-API-Key` header)

### Data provider abstraction
- **Config.DataProvider.Interfaces** — `IDataProvider`, `IAdminDataProvider`, `IApplicationDataAccess`, `IEnvironmentDataAccess`, `ISecretDataAccess`
- **Config.DataProvider.File** — File-based storage implementation (production-ready)
- **Config.DataProvider.SqlServer** — SQL Server provider (incomplete)

Both APIs must point to the same `FileDatabase.Directory` and use the same `EncryptionSettings.JsonEncryptionKey`.

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
