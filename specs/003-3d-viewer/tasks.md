# Tasks: 3D Viewer

**Input**: Design documents from `/specs/003-3d-viewer/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create PhysicsViewer project and test project with all dependencies. Use `/stride3d-fsharp` skill for project setup (§Project Setup, §Building & Testing, §Linux prerequisites).

- [x] T001 Create src/PhysicsViewer/PhysicsViewer.fsproj as F# console project targeting net10.0 with: RuntimeIdentifier linux-x64, StrideGraphicsApi OpenGL, StrideCompilerSkipBuild=true (conditional for CI). Add PackageReferences: Stride.CommunityToolkit, Stride.CommunityToolkit.Bepu, Stride.CommunityToolkit.Skyboxes, Stride.CommunityToolkit.Linux (all 1.0.0-preview.62), Grpc.Net.Client 2.*. Add ProjectReferences to PhysicsSandbox.Shared.Contracts and PhysicsSandbox.ServiceDefaults. Add MSBuild target to copy glslangValidator.bin from NuGet cache to linux-x64/ per stride3d-fsharp skill §Building & Testing
- [x] T002 [P] Create tests/PhysicsViewer.Tests/PhysicsViewer.Tests.fsproj as F# test project targeting net10.0 with: RuntimeIdentifier linux-x64, IsPackable false. Add PackageReferences: xunit 2.9.3, xunit.runner.visualstudio 3.1.4, Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4. Add ProjectReference to PhysicsViewer. Include Compile items for SceneManagerTests.fs, CameraControllerTests.fs, SurfaceAreaTests.fs (empty files initially)
- [x] T003 Add both projects to PhysicsSandbox.slnx. Add PhysicsViewer ProjectReference to src/PhysicsSandbox.AppHost/PhysicsSandbox.AppHost.csproj. Verify solution loads: `dotnet build PhysicsSandbox.slnx` (will fail until stubs exist — that's expected)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extend proto contract with StreamViewCommands, add server-side support with tests, create .fsi signature files for all viewer modules, create stub implementations so the project compiles. Use `/fsgrpc-proto` skill for proto work. Use `/fsgrpc-client` skill reference for client signature design.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Extend src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto: add `rpc StreamViewCommands (StateRequest) returns (stream ViewCommand);` to the PhysicsHub service block (after StreamState). No new messages needed — StateRequest and ViewCommand already exist. Verify proto compiles: `dotnet build src/PhysicsSandbox.Shared.Contracts/`
- [x] T005 [P] Add `readViewCommand` to src/PhysicsServer/Hub/MessageRouter.fsi: `val readViewCommand: MessageRouter -> CancellationToken -> Task<ViewCommand option>`. Implement in src/PhysicsServer/Hub/MessageRouter.fs: read from ViewCommandChannel.Reader (mirror existing readCommand pattern)
- [x] T006 Add `StreamViewCommands` override to src/PhysicsServer/Services/PhysicsHubService.fsi: `override StreamViewCommands: request: StateRequest * responseStream: IServerStreamWriter<ViewCommand> * context: ServerCallContext -> Task`. Implement in src/PhysicsServer/Services/PhysicsHubService.fs: loop calling readViewCommand and writing to responseStream until context.CancellationToken is cancelled (mirror existing StreamState pattern). Depends on T005 .fsi update
- [x] T007 [P] Add readViewCommand unit tests in tests/PhysicsServer.Tests/MessageRouterTests.fs: test readViewCommand returns submitted ViewCommand, test readViewCommand returns None on cancellation, test readViewCommand blocks when no commands available. Follow existing readCommand test patterns in the same file
- [x] T008 [P] Create src/PhysicsViewer/Rendering/SceneManager.fsi per specs/003-3d-viewer/contracts/viewer-modules.md: module PhysicsViewer.SceneManager with ShapeKind DU (Sphere|Box|Unknown), opaque SceneState type, create, classifyShape, applyState, simulationTime, isRunning, applyWireframe, isWireframe functions. Wireframe state lives in SceneManager (rendering concern)
- [x] T009 [P] Create src/PhysicsViewer/Rendering/CameraController.fsi per specs/003-3d-viewer/contracts/viewer-modules.md: module PhysicsViewer.CameraController with opaque CameraState type, defaultCamera, applySetCamera, applySetZoom, applyInput, applyToCamera functions. Note: wireframe is owned by SceneManager, not CameraController
- [x] T010 [P] Create src/PhysicsViewer/Streaming/ViewerClient.fsi per specs/003-3d-viewer/contracts/viewer-modules.md: module PhysicsViewer.ViewerClient with streamState and streamViewCommands functions taking serverAddress, ConcurrentQueue, CancellationToken
- [x] T011 Create minimal stub implementations for all three viewer modules (SceneManager.fs, CameraController.fs, ViewerClient.fs) and a stub Program.fs entry point so the project compiles against the .fsi signatures. Program.fs should have `[<EntryPoint>] let main _ = 0` initially. Verify build succeeds: `dotnet build PhysicsSandbox.slnx`
- [x] T012 Register PhysicsViewer in src/PhysicsSandbox.AppHost/AppHost.cs: add `builder.AddProject<Projects.PhysicsViewer>("viewer").WithReference(server).WaitFor(server);` after the simulation registration. Verify build: `dotnet build PhysicsSandbox.slnx`
- [x] T013 Verify all existing tests still pass: `dotnet test tests/PhysicsServer.Tests/ && dotnet test tests/PhysicsSimulation.Tests/`

**Checkpoint**: Foundation ready — proto extended, server supports StreamViewCommands with tests, viewer project scaffolded with .fsi contracts, all existing tests pass

---

## Phase 3: User Story 1 — View Live Simulation (Priority: P1) 🎯 MVP

**Goal**: Viewer connects to the server, subscribes to StreamState, and renders all physics bodies as colored 3D shapes (spheres blue, boxes orange) at correct positions and orientations. Updates in real time as simulation advances. Ground grid visible at Y=0.

**Independent Test**: Start the simulation with bodies, launch the viewer, verify bodies appear at correct positions and update as simulation steps.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation (constitution §VI TDD)**

- [x] T014 [P] [US1] Write tests/PhysicsViewer.Tests/SceneManagerTests.fs: test classifyShape returns Sphere for proto Sphere, Box for proto Box, Unknown for null/unset shape. Test simulationTime and isRunning return values from SceneState after create. Test isWireframe defaults to false after create

### Implementation for User Story 1

- [x] T015 [US1] Implement ViewerClient.streamState in src/PhysicsViewer/Streaming/ViewerClient.fs: create GrpcChannel to serverAddress, create PhysicsHub.PhysicsHubClient, call StreamState with empty StateRequest, loop reading ResponseStream.MoveNext and enqueuing SimulationState into ConcurrentQueue. Handle RpcException for connection loss. Use `/fsgrpc-client` skill §Server streaming pattern. Use CancellationToken for graceful shutdown. Log structured events via ILogger: "ViewerClient connected to {ServerAddress}", "State received with {BodyCount} bodies", "Connection lost: {Exception}", "Reconnecting to {ServerAddress}"
- [x] T016 [US1] Implement SceneManager in src/PhysicsViewer/Rendering/SceneManager.fs: (a) classifyShape maps proto Shape oneof to ShapeKind DU. (b) SceneState holds Map<string, Entity> for tracked bodies + latest SimulationState metadata + wireframe flag. (c) applyState diffs incoming Body list against current map: create new entities (Sphere→PrimitiveModelType.Sphere blue, Box→PrimitiveModelType.Cube orange, Unknown→Sphere red), update existing entity Transform.Position and Transform.Rotation from proto Vec3/Vec4, remove entities no longer in state. (d) applyWireframe toggles wireframe flag and swaps materials on all tracked entities. (e) isWireframe returns current flag. Use stride3d-fsharp StrideHelpers.createColouredPrimitive pattern for entity creation. Use Vector3 static methods per stride3d-fsharp §Critical F# Interop
- [x] T017 [US1] Implement Program.fs in src/PhysicsViewer/Program.fs: (a) Create a background WebApplication host with builder.AddServiceDefaults() and app.MapDefaultEndpoints() running on a separate thread — provides /health, /alive endpoints for Aspire dashboard monitoring plus OpenTelemetry tracing per constitution Principle VII and research.md R5. Use ILogger from the host's service provider for structured logging in ViewerClient and SceneManager. (b) Create Game instance. (c) In start callback: call game.AddGraphicsCompositor().AddCleanUIStage(), game.Add3DCamera() — do NOT call Add3DCameraController() as it conflicts with custom CameraController (US2); the custom CameraController handles all orbit/pan/zoom and REPL commands need to override camera state programmatically. Call game.AddDirectionalLight(), game.Add3DGround() for physics surface, game.AddSkybox(), GameExtensions.AddGroundGizmo(game, Nullable<Vector3>(Vector3(-5f, 0.1f, -5f)), showAxisName = true) for visual grid reference at Y=0 per FR-016. (d) Create ConcurrentQueue<SimulationState>. (e) Resolve server address from env var services__server__https__0 (Aspire service discovery). (f) Start ViewerClient.streamState on background Task. (g) In update callback: drain stateQueue via TryDequeue loop, call SceneManager.applyState for latest state. (h) Call game.Run(start, update) on main thread. (i) On exit: cancel CancellationTokenSource, stop background host. Log structured events: "Viewer starting", "Server address resolved: {Address}", "Game loop started", "Viewer shutting down"
- [x] T018 [US1] Verify build and tests: `dotnet build -p:StrideCompilerSkipBuild=true PhysicsSandbox.slnx && dotnet test tests/PhysicsViewer.Tests/ -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Viewer renders live simulation state — US1 complete and independently testable

