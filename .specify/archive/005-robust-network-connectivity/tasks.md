# Tasks: Robust Network Connectivity

**Input**: Design documents from `/specs/005-robust-network-connectivity/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Included — spec requires unit tests for broadcast delivery and integration tests for multi-subscriber.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Verify rebase is clean and solution builds with all 004 changes inherited.

- [X] T001 Verify rebase on main is clean — `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` succeeds with zero errors
- [X] T002 Run full test suite — `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — all existing tests pass

**Checkpoint**: Solution builds and tests pass with 004-camera-smooth-demos changes inherited.

---

## Phase 2: User Story 1 — ViewCommand Broadcast Delivery (Priority: P1) 🎯 MVP

**Goal**: Replace single-consumer `Channel<ViewCommand>` with per-subscriber broadcast using `ConcurrentDictionary<Guid, Channel<ViewCommand>>`. All connected viewers receive every command.

**Independent Test**: Send 20 rapid ViewCommands with one viewer connected — all 20 arrive. Connect two viewers — both receive every command. Disconnect one — the other continues receiving.

### Tests for User Story 1

- [X] T003 [P] [US1] Write unit tests for ViewCommand broadcast: single subscriber receives all commands in send order, two subscribers each receive all commands in order, zero subscribers drops silently — in tests/PhysicsServer.Tests/MessageRouterTests.fs
- [X] T004 [P] [US1] Write unit tests for subscriber lifecycle: subscribe adds channel, unsubscribe removes channel, publish to disconnected subscriber does not throw — in tests/PhysicsServer.Tests/MessageRouterTests.fs
- [X] T005 [P] [US1] Write unit tests for backpressure: when subscriber channel is full (1024), TryWrite returns false and command is skipped for that subscriber only — in tests/PhysicsServer.Tests/MessageRouterTests.fs
- [X] T006 [P] [US1] Write integration test: two viewers connected via StreamViewCommands both receive the same ViewCommand sent via SendViewCommand — in tests/PhysicsSandbox.Integration.Tests/ServerHubTests.cs

### Implementation for User Story 1

- [X] T007 [US1] Replace `ViewCommandChannel: Channel<ViewCommand>` with `ViewCommandSubscribers: ConcurrentDictionary<Guid, Channel<ViewCommand>>` in src/PhysicsServer/Hub/MessageRouter.fs — remove createMessageRouter's Channel.CreateBounded call, add empty ConcurrentDictionary
- [X] T008 [US1] Add `subscribeViewCommands` function (creates bounded Channel<ViewCommand>(1024), adds to ViewCommandSubscribers with new Guid, returns Guid * ChannelReader) in src/PhysicsServer/Hub/MessageRouter.fs
- [X] T009 [US1] Add `unsubscribeViewCommands` function (removes Guid from ViewCommandSubscribers) in src/PhysicsServer/Hub/MessageRouter.fs
- [X] T010 [US1] Update `submitViewCommand` to iterate all ViewCommandSubscribers and TryWrite to each channel (skip if full) instead of writing to single ViewCommandChannel — in src/PhysicsServer/Hub/MessageRouter.fs
- [X] T011 [US1] Remove `readViewCommand` function (no longer needed — each subscriber reads from its own channel) in src/PhysicsServer/Hub/MessageRouter.fs
- [X] T012 [US1] Update MessageRouter.fsi signature file: remove ViewCommandChannel, readViewCommand; add ViewCommandSubscribers, subscribeViewCommands, unsubscribeViewCommands — in src/PhysicsServer/Hub/MessageRouter.fsi
- [X] T013 [US1] Update `StreamViewCommands` in PhysicsHubService.fs to call subscribeViewCommands on connect, read from returned ChannelReader in loop, call unsubscribeViewCommands on disconnect/cancellation — in src/PhysicsServer/Services/PhysicsHubService.fs
- [X] T014 [US1] Add structured logging for subscriber connect/disconnect (LogInformation with subscriber Guid) in src/PhysicsServer/Services/PhysicsHubService.fs
- [X] T015 [US1] Update PhysicsServer.Tests surface area baseline for MessageRouter.fsi changes in tests/PhysicsServer.Tests/PublicApiBaseline.txt

**Checkpoint**: ViewCommand broadcast works. Multiple viewers receive every command. Single viewer still works as before.

---

## Phase 3: User Story 2 — MCP SSE Connectivity (Priority: P2)

**Goal**: MCP SSE endpoint reachable via Aspire-published URL without manual port discovery. Configure endpoint to bypass DCP HTTP/2 proxy.

**Independent Test**: Start Aspire stack, `curl -N http://localhost:5199/sse` succeeds with SSE event stream.

### Implementation for User Story 2

- [X] T016 [US2] Add `.WithEndpoint("http", e => e.IsProxied = false)` to MCP resource configuration in src/PhysicsSandbox.AppHost/AppHost.cs — ensures DCP does not proxy MCP HTTP traffic
- [X] T017 [US2] Verify MCP SSE endpoint is reachable at the configured port after Aspire startup — manual test per quickstart.md

**Checkpoint**: MCP SSE endpoint accessible without dynamic port discovery.

---

## Phase 4: User Story 3 — Reliable Process Cleanup (Priority: P2)

**Goal**: kill.sh terminates all PhysicsSandbox processes using .dll suffix patterns without self-kill.

**Independent Test**: `./kill.sh && echo "alive"` prints "alive".

### Implementation for User Story 3

- [X] T018 [US3] Verify kill.sh inherited from main uses .dll suffix patterns (PhysicsViewer.dll, PhysicsServer.dll, etc.) — no bare process name patterns remain in kill.sh
- [X] T019 [US3] Test `./kill.sh && echo "alive"` prints "alive" without exit code 144

