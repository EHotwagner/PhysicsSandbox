# Tasks: F# Scripting Library

**Input**: Design documents from `/specs/004-fsharp-scripting-library/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the library project, test project, and register them in the solution.

- [x] T001 Create F# class library project file with PhysicsClient project reference, version 0.1.0, OutputType Library, TargetFramework net10.0 in src/PhysicsSandbox.Scripting/PhysicsSandbox.Scripting.fsproj
- [x] T002 Add PhysicsSandbox.Scripting project to solution file PhysicsSandbox.slnx under the src/ folder
- [x] T003 Create F# test project with xUnit dependencies and project reference to PhysicsSandbox.Scripting in tests/PhysicsSandbox.Scripting.Tests/PhysicsSandbox.Scripting.Tests.fsproj
- [x] T004 Add PhysicsSandbox.Scripting.Tests project to solution file PhysicsSandbox.slnx under the tests/ folder
- [x] T005 Verify solution builds cleanly with `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Empty library and test projects compile within the solution.

---

## Phase 2: Foundational (Core Library Modules)

**Purpose**: Implement all library modules with .fsi signature files. These modules are the foundation for all user stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Signatures (.fsi contracts — define API surface first)

- [x] T006 [P] Create Helpers.fsi signature file declaring `ok`, `sleep`, `timed` in src/PhysicsSandbox.Scripting/Helpers.fsi
- [x] T007 [P] Create Vec3Builders.fsi signature file declaring `toVec3` in src/PhysicsSandbox.Scripting/Vec3Builders.fsi
- [x] T008 [P] Create CommandBuilders.fsi signature file declaring `makeSphereCmd`, `makeBoxCmd`, `makeImpulseCmd`, `makeTorqueCmd` in src/PhysicsSandbox.Scripting/CommandBuilders.fsi
- [x] T009 [P] Create BatchOperations.fsi signature file declaring `batchAdd` in src/PhysicsSandbox.Scripting/BatchOperations.fsi
- [x] T010 [P] Create SimulationLifecycle.fsi signature file declaring `resetSimulation`, `runFor`, `nextId` in src/PhysicsSandbox.Scripting/SimulationLifecycle.fsi
- [x] T011 [P] Create Prelude.fsi signature file with `[<AutoOpen>]` attribute re-exporting all functions from Helpers, Vec3Builders, CommandBuilders, BatchOperations, SimulationLifecycle in src/PhysicsSandbox.Scripting/Prelude.fsi

### Tests (write FIRST — must FAIL before implementation, per constitution TDD mandate)

- [x] T012 [P] Create HelpersTests.fs with tests: `ok` returns value on Ok, `ok` throws on Error, `timed` returns correct result and logs timing in tests/PhysicsSandbox.Scripting.Tests/HelpersTests.fs
- [x] T013 [P] Create Vec3BuildersTests.fs with test: `toVec3` maps tuple components to Vec3 X/Y/Z correctly in tests/PhysicsSandbox.Scripting.Tests/Vec3BuildersTests.fs
- [x] T014 [P] Create CommandBuildersTests.fs with tests: `makeSphereCmd` produces correct proto with sphere shape and position, `makeBoxCmd` produces correct proto with box halfExtents, `makeImpulseCmd` sets bodyId and impulse vector, `makeTorqueCmd` sets bodyId and torque vector in tests/PhysicsSandbox.Scripting.Tests/CommandBuildersTests.fs
- [x] T015 [P] Create surface-area baseline file listing all public types and members (one per line, sorted) in tests/PhysicsSandbox.Scripting.Tests/SurfaceAreaBaseline.txt
- [x] T016 [P] Create SurfaceAreaTests.fs verifying public API surface via reflection matches the baseline in SurfaceAreaBaseline.txt (all 6 modules, 12 functions) in tests/PhysicsSandbox.Scripting.Tests/SurfaceAreaTests.fs
- [x] T017 Update test .fsproj compile order to list all test files in tests/PhysicsSandbox.Scripting.Tests/PhysicsSandbox.Scripting.Tests.fsproj

### Implementation (make tests pass)