---

## Phase 4: User Story 2 — Camera Control via Commands (Priority: P2)

**Goal**: Viewer supports camera control from both interactive mouse/keyboard input (orbit, pan, zoom) and REPL commands (SetCamera, SetZoom). REPL commands override current camera position when received.

**Independent Test**: (a) Drag mouse in viewer window to orbit camera. (b) Send SetCamera command from client, verify viewer camera moves to specified position/target.

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation (constitution §VI TDD)**

- [x] T019 [P] [US2] Write tests/PhysicsViewer.Tests/CameraControllerTests.fs: test defaultCamera returns expected position/target/up/zoom. Test applySetCamera overrides position and target. Test applySetZoom updates zoom level. Test applySetCamera after applySetZoom preserves zoom

### Implementation for User Story 2

- [x] T020 [US2] Implement ViewerClient.streamViewCommands in src/PhysicsViewer/Streaming/ViewerClient.fs: create PhysicsHub.PhysicsHubClient (reuse channel from streamState or create new), call StreamViewCommands with empty StateRequest, loop reading ResponseStream.MoveNext and enqueuing ViewCommand into ConcurrentQueue<ViewCommand>. Handle RpcException for connection loss. Use `/fsgrpc-client` skill §Server streaming pattern
- [x] T021 [US2] Implement CameraController in src/PhysicsViewer/Rendering/CameraController.fs: (a) CameraState record with Position, Target, Up (Vector3), ZoomLevel (float). (b) defaultCamera returns position (10, 8, 10), target (0, 0, 0), up Y-axis, zoom 1.0. (c) applySetCamera replaces position/target/up from proto SetCamera. (d) applySetZoom updates ZoomLevel. (e) applyInput reads InputManager for mouse drag (orbit around target), scroll (zoom), middle-click (pan). Use Vector3 static methods per stride3d-fsharp interop. (f) applyToCamera sets Entity.Transform.Position and computes LookAt rotation via Matrix.LookAtRH or equivalent
- [x] T022 [US2] Wire camera + view commands in src/PhysicsViewer/Program.fs: (a) Create ConcurrentQueue<ViewCommand>. (b) Start ViewerClient.streamViewCommands on background Task. (c) In update loop: drain viewCmdQueue, dispatch ViewCommand oneof — SetCamera → CameraController.applySetCamera, SetZoom → CameraController.applySetZoom. (d) Call CameraController.applyInput each frame for interactive mouse/keyboard control. (e) Call CameraController.applyToCamera to sync camera entity transform
- [x] T023 [US2] Verify build and tests: `dotnet build -p:StrideCompilerSkipBuild=true PhysicsSandbox.slnx && dotnet test tests/PhysicsViewer.Tests/ -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Camera controllable via mouse/keyboard and REPL commands — US2 complete

---

## Phase 5: User Story 3 — Wireframe Toggle (Priority: P3)

**Goal**: ToggleWireframe commands switch all bodies between solid and wireframe rendering modes.

**Independent Test**: Send ToggleWireframe enabled=true, verify bodies render as wireframe. Send enabled=false, verify solid rendering resumes.

### Implementation for User Story 3

- [x] T024 [US3] Implement wireframe toggle in src/PhysicsViewer/Rendering/SceneManager.fs: implement applyWireframe — when ToggleWireframe command received, update wireframe flag in SceneState, iterate all tracked entities and swap their material between standard material (createMaterial) and wireframe material (createFlatMaterial or custom wireframe shader). isWireframe returns current flag from SceneState. New entities created via applyState should use the current wireframe mode
- [x] T025 [US3] Wire wireframe in src/PhysicsViewer/Program.fs update loop: dispatch ToggleWireframe from ViewCommand queue, call SceneManager.applyWireframe, pass updated SceneState so subsequent applyState calls create entities in the correct render mode
- [x] T026 [US3] Verify build and tests: `dotnet build -p:StrideCompilerSkipBuild=true PhysicsSandbox.slnx && dotnet test tests/PhysicsViewer.Tests/ -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Wireframe toggle works — US3 complete

