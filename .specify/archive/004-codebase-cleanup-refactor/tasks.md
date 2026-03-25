# Tasks: Codebase Cleanup and Refactoring

**Input**: Design documents from `/specs/004-codebase-cleanup-refactor/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Not explicitly requested. Existing 468-test suite is the regression gate. Surface area baseline tests updated where module structure changes.

**Organization**: Tasks grouped by user story. Each story is independently verifiable by running the full test suite after completion.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Baseline measurements and preparation

- [x] T00- [ ] T001 Record baseline line counts for all files in `src/` over 300 lines using `wc -l` and save to `specs/004-codebase-cleanup-refactor/baseline-metrics.txt`
- [x] T00- [ ] T002 Run full test suite (`dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`) and confirm all 468 tests pass as baseline

**Checkpoint**: Baseline established — refactoring can begin

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No foundational tasks needed — each user story is independently executable against the current codebase. Stories do not share new infrastructure.

**Checkpoint**: Foundation ready — user story implementation can begin

---

## Phase 3: User Story 1 - Eliminate Duplicate Utility Code (Priority: P1) — MVP

**Goal**: Consolidate all duplicate conversion functions, MeshResolver modules, and ID generators into canonical locations.

**Independent Test**: Run `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — all 468 tests pass. Grep for duplicate function names confirms single canonical definitions.

### PhysicsSimulation Vector Conversion Consolidation

- [x] T00- [ ] T003 [US1] Create `src/PhysicsSimulation/Conversions/ProtoConversions.fsi` with signatures for `toVector3`, `fromVector3`, `toQuaternion`, `fromQuaternion` and proto building functions (`buildBodyProto`, `buildConstraintStateProto`, `buildState`, `buildTickState`)
- [x] T00- [ ] T004 [US1] Create `src/PhysicsSimulation/Conversions/ProtoConversions.fs` extracting conversion functions from `src/PhysicsSimulation/World/SimulationWorld.fs` (lines 1-157) including type aliases for proto name conflicts
- [x] T00- [ ] T005 [US1] Update `src/PhysicsSimulation/PhysicsSimulation.fsproj` to add ProtoConversions.fsi/fs BEFORE SimulationWorld.fsi/fs in compilation order
- [x] T00- [ ] T006 [US1] Update `src/PhysicsSimulation/World/SimulationWorld.fs` to `open` ProtoConversions module and remove extracted functions (toVector3, fromVector3, toQuaternion, fromQuaternion, buildBodyProto, buildConstraintStateProto, buildState, buildTickState, type aliases)
- [x] T00- [ ] T007 [US1] Update `src/PhysicsSimulation/Queries/QueryHandler.fs` to `open` ProtoConversions module and remove local toVector3, fromVector3, toQuaternion functions (lines 8-21)
- [x] T00- [ ] T008 [US1] Update `src/PhysicsSimulation/World/SimulationWorld.fsi` to reflect any signature changes from extraction
- [x] T00- [ ] T009 [US1] Build PhysicsSimulation project and run `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` to verify zero regressions

### PhysicsViewer Vector Conversion Consolidation

- [x] T0- [ ] T010 [P] [US1] Create `src/PhysicsViewer/Rendering/ProtoConversions.fsi` with signatures for `protoVec3ToStride` and `protoQuatToStride`
- [x] T0- [ ] T011 [P] [US1] Create `src/PhysicsViewer/Rendering/ProtoConversions.fs` extracting the `protoVec3ToStride` and `protoQuatToStride` functions (with null checks) from `src/PhysicsViewer/Rendering/DebugRenderer.fs`
- [x] T0- [ ] T012 [US1] Update `src/PhysicsViewer/PhysicsViewer.fsproj` to add ProtoConversions.fsi/fs BEFORE CameraController, DebugRenderer, and SceneManager in compilation order
- [x] T0- [ ] T013 [US1] Update `src/PhysicsViewer/Rendering/CameraController.fs` to `open` ProtoConversions and remove local `protoVec3ToStride`
- [x] T0- [ ] T014 [P] [US1] Update `src/PhysicsViewer/Rendering/DebugRenderer.fs` to `open` ProtoConversions and remove local `protoVec3ToStride` and `protoQuatToStride`
- [x] T0- [ ] T015 [P] [US1] Update `src/PhysicsViewer/Rendering/SceneManager.fs` to `open` ProtoConversions and remove local `protoVec3ToStride`
- [x] T0- [ ] T016 [US1] Build PhysicsViewer project and run its unit tests to verify zero regressions