- [x] T018 [P] Create Helpers.fs implementing `ok` (Result unwrap with failwith), `sleep` (Thread.Sleep wrapper), `timed` (Stopwatch-based execution timer with label logging) in src/PhysicsSandbox.Scripting/Helpers.fs
- [x] T019 [P] Create Vec3Builders.fs implementing `toVec3` converting `(float * float * float)` to proto Vec3 in src/PhysicsSandbox.Scripting/Vec3Builders.fs
- [x] T020 [P] Create CommandBuilders.fs implementing all four command builders matching existing Prelude.fsx logic (proto Shape/AddBody/ApplyImpulse/ApplyTorque construction) in src/PhysicsSandbox.Scripting/CommandBuilders.fs
- [x] T021 [P] Create BatchOperations.fs implementing `batchAdd` with auto-chunking at 100 commands and failure logging, using PhysicsClient.SimulationCommands.batchCommands in src/PhysicsSandbox.Scripting/BatchOperations.fs
- [x] T022 [P] Create SimulationLifecycle.fs implementing `resetSimulation` (pause, reset/clearAll fallback, IdGenerator.reset, addPlane, setGravity, sleep 100), `runFor` (play, sleep, pause), `nextId` (IdGenerator.nextId delegate) in src/PhysicsSandbox.Scripting/SimulationLifecycle.fs
- [x] T023 [P] Create Prelude.fs with `[<AutoOpen>]` module re-exporting all functions in src/PhysicsSandbox.Scripting/Prelude.fs
- [x] T024 Update .fsproj compile order to list files: Helpers.fsi, Helpers.fs, Vec3Builders.fsi, Vec3Builders.fs, CommandBuilders.fsi, CommandBuilders.fs, BatchOperations.fsi, BatchOperations.fs, SimulationLifecycle.fsi, SimulationLifecycle.fs, Prelude.fsi, Prelude.fs in src/PhysicsSandbox.Scripting/PhysicsSandbox.Scripting.fsproj
- [x] T025 Verify library builds with `dotnet build src/PhysicsSandbox.Scripting/PhysicsSandbox.Scripting.fsproj`
- [x] T026 Verify all tests pass with `dotnet test tests/PhysicsSandbox.Scripting.Tests/PhysicsSandbox.Scripting.Tests.fsproj`

**Checkpoint**: All 6 library modules compile with .fsi signatures; all unit and surface area tests pass.

---

## Phase 3: User Story 1 - Use Library Functions in Scripts (Priority: P1) 🎯 MVP

**Goal**: A script author can reference the scripting library DLL and immediately use all Prelude functions without additional gRPC/Protobuf package directives.

**Independent Test**: Write a minimal .fsx script that references only PhysicsSandbox.Scripting.dll, creates a session, builds commands, and calls batch/lifecycle functions.

### Implementation for User Story 1

- [x] T027 [US1] Create scripts/Prelude.fsx with single `#r` directive to `../src/PhysicsSandbox.Scripting/bin/Debug/net10.0/PhysicsSandbox.Scripting.dll` and open statements for PhysicsClient.Session, PhysicsClient.SimulationCommands, PhysicsSandbox.Scripting.Prelude in scripts/Prelude.fsx
- [x] T028 [US1] Create scripts/HelloDrop.fsx as a minimal validation script that loads Prelude.fsx, connects, calls resetSimulation, makeSphereCmd, batchAdd, runFor, and disconnects — proving all library functions work from a script in scripts/HelloDrop.fsx
- [x] T029 [US1] Verify that scripts/HelloDrop.fsx references only PhysicsSandbox.Scripting.dll (no direct gRPC or Protobuf nuget directives) and that all 12 Prelude functions are accessible (FR-001, FR-002, FR-008)

**Checkpoint**: A script using only the library DLL can access all Prelude functions. MVP complete.

---

## Phase 4: User Story 2 - Experiment in Scratch Folder (Priority: P2)

**Goal**: Developers can experiment freely in scratch/ and promote successful scripts to scripts/ without code changes.

**Independent Test**: Create a scratch script, run it, move it to scripts/, run it again unchanged.

### Implementation for User Story 2

- [x] T030 [P] [US2] Create scratch/ directory with .gitkeep file at repo root in scratch/.gitkeep
- [x] T031 [P] [US2] Add `scratch/*` and `!scratch/.gitkeep` patterns to .gitignore
- [x] T032 [US2] Create scratch/Prelude.fsx identical to scripts/Prelude.fsx (same relative path pattern to library DLL) verifying FR-009 path compatibility in scratch/Prelude.fsx
- [x] T033 [US2] Verify that a script created in scratch/ using `#load "Prelude.fsx"` works, and that the same script moved to scripts/ works without modification (FR-009)

**Checkpoint**: Scratch and scripts folders are set up. Scripts are portable between them.

---

## Phase 5: User Story 3 - MCP Server Uses Library Functions (Priority: P2)

**Goal**: The MCP server references the scripting library and uses shared helper functions, eliminating duplicated code.

**Independent Test**: Add project reference, replace `toVec3` in ClientAdapter with the library's version, verify MCP server builds and existing tests pass.

### Implementation for User Story 3

