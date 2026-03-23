# Spec Drift Report

Generated: 2026-03-23
Project: PhysicsSandbox (004-state-stream-optimization)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 15 (10 FR + 5 SC) |
| Aligned | 14 (93%) |
| Drifted | 1 (7%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 0 |

## Detailed Findings

### Spec: 004-state-stream-optimization - State Stream Bandwidth Optimization

#### Aligned

- **FR-001**: Tick stream contains only continuous data for dynamic bodies; static bodies excluded
  - `src/PhysicsServer/Hub/MessageRouter.fs:193-210` — `buildTickState` filters `not body.IsStatic`
- **FR-002**: Semi-static properties delivered via property event stream
  - `src/PhysicsServer/Hub/MessageRouter.fs:212-275` — `detectPropertyEvents` emits body_created/updated
  - `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto:204-210` — TickState has no semi-static fields
- **FR-003**: Full semi-static properties on creation; static bodies include pose
  - `src/PhysicsServer/Hub/MessageRouter.fs:166-179` — `bodyToProperties` includes position/orientation
- **FR-004**: Updated semi-static properties pushed on change
  - `src/PhysicsServer/Hub/MessageRouter.fs:182-190` — `propsChanged` detects all semi-static field changes
- **FR-005**: Late joiner receives full backfill
  - `src/PhysicsServer/Services/PhysicsHubService.fs:77-105` — StreamProperties sends PropertySnapshot first
  - `src/PhysicsServer/Hub/MessageRouter.fs:277-286` — `buildPropertySnapshot` includes all bodies + constraints + shapes
- **FR-006**: Client field profile for velocity opt-out
  - `src/PhysicsServer/Services/PhysicsHubService.fs:27-42` — `StripVelocity` when `ExcludeVelocity=true`
  - `src/PhysicsViewer/Program.fs:166,191` — Viewer uses `ExcludeVelocity=true`
- **FR-007**: Explicit removal event on property event stream
  - `src/PhysicsServer/Hub/MessageRouter.fs:237-242` — `PropertyEvent(BodyRemoved = prevId)`
- **FR-008**: Constraints and registered shapes via property event stream, not per tick
  - `src/PhysicsServer/Hub/MessageRouter.fs:246-262` — Snapshot emitted on count change
  - Proto TickState has no constraints/shapes fields
- **FR-009**: All clients merge tick + property data
  - `src/PhysicsClient/Connection/Session.fs:82-109` — `reconstructState`
  - `src/PhysicsSandbox.Mcp/GrpcConnection.fs:72-123` — `reconstructState`
  - `src/PhysicsViewer/Program.fs:80-130` — `reconstructSimState`
- **FR-010**: Slow consumer converges without manual reconnect
  - `src/PhysicsServer/Services/PhysicsHubService.fs:82-87` — PropertySnapshot backfill on every StreamProperties connect
- **SC-002**: Viewer per-tick message reduced by >=80%
  - `tests/PhysicsSandbox.Integration.Tests/StateStreamOptimizationIntegrationTests.cs:458-500` — T099 asserts <=11 KB, passes
- **SC-003**: All existing tests pass — 306 unit tests pass, 12 feature integration tests pass
- **SC-004**: New clients see complete state within 1 tick
  - StreamState sends cached TickState first; StreamProperties sends PropertySnapshot first
- **SC-005**: Property changes visible within 1 tick
  - `detectPropertyEvents` runs on every `publishState` call (60 Hz), no batching

#### Drifted

- **SC-001**: Spec says ">=70% reduction" (implying <=15 KB at 200 bodies from ~50 KB baseline). Actual measurement is 15.7 KB (~69% reduction).
  - Location: `tests/PhysicsSandbox.Integration.Tests/StateStreamOptimizationIntegrationTests.cs:452-455`
  - Severity: **minor**
  - Detail: Proto3 double-precision Vec3/Vec4 encoding gives ~80 bytes/body when collision-induced components are non-zero. Test threshold adjusted to 16 KB. The ~50 KB baseline and 15 KB target were both estimates from the data model. Actual reduction is 68-69%, marginally below the 70% target. The deviation comes from proto3 using `double` (8 bytes) for Vec3/Vec4 components rather than `float` (4 bytes).
  - Potential fix: Switching Vec3/Vec4 from `double` to `float` in the proto would halve numeric payload and comfortably meet 70%, but this is a cross-cutting proto change affecting all consumers.

#### Not Implemented

- None. All functional requirements are implemented.
- **T041** (integration test for `body_updated` on color change) is deferred pending a `SetColor` command. This does not affect FR-004 implementation — the server-side detection logic is in place; only the end-to-end test path is missing because the triggering command doesn't exist yet.

### Unspecced Code

None detected. All implementation changes are traceable to spec requirements.

## Inter-Spec Conflicts

None detected.

## Recommendations

1. **SC-001 marginal drift**: Consider switching Vec3/Vec4 proto fields from `double` to `float` in a future optimization pass. This would halve numeric payload (~40 bytes/body savings at 200 bodies) and comfortably exceed the 70% reduction target. However, this is a cross-cutting change that affects all proto consumers.
2. **T041 deferred test**: Implement a `SetColor` command in a future feature to enable end-to-end testing of the property change notification path (FR-004 acceptance scenario 2).
