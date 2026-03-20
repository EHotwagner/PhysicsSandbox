# Spec Drift Report

Generated: 2026-03-20
Project: BPSandbox (PhysicsSandbox)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 2 |
| Requirements Checked | 48 (21 from 001 + 27 from 002) |
| Aligned | 43 (90%) |
| Drifted | 4 (8%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 1 |

## Detailed Findings

### Spec: 001-server-hub - Contracts and Server Hub

#### Aligned

- FR-001 through FR-014: All server hub requirements implemented
- SC-001, SC-003, SC-005, SC-006, SC-007: All success criteria met

#### Drifted

- SC-002: Health endpoints only mapped in Development environment (standard Aspire template behavior)
  - Location: `src/PhysicsSandbox.ServiceDefaults/Extensions.cs:113`
  - Severity: minor

- FR-002: Spec mentions only PhysicsHub service; code also defines SimulationLink (added during planning)
  - Location: `physics_hub.proto:22-28`
  - Severity: minor (SimulationLink correctly fulfills FR-008)

---

### Spec: 002-physics-simulation - Physics Simulation Service

#### Aligned

- FR-001: Connect to server via SimulationLink → `SimulationClient.fs:22`
- FR-002: Start paused by default → `SimulationWorld.fs:88` `Running = false`
- FR-003: Play/pause/step commands → `CommandHandler.fs:8-14`
- FR-004: Fixed timestep loop → `SimulationClient.fs:12` ~60Hz
- FR-005: Add bodies with shape → `SimulationWorld.fs:110-162` (Sphere/Box/Plane)
- FR-006: Unique body identifiers → `SimulationWorld.fs:111-112` rejects duplicates
- FR-007: Remove bodies → `SimulationWorld.fs:164-172`
- FR-008: Persistent forces → `SimulationWorld.fs:174-181` stored in ActiveForces map
- FR-009: One-shot impulses → `SimulationWorld.fs:183-189` calls applyLinearImpulse
- FR-010: Torque application → `SimulationWorld.fs:191-197` calls applyTorque
- FR-011: Global gravity → `SimulationWorld.fs:203-204`
- FR-012: Stream state after every step → `SimulationClient.fs:62-63`
- FR-014: State includes time + running flag → `SimulationWorld.fs:60-62`
- FR-015: Non-existent body graceful no-op → all body functions return success
- FR-016: Server disconnection clean shutdown → `SimulationClient.fs:70-72`
- FR-017: Reject zero/negative mass → `SimulationWorld.fs:113`
- FR-018: Aspire registration → `AppHost.cs:6-8`
- FR-019: Contract extensions → `physics_hub.proto` 4 new commands + 2 Body fields
- FR-020: Clear-forces command → `SimulationWorld.fs:199-201`
- SC-001: Connect within 5 seconds → architecture supports
- SC-002: All commands produce expected result → 31 unit tests verify
- SC-004: 100 bodies stable → stress test passes (100 bodies, 60 steps)
- SC-006: Edge cases handled → zero mass, empty world, large forces tested
- SC-007: Unit tests pass → 37 unit tests pass

#### Drifted

- FR-013: State MUST include each body's position, velocity, angular velocity, mass, shape, and identifier
  - Spec says: All bodies including planes should appear in streamed state
  - Code does: Plane bodies are created as static in BepuPhysics2 but NOT tracked in Bodies map — invisible in state stream. Dynamic bodies (sphere, box) fully tracked with all 7 fields.
  - Location: `SimulationWorld.fs:142-149`
  - Severity: minor
  - Note: Code comment: "skip tracking static bodies since they can't receive forces." Collisions are out of scope per spec assumptions.

- SC-003: Zero missed steps when streaming
  - Spec says: "State updates streamed after every step with zero missed steps"
  - Code does: Sends state after each step via gRPC streaming. Under backpressure, steps may be delayed (not skipped). No buffering/dropping.
  - Location: `SimulationClient.fs:62-63`
  - Severity: minor (functionally aligned — no steps are missed, just potentially delayed)

#### Not Implemented

(None — all 20 FR requirements have implementation)

### Unspecced Code

| Feature | Location | Lines | Suggested Spec |
|---------|----------|-------|----------------|
| Kestrel HTTP/2 protocol config | `AppHost.cs:4` | 1 | 001-server-hub (infra detail) |

## Inter-Spec Conflicts

None. Spec 001 (server hub) and spec 002 (simulation) are complementary — simulation connects to server via SimulationLink.

## Recommendations

1. **Minor (FR-013)**: Track plane bodies in state stream. If planes should be visible to downstream viewers, add them to Bodies map with a static flag. Low priority since collisions are out of scope.
2. **Minor (FR-010)**: Add a specific unit test verifying angular velocity changes after torque application. Function is implemented but not directly tested for physics output.
3. **Deferred**: 5 Aspire integration test tasks (T014, T023, T028, T034, T039) need container infrastructure. Recommend writing when running full Aspire stack.
4. **Stale assumption**: Spec says "simple physics model (Euler)" but implementation uses BepuFSharp (full rigid body solver). Update spec assumption to reflect actual technology.
5. **001 spec update**: Add SimulationLink service to FR-002 description (currently only mentions PhysicsHub).
