# Tasks: Test Suite Cleanup

**Input**: Design documents from `/specs/004-test-suite-cleanup/`
**Prerequisites**: plan.md, spec.md, research.md, quickstart.md

**Tests**: No new tests are being written. This is a behavior-preserving restructuring — existing tests are moved/consolidated, not created or deleted (except true duplicates).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: Record baseline metrics and verify green suite before any changes

- [x] T001 Run full test suite and record baseline test count: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [x] T002 Record baseline file count per test project (expected: ~63 files, ~463 tests across 7 projects)

**Checkpoint**: Baseline recorded, all tests green. Ready to begin restructuring.

---

## Phase 2: Foundational — Shared Test Infrastructure (US3)

**Purpose**: Create shared helpers that US1 depends on. Satisfies US3 (Extract Shared Test Data Builders).

**⚠️ CRITICAL**: US1 duplicate elimination depends on these shared builders existing first.

- [x] T003 [US3] Add `assertModuleSurface` helper to `tests/SharedTestHelpers.fs` — takes a Type, module name string, and expected member list; calls `getPublicMembers` and `assertContains` for each member
- [x] T004 [US3] Create `tests/CommonTestBuilders.fs` with shared helpers extracted from duplicate locations: `makeBody` (id, isStatic → Body with Mass=0/1.0), `makeState` (bodies → SimulationState with Time=1.0, Running=true), `makeResolver` (unit → MeshResolver with Unchecked.defaultof client)
- [x] T005 [US3] Add `<Compile Include="../CommonTestBuilders.fs" Link="CommonTestBuilders.fs" />` to `tests/PhysicsServer.Tests/PhysicsServer.Tests.fsproj` — AFTER SharedTestHelpers.fs, BEFORE all test files
- [x] T006 [P] [US3] Add `<Compile Include="../CommonTestBuilders.fs" Link="CommonTestBuilders.fs" />` to `tests/PhysicsSimulation.Tests/PhysicsSimulation.Tests.fsproj` — AFTER SharedTestHelpers.fs, BEFORE all test files
- [x] T007 [P] [US3] Add `<Compile Include="../CommonTestBuilders.fs" Link="CommonTestBuilders.fs" />` to `tests/PhysicsClient.Tests/PhysicsClient.Tests.fsproj` — AFTER SharedTestHelpers.fs, BEFORE all test files
- [x] T008 [P] [US3] Add `<Compile Include="../CommonTestBuilders.fs" Link="CommonTestBuilders.fs" />` to `tests/PhysicsViewer.Tests/PhysicsViewer.Tests.fsproj` — AFTER SharedTestHelpers.fs, BEFORE all test files
- [x] T009 [US3] Build to verify compilation: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Shared infrastructure compiles. No test changes yet — all tests still pass unchanged.

---

## Phase 3: User Story 1 — Eliminate Duplicate Tests (Priority: P1) 🎯 MVP

**Goal**: Consolidate duplicate test helpers and simplify surface area test boilerplate so each behavior is tested once at the appropriate layer.

**Independent Test**: Run full test suite — same or fewer tests, all green. Shared builders used in 3+ files.

### Implementation for User Story 1

- [x] T010 [P] [US1] Update `tests/PhysicsServer.Tests/StateStreamOptimizationTests.fs` — remove local `makeBody` and `makeState` helpers, open `CommonTestBuilders` module, verify tests still reference the shared versions
- [x] T011 [P] [US1] Update `tests/PhysicsSimulation.Tests/StateDecompositionTests.fs` — remove local `makeBody` and `makeState` helpers, open `CommonTestBuilders` module, verify tests still reference the shared versions
- [x] T012 [US1] Update `tests/PhysicsClient.Tests/MeshResolverTests.fs` — remove local `makeResolver` helper, open `CommonTestBuilders`, and add the missing "duplicate processNewMeshes does not overwrite" test (cache idempotency test from PhysicsViewer version)
- [x] T013 [US1] Update `tests/PhysicsViewer.Tests/MeshResolverTests.fs` — remove local `makeResolver` helper, open `CommonTestBuilders`
- [x] T014 [P] [US1] Simplify `tests/PhysicsServer.Tests/SurfaceAreaTests.fs` — replace manual getPublicMembers/assertContains loops with `assertModuleSurface` calls (one per module: MeshCache)
- [x] T015 [P] [US1] Simplify `tests/PhysicsSimulation.Tests/SurfaceAreaTests.fs` — replace manual loops with `assertModuleSurface` calls (modules: SimulationWorld, CommandHandler, SimulationClient, MeshIdGenerator)
- [x] T016 [P] [US1] Simplify `tests/PhysicsViewer.Tests/SurfaceAreaTests.fs` — replace manual loops with `assertModuleSurface` calls (modules: SceneManager, ShapeGeometry, CameraController, ViewerClient, etc.)
- [x] T017 [P] [US1] Simplify `tests/PhysicsClient.Tests/SurfaceAreaTests.fs` — replace manual loops with `assertModuleSurface` calls (modules: IdGenerator, Session, SimulationCommands, etc.)
- [x] T018 [P] [US1] Simplify `tests/PhysicsSandbox.Mcp.Tests/SurfaceAreaTests.fs` — replace manual loops with `assertModuleSurface` calls (modules: MeshResolver, MeshFetchQueryTools)
- [x] T019 [P] [US1] Simplify `tests/PhysicsSandbox.Scripting.Tests/SurfaceAreaTests.fs` — replace manual loops with `assertModuleSurface` calls (preserve baseline file test if present)
- [x] T020 [US1] Run full test suite to verify zero regressions: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: All duplicate helpers consolidated. Surface area tests simplified. Shared builders used in 4+ files (StateStreamOptimization, StateDecomposition, MeshResolver×2). All tests green.