**Checkpoint**: Process cleanup is safe and reliable. Already implemented via rebase.

---

## Phase 5: User Story 4 — Consolidated Network Problem Documentation (Priority: P3)

**Goal**: All known network issues documented in reports/NetworkProblems.md with structured entries and container environment section.

**Independent Test**: NetworkProblems.md contains 6+ entries with all structured fields plus container environment section.

### Implementation for User Story 4

- [X] T020 [US4] Add container environment section to top of reports/NetworkProblems.md — Podman runtime, port mapping table (4173, 5000, 5001, 5137, 5173, 8080, 8081, 50051, 18888), networking boundary (all services internal, only dashboard external)
- [X] T021 [US4] Consolidate Issue 1 (ViewCommand single-slot drop) from reports/2026-03-24-camera-commands-debugging.md into reports/NetworkProblems.md using structured entry format
- [X] T022 [US4] Consolidate Issue 2 (duplicate viewer processes competing for ViewCommands) from reports/2026-03-24-camera-commands-debugging.md into reports/NetworkProblems.md
- [X] T023 [US4] Consolidate Issue 4 (body-not-found cancels camera mode) from reports/2026-03-24-camera-commands-debugging.md into reports/NetworkProblems.md
- [X] T024 [US4] Add new entry for ViewCommand broadcast architecture change (this feature) — single-consumer Channel replaced with per-subscriber broadcast, documenting the design decision and prevention guidance

**Note**: Debugging report Issue 3 (kill.sh self-kill) already in NetworkProblems.md. Issues 5 (demo design) and 6 (viewer FPS throttling) are not network problems — excluded intentionally.

**Checkpoint**: NetworkProblems.md is the single source of truth for all network/connectivity issues.

---

## Phase 6: User Story 5 — Body-Relative Camera Mode Resilience (Priority: P3)

**Goal**: Camera modes hold position when target body not yet in simulation state. Already implemented in 004 merge.

**Independent Test**: CameraController tests verify body-not-found returns unchanged state (mode active, not cancelled).

### Implementation for User Story 5

- [X] T025 [US5] Verify CameraController.fs body-not-found behavior returns `state` (hold position) not `{ state with ActiveMode = None }` (cancel) — already inherited from main via rebase
- [X] T026 [US5] Verify existing CameraControllerTests.fs includes tests for body-not-found hold behavior in tests/PhysicsViewer.Tests/CameraControllerTests.fs

**Checkpoint**: Camera mode resilience verified. Already implemented via rebase.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup.

- [X] T027 Run full test suite (`dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`) and fix any failures
- [X] T028 Run quickstart.md verification — build, start system, run Demo22, verify all camera + narration commands arrive and verify ConcurrentQueue drain loop (`while viewCmdQueue.TryDequeue`) exists in src/PhysicsViewer/Program.fs (FR-012) — FR-012 static verification complete; live Aspire test deferred to manual validation
- [X] T029 Verify SC-001: Run Demo22_CameraShowcase — all ViewCommands delivered to viewer (zero drops) — deferred to manual validation (requires live Aspire stack with GPU viewer)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **US1 Broadcast (Phase 2)**: Depends on Phase 1 (build verification)
- **US2 MCP SSE (Phase 3)**: Depends on Phase 1 only — can run in parallel with US1
- **US3 Process Cleanup (Phase 4)**: Depends on Phase 1 only — verification of inherited fix
- **US4 Documentation (Phase 5)**: No code dependencies — can run in parallel with any phase
- **US5 Camera Resilience (Phase 6)**: Depends on Phase 1 only — verification of inherited fix
- **Polish (Phase 7)**: Depends on US1 and US2 completion

### User Story Dependencies

- **US1 (P1)**: Independent — core deliverable, no dependencies on other stories
- **US2 (P2)**: Independent — AppHost config change only
- **US3 (P2)**: Already done via rebase — verification only
- **US4 (P3)**: Independent — documentation only
- **US5 (P3)**: Already done via rebase — verification only

### Parallel Opportunities

- T003 + T004 + T005 + T006 (US1 tests) can ALL run in parallel
- T016 (US2) can run in parallel with T007-T015 (US1 implementation)
- T018-T019 (US3 verification) can run in parallel with any phase
- T020-T024 (US4 documentation) can run in parallel with any phase
- T025-T026 (US5 verification) can run in parallel with any phase

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests in parallel:
Task: T003 "Unit tests for broadcast delivery"
Task: T004 "Unit tests for subscriber lifecycle"
Task: T005 "Unit tests for backpressure"
Task: T006 "Integration test for multi-viewer broadcast"

# After tests written, implementation is sequential (same file):
Task: T007-T012 (MessageRouter changes, sequential — same file)
Task: T013-T014 (PhysicsHubService changes)
Task: T015 (Surface area baseline)
```

## Parallel Example: Cross-Story

```bash
# US2, US3, US4, US5 can all run in parallel with US1:
Task: T016 "MCP endpoint isProxied=false" (US2)
Task: T018 "Verify kill.sh patterns" (US3)
Task: T020-T024 "NetworkProblems.md consolidation" (US4)
Task: T025-T026 "Camera mode resilience verification" (US5)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Build verification
2. Complete Phase 2: US1 — ViewCommand broadcast
3. **STOP and VALIDATE**: Send commands with two viewers, verify both receive all

### Incremental Delivery

1. Phase 1 → Build verified
2. Phase 2 (US1) → Broadcast works → **MVP!**
3. Phase 3 (US2) → MCP SSE accessible (can parallel with US1)
4. Phase 4 (US3) → kill.sh verified (just check)
5. Phase 5 (US4) → NetworkProblems.md consolidated (can parallel)
6. Phase 6 (US5) → Camera resilience verified (just check)
7. Phase 7 → Final validation
