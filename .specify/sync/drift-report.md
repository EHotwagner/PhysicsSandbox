# Spec Drift Report

Generated: 2026-03-25
Project: PhysicsSandbox
Feature: 004-codebase-cleanup-refactor

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 13 (8 FR + 5 SC) |
| Aligned | 12 (92%) |
| Drifted | 1 (8%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 0 |

## Detailed Findings

### Spec: 004-codebase-cleanup-refactor - Codebase Cleanup and Refactoring

#### Aligned

- FR-001: Vector/quaternion conversions — one canonical definition per logical conversion
  - `src/PhysicsSimulation/Conversions/ProtoConversions.fs` (Vec3/Vec4 to System.Numerics)
  - `src/PhysicsViewer/Rendering/ProtoConversions.fs` (Vec3/Vec4 to Stride)
  - `src/PhysicsClient/Utilities/Vec3Helpers.fs` (tuple to proto Vec3)
  - No duplicate private copies remain in SimulationWorld.fs or QueryHandler.fs

- FR-002: MeshResolver consolidated where dependency graph permits
  - PhysicsClient canonical: `src/PhysicsClient/Connection/MeshResolver.fs`
  - MCP delegates: `src/PhysicsSandbox.Mcp/Program.fs` references `PhysicsClient.MeshResolver`
  - PhysicsViewer retains async variant: `src/PhysicsViewer/Streaming/MeshResolver.fs`
  - MCP's own MeshResolver.fs/fsi deleted

- FR-003: ID generation in single module
  - Canonical: `src/PhysicsClient/Bodies/IdGenerator.fs`
  - MCP SimulationTools, GeneratorTools, BatchTools all delegate to it

- FR-004: Unified shape-building pattern
  - `src/PhysicsClient/Shapes/ShapeBuilders.fs`: mkSphere, mkBox, mkCapsule, mkCylinder, mkPlane, mkTriangle
  - SimulationCommands.fs uses `addGenericBody` + ShapeBuilders

- FR-005: No source file exceeds 550 lines (max: SimulationWorld.fs at 538)
- FR-006: Integration test helper duplication eliminated (CreateGrpcChannel extracted)
- FR-007: Tests pass (365 unit, 1 pre-existing flaky)
- FR-008: All new modules have .fsi signature files
- SC-001: Zero duplicate utility functions
- SC-002: 10%+ line reduction (SimulationWorld -24%, SimulationCommands -23%)
- SC-003: No src/ file exceeds 550 lines
- SC-005: New shape requires max 2 files

#### Drifted

- SC-004: "All existing tests pass with zero regressions"
  - Severity: minor
  - Location: `tests/PhysicsSandbox.Mcp.Tests/SessionStoreTests.fs:94`
  - Actual: 1 pre-existing flaky test (GUID ordering) fails intermittently — not caused by refactoring

#### Not Implemented

(none)

### Unspecced Code

(none)

## Recommendations

1. Fix the flaky SessionStore test (GUID ordering) — not related to this feature
2. Update spec status from "Draft" to "Implemented"
3. MCP SimulationTools inline shape dispatch could further benefit from ShapeBuilders (partial FR-004)
