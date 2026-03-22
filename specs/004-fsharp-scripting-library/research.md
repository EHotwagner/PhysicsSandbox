# Research: F# Scripting Library

**Feature**: 004-fsharp-scripting-library | **Date**: 2026-03-22

## R1: Library vs Wrapper Architecture

**Decision**: Thin wrapper library that depends on PhysicsClient via project reference.

**Rationale**: The scripting library's primary value is convenience, not abstraction. PhysicsClient already provides the core functionality (session management, gRPC communication, body operations). The Prelude.fsx functions are thin helpers that compose PhysicsClient calls. A wrapper library preserves this relationship without duplicating logic.

**Alternatives considered**:
- **Fat library absorbing PhysicsClient**: Would create maintenance burden and break existing PhysicsClient consumers. Rejected.
- **Source-level includes (.fsx #load)**: Scripts already use this pattern; doesn't solve the MCP server reuse requirement. Rejected.
- **NuGet package**: Unnecessary for a single-solution library; project reference is simpler and keeps build in sync. Rejected for now; can be added later if needed.

## R2: Script Reference Pattern

**Decision**: Scripts use `#r` directives pointing to compiled DLLs in the build output directory.

**Rationale**: This is the established pattern in `demos/Prelude.fsx`. The new library consolidates references — instead of 3 DLLs + 4 NuGet packages, scripts reference 1 DLL. The library's dependencies (PhysicsClient, Shared.Contracts, gRPC, Protobuf) are resolved transitively at build time and available in the same output directory.

**Alternatives considered**:
- **NuGet package from local feed**: Adds packaging step to dev workflow; overkill for single-solution use. Rejected.
- **FSI `#r "nuget:..."` for the library**: Requires publishing; doesn't work for project references. Rejected.

## R3: Module Organization

**Decision**: Six modules organized by concern, with an AutoOpen Prelude module that re-exports everything.

**Rationale**: Matches the logical groupings in the existing Prelude.fsx. Keeping modules small (2-4 functions each) makes extending any single concern straightforward without touching unrelated code. The AutoOpen Prelude provides backward compatibility with the flat namespace that script authors expect.

**Modules**:
1. `Helpers` — `ok`, `sleep`, `timed` (general utilities)
2. `Vec3Builders` — `toVec3` (proto type construction)
3. `CommandBuilders` — `makeSphereCmd`, `makeBoxCmd`, `makeImpulseCmd`, `makeTorqueCmd` (proto command construction)
4. `BatchOperations` — `batchAdd` with auto-chunking (batch command execution)
5. `SimulationLifecycle` — `resetSimulation`, `runFor`, `nextId` (simulation control)
6. `Prelude` — AutoOpen module re-exporting all above for script convenience

## R4: MCP Server Integration

**Decision**: MCP server adds a project reference to `PhysicsSandbox.Scripting` and uses shared helper functions directly.

**Rationale**: The MCP server's `ClientAdapter.fs` already duplicates `toVec3` and command-building patterns from the Prelude. By referencing the scripting library, the MCP server can call these functions directly. The MCP server uses `GrpcConnection` (raw gRPC channel) rather than `PhysicsClient.Session`, so not all library functions apply — but the command builders and Vec3 utilities are directly reusable.

**Integration scope for this feature**: Replace duplicated `toVec3` and command-building code in ClientAdapter with calls to the scripting library. Deeper MCP integration (replacing more of ClientAdapter) is a future concern.

## R5: Scratch Folder Management

**Decision**: `scratch/` is gitignored with `.gitkeep`; `scripts/` is git-tracked. Both use identical `#r` paths.

**Rationale**: Scratch is for throwaway experimentation — tracking it in git adds noise. Scripts folder contains curated, reviewed content. Both folders sit at the repo root at the same depth as `demos/`, so relative paths to `src/PhysicsSandbox.Scripting/bin/Debug/net10.0/` are identical.

## R6: Testing Strategy

**Decision**: Unit tests for pure helper functions + SurfaceAreaTests for API contract verification.

**Rationale**: Follows the PhysicsClient.Tests pattern. Most library functions are either pure (command builders, vec3 conversion) or thin wrappers (batchAdd delegates to PhysicsClient). Pure functions get standard unit tests. SurfaceAreaTests verify the public API surface via reflection to prevent accidental API drift per Constitution Principle V.

**Test scope**:
- `HelpersTests.fs` — `ok` with success/failure, `timed` returns correct result
- `CommandBuildersTests.fs` — each builder produces correct proto messages
- `BatchOperationsTests.fs` — chunking logic at boundary (100 items)
- `SurfaceAreaTests.fs` — reflection-based public API baseline
