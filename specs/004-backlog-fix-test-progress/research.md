# Research: Backlog Fixes and Test Progress Reporting

**Date**: 2026-03-23 | **Branch**: `004-backlog-fix-test-progress`

## R1: TryAdd/TryRemove Silent Failure Instances

**Decision**: Fix all 10 instances (not 7 as originally estimated — actual count is higher).

**Findings**: All instances use `|> ignore` on the ConcurrentDictionary return value. None are breaking API changes since public signatures in `.fsi` files are already fixed.

| # | File | Line | Method | Context |
|---|------|------|--------|---------|
| 1 | SimulationCommands.fs | 70 | TryAdd | addSphere body registry |
| 2 | SimulationCommands.fs | 113 | TryAdd | addBox body registry |
| 3 | SimulationCommands.fs | 141 | TryAdd | addCapsule body registry |
| 4 | SimulationCommands.fs | 169 | TryAdd | addCylinder body registry |
| 5 | SimulationCommands.fs | 259 | TryAdd | addPlane body registry |
| 6 | SimulationCommands.fs | 273 | TryRemove | removeBody body registry |
| 7 | SimulationCommands.fs | 291 | TryRemove | clearAll loop body |
| 8 | MeshResolver.fs | 16 | TryAdd | processNewMeshes cache |
| 9 | MeshResolver.fs | 32 | TryAdd | fetchMissingSync cache |
| 10 | Session.fs | 119 | TryRemove | processPropertyEvent cache |

**Approach**: Instances 1-7 (SimulationCommands) return `Result<_, string>` — check TryAdd/TryRemove and return `Result.Error` on failure. Instances 8-10 (MeshResolver, Session) return `unit` — these are cache operations where duplicates are expected/benign. For these, log a warning via `Trace.TraceWarning` (always-on, not stripped in Release builds) rather than changing return types.

**Rationale**: Body registry operations (1-7) represent user-facing API calls where duplicates indicate a bug. Cache operations (8-10) are internal and duplicates are normal (e.g., mesh already cached). Different error handling is appropriate.

**Alternatives considered**: (A) Change all to Result.Error — rejected because cache operations returning unit would need signature changes for no user benefit. (B) Log all as warnings — rejected because body registry failures should be explicit errors per clarification Q1.

## R2: MessageRouter Pending Query Expiration

**Decision**: Add server-side timeout with periodic cleanup sweep.

**Findings**:
- `pendingQueries` is a module-level `ConcurrentDictionary<string, TaskCompletionSource<QueryResponse>>` at MessageRouter.fs:47
- `submitQuery` (lines 424-437) creates a TCS, stores it, awaits it, removes in `finally`
- `processQueryResponses` (lines 155-161) resolves TCS when response arrives
- Current timeout: caller-controlled via CancellationToken only
- No server-side expiration — if caller doesn't cancel, entries leak

**Approach**: Wrap TCS in a timestamped record. Add a periodic cleanup timer (every 10s) that scans for entries older than 30s timeout. On expiration, call `TrySetException` with a TimeoutException and remove from dictionary. The `finally` block in `submitQuery` already handles cleanup on normal/cancelled paths, so expired entries are the gap to fill.

**Rationale**: Server-side cleanup is necessary because callers may not always pass proper CancellationTokens. A sweep timer is simpler than per-query timers and has negligible overhead for the expected query volume (<100 concurrent).

**Alternatives considered**: (A) Per-query CancellationTokenSource with CancelAfter — more precise but creates N timers. (B) Rely on caller discipline — rejected since this is the current broken behavior.

## R3: Missing Constraint Builders

**Decision**: Add 6 builders following existing `make<Type>Cmd` pattern.

**Findings** (from proto physics_hub.proto lines 447-460):

| Constraint Type | Proto Fields | Builder Parameters |
|----------------|-------------|-------------------|
| DistanceSpringConstraint | local_offset_a/b, target_distance, spring | id, bodyA, bodyB, offsetA, offsetB, targetDistance |
| SwingLimitConstraint | axis_local_a/b, max_swing_angle, spring | id, bodyA, bodyB, axisA, axisB, maxAngle |
| TwistLimitConstraint | local_axis_a/b, min_angle, max_angle, spring | id, bodyA, bodyB, axisA, axisB, minAngle, maxAngle |
| LinearAxisMotorConstraint | local_offset_a/b, local_axis, target_velocity, motor | id, bodyA, bodyB, offsetA, offsetB, axis, targetVelocity, maxForce |
| AngularMotorConstraint | target_velocity, motor | id, bodyA, bodyB, targetVelocity, maxForce |
| PointOnLineConstraint | local_origin, local_direction, local_offset, spring | id, bodyA, bodyB, origin, direction, offset |

Existing pattern: `make<Type>Cmd : id -> bodyA -> bodyB -> ... -> SimulationCommand`. Uses `defaultSpring()` (freq=30, damping=1.0) and `toVec3` for tuple→Vec3 conversion. Motor types use `MotorConfig` (max_force, damping) instead of SpringSettings.

**Rationale**: Direct extension of existing pattern. All 10 types confirmed supported in SimulationWorld.fs `convertConstraintType` (lines 563-606).

## R4: Test Helper Duplication

**Decision**: Consolidate into two shared locations — F# helpers and C# integration helpers.

**Findings**:
- `getPublicMembers`: 6 identical copies across F# SurfaceAreaTests files
- `StartAppAndConnect`: 14 copies in C# integration tests (2 variants: basic + simulation-waiting)
- `assertContains`: 2 copies in F# test files
- SSL GrpcChannel setup: 14+ inline repetitions (8 lines each)
- Command factory methods: 2+ copies with minor variations

**Approach**:
- F# helpers: New shared file or project for `getPublicMembers` + `assertContains`
- C# helpers: New `IntegrationTestHelpers.cs` in Integration.Tests project with `StartAppAndConnect()`, `StartAppAndConnectWithSimulation()`, `StartServerOnly()`, `CreateSecureGrpcChannel()`

**Rationale**: F# test projects each reference different source projects, so a shared F# test utilities project would need minimal dependencies (just System.Reflection + xUnit). C# helpers stay in the integration test project since they're all in one project already.

## R5: Test Progress Reporting

**Decision**: Shell script wrapper parsing `dotnet test` console output per-project.

**Findings**:
- 7 test projects, 362 total tests
- `dotnet test` supports `--logger trx` (XML) by default, no extra packages
- No existing test runner scripts or custom loggers
- Integration tests run serially (xunit.runner.json: parallelizeAssembly=false)
- Standard invocation: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Approach**: Bash script (`test-progress.sh`) that:
1. Discovers test projects from .slnx
2. Runs `dotnet test` per-project sequentially
3. After each project: parses exit code + output for pass/fail/skip counts
4. Displays progress bar (N/7 projects), elapsed time, ETA
5. Surfaces failures immediately per-project
6. Final summary with total pass/fail/skip and wall time

**Rationale**: Per-project sequential execution is the simplest approach that matches the clarified per-project granularity. Parsing console output per-project is reliable because each `dotnet test` invocation produces a clean summary line. TRX parsing adds complexity for no benefit at project-level granularity.

**Alternatives considered**: (A) Custom xUnit logger NuGet package — over-engineered for 7 projects. (B) Parse TRX files — adds XML parsing dependency for no benefit. (C) Parallel execution with progress — conflicts with integration test serial requirement.
