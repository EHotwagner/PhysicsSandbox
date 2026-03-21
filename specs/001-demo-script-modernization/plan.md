# Implementation Plan: Demo Script Modernization

**Branch**: `001-demo-script-modernization` | **Date**: 2026-03-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-demo-script-modernization/spec.md`

## Summary

Modernize demo scripts to use server-side `reset` command and `batchCommands` API instead of manual multi-step reset and sequential individual gRPC calls. Add Prelude helpers for command construction and batch submission. Update all 10 demos, AllDemos, AutoRun, and RunAll.

## Technical Context

**Language/Version**: F# scripts (.fsx) on .NET 10.0
**Primary Dependencies**: PhysicsClient.dll, PhysicsSandbox.Shared.Contracts.dll (proto-generated types)
**Storage**: N/A
**Testing**: Manual execution via `dotnet fsi` (no xUnit — scripts only)
**Target Platform**: Linux (container with GPU)
**Project Type**: Demo scripts (scripting layer over compiled library)
**Performance Goals**: 30%+ faster demo setup via batching (SC-002)
**Constraints**: Batch limit 100 commands; `toVec3` is `internal` in library
**Scale/Scope**: 15 script files modified, ~7 new helper functions in Prelude

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | N/A | No service changes |
| II. Contract-First | PASS | No new contracts; uses existing proto messages |
| III. Shared Nothing | PASS | Scripts reference contracts DLL only |
| IV. Spec-First | PASS | Feature spec exists at spec.md |
| V. Compiler-Enforced Contracts | N/A | .fsx scripts are not public modules; no .fsi required |
| VI. Test Evidence | PASS | Verified by running AutoRun (all 10 demos pass) |
| VII. Observability | N/A | No service changes |

No violations. No complexity tracking needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-demo-script-modernization/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: API research decisions
├── data-model.md        # Phase 1: Entity reference
├── quickstart.md        # Phase 1: How to run/verify
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (files modified)

```text
demos/
├── Prelude.fsx              # Core: new helpers (reset, batch, command builders)
├── Demo01_HelloDrop.fsx     # resetScene → resetSimulation
├── Demo02_BouncingMarbles.fsx  # + batch 5 marbles
├── Demo03_CrateStack.fsx    # resetScene → resetSimulation
├── Demo04_BowlingAlley.fsx  # resetScene → resetSimulation
├── Demo05_MarbleRain.fsx    # resetScene → resetSimulation
├── Demo06_DominoRow.fsx     # + batch 12 boxes
├── Demo07_SpinningTops.fsx  # + batch 4 bodies + 4 torques
├── Demo08_GravityFlip.fsx   # + batch 5 beach balls
├── Demo09_Billiards.fsx     # + batch 16 spheres
├── Demo10_Chaos.fsx         # + batch 10 impulses
├── AllDemos.fsx             # Mirror all demo changes
├── AutoRun.fsx              # Self-contained: duplicate helpers + demo changes
└── RunAll.fsx               # One-line change: resetScene → resetSimulation
```

**Structure Decision**: No new files or directories. All changes are to existing `.fsx` files in `demos/`.
