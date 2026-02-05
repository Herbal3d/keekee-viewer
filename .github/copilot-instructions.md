# KeeKee Viewer - Copilot Instructions

## Repository Overview

**KeeKee Viewer** is a .NET 9.0 open-source 3D virtual world viewer built on LibreMetaverse. Originally based on the LookingGlass Viewer (2010), it's designed for modularity with pluggable communication protocols and rendering backends. Targets OpenSimulator-compatible virtual worlds.

**Stack**: C# (net9.0), LibreMetaverse (wildcard `*` version), Microsoft.Extensions.* (DI/config), NLog (logging), OpenTK (graphics), SkiaSharp (2D)

---

## Architecture: Provider Pattern & DI

### Core Design Philosophy
All major subsystems use **provider interfaces** resolved via dependency injection:
- `ICommProvider`: Communication layer (currently `CommLLLP` for Linden Lab Legacy Protocol)
- `IRenderProvider`: Rendering backend (switch between `RendererOGL`, `RendererMap` via config)
- `IWorld`: Entity management and world state
- `IUserInterfaceProvider`: UI abstraction layer

**Key Entry Point**: [src/KeeKee/Main.cs](src/KeeKee/Main.cs#L29) - `KeeKeeMain` configures the entire DI container using `Host.CreateDefaultBuilder()`. All services registered in `ConfigureServices()` method.

### Provider Selection Pattern
```csharp
// From Main.cs - runtime provider selection from config
services.AddSingleton<IRenderProvider>(sp => {
    var provider = context.Configuration.GetValue<string>("Renderer:RenderProvider") ?? "OGL";
    return provider.ToLowerInvariant() switch {
        "ogl" => sp.GetRequiredService<RendererOGL>(),
        "map" => sp.GetRequiredService<RendererMap>(),
        _ => throw new ApplicationException($"Unknown RenderProvider '{provider}'.")
    };
});
```

### Entity-Component System
World uses entity/component pattern (not ECS framework, custom implementation):
- `IEntity`: Base entity interface (avatars, prims, terrain)
- `IEntityComponent`: Components attachable to entities
- `IEntityCollection`: Manages entity lifecycle and queries
- `IRegionContext`: Per-region state container
- **LL Implementations**: `LLEntity`, `LLRegionContext`, `LLAssetContext` in [src/KeeKee.World.LL/](src/KeeKee.World.LL/)

---

## Dependency Injection Patterns

### Configuration: IOptions<T> Pattern
**All modules** use `IOptions<TConfig>` for configuration access:
```csharp
public class MyService {
    private readonly IOptions<MyServiceConfig> m_config;
    
    public MyService(IOptions<MyServiceConfig> pConfig) {
        m_config = pConfig;
        var setting = m_config.Value.SomeSetting;
    }
}
```

Config classes in [src/KeeKee.Config/](src/KeeKee.Config/): `KeeKeeConfig`, `CommConfig`, `RendererConfig`, `WorldConfig`, etc. All bind to sections in `appsettings.json`.

### Factory Pattern: IInstanceFactory
Use `IInstanceFactory.Create<T>()` to instantiate DI-registered types with additional parameters:
```csharp
// From InstanceFactory.cs
public interface IInstanceFactory {
    T Create<T>(params object[] parameters) where T : class;
}
// Usage: _factory.Create<MyClass>(extraParam1, extraParam2)
```

### BackgroundService Pattern
Long-running services extend `BackgroundService`:
- `RestManager`: HTTP server (port 9144 by default)
- `CommLLLP`: Communication handler
- `WorkQueueManager`: Manages all work queues

---

## Logging System: KLogger<T>

Custom logging wrapper in [src/KeeKee.Framework/Logging/KLogger.cs](src/KeeKee.Framework/Logging/KLogger.cs):

```csharp
// Standard usage pattern
private readonly KLogger<MyClass> m_log;

public MyClass(KLogger<MyClass> pLog) {
    m_log = pLog;
}

// Custom KeeKee log levels (configured per-feature in appsettings.json)
m_log.Log(KLogLevel.DCOMM, "Communication detail: {0}", data);
m_log.Log(KLogLevel.DWORLDDETAIL, "World update detail");
```

**Feature flags** in `appsettings.json` → `KLogger` section:
- `DCOMM`, `DCOMMDETAIL`: Communication layer
- `DWORLD`, `DWORLDDETAIL`: World/entity updates
- `DRENDER`, `DRENDERDETAIL`: Rendering
- `RestDetail`, `WorkQueueDetail`, `UIDetail`

Backed by NLog (configuration in `appsettings.json` → `NLog` section). Logs to `logs/log-{date}.log`.

---

## Module Organization (15 projects)

**Core Projects**:
- `KeeKee`: Main entry point, DI orchestration
- `KeeKee.Config`: All configuration classes
- `KeeKee.Framework`: Utilities (logging, work queues, factories, hashing, statistics)
- `KeeKee.Rest`: HTTP REST server for UI and API

**Communication** (protocol pluggable):
- `KeeKee.Comm`: Interface definitions (`ICommProvider`, `IChatProvider`)
- `KeeKee.Comm.LLLP`: Linden Lab Legacy Protocol implementation (LibreMetaverse client)

**World/Entity** (virtual world state):
- `KeeKee.World`: Core interfaces (`IWorld`, `IEntity`, `IRegionContext`)
- `KeeKee.World.LL`: Linden Lab/OpenSim specific implementations
- `KeeKee.World.Entity`, `KeeKee.World.Cmpt`: Entity and component types
- `KeeKee.World.Services`: Services operating on world state

**Rendering** (backend pluggable):
- `KeeKee.Renderer`: Core rendering interfaces (`IRenderProvider`, `IUserInterfaceProvider`)
- `KeeKee.Renderer.OGL`: OpenGL renderer (currently commented out in solution - incomplete)
- `KeeKee.Renderer.Map`: Map/2D renderer

**UI/View**:
- `KeeKee.View`: View interfaces and implementations

---

## REST API Architecture

**Design**: [src/KeeKee.Rest/RestManager.cs](src/KeeKee.Rest/RestManager.cs) - BackgroundService hosting HTTP listener on port 9144.

**URL Structure**:
- `http://localhost:9144/std/*` → Standard static files (JS libs, CSS)
- `http://localhost:9144/static/*` → Skinnable UI files
- `http://localhost:9144/api/SERVICE/*` → Dynamic REST endpoints

**Handler Pattern**: Services create REST endpoints using `RestHandler` subclasses:
```csharp
// Example: RestHandlerLogin in KeeKee.Comm.LLLP
public class RestHandlerLogin : RestHandler {
    public RestHandlerLogin(RestManager pRestManager, ICommProvider pComm) {
        // Register GET/POST handlers for /api/login/*
    }
}
```

Factory pattern via `RestHandlerFactory` instantiates handlers using DI.

---

## WorkQueue System

[src/KeeKee.Framework/WorkQueue/](src/KeeKee.Framework/WorkQueue/): Asynchronous task queuing.

**Types**:
- `BasicWorkQueue`: Persistent background queue
- `OnDemandWorkQueue`: Create threads on-demand

**Usage Pattern**:
```csharp
private BasicWorkQueue m_workQueue;

public MyService(BasicWorkQueue pQueue) {
    m_workQueue = pQueue;
}

// Queue async work
m_workQueue.DoLater(() => {
    // Background operation
});
```

All queues auto-register with `WorkQueueManager` (accessible via DI).

---

## Build & Development

### Essential Commands
```bash
# ALWAYS start with clean + restore (interdependent projects)
dotnet clean && dotnet restore && dotnet build

# Run the application
dotnet run --project src/KeeKee/KeeKee.csproj

# Build specific project
dotnet build src/KeeKee.Framework/KeeKee.Framework.csproj
```

**Critical**: `dotnet restore` must run first - projects have complex dependencies.

### Configuration Files
- `appsettings.json`: Main config (REST port, render provider, grid settings, asset paths)
- `Grids.json`: Virtual world grid definitions (OpenSim grids, login URIs)
- `appsettings.Development.json`: Dev logging overrides
- `Directory.Build.props`: Shared MSBuild properties (version metadata, git hash embedding)

---

## Code Style & Conventions

### Enforced by .editorconfig
- **Line length**: 100 chars (C#)
- **Indentation**: 4 spaces, LF line endings
- **Braces**: Opening brace on same line (K&R style)
- **Nullable**: Enabled in all projects

### Naming Rules
- **Private fields**: `m_fieldName` (NOT `_fieldName`)
- **Parameters**: `pParameterName` prefix
- **Interfaces**: `I*` prefix (e.g., `IWorld`, `ICommProvider`)
- **Providers**: `*Provider` suffix for abstraction interfaces

### License Header (MANDATORY)
All source files require MPL 2.0 header (see [copyrightInclude.txt](copyrightInclude.txt)):
```csharp
// Copyright 2025 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
```

---

## Testing

**Current State**: No automated test framework. Validation is manual.

**To validate changes**:
1. Build succeeds without errors: `dotnet build`
2. Configuration loads: Check logs for JSON parse errors on startup
3. Manual functional testing via REST API (use Postman/curl against `localhost:9144`)

**Known limitation**: `KeeKee.View` project incomplete (deliberate work-in-progress).

---

## Key Gotchas & Workarounds

1. **LibreMetaverse wildcard versioning**: All `.csproj` files use `<PackageReference Include="LibreMetaverse" Version="*" />` - always pulls latest. May cause breaking changes.

2. **Static service access**: `KeeKeeMain.GetService<T>()` provides static access to DI container. Use sparingly - prefer constructor injection.

3. **Global cancellation**: `KeeKeeMain.GlobalCTS` - application-wide cancellation token source.

4. **Renderer switching**: Change `Renderer:RenderProvider` in `appsettings.json` to `"OGL"` or `"Map"` (OGL currently incomplete).

5. **World coordinate systems**: World module handles LL grid coordinate conversion (region coordinates → global coordinates).

---

## Quick Reference: Finding Things

| Need | Location |
|------|----------|
| DI container setup | [src/KeeKee/Main.cs](src/KeeKee/Main.cs) `ConfigureServices()` |
| Add new config | [src/KeeKee.Config/](src/KeeKee.Config/) + `appsettings.json` |
| Add REST endpoint | Subclass `RestHandler` in appropriate module |
| Custom logging | Inject `KLogger<YourClass>`, use `KLogLevel` enums |
| Create entities | Use `IInstanceFactory` |
| Provider interfaces | Search `I*Provider` in [src/](src/) |
| Work queues | Inject `BasicWorkQueue` or `OnDemandWorkQueue` |

---

## When to Search vs. Trust These Docs

**Trust the docs** for:
- Build process, DI patterns, logging, configuration
- Naming conventions, code style, architecture overview

**Search the codebase** for:
- Specific entity component implementations
- REST endpoint URLs and payloads
- LibreMetaverse API usage patterns
- Renderer-specific implementations