---

## Phase 6: User Story 4 — Simulation Status Display (Priority: P3)

**Goal**: Viewer displays simulation time and running/paused indicator as a text overlay.

**Independent Test**: Verify simulation time value and running/paused text appear in the viewer window matching the streamed state.

### Implementation for User Story 4

- [x] T027 [US4] Implement status overlay in src/PhysicsViewer/Program.fs (or a new UIOverlay module if complexity warrants): use Stride UI system to display text elements showing simulation time (from SceneManager.simulationTime) and running/paused status (from SceneManager.isRunning). Reference stride3d-fsharp UIHelper.fs for Stride UI patterns. Use AddCleanUIStage (already set up in US1) for the UI rendering pipeline. Position text in a corner of the screen using Thickness for margins
- [x] T028 [US4] Update the game update loop in Program.fs to refresh the status text each frame from the latest SceneState metadata
- [x] T029 [US4] Verify build and tests: `dotnet build -p:StrideCompilerSkipBuild=true PhysicsSandbox.slnx && dotnet test tests/PhysicsViewer.Tests/ -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Status overlay displays simulation time and run state — US4 complete

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, public API baselines, integration tests, final validation

- [x] T030 [P] Create tests/PhysicsViewer.Tests/PublicApiBaseline.txt with serialized public API surface for SceneManager, CameraController, ViewerClient modules. Follow the format in tests/PhysicsSimulation.Tests/PublicApiBaseline.txt. Constitution Principle V: surface-area baseline files MUST exist for each public module
- [x] T031 [P] Write tests/PhysicsViewer.Tests/SurfaceAreaTests.fs: verify actual public API surface matches PublicApiBaseline.txt. Any divergence must fail the build. Follow the pattern in tests/PhysicsSimulation.Tests/SurfaceAreaTests.fs
- [x] T032 [P] Add StreamViewCommands integration test in tests/PhysicsSandbox.Integration.Tests/ServerHubTests.cs: verify viewer can subscribe and receive view commands sent via SendViewCommand. Use Aspire DistributedApplicationTestingBuilder pattern from existing tests
- [x] T033 Handle edge cases in src/PhysicsViewer/Program.fs and SceneManager.fs: (a) empty scene (no bodies) — show ground grid only, (b) unknown shape type — render as red sphere fallback, (c) connection lost — display last known state, log structured warning, attempt reconnect with exponential backoff, (d) zero-body state after previously having bodies — remove all entities from scene
- [x] T034 Run full test suite: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — verify all existing PhysicsServer tests, PhysicsSimulation (37 tests), and new PhysicsViewer tests pass
- [x] T035 Verify quickstart: `dotnet build PhysicsSandbox.slnx` succeeds cleanly with no warnings on viewer project

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3–6)**: All depend on Foundational phase completion
  - US1 (Phase 3): No dependencies on other stories — **this is the MVP**
  - US2 (Phase 4): Depends on US1 (needs Program.fs game loop infrastructure)
  - US3 (Phase 5): Depends on US2 (needs ViewCommand queue wiring from US2)
  - US4 (Phase 6): Depends on US1 (needs SceneState metadata)
- **Polish (Phase 7)**: Depends on all user stories being complete

### Within Each User Story

- .fsi signatures defined in Phase 2 (foundational)
- Test tasks before or parallel with implementation tasks (constitution §VI TDD)
- Build verification at end of each story phase
- Story complete before moving to next priority

### Parallel Opportunities

- T001 and T002 can run in parallel (different projects)
- T005, T007, T008, T009, T010 can run in parallel (different files)
- T014 (SceneManagerTests) can run in parallel with T015–T017
- T019 (CameraControllerTests) can run in parallel with T020–T022
- T030, T031, T032 can run in parallel (different files/projects)

---

## Parallel Example: User Story 1

```bash
# After Phase 2 foundational is complete:

