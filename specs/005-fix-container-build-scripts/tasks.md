# Tasks: Fix Container Build Scripts

**Input**: Design documents from `/specs/005-fix-container-build-scripts/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md

**Tests**: Not requested — verification is manual (run demo scripts).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: User Story 1 — F# Demo Scripts Run in Container (Priority: P1) MVP

**Goal**: Remove the `local` NuGet source from repo-level `nuget.config` so FSI can resolve packages without NU1301 errors on dev workstations and in the container.

**Independent Test**: Run `dotnet fsi Scripting/demos/Demo01_HelloDrop.fsx` on a dev workstation without a `local-packages/` directory — should load without NU1301 errors.

### Implementation for User Story 1

- [x] T001 [US1] Remove the `local` package source entry from `nuget.config`
- [x] T002 [US1] Verify `dotnet fsi Scripting/demos/Demo01_HelloDrop.fsx` runs without NU1301 errors
- [x] T003 [US1] Verify `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` still succeeds (the build uses `~/.local/share/nuget-local/` via global config, not the repo-level `local` source)

**Checkpoint**: F# demo scripts run on dev workstation without NuGet source errors.

---

## Phase 2: User Story 2 — Python Demo Scripts Run in Container (Priority: P2)

**Goal**: Commit regenerated Python gRPC stubs with correct relative imports and remove the PYTHONPATH Containerfile workaround.

**Independent Test**: Run `PYTHONPATH= python3 -c "import sys; sys.path.insert(0, '.'); from Scripting.demos_py.generated import physics_hub_pb2_grpc"` — should succeed without import errors.

### Implementation for User Story 2

- [x] T004 [P] [US2] Regenerate Python gRPC stubs by running `bash Scripting/demos_py/generate_stubs.sh` — verify `Scripting/demos_py/generated/physics_hub_pb2_grpc.py` contains `from . import physics_hub_pb2` (not bare `import physics_hub_pb2`)
- [x] T005 [P] [US2] Remove `PYTHONPATH=/src/Scripting/demos_py/generated` from the `ENV` block in `Containerfile`
- [x] T006 [US2] Verify Python imports work without PYTHONPATH: `PYTHONPATH= python3 -c "import sys; sys.path.insert(0, '.'); from Scripting.demos_py.generated import physics_hub_pb2 as pb; from Scripting.demos_py.generated import physics_hub_pb2_grpc as pb_grpc; print('OK')"`

**Checkpoint**: Python demo stubs import correctly without PYTHONPATH workaround.

---

## Phase 3: Polish & Cross-Cutting Concerns

**Purpose**: Final verification across both stories.

- [x] T007 Run quickstart.md validation steps to confirm both fixes work together

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (US1)**: No dependencies — can start immediately
- **Phase 2 (US2)**: No dependencies on Phase 1 — can run in parallel
- **Phase 3 (Polish)**: Depends on Phase 1 and Phase 2 completion

### User Story Dependencies

- **User Story 1 (P1)**: Independent — `nuget.config` change only
- **User Story 2 (P2)**: Independent — Python stubs + Containerfile only

### Parallel Opportunities

- T001 and T004+T005 can run in parallel (different files, no dependencies)
- T004 and T005 can run in parallel (different files)

---

## Parallel Example: Full Feature

```bash
# All independent tasks can run simultaneously:
Task T001: "Remove local source from nuget.config"
Task T004: "Regenerate Python stubs via generate_stubs.sh"
Task T005: "Remove PYTHONPATH from Containerfile"

# Then verify:
Task T002: "Verify F# demo script runs"
Task T003: "Verify dotnet build succeeds"
Task T006: "Verify Python imports work"

# Final:
Task T007: "Run quickstart.md validation"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T001 (remove `local` source from `nuget.config`)
2. Verify T002 + T003
3. **STOP and VALIDATE**: F# demos work — BLOCKER resolved

### Incremental Delivery

1. T001 → T002 + T003 → US1 complete (BLOCKER fixed)
2. T004 + T005 → T006 → US2 complete (Python stubs fixed)
3. T007 → Full validation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Both user stories are fully independent and can be implemented in parallel
- Commit after each phase
- This is a small fix — total scope is 3 files modified + 2 files regenerated
