# Tasks: Backlog Fixes and Test Progress Reporting

**Input**: Design documents from `/specs/004-backlog-fix-test-progress/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md

**Tests**: Included per Constitution Principle VI (test evidence required for behavior-changing code).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No new projects or infrastructure needed. All changes fit within existing project boundaries. This phase handles the one cross-cutting setup task.

- [x] T001 Create shared F# test helpers file at tests/SharedTestHelpers.fs with `getPublicMembers` and `assertContains` functions (module TestHelpers, open System.Reflection)

**Checkpoint**: Shared helpers file exists and compiles standalone.

---

## Phase 2: User Story 1 - Test Suite Progress and Time Estimates (Priority: P1) — MVP

**Goal**: Create a shell script that runs the test suite with per-project progress, elapsed time, and ETA.

**Independent Test**: Run `./test-progress.sh` and observe per-project progress output with timing.

### Tests for User Story 1

- [x] T002 [US1] Manually verify test-progress.sh against acceptance scenarios after implementation (no automated test — shell script validated by running it)

### Implementation for User Story 1

- [x] T003 [US1] Create test-progress.sh at repo root: parse PhysicsSandbox.slnx to discover test project paths under `<Folder Name="/tests/">`, store in array
- [x] T004 [US1] Add upfront build step to test-progress.sh: run `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` and exit on failure with clear error message
- [x] T005 [US1] Add per-project test loop to test-progress.sh: for each project, run `dotnet test <project> -p:StrideCompilerSkipBuild=true --no-build -v normal` capturing stdout+stderr, parse `Passed:` / `Failed:` / `Skipped:` summary line via grep/sed
- [x] T006 [US1] Add progress display to test-progress.sh: after each project completes, print `[N/7] ProjectName ✓ X passed (Ys) | ETA: ~Zs` or `✗ X failed` with immediate failure details
- [x] T007 [US1] Add ETA calculation to test-progress.sh: track cumulative elapsed time, compute average per-project duration, multiply by remaining projects
- [x] T008 [US1] Add final summary to test-progress.sh: total passed/failed/skipped, total wall time, overall pass/fail status with exit code
- [x] T009 [US1] Add edge case handling to test-progress.sh: handle zero-test projects (no summary line), handle build failures mid-run, handle missing .slnx gracefully
- [x] T010 [US1] Make test-progress.sh executable (`chmod +x`) and add usage documentation as a comment header

**Checkpoint**: `./test-progress.sh` runs all 7 test projects with progress bars, ETA, and final summary.

---

## Phase 3: User Story 2 - Fix Silent TryAdd/TryRemove Failures (Priority: P2)

**Goal**: Replace all 10 silent `TryAdd`/`TryRemove` failures with explicit error reporting (`Result.Error` for body registry, log warnings for caches).

**Independent Test**: Run PhysicsClient.Tests and verify new error-handling tests pass.

### Tests for User Story 2

- [x] T011 [P] [US2] Create tests/PhysicsClient.Tests/RegistryErrorTests.fs: test that addSphere with duplicate ID returns `Result.Error` containing the body ID in the message
- [x] T012 [P] [US2] Add tests to RegistryErrorTests.fs: test that removeBody with non-existent ID returns `Result.Error`, test that clearAll handles missing entries gracefully
- [x] T013 [US2] Add RegistryErrorTests.fs to tests/PhysicsClient.Tests/PhysicsClient.Tests.fsproj `<Compile Include>` list in correct order
- [x] T014 [US2] Update surface area baseline in tests/PhysicsClient.Tests/SurfaceAreaTests.fs if any public API signatures changed (verify against .fsi)

### Implementation for User Story 2

- [x] T015 [US2] Fix addSphere TryAdd in src/PhysicsClient/Commands/SimulationCommands.fs line ~70: replace `|> ignore` with check, return `Result.Error $"Body '{bodyId}' already exists in registry"` on false
- [x] T016 [US2] Fix addBox TryAdd in src/PhysicsClient/Commands/SimulationCommands.fs line ~113: same pattern as T015
- [x] T017 [US2] Fix addCapsule TryAdd in src/PhysicsClient/Commands/SimulationCommands.fs line ~141: same pattern as T015
- [x] T018 [US2] Fix addCylinder TryAdd in src/PhysicsClient/Commands/SimulationCommands.fs line ~169: same pattern as T015
- [x] T019 [US2] Fix addPlane TryAdd in src/PhysicsClient/Commands/SimulationCommands.fs line ~259: same pattern as T015
- [x] T020 [US2] Fix removeBody TryRemove in src/PhysicsClient/Commands/SimulationCommands.fs line ~273: replace `|> ignore` with check, return `Result.Error $"Body '{bodyId}' not found in registry"` on false
- [x] T021 [US2] Fix clearAll TryRemove in src/PhysicsClient/Commands/SimulationCommands.fs line ~291: check return value, if false increment a warning counter and include in result message
- [x] T022 [P] [US2] Fix processNewMeshes TryAdd in src/PhysicsClient/Connection/MeshResolver.fs line ~16: replace `|> ignore` with `if not then Trace.TraceWarning($"MeshResolver: mesh {mg.MeshId} already cached")`
- [x] T023 [P] [US2] Fix fetchMissingSync TryAdd in src/PhysicsClient/Connection/MeshResolver.fs line ~32: same warning pattern as T022
- [x] T024 [P] [US2] Fix processPropertyEvent TryRemove in src/PhysicsClient/Connection/Session.fs line ~119: replace `|> ignore` with `if not then Trace.TraceWarning($"Session: body {evt.BodyRemoved} not found in properties cache")`

**Checkpoint**: All 10 silent failures addressed. `dotnet test tests/PhysicsClient.Tests/ -p:StrideCompilerSkipBuild=true` passes including new RegistryErrorTests.

---

## Phase 4: User Story 3 - Add Pending Query Expiration (Priority: P3)

**Goal**: Add server-side 30s timeout for pending queries in MessageRouter with periodic sweep cleanup.

**Independent Test**: Run PhysicsServer.Tests and verify new query expiration tests pass.

### Tests for User Story 3

- [x] T025 [P] [US3] Create tests/PhysicsServer.Tests/QueryExpirationTests.fs: test that a pending query entry older than timeout threshold is removed by sweep
- [x] T026 [P] [US3] Add tests to QueryExpirationTests.fs: test that normal query resolution within timeout works unchanged, test that expired entry sets TimeoutException on TCS
- [x] T027 [US3] Add QueryExpirationTests.fs to tests/PhysicsServer.Tests/PhysicsServer.Tests.fsproj `<Compile Include>` list in correct order

### Implementation for User Story 3

- [x] T028 [US3] Define `PendingQueryEntry` record type in src/PhysicsServer/Hub/MessageRouter.fs: `{ Tcs: TaskCompletionSource<QueryResponse>; CreatedAt: DateTime }`
- [x] T029 [US3] Update `pendingQueries` dictionary type from `ConcurrentDictionary<string, TaskCompletionSource<QueryResponse>>` to `ConcurrentDictionary<string, PendingQueryEntry>` in src/PhysicsServer/Hub/MessageRouter.fs line ~47
- [x] T030 [US3] Update `submitQuery` in src/PhysicsServer/Hub/MessageRouter.fs lines ~424-437: wrap TCS in PendingQueryEntry with `CreatedAt = DateTime.UtcNow`, access `.Tcs` for await and cancellation registration
- [x] T031 [US3] Update `processQueryResponses` in src/PhysicsServer/Hub/MessageRouter.fs lines ~155-161: access `.Tcs` on PendingQueryEntry when setting result
- [x] T032 [US3] Add sweep timer function `expireStaleQueries` in src/PhysicsServer/Hub/MessageRouter.fs: iterate pendingQueries, for entries where `DateTime.UtcNow - entry.CreatedAt > TimeSpan.FromSeconds(30.0)`, call `entry.Tcs.TrySetException(TimeoutException("Query expired after 30s"))` and remove from dictionary
- [x] T033 [US3] Add `System.Threading.Timer` to MessageRouter that calls `expireStaleQueries` every 10 seconds, started during router initialization, disposed with router
- [x] T034 [US3] Update src/PhysicsServer/Hub/MessageRouter.fsi to expose `PendingQueryEntry` type as internal and add sweep timer disposal if MessageRouter gains IDisposable
- [x] T035 [US3] Update surface area baseline in tests/PhysicsServer.Tests/ if public API surface changed

**Checkpoint**: `dotnet test tests/PhysicsServer.Tests/ -p:StrideCompilerSkipBuild=true` passes including new QueryExpirationTests.

---

## Phase 5: User Story 4 - Complete Missing Constraint Builder Coverage (Priority: P4)

**Goal**: Add 6 new constraint builder functions to the Scripting library, completing all 10 types.

**Independent Test**: Run Scripting tests and verify all 10 builders exist in surface area + new builder tests pass.

### Tests for User Story 4

- [x] T036 [P] [US4] Create tests/PhysicsSandbox.Scripting.Tests/ConstraintBuilderTests.fs: test each of the 6 new builders returns a valid SimulationCommand with correct constraint type set in the oneof
- [x] T037 [US4] Add ConstraintBuilderTests.fs to tests/PhysicsSandbox.Scripting.Tests/PhysicsSandbox.Scripting.Tests.fsproj `<Compile Include>` list in correct order

### Implementation for User Story 4

- [x] T038 [US4] Add `defaultMotor` helper function in src/PhysicsSandbox.Scripting/ConstraintBuilders.fs: creates MotorConfig with maxForce=1000.0, damping=1.0 (analogous to existing `defaultSpring`)
- [x] T039 [US4] Add `makeDistanceSpringCmd` builder in src/PhysicsSandbox.Scripting/ConstraintBuilders.fs: parameters id, bodyA, bodyB, offsetA:(float*float*float), offsetB:(float*float*float), targetDistance:float. Uses defaultSpring, toVec3.
- [x] T040 [US4] Add `makeSwingLimitCmd` builder in src/PhysicsSandbox.Scripting/ConstraintBuilders.fs: parameters id, bodyA, bodyB, axisA:(float*float*float), axisB:(float*float*float), maxAngle:float. Uses defaultSpring, toVec3.
- [x] T041 [US4] Add `makeTwistLimitCmd` builder in src/PhysicsSandbox.Scripting/ConstraintBuilders.fs: parameters id, bodyA, bodyB, axisA:(float*float*float), axisB:(float*float*float), minAngle:float, maxAngle:float. Uses defaultSpring, toVec3.
- [x] T042 [US4] Add `makeLinearAxisMotorCmd` builder in src/PhysicsSandbox.Scripting/ConstraintBuilders.fs: parameters id, bodyA, bodyB, offsetA, offsetB, axis:(float*float*float), targetVelocity:float, maxForce:float. Uses defaultMotor, toVec3.
- [x] T043 [US4] Add `makeAngularMotorCmd` builder in src/PhysicsSandbox.Scripting/ConstraintBuilders.fs: parameters id, bodyA, bodyB, targetVelocity:(float*float*float), maxForce:float. Uses defaultMotor, toVec3 for target velocity vector.
- [x] T044 [US4] Add `makePointOnLineCmd` builder in src/PhysicsSandbox.Scripting/ConstraintBuilders.fs: parameters id, bodyA, bodyB, origin:(float*float*float), direction:(float*float*float), offset:(float*float*float). Uses defaultSpring, toVec3.
- [x] T045 [US4] Update src/PhysicsSandbox.Scripting/ConstraintBuilders.fsi: add signatures for `defaultMotor`, `makeDistanceSpringCmd`, `makeSwingLimitCmd`, `makeTwistLimitCmd`, `makeLinearAxisMotorCmd`, `makeAngularMotorCmd`, `makePointOnLineCmd`
- [x] T046 [US4] Update surface area baseline in tests/PhysicsSandbox.Scripting.Tests/SurfaceAreaTests.fs to include the 7 new public functions (6 builders + defaultMotor)

**Checkpoint**: `dotnet test tests/PhysicsSandbox.Scripting.Tests/ -p:StrideCompilerSkipBuild=true` passes including new ConstraintBuilderTests and updated surface area.

---

## Phase 6: User Story 5 - Extract Shared Test Helpers (Priority: P5)

**Goal**: Consolidate duplicated test helpers into shared locations (F# shared source file + C# IntegrationTestHelpers.cs).

**Independent Test**: Run full test suite and verify all tests pass with zero duplicated helper code remaining.

### Implementation for User Story 5 — F# Helpers

- [x] T047 [US5] Add `<Compile Include="../SharedTestHelpers.fs" Link="SharedTestHelpers.fs" />` to tests/PhysicsServer.Tests/PhysicsServer.Tests.fsproj (before first test file)
- [x] T048 [P] [US5] Add `<Compile Include="../SharedTestHelpers.fs" Link="SharedTestHelpers.fs" />` to tests/PhysicsSimulation.Tests/PhysicsSimulation.Tests.fsproj
- [x] T049 [P] [US5] Add `<Compile Include="../SharedTestHelpers.fs" Link="SharedTestHelpers.fs" />` to tests/PhysicsViewer.Tests/PhysicsViewer.Tests.fsproj
- [x] T050 [P] [US5] Add `<Compile Include="../SharedTestHelpers.fs" Link="SharedTestHelpers.fs" />` to tests/PhysicsClient.Tests/PhysicsClient.Tests.fsproj
- [x] T051 [P] [US5] Add `<Compile Include="../SharedTestHelpers.fs" Link="SharedTestHelpers.fs" />` to tests/PhysicsSandbox.Mcp.Tests/PhysicsSandbox.Mcp.Tests.fsproj
- [x] T052 [P] [US5] Add `<Compile Include="../SharedTestHelpers.fs" Link="SharedTestHelpers.fs" />` to tests/PhysicsSandbox.Scripting.Tests/PhysicsSandbox.Scripting.Tests.fsproj
- [x] T053 [US5] Remove local `getPublicMembers` from tests/PhysicsServer.Tests/MeshCacheTests.fs, replace with `open TestHelpers`
- [x] T054 [P] [US5] Remove local `getPublicMembers` from tests/PhysicsSimulation.Tests/SurfaceAreaTests.fs, replace with `open TestHelpers`
- [x] T055 [P] [US5] Remove local `getPublicMembers` from tests/PhysicsViewer.Tests/SurfaceAreaTests.fs, replace with `open TestHelpers`
- [x] T056 [P] [US5] Remove local `getPublicMembers` and `assertContains` from tests/PhysicsClient.Tests/SurfaceAreaTests.fs, replace with `open TestHelpers`
- [x] T057 [P] [US5] Remove local `getPublicMembers` from tests/PhysicsSandbox.Mcp.Tests/SurfaceAreaTests.fs, replace with `open TestHelpers`
- [x] T058 [P] [US5] Remove local `getPublicMembers` and `assertContains` from tests/PhysicsSandbox.Scripting.Tests/SurfaceAreaTests.fs, replace with `open TestHelpers`

### Implementation for User Story 5 — C# Helpers

- [x] T059 [US5] Create tests/PhysicsSandbox.Integration.Tests/IntegrationTestHelpers.cs: extract `StartAppAndConnect()` (basic: build app, start, wait for server healthy, create GrpcChannel with SSL bypass), `StartAppAndConnectWithSimulation()` (adds simulation wait), `StartServerOnly()` (from ErrorConditionTests pattern)
- [x] T060 [US5] Update tests/PhysicsSandbox.Integration.Tests/ServerHubTests.cs: remove local StartAppAndConnect, use IntegrationTestHelpers.StartAppAndConnect()
- [x] T061 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/SimulationConnectionTests.cs: remove local StartAppAndConnect, use shared helper
- [x] T062 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/BatchIntegrationTests.cs: remove local StartAppAndConnect, use shared helper
- [x] T063 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/StressTestIntegrationTests.cs: remove local StartAppAndConnect, use shared helper
- [x] T064 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/MetricsIntegrationTests.cs: remove local StartAppAndConnect, use shared helper
- [x] T065 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/ComparisonIntegrationTests.cs: remove local StartAppAndConnect, use shared helper
- [x] T066 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/DiagnosticsIntegrationTests.cs: remove local StartAppAndConnect, use shared helper
- [x] T067 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/RestartIntegrationTests.cs: remove local StartAppAndConnect, use shared helper
- [x] T068 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/StaticBodyTests.cs: remove local StartAppAndConnect, use shared helper
- [x] T069 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/StateStreamingTests.cs: remove local StartAppAndConnect variant, use StartAppAndConnectWithSimulation()
- [x] T070 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/CommandRoutingTests.cs: remove local StartAppAndConnect variant, use StartAppAndConnectWithSimulation()
- [x] T071 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/MeshCacheIntegrationTests.cs: remove local StartAppAndConnect variant, use StartAppAndConnectWithSimulation()
- [x] T072 [P] [US5] Update tests/PhysicsSandbox.Integration.Tests/StateStreamOptimizationIntegrationTests.cs: remove local StartAppAndConnect variant, use StartAppAndConnectWithSimulation()
- [x] T073 [US5] Update tests/PhysicsSandbox.Integration.Tests/ErrorConditionTests.cs: remove local StartServerOnly and StartAppAndConnect, use shared helpers

**Checkpoint**: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` passes. Zero duplicated helper functions remain.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation.

