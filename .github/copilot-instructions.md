# KeeKee Viewer - Copilot Instructions

## Repository Overview

**KeeKee Viewer** is a .NET 9.0 open-source 3D virtual world viewer, built upon the legacy LookingGlass Viewer (2010).
The project is designed to modularize both the communication protocols and rendering backends, allowing for easy extension and customization.  
It primarily targets OpenSimulator compatible virtual worlds using the LibreMetaverse library for communication.
**Primary Language**: C# (net9.0)  
**Type**: Multi-module .NET console/library application  
**Key Dependencies**: LibreMetaverse (NuGet: `*`), Microsoft.Extensions.*, NLog, OpenTK (graphics), SkiaSharp (2D graphics)

---

## Build & Development Setup

### Prerequisites
- **Required**: .NET 9.0 SDK (version 9.0.112 or later)
- **OS**: Linux, Windows, macOS (tested on Linux)
- All dependencies auto-resolve via NuGet restore

### Build Commands
```bash
# Clean previous builds (resolves most build issues)
dotnet clean

# Restore dependencies (always run this first, even if it says "up-to-date")
dotnet restore

# Full solution build (Debug config, default)
dotnet build

# Build with specific configuration
dotnet build --configuration Release

# Build specific project
dotnet build src/KeeKee.Config/KeeKee.Config.csproj

# Build and ignore errors for project inspection
dotnet build --no-restore --continue
```

**Important Notes**:
- Always run `dotnet restore` first - the solution has many interdependent projects

---

## Project Architecture & Layout


### Configuration Files
- **appsettings.json**: Main app configuration (REST port 9144, world settings, renderer config, asset paths)
- **appsettings.Development.json**: Logging overrides for development
- **appsettings.Debug.json**: Debug-specific settings
- **.editorconfig**: Code style enforcement (100 char line limit for C#, 4-space indents, LF endings, Nullable enabled)
- **.gitignore**: Excludes bin/, obj/, standard dotnet outputs
- **keekee-viewer.sln**: Solution file (Visual Studio compatible)

### Key Files & Patterns
- **Main Entry**: [src/KeeKee/Main.cs](../src/KeeKee/Main.cs) - Contains KeeKeeMain class with static service container
- **Logging**: Custom `KLogger<T>` wrapper around Microsoft.Extensions.Logging + NLog
- **Configuration**: All modules use `IOptions<TConfig>` dependency injection pattern
- **Extensibility**: IProvider/IModule interfaces, factory patterns, ServiceCollection DI

---

## Code Conventions & Style

### C# Style Guidelines (enforced by .editorconfig)
- **Line Length**: Max 100 characters
- **Indentation**: 4 spaces, never tabs
- **Line Endings**: LF (Unix style)
- **Nullable**: C# nullable reference types enabled (`<Nullable>enable</Nullable>`)
- **ImplicitUsings**: Enabled in all projects
- **Brace Style**: Opening brace on same line (`csharp_new_line_before_open_brace = none`)
- **Async Patterns**: Use `async`/`await` with proper cancellation tokens

### Licensing
All source files must include Mozilla Public License 2.0 header (checked in build):
```csharp
// Copyright 2025 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
```

### Naming & Patterns
- **Interfaces**: `I*` prefix (e.g., `IRenderProvider`, `IModule`)
- **Abstract Base Classes**: Often use `*Base` suffix (e.g., `KeeKeeBase`)
- **Method Parameters**: parameter variables prefixed with "p"
- **Private Fields**: `m_fieldName` convention
- **Constants**: `CONST_NAME` or class constants
- **Namespaces**: Match folder structure (`KeeKee.Module.SubModule`)

### Architectural Patterns
- **Dependency Injection**: All major components via IServiceCollection
- **Factory Pattern**: RestHandlerFactory, InstanceFactory for object creation
- **Component Architecture**: World uses entity/component pattern (IEntity with components)
- **Provider Pattern**: Abstract renderer/comm via *Provider interfaces

---

## Testing & Validation

### Current Status
- **No automated tests yet** - none exist in the repository
- All validation is manual or through configuration validation

### Manual Validation Steps
1. **Configuration validation**: All config files (appsettings*.json) load without JSON parse errors
2. **Individual project builds**: Each project in src/ should build cleanly
3. **Dependency checks**: Run `dotnet restore --no-cache` to ensure clean restore

### To Test Build After Changes
```bash
# Clean, restore, build specific project
dotnet clean && dotnet restore && dotnet build src/YourProject/YourProject.csproj --no-restore

# Or full solution (note: may show KeeKee.View errors)
dotnet build --configuration Debug 2>&1 | grep -E "error|Build succeeded"
```

---

## Known Limitations & Workarounds

### Build Constraints
- KeeKee.View project has **deliberate design issues** (incomplete, not blocking other work)
- LibreMetaverse uses wildcard NuGet version (`Version="*"`) - uses latest
- Some projects reference future/unstable dependencies

### Development Notes
- Async work queues available: `BasicWorkQueue`, `OnDemandWorkQueue` in Framework
- World module handles rendering coordinate system conversion
- Asset fetching and texture loading from LibreGrid infrastructure
- Custom logging with context via `KLogger<T>` instances

---

## Trust the Instructions

When working on this codebase, **trust these instructions**. If you encounter issues:
1. First check if the problem matches a known limitation listed above
2. Attempt the documented build/restore process
3. Only search the codebase if the instructions are unclear or appear incomplete
4. Report any inconsistencies found in these instructions

**Essential workflow**:
- Always: `dotnet clean && dotnet restore && dotnet build`
- For changed config: Validate JSON syntax
- For changed code: Build your target project first, then full solution
- Check .editorconfig compliance before committing
