# Tasks: Physics Simulation Service

**Input**: Design documents from `/specs/002-physics-simulation/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included per constitution Principle VI (test evidence required for behavior-changing code) and SC-007.

**Organization**: Tasks grouped by user story. US1 (Lifecycle) and US5 (State Streaming) are combined as they are inseparable — lifecycle produces state, streaming delivers it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Pack BepuFSharp, configure NuGet, scaffold the PhysicsSimulation project and test project, register in Aspire AppHost.

- [x] T001 Pack BepuFSharp to local NuGet feed by running `dotnet pack` in /home/developer/projects/BPEWrapper/BepuFSharp/BepuFSharp.fsproj and verify output at ~/.local/share/nuget-local/BepuFSharp.0.1.0.nupkg
- [x] T002 Add local NuGet feed source to NuGet.config at repository root (create if missing) with key "local" pointing to ~/.local/share/nuget-local/
- [x] T003 Create F# project src/PhysicsSimulation/PhysicsSimulation.fsproj with SDK Microsoft.NET.Sdk.Worker, targeting net10.0. Add PackageReferences: BepuFSharp 0.1.0, Grpc.Net.Client. Add ProjectReferences: PhysicsSandbox.Shared.Contracts, PhysicsSandbox.ServiceDefaults. Create directory structure: World/, Commands/, Client/
- [x] T004 Create F# test project tests/PhysicsSimulation.Tests/PhysicsSimulation.Tests.fsproj with xUnit 2.x, targeting net10.0. Add ProjectReference to PhysicsSimulation
- [x] T005 Add PhysicsSimulation and PhysicsSimulation.Tests projects to PhysicsSandbox.slnx
- [x] T006 Register PhysicsSimulation in Aspire AppHost at src/PhysicsSandbox.AppHost/AppHost.cs: add `builder.AddProject<Projects.PhysicsSimulation>("simulation").WithReference(server).WaitFor(server)`
- [x] T007 Verify solution builds with `dotnet build PhysicsSandbox.slnx`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extend proto contracts with new commands and fields, create .fsi signature files for all public modules. Use `/fsgrpc-proto` skill for proto work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T008 Extend proto contract at src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto: add Vec4 message (x,y,z,w doubles). Add RemoveBody (body_id string), ApplyImpulse (body_id string, impulse Vec3), ApplyTorque (body_id string, torque Vec3), ClearForces (body_id string) messages. Extend SimulationCommand oneof with remove_body=6, apply_impulse=7, apply_torque=8, clear_forces=9. Extend Body message with angular_velocity Vec3 field 6, orientation Vec4 field 7. Use `/fsgrpc-proto` skill for guidance. Verify backward compatibility by building existing projects.
- [x] T009 [P] Create SimulationWorld.fsi signature file at src/PhysicsSimulation/World/SimulationWorld.fsi per contracts/simulation-modules.md: module PhysicsSimulation.SimulationWorld with opaque World type, create, destroy, isRunning, time, step, currentState, addBody, removeBody, applyForce, applyImpulse, applyTorque, clearForces, setGravity, setRunning functions. Include all signatures from the start — stub implementations in T012 will satisfy the compiler
- [x] T010 [P] Create CommandHandler.fsi signature file at src/PhysicsSimulation/Commands/CommandHandler.fsi per contracts/simulation-modules.md: module PhysicsSimulation.CommandHandler with handle function (World -> SimulationCommand -> CommandAck)
- [x] T011 [P] Create SimulationClient.fsi signature file at src/PhysicsSimulation/Client/SimulationClient.fsi per contracts/simulation-modules.md: module PhysicsSimulation.SimulationClient with run function (serverAddress -> CancellationToken -> Async<unit>)
- [x] T012 Create minimal stub implementations for all three modules (SimulationWorld.fs, CommandHandler.fs, SimulationClient.fs) and a stub Program.fs entry point so the project compiles against the .fsi signatures. Verify build succeeds with `dotnet build PhysicsSandbox.slnx`

**Checkpoint**: Foundation ready — proto extended, project scaffolded, signatures defined, solution builds.

---

## Phase 3: User Story 1 + User Story 5 — Lifecycle Control + State Streaming (Priority: P1) 🎯 MVP

**Goal**: Simulation connects to server, supports play/pause/step, streams world state after every step.

**Independent Test**: Start simulation + server via Aspire, send play/pause/step commands via PhysicsHub, verify state updates arrive at server with advancing timestamps.

### Tests for US1+US5

- [x] T013 [P] [US1] Write unit tests in tests/PhysicsSimulation.Tests/SimulationWorldTests.fs: test create returns paused world with time=0; test step advances time by one timestep and returns SimulationState; test isRunning returns false initially; test currentState returns valid state with running flag
- [ ] T014 [P] [US1] Write integration test in tests/PhysicsSandbox.Integration.Tests/SimulationTests.cs: test simulation connects to server within 5 seconds; test sending PlayPause(running=true) then verifying state stream produces updates; test sending PlayPause(running=false) stops updates; test StepSimulation while paused produces exactly one state update. Use Aspire DistributedApplicationTestingBuilder with SSL bypass per CLAUDE.md gotchas

### Implementation for US1+US5

- [x] T015 [US1] Implement SimulationWorld.create in src/PhysicsSimulation/World/SimulationWorld.fs: create BepuFSharp PhysicsWorld with PhysicsConfig.defaults (zero gravity), initialize World record with Bodies=Map.empty, ActiveForces=Map.empty, Gravity=Vec3.Zero, SimulationTime=0.0, Running=false, TimeStep=1.0f/60.0f
- [x] T016 [US1] Implement SimulationWorld.step in src/PhysicsSimulation/World/SimulationWorld.fs: apply active forces to all bodies (mass*gravity + per-body forces via BepuFSharp.applyForce), call PhysicsWorld.step with TimeStep, increment SimulationTime, build and return SimulationState proto message from current body poses/velocities
- [x] T017 [US1] Implement SimulationWorld.currentState in src/PhysicsSimulation/World/SimulationWorld.fs: read all body poses/velocities from BepuFSharp world, build SimulationState proto with bodies, time, and running flag without stepping
- [x] T018 [US1] Implement SimulationWorld.destroy in src/PhysicsSimulation/World/SimulationWorld.fs: call PhysicsWorld.destroy to release BepuFSharp resources
- [x] T019 [US1] Implement SimulationClient.run in src/PhysicsSimulation/Client/SimulationClient.fs using `/fsgrpc-client` skill for bidirectional streaming: create GrpcChannel to serverAddress, create SimulationLink.SimulationLinkClient, call ConnectSimulation bidirectional stream. Read commands from server response stream, dispatch via CommandHandler.handle. When world.isRunning=true, run fixed-timestep loop: step world, send state via request stream. When paused, only step on StepSimulation commands. Handle server disconnect (RpcException) by logging and returning. Handle cancellation token for clean shutdown
- [x] T020 [US1] Implement CommandHandler.handle for PlayPause and StepSimulation commands in src/PhysicsSimulation/Commands/CommandHandler.fs: PlayPause toggles world Running state. StepSimulation calls world.step and returns success ack. Return CommandAck with success=true/false and descriptive message. Unknown commands return success ack (forward-compatible)
- [x] T021 [US1] Implement Program.fs entry point in src/PhysicsSimulation/Program.fs: create Host with AddServiceDefaults, resolve server address via Aspire service discovery (https+http://server), create SimulationWorld, run SimulationClient.run, destroy world on shutdown. Wire up IHostApplicationLifetime for graceful shutdown
- [x] T022 [US1] Run unit tests: `dotnet test tests/PhysicsSimulation.Tests/` — verify lifecycle tests pass
- [ ] T023 [US1] Run integration tests: `dotnet test tests/PhysicsSandbox.Integration.Tests/` — verify simulation connects, play/pause/step work end-to-end

**Checkpoint**: MVP complete. Simulation connects, responds to play/pause/step, streams empty world state. All existing tests still pass.

---

## Phase 4: User Story 2 — Body Management (Priority: P1)

**Goal**: Add and remove rigid bodies (sphere, box, plane) with unique IDs. Bodies appear in streamed state.

**Independent Test**: Send AddBody commands, step, verify bodies in state stream. Send RemoveBody, verify body disappears.

### Tests for US2

- [x] T024 [P] [US2] Write unit tests in tests/PhysicsSimulation.Tests/SimulationWorldTests.fs (append): test addBody with sphere shape returns success and body appears in currentState; test addBody with box/plane shapes; test addBody with duplicate ID returns error ack; test addBody with zero/negative mass returns error ack; test removeBody removes body from state; test removeBody with non-existent ID returns success (idempotent)
- [x] T025 [P] [US2] Write unit tests in tests/PhysicsSimulation.Tests/CommandHandlerTests.fs: test handle dispatches AddBody command correctly; test handle dispatches RemoveBody command correctly; test handle returns error for invalid mass

### Implementation for US2

- [x] T026 [US2] Implement body management in src/PhysicsSimulation/World/SimulationWorld.fs: add addBody function that validates mass>0, rejects duplicate IDs, maps proto Shape (Sphere/Box/Plane) to BepuFSharp PhysicsShape (Sphere→Sphere, Box→Box, Plane→large static Box), calls PhysicsWorld.addShape + addBody (or addStatic for planes), stores BodyRecord in Bodies map. Add removeBody function that looks up BodyRecord, calls PhysicsWorld.removeBody, removes from Bodies and ActiveForces maps. Update step and currentState to read pose/velocity/angular_velocity/orientation from BepuFSharp for all tracked bodies
- [x] T027 [US2] Extend CommandHandler.handle in src/PhysicsSimulation/Commands/CommandHandler.fs: add cases for AddBody (call world addBody, return ack) and RemoveBody (call world removeBody, return ack). Include validation error messages in CommandAck
- [ ] T028 [US2] Add integration test in tests/PhysicsSandbox.Integration.Tests/SimulationTests.cs: send AddBody via PhysicsHub.SendCommand, step simulation, verify body appears in StreamState with correct properties; send RemoveBody, step, verify body removed from state
- [x] T029 [US2] Run all tests: `dotnet test PhysicsSandbox.slnx` — verify body management works end-to-end

**Checkpoint**: Bodies can be added/removed. State stream includes all bodies with positions, velocities, angular velocities, and orientations.

---

## Phase 5: User Story 3 — Force, Torque, and Impulse Application (Priority: P2)

**Goal**: Apply persistent forces, one-shot impulses, and torques to bodies by ID. Clear forces per body.

**Independent Test**: Add body at rest, apply force, step, verify velocity changes. Apply impulse, verify one-shot. Apply torque, verify angular velocity. Clear forces, verify acceleration stops.

### Tests for US3

- [x] T030 [P] [US3] Write unit tests in tests/PhysicsSimulation.Tests/CommandHandlerTests.fs (append): test ApplyForce stores in ActiveForces; test ApplyImpulse calls BepuFSharp applyLinearImpulse; test ApplyTorque calls BepuFSharp applyTorque; test ClearForces removes all forces for body; test force/impulse/torque on non-existent body returns success (no-op)

### Implementation for US3

- [x] T031 [US3] Implement force management in src/PhysicsSimulation/World/SimulationWorld.fs: add applyForce function that appends force Vec3 to ActiveForces map for body ID. Add clearForces function that removes body's entry from ActiveForces map. Update step to iterate ActiveForces and call BepuFSharp.applyForce for each body's accumulated forces before stepping
- [x] T032 [US3] Implement impulse and torque in src/PhysicsSimulation/World/SimulationWorld.fs: add applyImpulse function that calls BepuFSharp.applyLinearImpulse on the body (one-shot, not stored). Add applyTorque function that calls BepuFSharp.applyTorque with the current TimeStep
- [x] T033 [US3] Extend CommandHandler.handle in src/PhysicsSimulation/Commands/CommandHandler.fs: add cases for ApplyForce, ApplyImpulse, ApplyTorque, ClearForces. Each looks up body ID, calls corresponding world function, returns success ack. Non-existent body → success ack (no-op per FR-015)
- [ ] T034 [US3] Add integration test in tests/PhysicsSandbox.Integration.Tests/SimulationTests.cs: add body at rest, apply force via SendCommand, step, verify velocity changed in state stream; apply impulse, step twice, verify impulse doesn't persist; clear forces, step, verify no more acceleration
- [x] T035 [US3] Run all tests: `dotnet test PhysicsSandbox.slnx`

**Checkpoint**: Forces, impulses, and torques work. Persistent forces accumulate, impulses are one-shot, clear-forces stops acceleration.

---

## Phase 6: User Story 4 — Gravity Configuration (Priority: P2)

**Goal**: Set global gravity vector affecting all bodies. Changeable at runtime.

**Independent Test**: Add body, set gravity downward, step, verify body accelerates downward. Set gravity to zero, verify constant velocity.

### Tests for US4

- [x] T036 [P] [US4] Write unit tests in tests/PhysicsSimulation.Tests/CommandHandlerTests.fs (append): test SetGravity updates world gravity; test gravity applied as force (mass*gravity) to each body on step; test changing gravity mid-simulation takes effect on next step; test zero gravity means no gravitational acceleration

### Implementation for US4

- [x] T037 [US4] Implement gravity in src/PhysicsSimulation/World/SimulationWorld.fs: add setGravity function that updates world Gravity vector. Update step to apply gravity as force (body.Mass * Gravity) to each dynamic body before calling BepuFSharp.applyForce, alongside existing ActiveForces
- [x] T038 [US4] Extend CommandHandler.handle in src/PhysicsSimulation/Commands/CommandHandler.fs: add case for SetGravity that calls world setGravity with the command's gravity Vec3
- [ ] T039 [US4] Add integration test in tests/PhysicsSandbox.Integration.Tests/SimulationTests.cs: add body, set gravity to (0, -9.81, 0), step multiple times, verify body position changes downward; set gravity to zero, verify constant velocity
- [x] T040 [US4] Run all tests: `dotnet test PhysicsSandbox.slnx`

**Checkpoint**: Gravity works. All command types from the spec are now implemented.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Surface-area baselines, edge case hardening, final validation.

- [x] T041 Create surface-area baseline tests for all three public modules (SimulationWorld, CommandHandler, SimulationClient) in tests/PhysicsSimulation.Tests/SurfaceAreaTests.fs per constitution Principle V: verify public API signatures match .fsi files via reflection
- [x] T042 Add structured logging to SimulationClient.fs: log connection established, log disconnection with reason, log command received (debug level), log step completed with body count (trace level). Use ILogger from ServiceDefaults
- [x] T043 Add edge case and stress tests in tests/PhysicsSimulation.Tests/SimulationWorldTests.fs (append): verify zero/negative mass rejection produces error CommandAck in AddBody; verify empty world stepping streams valid empty state; verify extremely large forces don't crash the simulation; verify stable operation with 100 bodies added simultaneously (SC-004) — add 100 sphere bodies, step 60 times, confirm all bodies present in state with no errors
- [x] T044 Run full test suite: `dotnet test PhysicsSandbox.slnx` — all unit tests, integration tests, and surface-area baselines pass
- [ ] T045 Run Aspire AppHost: `dotnet run --project src/PhysicsSandbox.AppHost` — verify both server and simulation appear in dashboard, simulation shows as connected

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup (T007 build success) — BLOCKS all user stories
- **US1+US5 (Phase 3)**: Depends on Phase 2 — MVP, must complete first
- **US2 (Phase 4)**: Depends on Phase 3 (needs working lifecycle + streaming to verify bodies in state)
- **US3 (Phase 5)**: Depends on Phase 4 (needs bodies to apply forces to)
- **US4 (Phase 6)**: Depends on Phase 4 (needs bodies to observe gravity). Can run parallel with US3
- **Polish (Phase 7)**: Depends on all user stories complete

### User Story Dependencies

- **US1+US5 (Lifecycle + Streaming)**: Foundation only — the MVP
- **US2 (Body Management)**: Depends on US1+US5 (need step/stream to verify bodies)
- **US3 (Force/Torque/Impulse)**: Depends on US2 (need bodies to apply forces)
- **US4 (Gravity)**: Depends on US2 (need bodies to observe gravity). Independent of US3

### Within Each User Story

- Tests written FIRST, verify they fail before implementation
- World module changes before CommandHandler changes
- CommandHandler before integration tests
- Unit tests before integration tests

### Parallel Opportunities

- T009, T010, T011 (all .fsi files) can run in parallel
- T013, T014 (US1+US5 tests) can run in parallel
- T024, T025 (US2 tests) can run in parallel
- US3 and US4 can run in parallel (both depend on US2, independent of each other)

---

## Parallel Example: Phase 2

```
# Launch all .fsi signature files together (different files, no dependencies):
T009: SimulationWorld.fsi
T010: CommandHandler.fsi
T011: SimulationClient.fsi
```

## Parallel Example: Phase 5 + Phase 6

```
# After US2 completes, US3 and US4 can proceed in parallel:
Phase 5 (US3): T030 → T031-T033 → T034-T035
Phase 6 (US4): T036 → T037-T038 → T039-T040
```

---

## Implementation Strategy

### MVP First (Phase 1-3: US1+US5)

1. Complete Phase 1: Setup (NuGet, project, Aspire registration)
2. Complete Phase 2: Foundational (proto extensions, .fsi signatures, stubs)
3. Complete Phase 3: US1+US5 (lifecycle + state streaming)
4. **STOP and VALIDATE**: Simulation connects, play/pause/step work, empty state streams correctly
5. Build succeeds, all tests pass

### Incremental Delivery

1. Setup + Foundational → project builds
2. US1+US5 → MVP: lifecycle + streaming work (empty world)
3. US2 → bodies visible in state stream
4. US3 → forces/impulses/torques affect bodies
5. US4 → gravity affects all bodies
6. Polish → surface-area baselines, logging, edge cases

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Use `/fsgrpc-proto` skill for T008 (proto extensions)
- Use `/fsgrpc-client` skill for T019 (bidirectional streaming client)
- Commit after each phase checkpoint
- Existing PhysicsServer and its tests must remain passing throughout