- [x] T074 Run full test suite via `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` and verify all 362+ tests pass (plus new tests)
- [x] T075 Run `./test-progress.sh` end-to-end and verify progress output, ETA accuracy, and failure reporting against spec acceptance scenarios
- [x] T076 Update CLAUDE.md Recent Changes section with feature summary
- [x] T077 Update spec.md status from Draft to Complete

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — T001 creates shared file used by Phase 6
- **User Story 1 (Phase 2)**: No dependencies on other phases — standalone shell script
- **User Story 2 (Phase 3)**: No dependencies on other stories — PhysicsClient changes only
- **User Story 3 (Phase 4)**: No dependencies on other stories — PhysicsServer changes only
- **User Story 4 (Phase 5)**: No dependencies on other stories — Scripting library changes only
- **User Story 5 (Phase 6)**: Depends on T001 (shared file from Setup). Independent of US1-US4.
- **Polish (Phase 7)**: Depends on all phases complete

### User Story Independence

All 5 user stories touch different projects and can proceed in parallel:
- **US1**: `test-progress.sh` (new file, no project dependencies)
- **US2**: `src/PhysicsClient/` (Commands, Connection)
- **US3**: `src/PhysicsServer/Hub/` (MessageRouter)
- **US4**: `src/PhysicsSandbox.Scripting/` (ConstraintBuilders)
- **US5**: `tests/` only (shared helpers, no production code)

