# Tasks: Fix Session State and Cache Synchronization

**Input**: Design documents from `/specs/005-fix-session-state-sync/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included (Constitution Principle VI requires test evidence for behavior-changing code).

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Proto contract changes and shared types that all user stories depend on

- [x] T001 Add `ConfirmedResetRequest` and `ConfirmedResetResponse` messages to `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T002 Add `rpc ConfirmedReset (ConfirmedResetRequest) returns (ConfirmedResetResponse)` to the `PhysicsHub` service definition in `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`
- [x] T003 Build `src/PhysicsSandbox.Shared.Contracts/` to verify proto compilation succeeds: `dotnet build src/PhysicsSandbox.Shared.Contracts/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Server-side confirmed reset handler and client-side cache clearing — MUST complete before user stories

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Implement `ConfirmedReset` handler in `src/PhysicsServer/Services/PhysicsHubService.fs` — submit a `ResetSimulation` command followed by a noop query via `submitQuery` to block until the simulation has processed the reset, then return `ConfirmedResetResponse` with body/constraint counts
- [x] T005 Update `src/PhysicsServer/Hub/MessageRouter.fsi` if any new public functions are needed for the confirmed reset path
- [x] T006 Add `clearCaches` internal function to `src/PhysicsClient/Connection/Session.fs` that clears `BodyRegistry`, `BodyPropertiesCache`, `CachedConstraints`, `CachedRegisteredShapes`, and sets `LatestState` to `None`
- [x] T007 Add `val internal clearCaches : session: Session -> unit` to `src/PhysicsClient/Connection/Session.fsi`
- [x] T008 Add `confirmedReset` function to `src/PhysicsClient/Commands/SimulationCommands.fs` that calls the `ConfirmedReset` gRPC RPC, then on success calls `clearCaches` and clears `BodyRegistry`
- [x] T009 Add `val confirmedReset : session: Session -> Result<ConfirmedResetResponse, string>` to `src/PhysicsClient/Commands/SimulationCommands.fsi`
- [x] T010 Build full solution to verify compilation: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Foundation ready — confirmed reset RPC works end-to-end, client can clear caches deterministically

---

## Phase 3: User Story 1 — Reliable Simulation Reset (Priority: P1) MVP

**Goal**: After `resetSimulation`, the session is in a clean state — no stale bodies, no ID collisions, auto-generated IDs work immediately.

**Independent Test**: Create bodies, reset, verify overlap queries return zero stale bodies, create new bodies with auto-generated IDs and verify they succeed.

### Tests for User Story 1

- [x] T011 [P] [US1] Write unit test in `tests/PhysicsClient.Tests/SessionTests.fs` verifying `clearCaches` clears `BodyRegistry` and `BodyPropertiesCache` on the session
- [x] T012 [P] [US1] Write integration test in `tests/PhysicsSandbox.Integration.Tests/ResetReliabilityTests.cs` — create 5 bodies, call confirmed reset via gRPC, verify success, then run an overlap query confirming zero bodies remain; also test ID reuse after reset

### Implementation for User Story 1

- [x] T013 [US1] Update `resetSimulation` in `src/PhysicsSandbox.Scripting/SimulationLifecycle.fs` to use `confirmedReset` instead of fire-and-forget `reset` (which handles cache clearing internally), then reset `IdGenerator`, add plane, and set gravity (remove the `sleep 100` since confirmation replaces it)
- [x] T014 [US1] Run unit tests: `dotnet test tests/PhysicsClient.Tests/ -p:StrideCompilerSkipBuild=true` — 79 passed
- [x] T015 [US1] Run integration tests: `dotnet test tests/PhysicsSandbox.Integration.Tests/ -p:StrideCompilerSkipBuild=true --filter ResetReliability` — 2 passed

**Checkpoint**: `resetSimulation` is now reliable — no stale bodies, no ID collisions. US1 acceptance scenarios verified.

---

## Phase 4: User Story 2 — Consistent Query Results (Priority: P2)

**Goal**: `overlapSphere` and `raycast` return consistent results that reflect the actual simulation state after any mutation (reset, add, remove).

**Independent Test**: Create bodies at known positions, verify overlap and raycast agree, remove bodies, verify both show them gone.

### Tests for User Story 2

- [x] T016 [US2] Write integration test in `tests/PhysicsSandbox.Integration.Tests/QueryConsistencyTests.cs` — create 5 bodies at known positions, call confirmed reset first, then recreate, verify `overlapSphere` and `raycast` both find exactly those 5 bodies. Also test removal consistency.

### Implementation for User Story 2

Note: Research (R1) confirmed both `overlapSphere` and `raycast` already query the server directly. The inconsistency was caused by the fire-and-forget reset (fixed in US1). No code changes needed beyond what US1 provides. This phase validates that the fix from US1 resolves the query consistency issue.

- [x] T017 [US2] Run query consistency integration test: `dotnet test tests/PhysicsSandbox.Integration.Tests/ -p:StrideCompilerSkipBuild=true --filter QueryConsistency` — 2 passed

**Checkpoint**: Query consistency verified end-to-end. Both overlap and raycast agree after reset and after body mutations.

---

## Phase 5: User Story 3 — Actionable Error Feedback from Batch Operations (Priority: P3)

**Goal**: `batchAdd` returns per-command success/failure status as a `BatchResult` instead of `unit`.

**Independent Test**: Submit a batch with a deliberate duplicate ID, verify the returned `BatchResult` reports the failure with the conflicting ID name.

### Tests for User Story 3

- [x] T018 [P] [US3] Write unit test in `tests/PhysicsSandbox.Scripting.Tests/BatchOperationsTests.fs` verifying `BatchResult` type construction — `Succeeded` count, `Failed` list with index and message

### Implementation for User Story 3

- [x] T019 [US3] Add `BatchResult` record type to `src/PhysicsSandbox.Scripting/BatchOperations.fs` with fields `Succeeded: int` and `Failed: (int * string) list`
- [x] T020 [US3] Update `batchAdd` in `src/PhysicsSandbox.Scripting/BatchOperations.fs` to accumulate results across chunks and return `BatchResult` instead of `unit` — keep the `printfn` for interactive console feedback
- [x] T021 [US3] Update `src/PhysicsSandbox.Scripting/BatchOperations.fsi` to export `BatchResult` type and change `batchAdd` return type from `unit` to `BatchResult`
- [x] T022 [US3] Update `batchAdd` re-export in `src/PhysicsSandbox.Scripting/Prelude.fsi` to reflect the new `BatchResult` return type
- [x] T023 [US3] Run unit tests: `dotnet test tests/PhysicsSandbox.Scripting.Tests/ -p:StrideCompilerSkipBuild=true` — 29 passed
- [x] T024 [US3] Write integration test in `tests/PhysicsSandbox.Integration.Tests/BatchResultTests.cs` — batch success, max-size rejection, per-command result index tracking
- [x] T025 [US3] Run integration tests: `dotnet test tests/PhysicsSandbox.Integration.Tests/ -p:StrideCompilerSkipBuild=true --filter BatchResult` — 3 passed

**Checkpoint**: `batchAdd` returns actionable results. Silent failures eliminated. US3 acceptance scenarios verified.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Surface area baselines, build validation, and full test suite

- [x] T026 [P] Verified surface area baseline in `tests/PhysicsSandbox.Scripting.Tests/SurfaceAreaTests.fs` — `BatchResult` is a record type, not a module member; existing baselines pass unchanged
- [x] T027 [P] Updated surface area baseline in `tests/PhysicsClient.Tests/SurfaceAreaTests.fs` — added `reset` and `confirmedReset` to SimulationCommands expected members
- [x] T028 Run full test suite — all unit tests pass: PhysicsClient (79), Scripting (29), Server (48), Simulation (114), Viewer (99), MCP (18); all 7 new integration tests pass
- [x] T029 Run full solution build: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — 0 errors, 4 warnings (pre-existing)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — proto changes first
- **Foundational (Phase 2)**: Depends on Phase 1 (proto must compile) — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — core fix
- **US2 (Phase 4)**: Depends on Phase 3 (US1 fix resolves the root cause)
- **US3 (Phase 5)**: Depends on Phase 2 only — can run in parallel with US1/US2
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational (Phase 2). This is the MVP.
- **User Story 2 (P2)**: Depends on US1 (the query consistency issue is resolved by the confirmed reset fix)
- **User Story 3 (P3)**: Depends on Foundational (Phase 2) only. Can run in parallel with US1.

### Within Each User Story

- Tests written first (TDD per Constitution Principle VI)
- Implementation follows
- Verification step confirms tests pass

### Parallel Opportunities

- T011 and T012 (US1 tests) can run in parallel
- T018 (US3 unit test) can run in parallel with US1 implementation
- T019-T022 (US3 implementation) is independent of US1/US2 — only needs Phase 2
- T026 and T027 (surface area updates) can run in parallel
- US1 and US3 can be implemented in parallel after Phase 2

---

## Parallel Example: After Phase 2

```text
# Stream A: User Story 1 (reset reliability)
Task T011: "Unit test for confirmedReset cache clearing in tests/PhysicsClient.Tests/SimulationCommandsTests.fs"
Task T012: "Integration test for reset reliability in tests/PhysicsSandbox.Integration.Tests/ResetReliabilityTests.cs"
Task T013: "Update resetSimulation in src/PhysicsSandbox.Scripting/SimulationLifecycle.fs"