- [x] T034 [US3] Add project reference to PhysicsSandbox.Scripting in src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj
- [x] T035 [US3] Replace duplicated `toVec3` implementation in src/PhysicsSandbox.Mcp/ClientAdapter.fs with call to PhysicsSandbox.Scripting.Vec3Builders.toVec3, updating ClientAdapter.fsi if needed
- [x] T036 [US3] Verify MCP server builds and all existing integration tests pass with `dotnet test tests/PhysicsSandbox.Integration.Tests/ -p:StrideCompilerSkipBuild=true`

**Checkpoint**: MCP server uses shared library functions. No duplicated toVec3 code.

---

## Phase 6: User Story 4 - Extend the Library with New Functions (Priority: P3)

**Goal**: Validate that adding a new function to the library is straightforward — requires only 2 files (implementation + signature) and doesn't break existing consumers.

**Independent Test**: Add a test function, rebuild, verify existing scripts and MCP server still compile.

### Implementation for User Story 4

- [x] T037 [US4] Validate extensibility by confirming the module structure supports adding a new function: add a simple utility function (e.g., `toTuple` converting Vec3 back to tuple) to Vec3Builders.fsi and Vec3Builders.fs, verify it compiles and is accessible from scripts without changes to other modules (SC-004)
- [x] T038 [US4] Update SurfaceAreaBaseline.txt and SurfaceAreaTests.fs to include the new function, verify all tests still pass in tests/PhysicsSandbox.Scripting.Tests/

**Checkpoint**: Extensibility validated. Adding functions requires only .fsi + .fs changes.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, CI integration, and cleanup.

- [x] T039 Verify full solution builds with `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [x] T040 Verify full test suite passes with `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [x] T041 Verify library is packable with `dotnet pack src/PhysicsSandbox.Scripting/ --no-build` (constitution engineering constraint)
- [x] T042 Validate quickstart.md scenario: create a fresh script in scratch/, run it, move to scripts/, run again unchanged

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 — MVP delivery point
- **User Story 2 (Phase 4)**: Depends on Phase 2 — can run in parallel with US1 and US3
- **User Story 3 (Phase 5)**: Depends on Phase 2 — can run in parallel with US1 and US2
- **User Story 4 (Phase 6)**: Depends on Phase 2 — can run in parallel with others but logically after US1
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational only. No cross-story dependencies.
- **US2 (P2)**: Depends on Foundational only. Uses same Prelude.fsx pattern as US1 but can be created independently.
- **US3 (P2)**: Depends on Foundational only. MCP integration is independent of script folder setup.
- **US4 (P3)**: Depends on Foundational only. Extensibility validation can use any module.

### Parallel Opportunities

Within Phase 2, all signature tasks (T006-T011) can be written in parallel. Then all test tasks (T012-T016) can be written in parallel. Then all implementation tasks (T018-T023) can be written in parallel. This follows the TDD flow: signatures → tests (fail) → implementations (pass).

After Phase 2, all four user stories can proceed in parallel:
- US1 (scripts/Prelude.fsx + validation script)
- US2 (scratch folder + gitignore)
- US3 (MCP project reference + ClientAdapter update)
- US4 (extensibility validation)

---

## Parallel Example: Foundational Phase

```bash
# Step 1: Launch all signatures in parallel:
Task: "Create Helpers.fsi"
Task: "Create Vec3Builders.fsi"
Task: "Create CommandBuilders.fsi"
Task: "Create BatchOperations.fsi"
Task: "Create SimulationLifecycle.fsi"
Task: "Create Prelude.fsi"

# Step 2: Launch all tests in parallel (must FAIL before implementation):
Task: "Create HelpersTests.fs"
Task: "Create Vec3BuildersTests.fs"
Task: "Create CommandBuildersTests.fs"
Task: "Create SurfaceAreaBaseline.txt + SurfaceAreaTests.fs"

# Step 3: Launch all implementations in parallel (make tests pass):
Task: "Create Helpers.fs"
Task: "Create Vec3Builders.fs"
Task: "Create CommandBuilders.fs"
Task: "Create BatchOperations.fs"
Task: "Create SimulationLifecycle.fs"
Task: "Create Prelude.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T005)
2. Complete Phase 2: Foundational — signatures → tests → implementations (T006-T026)
3. Complete Phase 3: User Story 1 — scripts/Prelude.fsx + validation (T027-T029)
4. **STOP and VALIDATE**: Run HelloDrop.fsx against running sandbox
5. MVP delivered — scripts can use the library

### Incremental Delivery

1. Setup + Foundational → Library compiles with full API surface
2. Add US1 → Script validation proves library works (MVP!)
3. Add US2 → Scratch/scripts folders ready for experimentation
4. Add US3 → MCP server shares code with scripts
5. Add US4 → Extensibility validated
6. Polish → Full suite green, quickstart validated

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- The library is a thin wrapper — most implementations are direct ports from demos/Prelude.fsx
