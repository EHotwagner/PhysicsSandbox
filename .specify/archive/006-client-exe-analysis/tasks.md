# Tasks: PhysicsClient Exe vs Library Conversion

**Input**: Design documents from `/specs/006-client-exe-analysis/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: Not explicitly requested. Existing test suite covers all library modules — no new tests needed since no public API changes.

**Organization**: Tasks grouped by user story. This is a small refactor (5 files modified, 1 deleted) so phases are compact.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No setup needed — this is a refactor of existing projects, not a new project.

*(No tasks)*

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No foundational infrastructure needed — all changes are direct file modifications.

*(No tasks)*

---

## Phase 3: User Story 1 - Script Author Uses PhysicsClient as Library (Priority: P1) MVP

**Goal**: Convert PhysicsClient from Exe to Library so it produces a clean DLL without a vestigial entry point.

**Independent Test**: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` succeeds, `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` passes all tests, and PhysicsClient produces a DLL (not an EXE).

### Implementation for User Story 1

- [x] T001 [US1] Delete the no-op entry point file `src/PhysicsClient/Program.fs`
- [x] T002 [US1] Update `src/PhysicsClient/PhysicsClient.fsproj`: change `OutputType` from `Exe` to `Library`, remove `<Compile Include="Program.fs" />`, remove `ProjectReference` to `PhysicsSandbox.ServiceDefaults`, bump `Version` from `0.5.0` to `0.6.0`
- [x] T003 [US1] Verify solution builds: run `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` and confirm PhysicsClient output is a DLL
- [x] T004 [US1] Verify all tests pass: run `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` and confirm all 468+ tests pass with no regressions

**Checkpoint**: PhysicsClient is now a library. All existing consumers (Scripting, Tests) work unchanged.

---

## Phase 4: User Story 2 - Aspire Orchestrator No Longer Manages PhysicsClient (Priority: P2)

**Goal**: Remove PhysicsClient from AppHost orchestration since the process did no useful work.

**Independent Test**: `dotnet run --project src/PhysicsSandbox.AppHost` starts successfully with 4 resources (server, simulation, viewer, mcp) instead of 5. No errors in logs.

### Implementation for User Story 2

- [x] T005 [US2] Remove the PhysicsClient resource block (lines 15-17: `builder.AddProject<Projects.PhysicsClient>("client").WithReference(server).WaitFor(server);`) from `src/PhysicsSandbox.AppHost/AppHost.cs`
- [x] T006 [US2] Remove `<ProjectReference Include="..\PhysicsClient\PhysicsClient.fsproj" />` from `src/PhysicsSandbox.AppHost/PhysicsSandbox.AppHost.csproj`
- [x] T007 [US2] Verify AppHost builds: run `dotnet build src/PhysicsSandbox.AppHost` and confirm no compilation errors from removed PhysicsClient reference

**Checkpoint**: AppHost starts 4 services (server, simulation, viewer, mcp). No PhysicsClient process.

---

## Phase 5: User Story 3 - NuGet Repackaging (Priority: P3)

**Goal**: Produce a clean PhysicsClient 0.6.0 NuGet package and update demo script references.

**Independent Test**: F# demo scripts load `PhysicsClient 0.6.0` via NuGet and all modules are accessible.

### Implementation for User Story 3

- [x] T008 [US3] Pack PhysicsClient as NuGet 0.6.0: run `dotnet pack src/PhysicsClient -c Release -o ~/.local/share/nuget-local/`
- [x] T009 [US3] Update `Scripting/demos/Prelude.fsx`: change `#r "nuget: PhysicsClient, 0.5.0"` to `#r "nuget: PhysicsClient, 0.6.0"`
- [x] T010 [US3] Update any other files referencing PhysicsClient version 0.5.0 (search codebase for `PhysicsClient, 0.5.0` and `PhysicsClient 0.5.0` — update CLAUDE.md known issues section)

**Checkpoint**: Clean NuGet package without ServiceDefaults dependency. Demo scripts reference 0.6.0.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation updates.

- [x] T011 Run full solution build and test suite to confirm zero regressions: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true && dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 3 (US1)**: No dependencies — can start immediately
- **Phase 4 (US2)**: Depends on Phase 3 (AppHost needs PhysicsClient to be a Library before removing the project reference, since the `.csproj` reference will fail if OutputType is still Exe and Program.fs is deleted)
- **Phase 5 (US3)**: Depends on Phase 3 (NuGet pack requires Library output)
- **Phase 6 (Polish)**: Depends on Phases 3, 4, 5

### User Story Dependencies

- **User Story 1 (P1)**: Independent — convert project type
- **User Story 2 (P2)**: Depends on US1 completion (AppHost reference removal)
- **User Story 3 (P3)**: Depends on US1 completion (NuGet repack needs Library output)
- US2 and US3 can run in parallel after US1 completes

### Within Each User Story

- T001 before T002 (delete Program.fs before removing its Compile entry)
- T002 before T003 (fsproj changes before build verification)
- T005 before T006 (code change before project reference removal)
- T008 before T009 (pack before updating references)

### Parallel Opportunities

- T005 and T006 can run in parallel (different files)
- T009 and T010 can run in parallel (different files)
- US2 (Phase 4) and US3 (Phase 5) can run in parallel after US1 completes

---

## Parallel Example: After User Story 1

```bash
# After US1 completes, launch US2 and US3 in parallel:
# Stream 1 (US2): Remove from AppHost
Task T005: "Remove PhysicsClient resource from AppHost.cs"
Task T006: "Remove ProjectReference from AppHost.csproj"
Task T007: "Verify AppHost builds"

# Stream 2 (US3): Repackage NuGet
Task T008: "Pack PhysicsClient 0.6.0"
Task T009: "Update Prelude.fsx version"
Task T010: "Update other version references"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 3: Convert PhysicsClient to Library
2. **STOP and VALIDATE**: Build + test the entire solution
3. All library consumers work identically

### Incremental Delivery

1. US1: Convert to Library → Build + test passes (MVP)
2. US2: Remove from AppHost → AppHost starts with 4 services
3. US3: Repack NuGet 0.6.0 → Demo scripts updated
4. Polish: Final validation

---

## Notes

- Total tasks: 11
- US1: 4 tasks (core conversion)
- US2: 3 tasks (AppHost cleanup)
- US3: 3 tasks (NuGet repackage)
- Polish: 1 task (final validation)
- No new tests needed — existing 468+ tests cover all library modules
- Entire feature is a small, low-risk refactor with no public API changes