### PhysicsClient toVec3 Consolidation

- [x] T0- [ ] T017 [US1] Move `toVec3` from `src/PhysicsSandbox.Scripting/Vec3Builders.fs` to a new internal utility in `src/PhysicsClient/Utilities/Vec3Helpers.fsi` and `src/PhysicsClient/Utilities/Vec3Helpers.fs` (PhysicsClient is the lower-level library; Scripting depends on it)
- [x] T0- [ ] T018 [US1] Update `src/PhysicsClient/PhysicsClient.fsproj` to add Vec3Helpers.fsi/fs BEFORE SimulationCommands in compilation order
- [x] T0- [ ] T019 [US1] Update `src/PhysicsClient/Commands/SimulationCommands.fs` to use Vec3Helpers.toVec3 instead of the internal toVec3 definition; remove the `internal toVec3` function
- [x] T0- [ ] T020 [US1] Update `src/PhysicsClient/Commands/SimulationCommands.fsi` to remove `toVec3` from internal signatures if present
- [x] T0- [ ] T021 [US1] Update `src/PhysicsSandbox.Scripting/Vec3Builders.fs` to delegate `toVec3` to `PhysicsClient.Vec3Helpers.toVec3` instead of defining its own copy
- [x] T0- [ ] T022 [US1] Update surface area baseline tests in `tests/PhysicsClient.Tests/` for the new Vec3Helpers public module (Constitution Principle V requires baselines for all public modules)
- [x] T0- [ ] T023 [US1] Build and run PhysicsClient + Scripting tests to verify zero regressions

### MCP MeshResolver Consolidation

- [x] T0- [ ] T024 [US1] Delete `src/PhysicsSandbox.Mcp/MeshResolver.fs` and `src/PhysicsSandbox.Mcp/MeshResolver.fsi`
- [x] T0- [ ] T025 [US1] Update `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` to remove MeshResolver.fsi/fs from compilation list
- [x] T0- [ ] T026 [US1] Update `src/PhysicsSandbox.Mcp/Program.fs` to use `PhysicsClient.MeshResolver` (available via transitive Scripting dependency) — update type references from `PhysicsSandbox.Mcp.MeshResolver.MeshResolverState` to `PhysicsClient.MeshResolver.MeshResolverState`
- [x] T0- [ ] T027 [US1] Build and run MCP unit tests + integration tests to verify zero regressions

### MCP ID Generator Consolidation

- [x] T0- [ ] T028 [US1] Update `src/PhysicsSandbox.Mcp/SimulationTools.fs` to remove local `nextId` function and private counters dictionary; replace with `PhysicsClient.IdGenerator.nextId` (import via `open PhysicsClient.IdGenerator`)
- [x] T0- [ ] T029 [US1] Update `src/PhysicsSandbox.Mcp/GeneratorTools.fs` to remove local `genCount` counter and local ID generation; replace with `PhysicsClient.IdGenerator.nextId`
- [x] T030 [US1] Build and run full test suite to verify zero regressions

