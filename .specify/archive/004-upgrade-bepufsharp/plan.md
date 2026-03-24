# Implementation Plan: Upgrade BepuFSharp

**Branch**: `004-upgrade-bepufsharp` | **Date**: 2026-03-24 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-upgrade-bepufsharp/spec.md`

## Summary

Upgrade BepuFSharp from 0.2.0-beta.1 to 0.3.0 in PhysicsSimulation.fsproj. The new version drops the Stride.BepuPhysics dependency and removes the unused StrideInterop module. All other APIs are identical. Validate with a full build and test run, then update docs.

## Technical Context

**Language/Version**: F# on .NET 10.0 (PhysicsSimulation), C# on .NET 10.0 (integration tests)
**Primary Dependencies**: BepuFSharp 0.2.0-beta.1 → 0.3.0 (local NuGet at `~/.local/share/nuget-local/`). Transitive: BepuPhysics 2.5.0-beta.28 (unchanged), BepuUtilities 2.5.0-beta.28 (unchanged), FSharp.Core 10.0.104 (unchanged)
**Storage**: N/A (no storage changes)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x — 7 unit test projects + 1 integration test project
**Target Platform**: Linux (container with GPU passthrough)
**Project Type**: Microservices (F# services orchestrated by .NET Aspire)
**Performance Goals**: N/A (no behavioral changes)
**Constraints**: Zero test failures, zero production code changes beyond version bump
**Scale/Scope**: Single line change in one .fsproj + doc updates

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | No cross-service changes |
| II. Contract-First | PASS | No contract changes — same proto, same APIs |
| III. Shared Nothing | PASS | BepuFSharp is a NuGet dependency, not a project reference |
| IV. Spec-First Delivery | PASS | Spec exists at specs/004-upgrade-bepufsharp/spec.md |
| V. Compiler-Enforced Contracts | PASS | No public API changes — no .fsi or surface area baseline changes needed |
| VI. Test Evidence | PASS | Existing tests validate backward compatibility; no new behavior to test |
| VII. Observability | PASS | No service changes |

**Dependencies minimized**: PASS — BepuFSharp 0.3.0 actually *removes* a dependency (Stride.BepuPhysics), simplifying the graph.

**Gate result**: ALL PASS. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/004-upgrade-bepufsharp/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── spec.md              # Feature specification
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
src/
  PhysicsSimulation/PhysicsSimulation.fsproj  # Version bump: BepuFSharp 0.2.0-beta.1 → 0.3.0
```

**Structure Decision**: No new files or directories. Single version number change in an existing project file, plus documentation updates.

## Complexity Tracking

No constitution violations — table not needed.
