# Tasks: Scripting Library NuGet Package

**Input**: Design documents from `/specs/004-scripting-nuget-package/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Tests**: Not explicitly requested. Existing tests (19 scripting + 42 integration) serve as regression validation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: No new project creation needed. Verify local NuGet feed exists and is configured.

- [x] T001 Verify local NuGet feed directory exists at `~/.local/share/nuget-local/` and NuGet.config at repo root includes `local` source

---

## Phase 2: Foundational (Layer 0 Packaging)

**Purpose**: Add packaging metadata to projects with no internal dependencies and publish them to the local feed. MUST complete before User Story 1 can proceed.

- [x] T002 [P] Add `IsPackable=true`, `Version=0.1.0`, and `PackageId=PhysicsSandbox.Shared.Contracts` to `src/PhysicsSandbox.Shared.Contracts/PhysicsSandbox.Shared.Contracts.csproj`
- [x] T003 [P] Add `IsPackable=true`, `Version=0.1.0`, `PackageId=PhysicsSandbox.ServiceDefaults` to `src/PhysicsSandbox.ServiceDefaults/PhysicsSandbox.ServiceDefaults.csproj` and set `IsAspireSharedProject=false`
- [x] T004 Pack and publish Shared.Contracts to local feed: `dotnet pack src/PhysicsSandbox.Shared.Contracts -c Release -p:NoWarn=NU5104 -o ~/.local/share/nuget-local/` (depends on T002)
- [x] T005 Pack and publish ServiceDefaults to local feed: `dotnet pack src/PhysicsSandbox.ServiceDefaults -c Release -p:NoWarn=NU5104 -o ~/.local/share/nuget-local/` (depends on T003)

**Checkpoint**: Layer 0 packages (Contracts 0.1.0, ServiceDefaults 0.1.0) are in the local NuGet feed.

---

## Phase 3: User Story 1 - Pack and Publish Libraries (Priority: P1)

**Goal**: Package PhysicsClient and PhysicsSandbox.Scripting as local NuGet packages with correct dependency declarations.

**Independent Test**: Verify all 4 .nupkg files exist in `~/.local/share/nuget-local/` and a test `dotnet restore` resolves PhysicsSandbox.Scripting with all transitive dependencies.

### Implementation for User Story 1

- [x] T006 [US1] In `src/PhysicsClient/PhysicsClient.fsproj`, replace ProjectReference to `PhysicsSandbox.Shared.Contracts` with `<PackageReference Include="PhysicsSandbox.Shared.Contracts" Version="0.1.0" />`
- [x] T007 [US1] In `src/PhysicsClient/PhysicsClient.fsproj`, replace ProjectReference to `PhysicsSandbox.ServiceDefaults` with `<PackageReference Include="PhysicsSandbox.ServiceDefaults" Version="0.1.0" />`
- [x] T008 [US1] Pack and publish PhysicsClient to local feed: `dotnet pack src/PhysicsClient -c Release -p:NoWarn=NU5104 -o ~/.local/share/nuget-local/` (depends on T006, T007)
- [x] T009 [US1] In `src/PhysicsSandbox.Scripting/PhysicsSandbox.Scripting.fsproj`, replace ProjectReference to `PhysicsClient` with `<PackageReference Include="PhysicsClient" Version="0.1.0" />`
- [x] T010 [US1] Pack and publish PhysicsSandbox.Scripting to local feed: `dotnet pack src/PhysicsSandbox.Scripting -c Release -p:NoWarn=NU5104 -o ~/.local/share/nuget-local/` (depends on T009)
- [x] T011 [US1] Verify all 4 packages exist in `~/.local/share/nuget-local/` with correct names and version 0.1.0

**Checkpoint**: All 4 NuGet packages are published to the local feed. Transitive dependency chain: Scripting → PhysicsClient → Contracts + ServiceDefaults.

---

## Phase 4: User Story 2 - Migrate In-Solution Project References (Priority: P2)

**Goal**: Consumer projects (MCP server, test project) switch from ProjectReference to PackageReference for PhysicsSandbox.Scripting.

**Independent Test**: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` succeeds and `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` passes all tests.

### Implementation for User Story 2

