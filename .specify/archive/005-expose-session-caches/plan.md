# Implementation Plan: Expose Session Caches

**Branch**: `005-expose-session-caches` | **Date**: 2026-03-29 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-expose-session-caches/spec.md`

## Summary

Expose four internal Session fields (`bodyPropertiesCache`, `cachedConstraints`, `cachedRegisteredShapes`, `serverAddress`) as public accessor functions via `.fsi` signature file additions, following the identical pattern established in 005-expose-session-state. Update surface area baseline tests to reflect the expanded public API.

## Technical Context

**Language/Version**: F# on .NET 10.0
**Primary Dependencies**: PhysicsClient (target), Google.Protobuf 3.x (BodyProperties, ConstraintState, RegisteredShapeState types)
**Storage**: N/A (in-memory caches only)
**Testing**: xUnit 2.x (surface area baseline tests, unit tests)
**Target Platform**: .NET 10.0 library
**Project Type**: Library (public API surface change)
**Performance Goals**: N/A (accessor functions only, zero overhead)
**Constraints**: Must not break existing internal consumers; must follow constitution Principle V (.fsi enforcement)
**Scale/Scope**: 4 new public accessor functions, 2 files changed, 1 test updated

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | No cross-service changes; PhysicsClient is a standalone library |
| II. Contract-First | PASS | No new gRPC contracts; exposing existing in-memory types |
| III. Shared Nothing | PASS | Only PhysicsClient and its existing Contracts dependency involved |
| IV. Spec-First Delivery | PASS | Feature spec exists at `specs/005-expose-session-caches/spec.md` |
| V. Compiler-Enforced Structural Contracts | PASS | Changes go through `.fsi` file; surface area baseline test updated |
| VI. Test Evidence | PASS | Surface area baseline test will verify new public members |
| VII. Observability by Default | PASS | No new operational behavior; read-only accessors |

**Gate result**: ALL PASS — proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/005-expose-session-caches/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/PhysicsClient/Connection/
├── Session.fsi          # Add 4 new public val declarations
└── Session.fs           # Add 4 new accessor functions

tests/PhysicsClient.Tests/
└── SurfaceAreaTests.fs  # Update Session baseline with 4 new members
```

**Structure Decision**: No new files or directories. All changes are additions to existing files in the established PhysicsClient project structure.

## Complexity Tracking

No violations — no complexity justifications needed.
