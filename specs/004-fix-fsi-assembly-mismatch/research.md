# Research: Fix FSI Assembly Version Mismatch

**Date**: 2026-03-27
**Feature**: 004-fix-fsi-assembly-mismatch

## Root Cause Analysis

### The Dependency Chain

```
PhysicsClient.fsproj (net10.0)
├── ProjectReference: PhysicsSandbox.ServiceDefaults.csproj
│   ├── FrameworkReference: Microsoft.AspNetCore.App
│   └── PackageReference: Microsoft.Extensions.Http.Resilience 10.1.0
│       └── Transitive: Microsoft.Extensions.Logging.Abstractions (min 8.0.0)
├── ProjectReference: PhysicsSandbox.Shared.Contracts.csproj
├── PackageReference: Grpc.Net.Client 2.*
├── PackageReference: Google.Protobuf 3.*
└── PackageReference: Spectre.Console 0.49.*
```

### Why It Fails in FSI

1. `dotnet pack src/PhysicsClient` produces a NuGet package. ProjectReferences (ServiceDefaults, Contracts) become NuGet dependencies. The minimum version of Microsoft.Extensions.Logging.Abstractions in the resolved graph is **8.0.0**.
2. FSI loads PhysicsClient.dll, which was compiled against assembly version 8.0.0.0 of Microsoft.Extensions.Logging.Abstractions.
3. The .NET 10 SDK runtime only has version 10.0.0.0 of that assembly. FSI doesn't apply binding redirects or roll-forward like compiled apps do.
4. Result: `FileNotFoundException` for `Microsoft.Extensions.Logging.Abstractions, Version=8.0.0.0`.

### Why Compiled Apps Work

Compiled apps get a `.runtimeconfig.json` that enables assembly version roll-forward. FSI scripts don't get this — they run in a more rigid assembly loading environment.

## Fix Options Evaluated

### Option A: Add explicit PackageReference in PhysicsClient.fsproj

- **Decision**: SELECTED
- **Approach**: Add `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.*" />` to PhysicsClient.fsproj
- **Rationale**: Forces the NuGet dependency recorded in the .nupkg to require >= 10.x, which aligns with the .NET 10 runtime's assembly version. FSI then resolves 10.x and the CLR finds a matching assembly.
- **Risk**: Low — the 10.x API is backward-compatible with 8.x usage. PhysicsClient uses only basic logging abstractions (ILogger, ILoggerFactory).

### Option B: Pin version in Prelude.fsx

- **Decision**: REJECTED as primary fix (but currently used as workaround)
- **Approach**: `#r "nuget: Microsoft.Extensions.Logging.Abstractions, 10.0.0"` in Prelude.fsx
- **Rationale rejected**: This is a workaround, not a fix. Any new script that references PhysicsClient without also pinning this dependency would still fail. The Prelude.fsx already has this unpinned line, suggesting someone tried this approach but didn't pin the version.

### Option C: Remove ServiceDefaults ProjectReference from PhysicsClient

- **Decision**: REJECTED
- **Approach**: Stop referencing ServiceDefaults from PhysicsClient to eliminate the transitive dependency
- **Rationale rejected**: PhysicsClient uses ServiceDefaults for OpenTelemetry and health check configuration. Removing it would break observability.

### Option D: Use AssemblyLoadContext in FSI scripts

- **Decision**: REJECTED
- **Approach**: Add custom assembly resolution logic in Prelude.fsx
- **Rationale rejected**: Fragile, complex, and would need to be maintained in every script entry point.

## Secondary Change: Prelude.fsx Cleanup

Once Option A is applied and the PhysicsClient NuGet package correctly declares its dependency on Microsoft.Extensions.Logging.Abstractions 10.x:

- **Remove** `#r "nuget: Microsoft.Extensions.Logging.Abstractions"` from Prelude.fsx (line 5)
- PhysicsClient's NuGet dependency will pull in the correct version transitively
- This satisfies FR-003 (no manual pinning of transitive dependencies)

## Version Bump Decision

- **Decision**: Bump PhysicsClient from 0.4.0 to 0.5.0
- **Rationale**: The dependency change is a meaningful fix. A new minor version ensures stale NuGet caches don't serve the broken 0.4.0 package. All Prelude.fsx and script references must be updated to pin 0.5.0.

## Affected Files

| File | Change |
|------|--------|
| `src/PhysicsClient/PhysicsClient.fsproj` | Add explicit PackageReference for Microsoft.Extensions.Logging.Abstractions 10.* |
| `src/PhysicsClient/PhysicsClient.fsproj` | Bump Version from 0.4.0 to 0.5.0 |
| `src/PhysicsSandbox.Shared.Contracts/PhysicsSandbox.Shared.Contracts.csproj` | Bump Version to 0.5.0 (PhysicsClient depends on it) |
| `src/PhysicsSandbox.ServiceDefaults/PhysicsSandbox.ServiceDefaults.csproj` | Bump Version to 0.2.0 (PhysicsClient depends on it) |
| `Scripting/demos/Prelude.fsx` | Remove `#r "nuget: Microsoft.Extensions.Logging.Abstractions"`, update PhysicsClient to 0.5.0 |
| `Scripting/scripts/PhysicsClient.fsx` | Update PhysicsClient version reference if present |
| `Containerfile` | No changes needed (pack commands are version-agnostic) |
| `CLAUDE.md` | Update PhysicsClient NuGet version references from 0.4.0 to 0.5.0 |