---

## Phase 4: User Story 2 — Consolidate Small Integration Test Files (Priority: P2)

**Goal**: Merge 5 single-test integration files into 3 existing domain-grouped files.

**Independent Test**: All integration tests pass. Zero single-test files remain. Total integration test count unchanged.

### Implementation for User Story 2

- [x] T021 [US2] Merge `tests/PhysicsSandbox.Integration.Tests/DiagnosticsIntegrationTests.cs` test method into `tests/PhysicsSandbox.Integration.Tests/MetricsIntegrationTests.cs` — add the GetMetrics test to the MetricsIntegrationTests class, preserving its test name and assertions
- [x] T022 [US2] Merge `tests/PhysicsSandbox.Integration.Tests/ComparisonIntegrationTests.cs` test method into `tests/PhysicsSandbox.Integration.Tests/BatchIntegrationTests.cs` — add the batch-vs-individual comparison test
- [x] T023 [US2] Merge `tests/PhysicsSandbox.Integration.Tests/StressTestIntegrationTests.cs` test method into `tests/PhysicsSandbox.Integration.Tests/BatchIntegrationTests.cs` — add the 50-body batch scaling test
- [x] T024 [US2] Merge `tests/PhysicsSandbox.Integration.Tests/StaticBodyTests.cs` test method into `tests/PhysicsSandbox.Integration.Tests/SimulationConnectionTests.cs` — add the static plane state test
- [x] T025 [US2] Merge `tests/PhysicsSandbox.Integration.Tests/RestartIntegrationTests.cs` test method into `tests/PhysicsSandbox.Integration.Tests/SimulationConnectionTests.cs` — add the reset-clears-bodies test
- [x] T026 [US2] Delete the 5 now-empty source files and remove their entries from `tests/PhysicsSandbox.Integration.Tests/PhysicsSandbox.Integration.Tests.csproj` (if explicitly listed; C# projects may use glob includes)
- [x] T027 [US2] Run integration tests to verify: `dotnet test tests/PhysicsSandbox.Integration.Tests -p:StrideCompilerSkipBuild=true`

**Checkpoint**: 5 single-test files eliminated. 3 target files each have ≤8 tests. All integration tests green.

---

## Phase 5: User Story 4 — Rebalance Oversized Test Files (Priority: P3)

**Goal**: Split 4 oversized test files (30-40+ tests) into focused files with ≤25 tests each.

**Independent Test**: All tests pass. No file exceeds 25 tests. Total test count unchanged.

### Implementation for User Story 4

- [x] T028 [US4] Split `tests/PhysicsViewer.Tests/SceneManagerTests.fs` — create `tests/PhysicsViewer.Tests/ShapeRenderingTests.fs` (primitiveType mapping, defaultColor tests, ~20 tests) and rename remaining to `tests/PhysicsViewer.Tests/SceneStateBehaviorTests.fs` (state application, narration, wireframe, ~20 tests). Update `tests/PhysicsViewer.Tests/PhysicsViewer.Tests.fsproj` compilation order.
- [x] T029 [US4] Split `tests/PhysicsSimulation.Tests/ExtendedFeatureTests.fs` — create `tests/PhysicsSimulation.Tests/ShapeConversionTests.fs` (~12 tests, lines 20-150), `tests/PhysicsSimulation.Tests/ConstraintTests.fs` (~12 tests, lines 150-250), and `tests/PhysicsSimulation.Tests/KinematicBodyTests.fs` (~12 tests, lines 250+). Update `tests/PhysicsSimulation.Tests/PhysicsSimulation.Tests.fsproj` compilation order.
- [x] T030 [P] [US4] Split `tests/PhysicsSimulation.Tests/SimulationWorldTests.fs` — create `tests/PhysicsSimulation.Tests/SimulationWorldBasicsTests.fs` (~15 tests, world lifecycle, lines 23-200) and `tests/PhysicsSimulation.Tests/SimulationWorldForcesTests.fs` (~15 tests, forces/queries, lines 200-428). Update .fsproj compilation order.
- [x] T031 [P] [US4] Split `tests/PhysicsViewer.Tests/CameraControllerTests.fs` — create `tests/PhysicsViewer.Tests/CameraBasicsTests.fs` (~10 tests, default/set/zoom, lines 12-100) and `tests/PhysicsViewer.Tests/CameraModeTests.fs` (~22 tests, orbiting/chasing/framing, lines 100-354). Update .fsproj compilation order.
- [x] T032 [US4] Run full test suite to verify zero regressions and no file >25 tests: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: All 4 oversized files split. Each new file ≤25 tests. Total test count unchanged. All tests green.

---

## Phase 6: Validation & Polish

**Purpose**: Final verification of all success criteria

- [x] T033 Run full test suite: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [x] T034 Verify SC-001: total test count within 5% of baseline (~463)
- [x] T035 Verify SC-003: no test file contains more than 25 tests (count [<Fact>] and [<Theory>] attributes per file)
- [x] T036 Verify SC-004: zero single-test integration files remain
- [x] T037 Verify SC-002: shared helper usage — CommonTestBuilders referenced in 4+ test files, assertModuleSurface used in 6 SurfaceAreaTests files

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational/US3 (Phase 2)**: Depends on Setup — BLOCKS US1
- **US1 (Phase 3)**: Depends on US3 (shared builders must exist)
- **US2 (Phase 4)**: Depends on Setup only — can run in parallel with US1
- **US4 (Phase 5)**: Depends on Setup only — can run in parallel with US1 and US2
- **Validation (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **US3 (P2)**: Foundational — must complete first (creates shared infrastructure)
- **US1 (P1)**: Depends on US3 — uses shared builders to replace duplicates
- **US2 (P2)**: Independent — C# integration files, no overlap with F# shared builders
- **US4 (P3)**: Independent — file splits don't depend on shared builders or integration merges

### Within Each User Story

- T010-T011 (makeBody/makeState consolidation) can run in parallel
- T012-T013 (MeshResolver consolidation) must be sequential (T012 adds test, T013 removes helper)
- T014-T019 (surface area simplification) can all run in parallel
- T021-T025 (integration merges) are sequential (each modifies a target file)
- T028-T031 (file splits) — T030 and T031 can run in parallel (different projects)

### Parallel Opportunities

```text
# After Phase 2 completes, these can run in parallel:

Stream A (US1): T010 + T011 in parallel → T012 → T013 → T014-T019 in parallel → T020
Stream B (US2): T021 → T022 → T023 → T024 → T025 → T026 → T027
Stream C (US4): T028 + T030 in parallel, T029 + T031 in parallel → T032

# Streams B and C are fully independent of Stream A
```

---

## Implementation Strategy

### MVP First (US3 + US1)

1. Complete Phase 1: Setup (record baseline)
2. Complete Phase 2: US3 (shared infrastructure)
3. Complete Phase 3: US1 (eliminate duplicates)
4. **STOP and VALIDATE**: All shared builders in use, surface area simplified, tests green
5. This delivers the highest-value improvement (duplicate elimination)

### Incremental Delivery

1. Setup + US3 → Shared infrastructure ready
2. US1 → Duplicate tests eliminated → Validate (MVP!)
3. US2 → Integration files consolidated → Validate
4. US4 → Oversized files rebalanced → Validate
5. Each phase adds structural improvement without breaking prior work

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- F# .fsproj compilation order is critical — SharedTestHelpers → CommonTestBuilders → test files
- No production code is modified — only test files
- Each phase should end with a `dotnet test` run to catch regressions early
- Surface area tests in PhysicsSandbox.Scripting.Tests may have a baseline file test — preserve it