### Within Each User Story

- Tests written first (TDD per Constitution Principle VI)
- Implementation follows tests
- Surface area baselines updated last

### Parallel Opportunities

- **US1 through US4**: Fully parallelizable (different projects, different files)
- **US5 F# helpers** (T047-T058): All [P]-marked removals can run in parallel after project file updates
- **US5 C# helpers** (T060-T073): All [P]-marked file updates can run in parallel after T059 creates shared helper
- **US4 builders** (T039-T044): All 6 builders are [P] — different functions in same file, no interdependencies

---

## Parallel Example: User Story 4

```bash
# After T038 (defaultMotor helper), launch all 6 builders in parallel:
Task T039: "makeDistanceSpringCmd in ConstraintBuilders.fs"
Task T040: "makeSwingLimitCmd in ConstraintBuilders.fs"
Task T041: "makeTwistLimitCmd in ConstraintBuilders.fs"
Task T042: "makeLinearAxisMotorCmd in ConstraintBuilders.fs"
Task T043: "makeAngularMotorCmd in ConstraintBuilders.fs"
Task T044: "makePointOnLineCmd in ConstraintBuilders.fs"
```

## Parallel Example: User Story 5 (C# Helpers)

```bash
# After T059 (create IntegrationTestHelpers.cs), launch all file updates in parallel:
Task T060-T073: "Update each integration test file to use shared helpers"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: User Story 1 — test progress script (T003-T010)
3. **STOP and VALIDATE**: Run `./test-progress.sh` and verify output
4. This delivers immediate daily-use value

### Incremental Delivery

1. US1 (test progress) → immediate developer workflow improvement
2. US2 (silent failures) → correctness fix, prevents subtle bugs
3. US3 (query expiration) → stability fix, prevents memory leaks
4. US4 (constraint builders) → API completeness for scripting
5. US5 (shared helpers) → maintainability improvement

### Parallel Strategy

All 5 user stories can proceed simultaneously since they touch disjoint projects:
- US1: shell script (no .NET code)
- US2: PhysicsClient project
- US3: PhysicsServer project
- US4: Scripting project
- US5: test projects only

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Constitution Principle V: update .fsi files for all public API changes (US3, US4)
- Constitution Principle VI: tests before implementation (US2, US3, US4)
- Spec updated count from "7 instances" to "10 instances" per research findings
- Cache operations (MeshResolver, Session) get warnings, not Result.Error — per design decision D1
