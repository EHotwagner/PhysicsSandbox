# Implementation Plan: Expose Session State

**Branch**: `005-expose-session-state` | **Date**: 2026-03-29 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-expose-session-state/spec.md`

## Summary

Make three internal Session module accessors (`latestState`, `bodyRegistry`, `lastStateUpdate`) publicly accessible from the PhysicsClient `.fsi` signature file. This is a visibility-only change — remove the `internal` keyword from three function signatures and update the surface area baseline test.

## Technical Context

**Language/Version**: F# on .NET 10.0
**Primary Dependencies**: PhysicsClient (target), PhysicsSandbox.Scripting (downstream consumer), Google.Protobuf 3.x (SimulationState type)
**Storage**: N/A (in-memory state only)
**Testing**: xUnit 2.x, surface area baseline tests, integration tests
**Target Platform**: Linux (container with GPU passthrough)
**Project Type**: Library (PhysicsClient)
**Performance Goals**: N/A (read-only accessors, no performance impact)
**Constraints**: Thread-safe access (already satisfied — ConcurrentDictionary, atomic mutable fields)
**Scale/Scope**: 3 function signatures changed in 1 file, 1 surface area test updated

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | No cross-service changes. PhysicsClient is a client library. |
| II. Contract-First | PASS | No gRPC/proto contract changes. This modifies the F# module's public API surface only. |
| III. Shared Nothing | PASS | No new cross-service dependencies. |
| IV. Spec-First Delivery | PASS | Spec exists at `specs/005-expose-session-state/spec.md`. |
| V. Compiler-Enforced Structural Contracts | PASS | `.fsi` signature file will be updated. Surface area baseline test will be updated. |
| VI. Test Evidence | PASS | Surface area test update verifies the new public API. Existing tests remain green. |
| VII. Observability by Default | PASS | No new service behavior — read-only accessors. |

**All gates pass. No violations to justify.**

## Project Structure

### Documentation (this feature)

```text
specs/005-expose-session-state/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/PhysicsClient/Connection/
├── Session.fsi          # MODIFY: Remove `internal` from 3 function signatures
└── Session.fs           # NO CHANGE: Implementation unchanged

tests/PhysicsClient.Tests/
└── SurfaceAreaTests.fs  # MODIFY: Add 3 new entries to Session baseline
```

**Structure Decision**: Existing project structure. No new files or directories needed. This is a 2-file change.

## Complexity Tracking

No violations. No complexity justification needed.
