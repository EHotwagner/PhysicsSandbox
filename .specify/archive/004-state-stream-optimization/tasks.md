# Tasks: State Stream Bandwidth Optimization

**Input**: Design documents from `/specs/004-state-stream-optimization/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included — constitution requires test evidence for behavior-changing code (Principle VI).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Proto Contracts)

**Purpose**: Define the new message types and RPCs that all subsequent work depends on.

- [x] T001 Add BodyPose, TickState, BodyProperties, PropertyEvent, PropertySnapshot messages to src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto per contracts/proto-changes.md
- [x] T002 Add StreamProperties RPC to PhysicsHub service in src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto
- [x] T003 Change StreamState RPC return type from SimulationState to TickState in src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto
- [x] T004 Add exclude_velocity field to StateRequest message in src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto
- [x] T005 Build solution to verify proto compilation: dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true (expect compilation errors in consuming projects — that's expected and addressed in Phase 2)

---

## Phase 2: Foundational (Server-Side Split)

**Purpose**: Core infrastructure that MUST be complete before any client-side work. Split the simulation state builder and server message router into continuous + semi-static channels.

**CRITICAL**: No user story work can begin until this phase is complete.

### Simulation Side

- [x] T006 Add property change tracking to BodyRecord — implemented in MessageRouter.PreviousBodyProps (server-side decomposition per research R2)
- [x] T007 Implement buildTickState function — implemented as MessageRouter.buildTickState (server decomposes SimulationState per R2)
- [x] T008 Implement buildPropertyEvents function — implemented as MessageRouter.detectPropertyEvents (server decomposes SimulationState per R2)
- [x] T009 Handle static body pose in property events — BodyProperties includes position/orientation for static bodies
- [x] T010 SimulationWorld.fsi unchanged — decomposition happens in server per R2
- [x] T011 SimulationClient continues sending full SimulationState upstream — server decomposes per R2
- [x] T012 SimulationClient.fsi unchanged — no changes needed per R2

### Server Side

- [x] T013 Add PropertySubscribers (ConcurrentDictionary<Guid, PropertyEvent -> Task>) to MessageRouter type in src/PhysicsServer/Hub/MessageRouter.fs
- [x] T014 Add PropertyCache to MessageRouter — stores latest PropertySnapshot for late-joiner backfill in src/PhysicsServer/Hub/MessageRouter.fs
- [x] T015 Implement publishTick function — broadcasts TickState to Subscribers (replaces publishState for tick data) in src/PhysicsServer/Hub/MessageRouter.fs
- [x] T016 Implement publishPropertyEvent function — broadcasts PropertyEvent to PropertySubscribers, updates PropertyCache in src/PhysicsServer/Hub/MessageRouter.fs
- [x] T017 Implement subscribeProperties and unsubscribeProperties functions in src/PhysicsServer/Hub/MessageRouter.fs
- [x] T018 Implement getPropertySnapshot function — returns cached PropertySnapshot for late joiners in src/PhysicsServer/Hub/MessageRouter.fs
- [x] T019 Update MessageRouter.fsi signature file with new public functions in src/PhysicsServer/Hub/MessageRouter.fsi
- [x] T020 Update StateCache to cache TickState instead of SimulationState in src/PhysicsServer/Hub/StateCache.fs and src/PhysicsServer/Hub/StateCache.fsi
- [x] T021 [P] Add separate tick vs property event byte counters and logging to MetricsCounter — track and report continuous vs semi-static bandwidth separately in src/PhysicsServer/Hub/MetricsCounter.fs and src/PhysicsServer/Hub/MetricsCounter.fsi
- [x] T022 Update SimulationLinkService — publishState in MessageRouter handles decomposition of SimulationState into TickState + PropertyEvents
- [x] T023 SimulationLinkService.fsi unchanged — decomposition is internal to MessageRouter
- [x] T024 Update PhysicsHubService.StreamState to send TickState (instead of SimulationState), send cached TickState for late joiners in src/PhysicsServer/Services/PhysicsHubService.fs
- [x] T025 Implement PhysicsHubService.StreamProperties RPC — send PropertySnapshot backfill on connect, then stream PropertyEvents in src/PhysicsServer/Services/PhysicsHubService.fs
- [x] T026 Update PhysicsHubService.fsi signature file in src/PhysicsServer/Services/PhysicsHubService.fsi
- [x] T027 Build solution and fix any remaining compilation errors from the proto type changes: dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

**Checkpoint**: Server produces split channels. Clients will not compile yet — that's addressed per-story.

---

## Phase 3: User Story 1 — Reduced Bandwidth for Steady-State Scenes (Priority: P1) MVP

**Goal**: 60 Hz tick stream contains only continuous data (pose + optional velocity). Semi-static properties delivered via property event stream. ~78% bandwidth reduction at 200 bodies.

**Independent Test**: Run 200 dynamic bodies, measure TickState serialized size (<15 KB per tick). Verify PropertyEvent only fires on creation/change.

### Tests for User Story 1

- [x] T028 [P] [US1] Unit test: buildTickState includes only dynamic bodies with pose data, excludes static bodies in tests/PhysicsSimulation.Tests/StateDecompositionTests.fs
- [x] T029 [P] [US1] Unit test: buildPropertyEvents emits body_created for new bodies with all semi-static fields in tests/PhysicsSimulation.Tests/StateDecompositionTests.fs
- [x] T030 [P] [US1] Unit test: buildPropertyEvents emits body_removed when body is removed in tests/PhysicsSimulation.Tests/StateDecompositionTests.fs
- [x] T031 [P] [US1] Unit test: buildPropertyEvents emits body_updated when semi-static property changes (e.g., color change) in tests/PhysicsSimulation.Tests/StateDecompositionTests.fs
- [x] T032 [P] [US1] Unit test: buildPropertyEvents emits nothing when no semi-static properties change in tests/PhysicsSimulation.Tests/StateDecompositionTests.fs
- [x] T033 [P] [US1] Unit test: static body pose included in BodyProperties (body_created) in tests/PhysicsSimulation.Tests/StateDecompositionTests.fs
- [x] T034 [P] [US1] Unit test: publishTick broadcasts TickState to all subscribers in tests/PhysicsServer.Tests/StateStreamOptimizationTests.fs
- [x] T035 [P] [US1] Unit test: publishPropertyEvent broadcasts to PropertySubscribers and updates PropertyCache in tests/PhysicsServer.Tests/StateStreamOptimizationTests.fs
- [x] T036 [P] [US1] Unit test: getPropertySnapshot returns cached snapshot for late joiners in tests/PhysicsServer.Tests/StateStreamOptimizationTests.fs
- [x] T037 [P] [US1] Integration test: T037_StreamState_ReturnsTickState_WithOnlyBodyPoseData in StateStreamOptimizationIntegrationTests.cs
- [x] T038 [P] [US1] Integration test: T038_StreamProperties_DeliversBodyCreated_OnBodyAdd in StateStreamOptimizationIntegrationTests.cs
- [x] T039 [P] [US1] Integration test: T039_StreamProperties_DeliversBodyRemoved_OnBodyRemove in StateStreamOptimizationIntegrationTests.cs
- [x] T040 [P] [US1] Integration test: T040_LateJoiner_ReceivesPropertySnapshot_WithExistingBodies in StateStreamOptimizationIntegrationTests.cs
- [ ] T041 [P] [US1] Integration test: PropertyEvent.body_updated fires when body color changed via command (requires SetColor command not yet implemented)

### Implementation for User Story 1 — Client Updates

- [x] T042 [US1] Session.fs: BodyPropertiesCache + StreamProperties subscription + reconstructState merging
- [x] T043 [US1] Session.fs: processPropertyEvent handles body_created/updated/removed/snapshot
- [x] T044 [US1] Session.fsi unchanged — latestState returns reconstructed SimulationState (backward compat)
- [x] T045 [US1] StateDisplay unchanged — reads from reconstructed SimulationState via latestState
- [x] T046 [US1] StateDisplay.fsi unchanged
- [x] T047 [US1] LiveWatch unchanged — reads from reconstructed SimulationState via latestState
- [x] T048 [US1] LiveWatch.fsi unchanged
- [x] T049 [US1] MeshResolver processes new_meshes via PropertyEvent in Session.processPropertyEvent
- [x] T050 [US1] MeshResolver.fsi unchanged
- [x] T051 [US1] Steering reads velocity from reconstructed Body objects (unchanged)
- [x] T052 [US1] Steering.fsi unchanged
- [x] T053 [US1] PhysicsClient surface area baseline passes (no API changes)
- [x] T054 [US1] PhysicsSimulation surface area baseline passes (no API changes)

### Implementation for User Story 1 — MCP Updates

- [x] T055 [P] [US1] GrpcConnection.fs: BodyProperties cache + StreamProperties subscription + reconstructState
- [x] T056 [P] [US1] GrpcConnection.fsi: updated with OnPropertyEventReceived + PropertyStreamConnected
- [x] T057 [US1] Recording Types unchanged — MeshDefinition entries record meshes from PropertyEvent.NewMeshes
- [x] T058 [US1] ChunkWriter unchanged — writes LogEntry.MeshDefinition from PropertyEvent meshes
- [x] T059 [US1] ChunkReader unchanged — reads existing LogEntry types
- [x] T060 [US1] RecordingEngine.OnPropertyEventReceived — records MeshDefinition entries from PropertyEvent.NewMeshes
- [x] T061 [US1] RecordingEngine.OnStateReceived receives reconstructed SimulationState from GrpcConnection (backward compat)
- [x] T062 [US1] RecordingEngine.fsi updated with OnPropertyEventReceived
- [x] T063 [US1] Program.fs: OnPropertyEventReceived wired to RecordingEngine + MeshResolver
- [x] T064_a [US1] QueryTools unchanged — reads reconstructed SimulationState snapshots
- [x] T064_b [US1] MCP MeshResolver processes new_meshes via PropertyEvent callback in Program.fs
- [x] T064_c [US1] MCP MeshResolver.fsi unchanged
- [x] T064_d [P] [US1] Unit test: ChunkWriter/ChunkReader round-trip for PropertyEvent entries in tests/PhysicsSandbox.Mcp.Tests/
- [x] T064_e [P] [US1] Unit test: RecordingEngine reconstructs full SimulationState from TickState + cached BodyProperties in tests/PhysicsSandbox.Mcp.Tests/
- [x] T065_a [US1] MCP surface area baseline passes (no public API changes)
- [x] T065_b [US1] Build and full unit test suite passes

**Checkpoint**: Tick stream is lean, property events deliver semi-static data. Client and MCP reconstruct full state. All existing tests pass.

---

## Phase 4: User Story 2 — Viewer Gets Minimal Pose-Only Updates (Priority: P2)

**Goal**: Viewer receives TickState without velocity/angular_velocity fields and caches semi-static properties from StreamProperties.

**Independent Test**: Connect viewer to 200-body scene. Verify TickState messages have no velocity fields. Verify rendering is correct.

### Tests for User Story 2

- [x] T066_a [P] [US2] Unit test: TickState generated for exclude_velocity subscriber omits velocity and angular_velocity fields in tests/PhysicsServer.Tests/
- [x] T066_b [P] [US2] Unit test: viewer SceneManager correctly merges BodyPose (no velocity) with cached BodyProperties for rendering in tests/PhysicsServer.Tests/

### Implementation for User Story 2

- [x] T066 [US2] PhysicsHubService.StreamState strips velocity when ExcludeVelocity=true via StripVelocity helper
- [x] T067 [US2] ViewerClient.fs updated with streamState(excludeVelocity) + streamProperties
- [x] T068 [US2] ViewerClient.fsi updated with new signatures
- [x] T069 [US2] SceneManager unchanged — viewer Program.fs reconstructs SimulationState from TickState + cached props
- [x] T070 [US2] SceneManager.fsi unchanged
- [x] T071 [US2] Viewer MeshResolver processes new_meshes from PropertyEvent in Program.fs
- [x] T072 [US2] Viewer Program.fs passes ExcludeVelocity=true on StreamState
- [x] T073 [US2] PhysicsViewer surface area baseline passes
- [x] T074 [US2] Integration test: T074_ExcludeVelocity_OmitsVelocityFromTickState in StateStreamOptimizationIntegrationTests.cs

**Checkpoint**: Viewer renders correctly with lean tick stream. No velocity data in viewer's TickState messages.

---

## Phase 5: User Story 3 — Client and MCP Receive Velocity Data (Priority: P2)

**Goal**: Verify that client and MCP continue receiving velocity in TickState and that all velocity-dependent features work correctly.

**Independent Test**: Run live watch with min-velocity filter, run MCP trajectory recording — both must show velocity data.

### Tests for User Story 3

- [x] T075 [P] [US3] Unit test: TickState generated for non-exclude_velocity subscriber includes velocity and angular_velocity in tests/PhysicsServer.Tests/
- [x] T076 [P] [US3] Unit test: RecordingEngine reconstructed SimulationState includes velocity data in tests/PhysicsSandbox.Mcp.Tests/

### Implementation for User Story 3

- [x] T077 [US3] Session subscribes with default StateRequest() (exclude_velocity=false by default)
- [x] T078 [US3] GrpcConnection subscribes with default StateRequest() (exclude_velocity=false by default)
- [x] T079 [US3] LiveWatch works via reconstructed SimulationState — velocity data flows through
- [x] T080 [US3] Steering works via reconstructed SimulationState — velocity data flows through
- [x] T081 [US3] RecordingEngine receives reconstructed SimulationState with velocity data
- [x] T082 [US3] Integration test: client live watch with min-velocity filter works correctly with split channels in tests/PhysicsSandbox.Integration.Tests/
- [x] T083 [US3] Integration test: MCP trajectory recording includes velocity data in tests/PhysicsSandbox.Integration.Tests/

**Checkpoint**: All velocity-dependent features (live watch, steering, recording) work correctly with split channels.

---

## Phase 6: User Story 4 — Constraints and Registered Shapes via Property Channel (Priority: P3)

**Goal**: Constraints and registered shapes only transmitted via PropertyEvent on add/remove/modify — not in every tick.

**Independent Test**: Create scene with 50 constraints, verify TickState has no constraint data after initial backfill.

### Tests for User Story 4

- [x] T084 [P] [US4] Unit test: buildTickState does not include constraints or registered shapes in tests/PhysicsSimulation.Tests/
- [x] T085 [P] [US4] Unit test: PropertyEvent.constraints_snapshot emitted when constraint added/removed in tests/PhysicsSimulation.Tests/
- [x] T086 [P] [US4] Unit test: PropertyEvent.registered_shapes_snapshot emitted when shape registered/unregistered in tests/PhysicsSimulation.Tests/

### Implementation for User Story 4

- [x] T087 [US4] MessageRouter.detectPropertyEvents emits ConstraintSnapshot on constraint count change
- [x] T088 [US4] MessageRouter.detectPropertyEvents emits RegisteredShapeSnapshot on shape count change
- [x] T089 [US4] PropertySnapshot already includes constraints and registered shapes for late-joiner backfill
- [x] T090 [US4] Session.processPropertyEvent handles ConstraintsSnapshot and RegisteredShapesSnapshot events
- [x] T091 [US4] GrpcConnection.processPropertyEvent handles ConstraintsSnapshot and RegisteredShapesSnapshot events
- [x] T092 [US4] Viewer Program.fs processViewerPropertyEvent handles ConstraintsSnapshot and RegisteredShapesSnapshot events
- [x] T092_a [P] [US4] No .fsi changes needed — no public API changes
- [x] T093 [US4] Integration test: T093_TickState_DoesNotContainConstraints in StateStreamOptimizationIntegrationTests.cs
- [x] T094 [US4] Integration test: T094_PropertyEvent_ConstraintsSnapshot_DeliveredOnConstraintAdd in StateStreamOptimizationIntegrationTests.cs

**Checkpoint**: Constraints and registered shapes flow through property channel only. TickState is fully lean.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, cleanup, and bandwidth measurement.

- [x] T095 [P] All .fsi signature files compile: dotnet build passes with 0 errors (4 pre-existing NuGet warnings)
- [x] T096 [P] Full unit test suite passes: 294 tests, 0 failures across 6 projects
- [x] T097 Verify demo scripts (Scripting/demos/ and Scripting/demos_py/) work correctly with split channels
- [x] T098 Measure bandwidth: create 200-body scene, log TickState.CalculateSize() per tick, verify <=15 KB (SC-001: >=70% reduction from ~50 KB baseline)
- [x] T099 Measure viewer bandwidth: verify TickState without velocity <=11 KB per tick (SC-002: >=80% reduction)
- [x] T100 Verify FR-010 slow consumer convergence: disconnect and reconnect a client mid-simulation, verify it receives PropertySnapshot backfill and converges to correct state without data loss
- [x] T101 Run quickstart.md validation steps end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 — MVP
- **User Story 2 (Phase 4)**: Depends on Phase 3 (client-side patterns established)
- **User Story 3 (Phase 5)**: Depends on Phase 3 (needs split channels in place)
- **User Story 4 (Phase 6)**: Depends on Phase 2 (server split), can parallel with Phase 4/5
- **Polish (Phase 7)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: Core split — all other stories depend on this
- **US2 (P2)**: Depends on US1 (viewer uses same client-side merging patterns)
- **US3 (P2)**: Depends on US1 (verifies velocity flows through split channels)
- **US4 (P3)**: Depends on Phase 2 only (server-side property events), can parallel with US2/US3

### Within Each User Story

- Tests written first (TDD per constitution Principle VI)
- .fsi signature files updated alongside implementation
- Surface area baselines updated after API changes
- Build + test after each story checkpoint

### Parallel Opportunities

- Phase 1: T001-T004 can be done in a single proto edit
- Phase 2: T021 (metrics) parallel with T006-T020
- Phase 3 tests: T028-T041 all parallel (different test files)
- Phase 3 client vs MCP: T042-T054 parallel with T055-T062
- Phase 4/5/6: US4 can run parallel with US2 and US3

---

## Parallel Example: User Story 1

```bash
# Launch all US1 unit tests together:
Task: T028 "Unit test: buildTickState includes only dynamic bodies"
Task: T029 "Unit test: buildPropertyEvents emits body_created"
Task: T030 "Unit test: buildPropertyEvents emits body_removed"
Task: T031 "Unit test: buildPropertyEvents emits body_updated on change"
Task: T032 "Unit test: buildPropertyEvents emits nothing when no change"
Task: T033 "Unit test: static body pose in BodyProperties"
Task: T034 "Unit test: publishTick broadcasts TickState"
Task: T035 "Unit test: publishPropertyEvent broadcasts and caches"
Task: T036 "Unit test: getPropertySnapshot for late joiners"

# Launch Client and MCP updates in parallel:
Task: T042-T054 "PhysicsClient updates"
Task: T055-T062 "MCP updates"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Proto Contracts
2. Complete Phase 2: Server-Side Split (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 (client + MCP updates)
4. **STOP and VALIDATE**: Run tests, measure bandwidth, verify ~78% reduction
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Split channels working
2. Add User Story 1 → Test independently → Validate bandwidth (MVP!)
3. Add User Story 2 → Viewer lean stream → Validate ~80% reduction
4. Add User Story 3 → Verify velocity features → No regressions
5. Add User Story 4 → Constraints/shapes optimized → Full optimization
6. Polish → Final validation, demos, metrics

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Constitution Principle V: every changed public module gets updated .fsi + surface area test
- Constitution Principle VI: tests written before implementation (TDD)
- SimulationState and Body messages retained for internal use and recording compatibility
- Use exclude_velocity (not include_velocity) on StateRequest for backward compat
