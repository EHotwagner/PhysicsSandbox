# Implementation Plan: F# Scripting Library

**Branch**: `004-fsharp-scripting-library` | **Date**: 2026-03-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-fsharp-scripting-library/spec.md`

## Summary

Create a new F# library project (`PhysicsSandbox.Scripting`) that bundles all existing `Prelude.fsx` convenience functions into compiled, reusable modules with `.fsi` signature files. The library wraps `PhysicsClient` to provide a script-friendly API surface: result unwrapping, command builders, batch operations, timing, and simulation lifecycle helpers. It is referenced by scripts via a single `#r` directive and by the MCP server via a standard project reference. Two new folders (`scratch/` and `scripts/`) at the repo root provide experimentation and curated script locations.

## Technical Context

**Language/Version**: F# on .NET 10.0
**Primary Dependencies**: PhysicsClient (project ref), PhysicsSandbox.Shared.Contracts (transitive), Grpc.Net.Client 2.x, Google.Protobuf 3.x
**Storage**: N/A (stateless library)
**Testing**: xUnit 2.x, dotnet test, SurfaceAreaTests pattern
**Target Platform**: Linux (container with GPU passthrough)
**Project Type**: Library (class library, packable)
**Performance Goals**: N/A (thin wrapper, no runtime overhead beyond underlying PhysicsClient calls)
**Constraints**: Must not break existing PhysicsClient public API; must be usable from .fsx scripts via `#r` directive
**Scale/Scope**: ~6 modules wrapping ~12 existing Prelude functions plus re-exports of PhysicsClient types

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | Library is not a service; it is consumed in-process by scripts and the MCP server |
| II. Contract-First | PASS | No new service boundary; library wraps existing PhysicsClient contracts |
| III. Shared Nothing | PASS | Library references PhysicsClient via project reference (same solution, not cross-service) |
| IV. Spec-First Delivery | PASS | Spec and plan created before implementation |
| V. Compiler-Enforced Structural Contracts | PASS | All public modules will have `.fsi` signature files; SurfaceAreaTests will be added |
| VI. Test Evidence | PASS | Unit tests + surface area tests planned |
| VII. Observability by Default | N/A | Library project, no runtime services |

**Gate result: PASS** — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/004-fsharp-scripting-library/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
└── PhysicsSandbox.Scripting/           # NEW: F# class library
    ├── PhysicsSandbox.Scripting.fsproj
    ├── Helpers.fsi                      # Result unwrapping, sleep, timed
    ├── Helpers.fs
    ├── Vec3Builders.fsi                 # toVec3 and vector utilities
    ├── Vec3Builders.fs
    ├── CommandBuilders.fsi              # makeSphereCmd, makeBoxCmd, makeImpulseCmd, makeTorqueCmd
    ├── CommandBuilders.fs
    ├── BatchOperations.fsi              # batchAdd with auto-chunking
    ├── BatchOperations.fs
    ├── SimulationLifecycle.fsi          # resetSimulation, runFor
    ├── SimulationLifecycle.fs
    ├── Prelude.fsi                      # AutoOpen module re-exporting all functions
    └── Prelude.fs

tests/
└── PhysicsSandbox.Scripting.Tests/     # NEW: unit + surface area tests
    ├── PhysicsSandbox.Scripting.Tests.fsproj
    ├── HelpersTests.fs
    ├── CommandBuildersTests.fs
    ├── BatchOperationsTests.fs
    └── SurfaceAreaTests.fs

scratch/                                 # NEW: gitignored experimentation folder
├── .gitkeep
└── (developer .fsx files, not tracked)

scripts/                                 # NEW: curated scripts (tracked in git)
├── Prelude.fsx                          # Minimal prelude: #r to Scripting DLL + opens
└── (curated .fsx scripts)
```

**Structure Decision**: Single new library project under `src/` following existing conventions. New test project under `tests/`. Two new root-level folders for scripts. The library depends on `PhysicsClient` (project reference) and transitively gets `PhysicsSandbox.Shared.Contracts`. The MCP server will add a project reference to `PhysicsSandbox.Scripting`.