# TDD: T014 (SceneManagerTests — write tests first, verify they fail)
# Parallel with tests: T015 → T016 → T017 (ViewerClient → SceneManager → Program.fs wiring)

# Then: T018 (verify all tests pass)
```

## Parallel Example: User Story 2

```bash
# After US1 is complete:

# TDD: T019 (CameraControllerTests — write tests first, verify they fail)
# Parallel with tests: T020 → T021 → T022 (ViewerClient.streamViewCommands → CameraController → Program.fs wiring)

# Then: T023 (verify all tests pass)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 — viewer renders simulation bodies
4. **STOP and VALIDATE**: Launch AppHost, verify viewer shows bodies updating
5. Demo if ready

### Incremental Delivery

1. Setup + Foundational → project compiles, proto extended, stubs in place
2. Add US1 → viewer renders bodies live → **MVP!**
3. Add US2 → camera control via mouse + REPL commands
4. Add US3 → wireframe toggle
5. Add US4 → status overlay
6. Polish → edge cases, API baselines, integration tests

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Use `/stride3d-fsharp` skill for all Stride3D entity creation, scene setup, input handling, F# interop
- Use `/fsgrpc-proto` skill for T004 (proto extension)
- Use `/fsgrpc-client` skill for T015 and T020 (gRPC streaming clients)
- Build with `-p:StrideCompilerSkipBuild=true` for headless/CI environments
- Commit after each phase checkpoint
- Existing PhysicsServer and PhysicsSimulation tests must remain passing throughout
