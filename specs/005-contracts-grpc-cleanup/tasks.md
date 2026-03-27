# Tasks: Contracts gRPC Package Cleanup

**Input**: Design documents from `/specs/005-contracts-grpc-cleanup/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Tests**: Not explicitly requested. Existing test suite (468 tests) serves as regression validation.

**Organization**: Tasks grouped by user story. This is a minimal feature — single file change with validation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: No setup needed — the project and all infrastructure already exist.

*(No tasks — existing project, no new files or directories)*

---

## Phase 2: User Story 1 - Leaner Contracts Package (Priority: P1) MVP

**Goal**: Replace `Grpc.AspNetCore` with `Grpc.Net.Client` in Shared.Contracts so the project no longer transitively pulls in the ASP.NET Core server stack.

**Independent Test**: Build the entire solution and run all 468 existing tests. Zero errors, zero regressions.

### Implementation for User Story 1

- [x] T001 [US1] Replace `Grpc.AspNetCore` package reference with `Grpc.Net.Client` in `src/PhysicsSandbox.Shared.Contracts/PhysicsSandbox.Shared.Contracts.csproj`
- [x] T002 [US1] Build the full solution (`dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`) and verify zero errors
- [x] T003 [US1] Run full test suite (`dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`) and verify all tests pass

**Checkpoint**: Solution builds cleanly and all tests pass with the narrower dependency.

---

## Phase 3: User Story 2 - NuGet Package Remains Publishable (Priority: P2)

**Goal**: Verify the Contracts NuGet package can still be packed and has correct dependencies listed.

**Independent Test**: Run `dotnet pack` on Shared.Contracts and inspect the .nupkg dependency list.

### Implementation for User Story 2

- [x] T004 [US2] Run `dotnet pack` on `src/PhysicsSandbox.Shared.Contracts/PhysicsSandbox.Shared.Contracts.csproj` and verify it produces a valid .nupkg
- [x] T005 [US2] Inspect the .nupkg to confirm it lists `Grpc.Net.Client` and `Google.Protobuf` as dependencies but not `Grpc.AspNetCore`

**Checkpoint**: NuGet package is valid with correct dependency metadata.

---

## Dependencies & Execution Order

### Phase Dependencies

- **User Story 1 (Phase 2)**: No prerequisites — can start immediately
- **User Story 2 (Phase 3)**: Depends on T001 (the actual file change from US1)
- T002 and T003 are sequential (build must pass before tests run)
- T004 and T005 are sequential (pack must succeed before inspection)

### User Story Dependencies

- **User Story 1 (P1)**: Independent — the core change + validation
- **User Story 2 (P2)**: Depends on T001 completing (needs the updated .csproj)

### Parallel Opportunities

- T004 can run in parallel with T002/T003 (packing vs building/testing are independent operations, both only need T001 complete)

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T001: Single-line .csproj edit
2. Complete T002: Full solution build
3. Complete T003: Full test suite
4. **STOP and VALIDATE**: All 468 tests pass

### Incremental Delivery

1. T001 → T002 → T003 → Core change validated (MVP!)
2. T004 → T005 → NuGet packaging verified

---

## Notes

- This is a single-line change in one `.csproj` file — the smallest possible feature
- The entire validation relies on the existing test suite (468 tests across 7 projects)
- No new code, no new files, no .fsi changes, no public API surface changes
- If T002 fails, investigate which downstream project lost a transitive dependency and add an explicit reference (per FR-006)
