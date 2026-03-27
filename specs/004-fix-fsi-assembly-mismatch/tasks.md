# Tasks: Fix FSI Assembly Version Mismatch

**Input**: Design documents from `/specs/004-fix-fsi-assembly-mismatch/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Tests**: Not explicitly requested. Verification is via existing test suite (`dotnet test`) and manual FSI script execution.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Version bumps and dependency changes to project files

- [x] T001 [P] Add explicit PackageReference for Microsoft.Extensions.Logging.Abstractions Version="10.*" and bump Version from 0.4.0 to 0.5.0 in src/PhysicsClient/PhysicsClient.fsproj
- [x] T002 [P] Bump Version from 0.4.0 to 0.5.0 in src/PhysicsSandbox.Shared.Contracts/PhysicsSandbox.Shared.Contracts.csproj
- [x] T003 [P] Bump Version from 0.1.0 to 0.2.0 in src/PhysicsSandbox.ServiceDefaults/PhysicsSandbox.ServiceDefaults.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Verify the solution builds and existing tests pass before modifying scripts

**⚠️ CRITICAL**: No script updates can proceed until the build is verified

- [x] T004 Run `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` and verify no build errors from the dependency change
- [x] T005 Run `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` and verify all existing tests pass (no regressions from FR-005)

**Checkpoint**: Build and tests green — script updates can proceed

---

## Phase 3: User Story 1 - Demo Scripts Run Without Assembly Errors (Priority: P1) 🎯 MVP

**Goal**: PhysicsClient NuGet package declares correct dependency versions so FSI resolves assemblies compatible with .NET 10

**Independent Test**: Pack PhysicsClient, reference it in a minimal FSI script, and verify it loads without FileNotFoundException

### Implementation for User Story 1

- [x] T006 [US1] Pack PhysicsClient into local NuGet feed by running `dotnet pack src/PhysicsClient -c Release -o ~/.local/share/nuget-local/` and verify the .nupkg contains Microsoft.Extensions.Logging.Abstractions >= 10.0.0 dependency
- [x] T007 [US1] Verify FSI can load the new PhysicsClient 0.5.0 package without assembly errors by running a minimal test script: `dotnet fsi --exec -e '#r "nuget: PhysicsClient, 0.5.0"'` (or equivalent quick smoke test)

**Checkpoint**: PhysicsClient 0.5.0 NuGet package resolves correctly in FSI on .NET 10

---

## Phase 4: User Story 2 - Prelude.fsx Works Without Manual Dependency Pinning (Priority: P2)

**Goal**: Clean up Prelude.fsx and all script references so developers don't need to manually pin transitive dependencies

**Independent Test**: Load Prelude.fsx in a minimal script and verify it works without extra `#r` directives

### Implementation for User Story 2

- [x] T008 [US2] Remove `#r "nuget: Microsoft.Extensions.Logging.Abstractions"` (line 5) and update `#r "nuget: PhysicsClient, 0.4.0"` to `#r "nuget: PhysicsClient, 0.5.0"` in Scripting/demos/Prelude.fsx
- [x] T009 [US2] Update PhysicsClient version reference in Scripting/scripts/PhysicsClient.fsx (if it pins a version) — N/A, no version-pinned reference
- [x] T010 [US2] Search for any other .fsx files that reference `PhysicsClient, 0.4.0` or `Microsoft.Extensions.Logging.Abstractions` and update them — none found

**Checkpoint**: Prelude.fsx and all scripts reference PhysicsClient 0.5.0 with no manual transitive dependency pins

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Documentation updates and final validation

- [x] T011 [P] Update CLAUDE.md PhysicsClient NuGet version references from 0.4.0 to 0.5.0
- [x] T012 [P] Pack all dependent packages for container consistency: run `dotnet pack src/PhysicsSandbox.ServiceDefaults -c Release -o ~/.local/share/nuget-local/ && dotnet pack src/PhysicsSandbox.Shared.Contracts -c Release -o ~/.local/share/nuget-local/` to ensure Containerfile pack sequence will work
- [x] T013 Run quickstart.md validation: start AppHost, execute one demo script (e.g., `dotnet fsi Scripting/demos/001-basic-physics.fsx`), verify no assembly errors

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — T001, T002, T003 can all run in parallel
- **Foundational (Phase 2)**: Depends on Phase 1 completion — T004 then T005 sequentially
- **User Story 1 (Phase 3)**: Depends on Phase 2 (build + tests green) — T006 then T007 sequentially
- **User Story 2 (Phase 4)**: Depends on Phase 3 (PhysicsClient 0.5.0 packaged and verified) — T008, T009, T010 sequentially
- **Polish (Phase 5)**: T011 can start after Phase 1; T012 after Phase 3; T013 after Phase 4

### User Story Dependencies

- **User Story 1 (P1)**: Independent — only needs project file changes from Phase 1
- **User Story 2 (P2)**: Depends on US1 (needs PhysicsClient 0.5.0 package to exist before updating script references)

### Parallel Opportunities

- T001, T002, T003 (all Phase 1 project file edits — different files)
- T011 can run in parallel with Phase 3-4 work (documentation only)
- T012 can run in parallel with Phase 4 work (packing, independent of scripts)

---

## Parallel Example: Phase 1

```bash
# Launch all project file edits together:
Task: "Add PackageReference + bump version in src/PhysicsClient/PhysicsClient.fsproj"
Task: "Bump version in src/PhysicsSandbox.Shared.Contracts/PhysicsSandbox.Shared.Contracts.csproj"
Task: "Bump version in src/PhysicsSandbox.ServiceDefaults/PhysicsSandbox.ServiceDefaults.csproj"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Project file changes (3 parallel edits)
2. Complete Phase 2: Build + test verification
3. Complete Phase 3: Pack and verify FSI loads PhysicsClient 0.5.0
4. **STOP and VALIDATE**: Confirm FSI resolves all assemblies correctly
5. This alone fixes the root cause (FR-001, FR-004)

### Full Delivery

1. Phase 1 → Phase 2 → Phase 3 → MVP verified
2. Phase 4 → Script cleanup (FR-003)
3. Phase 5 → Documentation + end-to-end validation (SC-001 through SC-004)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US2 depends on US1 because Prelude.fsx must reference the new 0.5.0 package
- No new .fsi files needed (no public API changes)
- No new tests needed (existing test suite covers regression; FSI verification is manual)
- Commit after each phase for clean git history
