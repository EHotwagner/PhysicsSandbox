# Data Model: Scripting Library NuGet Package

**Feature**: 004-scripting-nuget-package | **Date**: 2026-03-22

## Entities

This feature operates on NuGet package metadata and project file configuration rather than domain data. The "data model" is the dependency graph and packaging metadata.

### NuGet Package Metadata

Each packaged project requires these properties in its .csproj/.fsproj:

| Property | Contracts | ServiceDefaults | PhysicsClient | Scripting |
|----------|-----------|-----------------|---------------|-----------|
| IsPackable | `true` (add) | `true` (add) | `true` (exists) | `true` (exists) |
| PackageId | `PhysicsSandbox.Shared.Contracts` (add) | `PhysicsSandbox.ServiceDefaults` (add) | `PhysicsClient` (exists) | `PhysicsSandbox.Scripting` (exists) |
| Version | `0.1.0` (add) | `0.1.0` (add) | `0.1.0` (exists) | `0.1.0` (exists) |

### Dependency Graph (Package References)

```text
PhysicsSandbox.Shared.Contracts (0.1.0)
  └── External: Grpc.AspNetCore 2.*, Google.Protobuf 3.*

PhysicsSandbox.ServiceDefaults (0.1.0)
  └── External: Microsoft.Extensions.Http.Resilience 10.1.0,
                Microsoft.Extensions.ServiceDiscovery 10.1.0,
                OpenTelemetry.* 1.14.0

PhysicsClient (0.1.0)
  ├── PackageRef: PhysicsSandbox.Shared.Contracts 0.1.0
  ├── PackageRef: PhysicsSandbox.ServiceDefaults 0.1.0
  └── External: Grpc.Net.Client 2.*, Google.Protobuf 3.*, Spectre.Console 0.49.*

PhysicsSandbox.Scripting (0.1.0)
  └── PackageRef: PhysicsClient 0.1.0
      (transitively includes Contracts, ServiceDefaults, and all externals)
```

### Reference Migration Map

| Consumer Project | Current Reference | Target Reference |
|-----------------|-------------------|------------------|
| PhysicsClient.fsproj | ProjectRef → Contracts | PackageRef → Contracts 0.1.0 |
| PhysicsClient.fsproj | ProjectRef → ServiceDefaults | PackageRef → ServiceDefaults 0.1.0 |
| Scripting.fsproj | ProjectRef → PhysicsClient | PackageRef → PhysicsClient 0.1.0 |
| Mcp.fsproj | ProjectRef → Scripting | PackageRef → Scripting 0.1.0 |
| Scripting.Tests.fsproj | ProjectRef → Scripting | PackageRef → Scripting 0.1.0 |

### Script Reference Migration Map

| Script File | Current `#r` | Target `#r` |
|------------|-------------|-------------|
| Scripting/scripts/Prelude.fsx | `#r "../../src/.../PhysicsSandbox.Scripting.dll"` | `#r "nuget: PhysicsSandbox.Scripting"` |
| Scripting/demos/Prelude.fsx | `#r "../../src/.../PhysicsClient.dll"` + Contracts + ServiceDefaults + 4 nuget refs | `#r "nuget: PhysicsClient"` (transitives auto-resolve) |
| Scripting/demos/AutoRun.fsx | Same as demos/Prelude.fsx | Same pattern |

### Port Migration Map

| Context | Current | Target |
|---------|---------|--------|
| F# demos default server | `http://localhost:5000` | `http://localhost:5180` |
| Python demos default server | `http://localhost:5000` | `http://localhost:5180` |
| PhysicsClient fallback | `http://localhost:5000` | `http://localhost:5180` |
| PhysicsViewer fallback | `http://localhost:5000` | `http://localhost:5180` |
| .mcp.json SSE endpoint | `http://localhost:5000/sse` | `http://localhost:5180/sse` |

## State Transitions

### Package Lifecycle

```
Source → Build → Pack → Publish → Available in local feed → Consumed by PackageReference or #r "nuget: ..."
```

Version must increment on each publish to avoid NuGet cache staleness.

## Validation Rules

- No hardcoded DLL paths (`#r ".../.dll"`) to packaged projects may remain in any script
- No `localhost:5000` references may remain in scripts or documentation
- All 4 packages must be restorable from the local NuGet feed
- Transitive dependency resolution must work (adding Scripting should pull in PhysicsClient → Contracts/ServiceDefaults)
