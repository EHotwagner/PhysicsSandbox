# Research: Scripting Library NuGet Package

**Feature**: 004-scripting-nuget-package | **Date**: 2026-03-22

## R1: Local NuGet Packaging Pattern (BepuFSharp Reference)

**Decision**: Follow the established BepuFSharp pattern — `dotnet pack` → copy .nupkg to `~/.local/share/nuget-local/`

**Rationale**: BepuFSharp already uses this pattern successfully. The local NuGet feed is configured in both project-level `NuGet.config` and global `~/.nuget/NuGet/NuGet.Config`. All projects in the solution can resolve packages from this feed.

**Pack command**: `dotnet pack -c Release -p:NoWarn=NU5104 -o ~/.local/share/nuget-local/`

**Alternatives considered**:
- `dotnet nuget push` to feed: Unnecessary complexity for a local folder-based feed; direct output to the folder is simpler
- NuGet.Server hosting: Overkill for a single-developer local environment

## R2: ProjectReference vs PackageReference in Packaged Projects

**Decision**: Convert ProjectReferences to PackageReferences within the packaging chain (PhysicsClient→Contracts/ServiceDefaults, Scripting→PhysicsClient). Leave service projects (PhysicsServer, PhysicsSimulation, PhysicsViewer) unchanged.

**Rationale**: When `dotnet pack` encounters a ProjectReference, it does create a NuGet dependency automatically. However, explicitly using PackageReferences makes the dependency chain explicit and ensures the package works identically whether consumed from the solution or from the NuGet feed. It also avoids the ambiguity of having the same dependency available as both a ProjectReference (in-solution) and a PackageReference (from feed).

**Trade-off**: After migration, building PhysicsClient locally requires Contracts and ServiceDefaults packages in the local feed first. This matches the BepuFSharp pattern where you pack before you can build consumers.

**Alternatives considered**:
- Keep ProjectReferences and rely on `dotnet pack` auto-conversion: Works but makes dependency chain implicit; consumers may get different behavior than the developer
- Conditional references (PackageRef for packing, ProjectRef for development): Too complex for a local-only workflow

## R3: Projects Missing Packaging Metadata

**Decision**: Add explicit `IsPackable`, `Version`, and `PackageId` to Contracts and ServiceDefaults .csproj files.

**Findings**:
| Project | IsPackable | Version | PackageId |
|---------|-----------|---------|-----------|
| Shared.Contracts | NOT SET | NOT SET | NOT SET |
| ServiceDefaults | NOT SET | NOT SET | NOT SET |
| PhysicsClient | `true` | `0.1.0` | `PhysicsClient` |
| Scripting | `true` | `0.1.0` | `PhysicsSandbox.Scripting` |

**Action**: Set all 4 to `IsPackable=true`, `Version=0.1.0`, with explicit PackageIds matching their assembly names.

## R4: ServiceDefaults IsAspireSharedProject Flag

**Decision**: Set `IsAspireSharedProject` to `false` (or remove it) when making ServiceDefaults packable.

**Rationale**: The `IsAspireSharedProject=true` flag tells Aspire tooling to treat this as a shared project that gets source-included rather than referenced as a binary. For NuGet packaging, we need it to behave as a standard library. Service projects that still use ProjectReference will continue to get the same behavior. Projects consuming it via NuGet PackageReference will get the compiled binary, which is the correct behavior for packaged libraries.

**Alternatives considered**:
- Keep the flag and hope it doesn't interfere with packing: Risky; the flag may suppress library output behavior

## R5: F# Script NuGet Resolution

**Decision**: Use `#r "nuget: PackageName"` (without version) in all F# scripts and demos.

**Rationale**: F# Interactive supports `#r "nuget: ..."` for NuGet package references. Without a version specifier, it resolves the latest available version from configured feeds. The demos already use this pattern for external packages (Grpc.Net.Client, Google.Protobuf, Spectre.Console). Applying it to internal packages is consistent.

**Findings**: Current scripts use a mix of:
- `#r "../../src/PhysicsSandbox.Scripting/bin/Debug/net10.0/PhysicsSandbox.Scripting.dll"` (scripts/Prelude.fsx)
- `#r "../../src/PhysicsClient/bin/Debug/net10.0/PhysicsClient.dll"` (demos/Prelude.fsx)
- `#r "nuget: Grpc.Net.Client"` (demos — already version-agnostic)

**Target state**: All `#r` directives for internal packages become `#r "nuget: PackageName"` without versions.

## R6: PhysicsClient OutputType

**Decision**: PhysicsClient has `OutputType=Exe` and `IsPackable=true`. This is valid — `dotnet pack` produces a NuGet package containing the library DLL regardless of OutputType. The EXE output is for the standalone REPL client; the NuGet package is for library consumers.

**Rationale**: No change needed. The existing configuration already supports both use cases.

## R7: Port Canonicalization Scope

**Decision**: Replace all `localhost:5000` references with `localhost:5180` (HTTP) across scripts, demos, and service fallback defaults. Leave `localhost:7180` (HTTPS) references as-is since they're already correct.

**Findings**: 5180 and 7180 are both correct — they're the HTTP and HTTPS ports from PhysicsServer's launchSettings.json. The `5000` port appears to be a legacy default that was never updated when the actual server ports were configured.

**Files affected**: ~15 files across F# demos, Python demos, PhysicsClient Program.fs, PhysicsViewer Program.fs, PhysicsClient.fsx, .mcp.json.

## R8: Consumer Migration Scope

**Decision**: Only migrate ProjectReferences to PackageReferences for direct consumers of the packaging chain. Leave service projects unchanged.

**Projects to migrate** (ProjectRef→PackageRef):
- PhysicsSandbox.Mcp: Scripting (remove redundant PhysicsClient/Contracts/ServiceDefaults refs if transitive)
- PhysicsSandbox.Scripting.Tests: Scripting

**Projects unchanged**:
- PhysicsServer, PhysicsSimulation, PhysicsViewer (keep ProjectRefs to Contracts/ServiceDefaults)
- PhysicsSandbox.AppHost (orchestrator, references everything)
- PhysicsClient.Tests, PhysicsServer.Tests, etc. (keep ProjectRefs to their test targets)

**Rationale**: Service projects are co-developed and deployed together; ProjectReferences give them always-current builds. Only external consumers (scripts, MCP as a tool host, test project for the library) benefit from NuGet consumption.