**Checkpoint**: All duplicate utility code eliminated. Run `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — all tests pass. Grep confirms single canonical definitions for toVec3, toVector3, fromVector3, toQuaternion, fromQuaternion, MeshResolver, nextId.

---

## Phase 4: User Story 2 - Reduce Shape-Building Boilerplate (Priority: P2)

**Goal**: Extract shape construction helpers and reduce per-shape boilerplate in SimulationCommands from ~577 to ~400 lines.

**Independent Test**: Run full test suite — all tests pass. Measure SimulationCommands.fs line count is under 430 lines.

### Shape Builders Module

- [x] T031 [US2] Create `src/PhysicsClient/Shapes/ShapeBuilders.fsi` with signatures for `mkSphere`, `mkBox`, `mkCapsule`, `mkCylinder`, `mkPlane`, `mkTriangle` (each returns `Shape`)
- [x] T032 [US2] Create `src/PhysicsClient/Shapes/ShapeBuilders.fs` implementing shape construction helpers — each function creates the proto Shape message with shape-specific fields
- [x] T033 [US2] Update `src/PhysicsClient/PhysicsClient.fsproj` to add ShapeBuilders.fsi/fs BEFORE SimulationCommands in compilation order

### SimulationCommands Refactoring

- [x] T034 [US2] Create `addGenericBody` higher-order function in `src/PhysicsClient/Commands/SimulationCommands.fs` that takes a `Shape`, position, mass, id, and body options — extracts the common AddBody→SimulationCommand→send→register pattern
- [x] T035 [US2] Refactor `addSphere` in `src/PhysicsClient/Commands/SimulationCommands.fs` to use `ShapeBuilders.mkSphere` + `addGenericBody`
- [x] T036 [US2] Refactor `addBox` in `src/PhysicsClient/Commands/SimulationCommands.fs` to use `ShapeBuilders.mkBox` + `addGenericBody`
- [x] T037 [US2] Refactor `addCapsule` in `src/PhysicsClient/Commands/SimulationCommands.fs` to use `ShapeBuilders.mkCapsule` + `addGenericBody`
- [x] T038 [US2] Refactor `addCylinder` in `src/PhysicsClient/Commands/SimulationCommands.fs` to use `ShapeBuilders.mkCylinder` + `addGenericBody`
- [x] T039 [US2] Refactor `addPlane` in `src/PhysicsClient/Commands/SimulationCommands.fs` to use `ShapeBuilders.mkPlane` + `addGenericBody`
- [x] T040 [US2] Update `src/PhysicsClient/Commands/SimulationCommands.fsi` to reflect any signature changes
- [x] T041 [US2] Build and run PhysicsClient tests to verify zero regressions

### MCP ClientAdapter Delegation

- [x] T042 [US2] Update `src/PhysicsSandbox.Mcp/ClientAdapter.fs` to use `PhysicsClient.ShapeBuilders` for shape construction instead of inline proto building
- [x] T043 [US2] Update `src/PhysicsSandbox.Mcp/SimulationTools.fs` to use `PhysicsClient.ShapeBuilders.mkSphere`, `mkBox`, etc. in the shape dispatch match expression
- [x] T044 [US2] Build and run full test suite to verify zero regressions

### Surface Area Baseline Updates

- [x] T045 [US2] Update surface area baseline tests in `tests/PhysicsClient.Tests/` if the ShapeBuilders module changes PhysicsClient's public API surface
- [x] T046 [US2] Run full test suite (`dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`) — all tests pass

**Checkpoint**: Shape-building boilerplate reduced. SimulationCommands.fs under 430 lines. All tests pass.

---

## Phase 5: User Story 3 - Simplify Large Modules (Priority: P3)

**Goal**: Split SimulationWorld.fs from 708 lines to ~400 lines by extracting ShapeConversion module.

**Independent Test**: Run full test suite — all tests pass. SimulationWorld.fs line count is under 450 lines. No `src/` file exceeds 550 lines.

### ShapeConversion Module Extraction

- [x] - [ ] T047 [US3] Create `src/PhysicsSimulation/Conversions/ShapeConversion.fsi` with signatures for `convertShape`, `convertConstraintType`, `toBepuMaterial`
- [x] - [ ] T048 [US3] Create `src/PhysicsSimulation/Conversions/ShapeConversion.fs` extracting `convertShape` (recursive shape parser for all 10 types), `convertConstraintType` (all 10 constraint types), and `toBepuMaterial` from `src/PhysicsSimulation/World/SimulationWorld.fs`
- [x] - [ ] T049 [US3] Update `src/PhysicsSimulation/PhysicsSimulation.fsproj` to add ShapeConversion.fsi/fs AFTER ProtoConversions and BEFORE SimulationWorld in compilation order
- [x] - [ ] T050 [US3] Update `src/PhysicsSimulation/World/SimulationWorld.fs` to `open` ShapeConversion module and remove extracted functions (`convertShape`, `convertConstraintType`, `toBepuMaterial`)
- [x] - [ ] T051 [US3] Update `src/PhysicsSimulation/World/SimulationWorld.fsi` if any public/internal signatures change
- [x] - [ ] T052 [US3] Build and run PhysicsSimulation tests to verify zero regressions

### Surface Area Verification

- [x] - [ ] T053 [US3] Update surface area baseline tests in `tests/PhysicsSimulation.Tests/` if module restructuring changes public API surface
- [x] - [ ] T054 [US3] Run full test suite (`dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`) — all tests pass
- [x] - [ ] T055 [US3] Verify no `src/` file exceeds 550 lines using `wc -l`

**Checkpoint**: SimulationWorld.fs under 450 lines. All tests pass. No src/ file over 550 lines.

---

## Phase 6: User Story 4 - Consolidate Integration Test Helpers (Priority: P4)

**Goal**: Extract duplicated gRPC channel creation from integration test helpers.

**Independent Test**: Run integration test suite — all 84 tests pass.

- [x] T056 [US4] Add private static `CreateGrpcChannel(DistributedApplication app)` method to `tests/PhysicsSandbox.Integration.Tests/IntegrationTestHelpers.cs` extracting the gRPC channel + SSL setup code
- [x] T057 [US4] Update `StartAppAndConnect()` in `tests/PhysicsSandbox.Integration.Tests/IntegrationTestHelpers.cs` to use `CreateGrpcChannel`
- [x] T058 [P] [US4] Update `StartAppAndConnectWithSimulation()` in `tests/PhysicsSandbox.Integration.Tests/IntegrationTestHelpers.cs` to use `CreateGrpcChannel`
- [x] T059 [P] [US4] Update `StartServerOnly()` in `tests/PhysicsSandbox.Integration.Tests/IntegrationTestHelpers.cs` to use `CreateGrpcChannel`
- [x] T060 [US4] Run integration tests (`dotnet test tests/PhysicsSandbox.Integration.Tests/ -p:StrideCompilerSkipBuild=true`) — all 84 tests pass

**Checkpoint**: Integration test helper duplication eliminated. All tests pass.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final verification and metrics

- [x] T061 Record final line counts for all modified files and compare against baseline from T001
- [x] T062 Run full test suite one final time (`dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`) confirming all 468 tests pass
- [x] T063 Verify success criteria: SC-001 (zero duplicates), SC-002 (10%+ line reduction in affected files), SC-003 (no src/ file over 550 lines), SC-004 (all tests pass), SC-005 (new shape type requires max 2 files: ShapeBuilders.fs + SimulationCommands.fs)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: N/A — no blocking prerequisites
- **US1 (Phase 3)**: Can start after Setup. Internal sub-groups (SimConversions, ViewerConversions, ClientToVec3, MCP MeshResolver, MCP IdGen) can run in parallel once T001-T002 complete
- **US2 (Phase 4)**: Depends on US1 T017-T023 (Vec3Helpers must exist before ShapeBuilders can use them)
- **US3 (Phase 5)**: Depends on US1 T003-T009 (ProtoConversions must exist before ShapeConversion can reference them)
- **US4 (Phase 6)**: Independent — can run in parallel with US2 or US3
- **Polish (Phase 7)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: No dependencies on other stories — MVP
- **US2 (P2)**: Depends on US1's Vec3Helpers (T017-T023) for toVec3 in ShapeBuilders
- **US3 (P3)**: Depends on US1's ProtoConversions (T003-T009) since ShapeConversion uses those conversions
- **US4 (P4)**: Fully independent — can run anytime after Setup

### Parallel Opportunities

Within US1:
- T010-T016 (Viewer conversions) can run in parallel with T003-T009 (Simulation conversions)
- T017-T023 (Client toVec3 + baseline) can run in parallel with T010-T016 (Viewer)
- T024-T027 (MCP MeshResolver) can run in parallel with T028-T030 (MCP IdGen)

Within US2:
- T037, T038, T039 (addCapsule, addCylinder, addPlane refactoring) run sequentially (same file)

Within US4:
- T058, T059 (two helper updates) can run in parallel after T056-T057

---

## Parallel Example: User Story 1

```text
# Track 1: PhysicsSimulation conversions
T003 → T004 → T005 → T006 → T007 → T008 → T009