# Stream B: User Story 3 (batch results) — can run simultaneously
Task T018: "Unit test for BatchResult in tests/PhysicsSandbox.Scripting.Tests/BatchOperationsTests.fs"
Task T019: "Add BatchResult type to src/PhysicsSandbox.Scripting/BatchOperations.fs"
Task T020: "Update batchAdd return type in src/PhysicsSandbox.Scripting/BatchOperations.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Proto contract (T001-T003)
2. Complete Phase 2: Server + client foundation (T004-T010)
3. Complete Phase 3: User Story 1 — reliable reset (T011-T015)
4. **STOP and VALIDATE**: Run integration test to verify reset-and-recreate works
5. This alone fixes the core issue chain from the problem report

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Add US1 → Reset works reliably (MVP — fixes Issues 1, 2, 3 from problem report)
3. Add US2 → Query consistency verified (validates US1 fix, adds test coverage)
4. Add US3 → Batch errors surfaced (fixes Issue 4 from problem report)
5. Polish → Surface area baselines, full test suite green

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US2 requires no code changes beyond US1 — it's a validation/test-only phase
- The `batchAdd` return type change (US3) is the only API-visible breaking change
- `resetSimulation` signature stays `Session -> unit` — callers unaffected
- The `sleep 100` in `resetSimulation` is removed since confirmed reset replaces it
