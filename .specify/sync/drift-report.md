# Spec Drift Report

Generated: 2026-03-23
Project: PhysicsSandbox (004-backlog-fix-test-progress)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 9 (FR-001 through FR-009) |
| Aligned | 8 (89%) |
| Drifted | 1 (11%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 0 |

## Detailed Findings

### Spec: 004-backlog-fix-test-progress — Backlog Fixes and Test Progress Reporting

#### Aligned

- **FR-001**: Per-project progress display (`[1/7]`...`[7/7]` format) → `test-progress.sh` lines 172, 175, 186, 188, 193, 195
- **FR-002**: ETA after first project completes → `test-progress.sh` lines 161-163, 186, 193 (avg time × remaining)
- **FR-003**: Immediate failure surfacing with details → `test-progress.sh` lines 169-181 (inline `✗` + grep failure details)
- **FR-004b**: Cache operations emit `Trace.TraceWarning` — 3/3 instances fixed → `MeshResolver.fs:16-17`, `MeshResolver.fs:33-34`, `Session.fs:120-121`
- **FR-005**: Pending query expiration (30s timeout, 10s sweep timer) → `MessageRouter.fs` lines 169-186, 4 unit tests passing
- **FR-006**: All 10 constraint types have builders → `ConstraintBuilders.fs` / `.fsi` (BallSocket, Hinge, Weld, DistanceLimit, DistanceSpring, SwingLimit, TwistLimit, LinearAxisMotor, AngularMotor, PointOnLine)
- **FR-007**: Shared test helpers consolidated → `tests/SharedTestHelpers.fs` (F#, referenced by 6 projects), `IntegrationTestHelpers.cs` (C#, used by 14 test files). Zero duplicates remain.
- **FR-009**: Headless build support → `test-progress.sh` lines 81, 115 pass `-p:StrideCompilerSkipBuild=true`

#### Drifted

- **FR-004a**: Spec says "all 7 body registry TryAdd/TryRemove in SimulationCommands return Result.Error" but code has **6/7 fixed**. The `clearAll` function (`SimulationCommands.fs` ~line 307-308) silently counts TryRemove failures into a local `registryWarnings` counter instead of propagating Result.Error. Function returns `Ok count` even when registry removals fail.
  - Location: `src/PhysicsClient/Commands/SimulationCommands.fs:307-308`
  - Severity: **minor** (clearAll is a bulk operation; individual remove failures during clear are less critical than single-body operations, but still diverges from spec)

#### Not Implemented

None.

### Success Criteria Status

| Criterion | Status | Notes |
|-----------|--------|-------|
| SC-001 | Aligned | Progress within 2s of first project — output is synchronous after each `dotnet test` completes |
| SC-002 | Aligned | ETA accurate within 20% after 25% complete — rolling average improves with each project |
| SC-003 | **Drifted** | 9/10 silent failures fixed — clearAll has silent counter instead of Result.Error |
| SC-004 | Aligned | Pending queries cleaned up within 30s timeout, 10s sweep interval |
| SC-005 | Aligned | 10/10 constraint builders implemented with tests |
| SC-006 | Aligned | Zero duplicated test helpers — SharedTestHelpers.fs + IntegrationTestHelpers.cs |
| SC-007 | Aligned | All existing tests pass (362+ tests across 7 projects) |

### Acceptance Scenario Coverage

| Story | Scenario | Status |
|-------|----------|--------|
| US1-1 | Progress shows projects completed/total | Aligned |
| US1-2 | ETA displayed after first project | Aligned |
| US1-3 | Failures surfaced immediately | Aligned |
| US1-4 | Works with headless flags | Aligned |
| US2-1 | Duplicate add returns Result.Error | Aligned (6 of 7) |
| US2-2 | Missing remove returns Result.Error | Aligned |
| US2-3 | All 10 instances addressed | **Drifted** (9/10) |
| US3-1 | Timeout removes entry + notifies caller | Aligned |
| US3-2 | Cleanup removes expired entries | Aligned |
| US3-3 | Normal query resolves without expiration | Aligned |
| US4-1 | Builder for each of 10 types | Aligned |
| US4-2 | Builders match manual proto construction | Aligned |
| US5-1 | Helpers consolidated to shared location | Aligned |
| US5-2 | Shared helpers propagate changes | Aligned |

## Observations

1. **Timer disposal gap**: `MessageRouter.disposeExpirationTimer()` is exported but never called during server shutdown. Not a spec drift (spec doesn't require graceful shutdown), but a resource cleanup concern.
2. **Timeout not configurable**: FR-005 says "configurable timeout" but 30s is hardcoded. The spec assumption says "default to 30 seconds, can be made configurable without requiring user input" — current implementation matches the default but lacks the configurability mechanism.

## Recommendations

1. **Fix clearAll drift** (FR-004a): Either return `Result.Error` when any registry removal fails during `clearAll`, or emit a `Trace.TraceWarning` (consistent with FR-004b's cache pattern, since bulk clear failures are arguably benign). Update spec if the warning approach is preferred.
2. **Consider timeout configurability**: If the 30s default is always appropriate, document that the hardcoded value is intentional. Otherwise, accept the timeout as a parameter to `create()`.