# Track 2: PhysicsViewer conversions (parallel with Track 1)
T010 + T011 → T012 → T013 + T014 + T015 → T016

# Track 3: PhysicsClient toVec3 + baseline (parallel with Track 1 & 2)
T017 → T018 → T019 → T020 → T021 → T022 → T023

# Track 4: MCP consolidation (after Track 3 for IdGenerator access)
T024 → T025 → T026 → T027
T028 → T029 → T030
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (baseline metrics)
2. Complete Phase 3: US1 — Eliminate Duplicate Utility Code
3. **STOP and VALIDATE**: All 468 tests pass; grep confirms single canonical definitions
4. This alone delivers the highest-value cleanup

### Incremental Delivery

1. Setup → US1 → Validate (MVP — duplicate elimination)
2. Add US2 → Validate (shape boilerplate reduction)
3. Add US3 → Validate (large module splitting)
4. Add US4 → Validate (test helper cleanup)
5. Polish → Final metrics comparison

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Every phase ends with a test suite run as regression gate
- No new tests needed — existing 468-test suite covers all refactored behavior
- Surface area baseline tests may need updating if public module structure changes (T022, T045, T053)
- F# compilation order is critical — new .fsi/.fs files must be added BEFORE their consumers in .fsproj
- Commit after each phase checkpoint for safe rollback points