- [x] T012 [P] [US2] In `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`, replace ProjectReference to PhysicsSandbox.Scripting with `<PackageReference Include="PhysicsSandbox.Scripting" Version="0.1.0" />` and remove any now-transitive ProjectReferences to PhysicsClient, Shared.Contracts, or ServiceDefaults
- [x] T013 [P] [US2] In `tests/PhysicsSandbox.Scripting.Tests/PhysicsSandbox.Scripting.Tests.fsproj`, replace ProjectReference to PhysicsSandbox.Scripting with `<PackageReference Include="PhysicsSandbox.Scripting" Version="0.1.0" />`
- [x] T014 [US2] Build full solution: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` (depends on T012, T013)
- [x] T015 [US2] Run all tests: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` and verify all 19 scripting tests and 42 integration tests pass (depends on T014)

**Checkpoint**: Solution builds and all tests pass with PackageReferences. No ProjectReferences to PhysicsSandbox.Scripting remain in consumer projects.

---

## Phase 5: User Story 3 - Update All Script and Demo References (Priority: P3)

**Goal**: Replace all hardcoded DLL `#r` paths in F# scripts and demos with version-agnostic `#r "nuget: ..."` references.

**Independent Test**: Run `Scripting/scripts/HelloDrop.fsx` and verify it loads the scripting library from the NuGet package.

### Implementation for User Story 3

- [x] T016 [US3] Update `Scripting/scripts/Prelude.fsx`: replace `#r "../../src/PhysicsSandbox.Scripting/bin/Debug/net10.0/PhysicsSandbox.Scripting.dll"` with `#r "nuget: PhysicsSandbox.Scripting"`
- [x] T017 [P] [US3] Update `Scripting/demos/Prelude.fsx`: replace all hardcoded DLL `#r` directives (PhysicsClient.dll, PhysicsSandbox.Shared.Contracts.dll, PhysicsSandbox.ServiceDefaults.dll) with `#r "nuget: PhysicsClient"`, keep only non-transitive external `#r "nuget: ..."` refs (e.g., Spectre.Console), and fix `localhost:5000` → `localhost:5180`
- [x] T018 [P] [US3] Update `Scripting/demos/AutoRun.fsx`: same DLL-to-NuGet migration as T017 — replace hardcoded DLL paths with `#r "nuget: PhysicsClient"`, keep only non-transitive external refs, and fix `localhost:5000` → `localhost:5180`
- [x] T019 [US3] Verify no hardcoded DLL `#r` paths to packaged projects remain: grep for `#r "../../src/` across `Scripting/` directory

**Checkpoint**: All F# scripts and demos use `#r "nuget: ..."` references. No build-output DLL paths remain.

---

## Phase 6: User Story 4 - Fix Port Consistency (Priority: P4)

**Goal**: Replace all `localhost:5000` references with canonical port `localhost:5180` (HTTP) across all scripts, demos, service fallbacks, and config files.

**Independent Test**: `grep -r "localhost:5000" Scripting/ src/ .mcp.json` returns zero matches.

### Implementation for User Story 4

- [x] T020 [P] [US4] Fix port in `Scripting/demos/RunAll.fsx`: replace `localhost:5000` with `localhost:5180`
- [x] T021 [P] [US4] Fix port in `Scripting/demos/Demo11_BodyScaling.fsx`: replace `localhost:5000` with `localhost:5180`
- [x] T022 [P] [US4] Fix port in `Scripting/demos/Demo12_CollisionPit.fsx`: replace `localhost:5000` with `localhost:5180`
- [x] T023 [P] [US4] Fix port in `Scripting/demos/Demo13_ForceFrenzy.fsx`: replace `localhost:5000` with `localhost:5180`
- [x] T024 [P] [US4] Fix port in `Scripting/demos/Demo14_DominoCascade.fsx`: replace `localhost:5000` with `localhost:5180`
- [x] T025 [P] [US4] Fix port in `Scripting/demos/Demo15_Overload.fsx`: replace `localhost:5000` with `localhost:5180`
- [x] T026 [P] [US4] Fix port in `Scripting/demos_py/prelude.py`: replace `localhost:5000` with `localhost:5180`
- [x] T027 [P] [US4] Fix port in `Scripting/demos_py/auto_run.py`: replace `localhost:5000` with `localhost:5180`
- [x] T028 [P] [US4] Fix port in `Scripting/demos_py/run_all.py`: replace `localhost:5000` with `localhost:5180`
- [x] T029 [P] [US4] Fix port in `src/PhysicsClient/Program.fs`: replace fallback `http://localhost:5000` with `http://localhost:5180`
- [x] T030 [P] [US4] Fix port in `src/PhysicsViewer/Program.fs`: replace fallback `http://localhost:5000` with `http://localhost:5180`
- [x] T031 [P] [US4] Fix port in `src/PhysicsClient/PhysicsClient.fsx`: replace `http://localhost:5000` with `http://localhost:5180`
- [x] T032 [P] [US4] Fix port in `.mcp.json`: replace `http://localhost:5000/sse` with `http://localhost:5180/sse`
- [x] T033 [US4] Verify no `localhost:5000` references remain: `grep -r "localhost:5000" Scripting/ src/ .mcp.json` returns zero matches (note: `Prelude.fsx` and `AutoRun.fsx` port fixes already done in US3 T017/T018)

