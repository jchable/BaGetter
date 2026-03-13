# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build BaGetter.sln

# Run the application
dotnet run --project src/BaGetter

# Run all tests
dotnet test

# Run tests for a specific project
dotnet test tests/BaGetter.Core.Tests

# Run a single test class or method
dotnet test tests/BaGetter.Core.Tests --filter "FullyQualifiedName~PackageIndexingServiceTests"

# Publish for deployment
dotnet publish src/BaGetter -c Release -o ./publish
```

## Architecture

BaGetter is an ASP.NET Core NuGet and symbol server using a **provider-based dependency injection** pattern. The runtime selects implementations based on `appsettings.json` configuration.

### Project Layout

- **`src/BaGetter/`** — Web host entry point (`Program.cs`, `Startup.cs`), CLI commands
- **`src/BaGetter.Core/`** — All business logic, service abstractions, entities, EF Core context
- **`src/BaGetter.Web/`** — ASP.NET Core controllers, Razor pages, routing, authentication middleware
- **`src/BaGetter.Protocol/`** — NuGet v3 protocol models; targets `netstandard2.0` for broad compatibility
- **`src/BaGetter.Database.*/`** — EF Core provider implementations (Sqlite, SqlServer, MySql, PostgreSql)
- **`src/BaGetter.Aws/`, `BaGetter.Azure/`, `BaGetter.Gcp/`, `BaGetter.Aliyun/`, `BaGetter.Tencent/`** — Cloud storage/search provider implementations
- **`tests/`** — xUnit test projects mirroring src structure
- **`samples/`** — Protocol SDK samples

### Provider Pattern

The core extensibility mechanism is in [src/BaGetter.Core/Extensions/IProvider.cs](src/BaGetter.Core/Extensions/IProvider.cs). Each provider implementation checks if it is configured and returns a service instance if active:

```csharp
services.GetServiceFromProviders<IStorageService>()
```

Providers are selected at startup via configuration keys:
- `Database:Type` → `Sqlite` | `SqlServer` | `MySql` | `PostgreSql`
- `Storage:Type` → `FileSystem` | `AwsS3` | `AzureBlob` | `GoogleCloud` | `AliyunOss` | `TencentCos`
- `Search:Type` → `Database` (default; Azure Cognitive Search via `BaGetter.Azure`)

### Request Flow

```
HTTP Request
  → OperationCancelledMiddleware
  → Static files / Routing
  → CORS → Authentication → Authorization
  → Controllers (BaGetter.Web) / Razor Pages
  → Services (BaGetter.Core): PackageService, PackageIndexingService, etc.
  → IPackageDatabase / IStorageService (provider-selected implementation)
```

### Key Service Abstractions (in `BaGetter.Core`)

| Abstraction | Purpose |
|---|---|
| `IPackageDatabase` | CRUD for package metadata |
| `IStorageService` | Binary package/symbol file storage |
| `ISearchService` | Package search queries |
| `ISearchIndexer` | Indexes packages into search backend |
| `IPackageIndexingService` | Orchestrates full package upload flow |
| `IPackageContentService` | Serves `.nupkg`, `.nuspec`, readme files |
| `ISymbolIndexingService` | Processes `.snupkg` symbol packages |

### Authentication

Two schemes are supported, configured via `appsettings.json`:
- **API key** via `X-NuGet-ApiKey` header — for package publishing
- **Basic HTTP auth** — optional username/password credentials list

Read-only endpoints (search, download) have a separate CORS policy allowing public access.

### CLI Commands

The main binary doubles as a CLI tool (via `McMaster.Extensions.CommandLineUtils`):

```bash
dotnet BaGetter.dll import downloads   # Import download statistics
dotnet BaGetter.dll deprecate          # Deprecate a package
```

## Configuration

Key `appsettings.json` settings:

```json
{
  "Database": { "Type": "Sqlite", "ConnectionString": "Data Source=bagetter.db" },
  "Storage": { "Type": "FileSystem", "Path": "" },
  "Search": { "Type": "Database" },
  "Mirror": { "Enabled": false, "PackageSource": "https://api.nuget.org/v3/index.json" },
  "ApiKey": "",
  "AllowPackageOverwrites": false,
  "PackageDeletionBehavior": "Unlist",
  "MaxPackageSizeGiB": 8
}
```

## Code Style

Enforced via [.editorconfig](.editorconfig):
- 4-space indentation in C#; 2-space in XML/JSON/YAML
- File-scoped namespaces required
- `var` preferred for built-in and apparent types
- Expression-bodied members preferred for simple accessors
- Accessibility modifiers always required
- System `using` directives sorted first