**Checkpoint**: All port references use canonical values (5180 HTTP, 7180 HTTPS). Zero `localhost:5000` matches remain.

---

## Phase 7: Polish & Verification

**Purpose**: Full regression verification across the entire solution after all changes.

- [x] T034 Build full solution: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [x] T035 Run all tests: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — verify all tests pass (depends on T034)
- [x] T036 Verify no hardcoded DLL `#r` paths to packaged projects remain in any `.fsx` file under `Scripting/`
- [x] T037 [P] Verify no `localhost:5000` references remain in scripts, source, or config files
- [x] T038 Update `CLAUDE.md` Recent Changes section with feature summary

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS User Story 1
- **User Story 1 (Phase 3)**: Depends on Foundational — BLOCKS User Stories 2 and 3
- **User Story 2 (Phase 4)**: Depends on User Story 1 (packages must exist in feed)
- **User Story 3 (Phase 5)**: Depends on User Story 1 (packages must exist in feed)
- **User Story 4 (Phase 6)**: Port fixes on `Prelude.fsx` and `AutoRun.fsx` are merged into US3 (T017/T018) to avoid file conflicts. Remaining US4 tasks can run in parallel with US2/US3
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational phase — Layer 0 packages must be in feed
- **User Story 2 (P2)**: Depends on US1 — all 4 packages must be in the local feed before consumers can switch
- **User Story 3 (P3)**: Depends on US1 — packages must be in the local feed for `#r "nuget: ..."` to resolve
- **User Story 4 (P4)**: Independent — port fixes have no dependency on NuGet packaging

### Parallel Opportunities

- T002/T003 can run in parallel (different project files)
- T004/T005 can run in parallel after their respective metadata tasks
- T006/T007 can be done together (same file)
- T012/T013 can run in parallel (different project files)
- T016/T017/T018 can run in parallel (different script files)
- All US4 port fix tasks (T020-T032) can run in parallel (all different files)
- US2 and US3 can start in parallel after US1 completes
- US4 can run in parallel with US2/US3 (but Prelude.fsx/AutoRun.fsx port fixes are merged into US3)

---

## Parallel Example: User Story 4

```bash
# All US4 port fix tasks can run simultaneously (different files):
Task: "Fix port in Scripting/demos/RunAll.fsx"
Task: "Fix port in Scripting/demos/Demo11-15*.fsx"
Task: "Fix port in Scripting/demos_py/prelude.py"
Task: "Fix port in src/PhysicsClient/Program.fs"
Task: "Fix port in src/PhysicsViewer/Program.fs"
Task: "Fix port in .mcp.json"
# Note: Prelude.fsx and AutoRun.fsx port fixes are in US3 (T017/T018)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational — pack Layer 0 (T002-T005)
3. Complete Phase 3: User Story 1 — pack Layer 1+2 (T006-T011)
4. **STOP and VALIDATE**: All 4 packages in local feed, transitive deps resolve
5. Proceed to remaining stories

### Incremental Delivery

1. Setup + Foundational → Layer 0 packages ready
2. User Story 1 → All 4 packages in feed (MVP!)
3. User Story 2 → Consumer projects migrated, solution builds and tests pass
4. User Story 3 → Scripts use NuGet references
5. User Story 4 → Port consistency fixed (can be done anytime)
6. Polish → Full verification

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Pack commands must run in dependency order: Contracts/ServiceDefaults → PhysicsClient → Scripting
- Use `-p:NoWarn=NU5104` flag on all pack commands (BepuFSharp convention)
- Version must be incremented on subsequent publishes to avoid NuGet cache staleness
- Service projects (PhysicsServer, PhysicsSimulation, PhysicsViewer) keep their ProjectReferences unchanged
