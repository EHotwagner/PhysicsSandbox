# Tasks: Client REPL Library

**Input**: Design documents from `/specs/004-client-repl/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Create the PhysicsClient library project and test project with all dependencies

- [x] T001 Create F# library project `src/PhysicsClient/PhysicsClient.fsproj` (net10.0, Microsoft.NET.Sdk) with PackageReferences for Grpc.Net.Client 2.*, Google.Protobuf 3.*, Spectre.Console 0.49.*, and ProjectReferences to PhysicsSandbox.Shared.Contracts and PhysicsSandbox.ServiceDefaults. Include IsPackable=true, PackageId, and Version properties for `dotnet pack` compliance (constitution engineering constraint)
- [x] T002 Create F# test project `tests/PhysicsClient.Tests/PhysicsClient.Tests.fsproj` with xUnit 2.x, referencing PhysicsClient and PhysicsSandbox.Shared.Contracts
- [x] T003 Add both projects to `PhysicsSandbox.slnx`
- [x] T004 Register PhysicsClient in AppHost: add `builder.AddProject<Projects.PhysicsClient>("client").WithReference(server).WaitFor(server)` in `src/PhysicsSandbox.AppHost/AppHost.cs` and add ProjectReference to PhysicsClient.fsproj in `src/PhysicsSandbox.AppHost/PhysicsSandbox.AppHost.csproj`
- [x] T005 Verify solution builds: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure modules that all user stories depend on — Session management, ID generation, and gRPC connection

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T006 [P] Create `.fsi` signature file `src/PhysicsClient/Bodies/IdGenerator.fsi` defining: `module PhysicsClient.IdGenerator` with `val nextId : shapeKind: string -> string` and `val reset : unit -> unit`
- [x] T007 [P] Create `.fsi` signature file `src/PhysicsClient/Connection/Session.fsi` defining the Session type (opaque), `connect`, `disconnect`, `reconnect`, `isConnected` per contracts/public-api.md
- [x] T008 Add Compile entries to `src/PhysicsClient/PhysicsClient.fsproj` for IdGenerator.fsi, IdGenerator.fs, Session.fsi, Session.fs (in dependency order)
- [x] T009 [P] Write unit tests in `tests/PhysicsClient.Tests/IdGeneratorTests.fs` — test sequential IDs per shape, reset, thread safety (tests MUST fail before implementation)
- [x] T010 [P] Write unit tests in `tests/PhysicsClient.Tests/SessionTests.fs` — test connect failure returns Error, isConnected reflects state, disconnect sets isConnected false (tests MUST fail before implementation)
- [x] T011 Implement `src/PhysicsClient/Bodies/IdGenerator.fs` — thread-safe per-shape-type counter using ConcurrentDictionary, generates "sphere-1", "box-2" style IDs. Reset clears all counters
- [x] T012 Implement `src/PhysicsClient/Connection/Session.fs` — Session record holding GrpcChannel, PhysicsHubClient, CancellationTokenSource, ConcurrentDictionary body registry, mutable LatestState, mutable IsConnected. Use SocketsHttpHandler with SSL bypass (per ViewerClient.fs pattern). Connect starts background StreamState subscription caching latest state and last-updated timestamp. Disconnect cancels CTS and disposes channel. Reconnect creates new channel to same address. Wrap gRPC calls catching RpcException → Result Error
- [x] T013 Verify build and tests pass: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true && dotnet test tests/PhysicsClient.Tests -p:StrideCompilerSkipBuild=true`

**Checkpoint**: Session connects to server, IDs generate correctly, tests pass

---

## Phase 3: User Story 1 — Connect and Control Simulation (Priority: P1) MVP

**Goal**: User can connect to server and send all simulation commands (add/remove body, forces, play/pause/step)

**Independent Test**: Load library, connect, add a sphere, step simulation, verify acknowledgement returned

### Implementation for User Story 1

- [x] T014 [P] [US1] Create `.fsi` signature file `src/PhysicsClient/Commands/SimulationCommands.fsi` per contracts/public-api.md — all simulation command functions taking Session + tuple args returning Result
- [x] T015 [P] [US1][US5] Create `.fsi` signature file `src/PhysicsClient/Commands/ViewCommands.fsi` per contracts/public-api.md — setCamera, setZoom, wireframe (also serves US5)
- [x] T016 [US1] Add Compile entries to `.fsproj` for SimulationCommands.fsi/.fs, ViewCommands.fsi/.fs, Program.fs in correct order
- [x] T017 [P] [US1] Write unit tests in `tests/PhysicsClient.Tests/SimulationCommandsTests.fs` — test Vec3 tuple conversion helper, test clearAll removes all registry entries, test addSphere generates sequential IDs, test addSphere with custom ID uses provided ID (tests MUST fail before implementation)
- [x] T018 [US1] Implement `src/PhysicsClient/Commands/SimulationCommands.fs` — each function constructs the proto message (SimulationCommand with appropriate oneof case), sends via PhysicsHubClient.SendCommand, maps CommandAck to Result. addSphere/addBox/addPlane use IdGenerator for auto-ID, register in Session body registry. removeBody removes from registry. clearAll iterates registry sending RemoveBody for each, returns count. Helper to convert (float*float*float) tuples to Vec3 proto messages
- [x] T019 [US1][US5] Implement `src/PhysicsClient/Commands/ViewCommands.fs` — setCamera constructs SetCamera proto with position/target/up Vec3s, sends via SendViewCommand. setZoom and wireframe similarly (also serves US5)
- [x] T020 [US1] Create `src/PhysicsClient/Program.fs` as minimal entry point for Aspire-managed runs — connect to server via service discovery env vars (same pattern as PhysicsSimulation/Program.fs), keep alive until shutdown
- [x] T021 [US1] Write integration test in `tests/PhysicsSandbox.Integration.Tests/` — connect PhysicsClient to server, send addSphere + step + play/pause, verify CommandAck success. Use Aspire DistributedApplicationTestingBuilder pattern (see existing integration tests)
- [x] T022 [US1] Verify all tests pass: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

**Checkpoint**: User Story 1 fully functional — connect, send all commands, receive acks, auto-ID generation works

---

## Phase 4: User Story 2 — Ready-Made Body Builders (Priority: P2)

**Goal**: Pre-configured body presets and random generators for quick scene population

**Independent Test**: Call `bowlingBall`, `randomSpheres 10`, `stack 5` — verify bodies appear with expected properties

### Implementation for User Story 2

- [x] T023 [P] [US2] Create `.fsi` signature file `src/PhysicsClient/Bodies/Presets.fsi` per contracts/public-api.md — marble, bowlingBall, beachBall, crate, brick, boulder, die; each takes optional `?position` and `?mass` overrides
- [x] T024 [P] [US2] Create `.fsi` signature file `src/PhysicsClient/Bodies/Generators.fsi` per contracts/public-api.md — randomSpheres, randomBoxes, randomBodies, stack, row, grid, pyramid
- [x] T025 [US2] Add Compile entries to `.fsproj` for Presets.fsi/.fs, Generators.fsi/.fs
- [x] T026 [P] [US2] Write unit tests in `tests/PhysicsClient.Tests/PresetsTests.fs` — verify each preset produces correct shape type and mass values, verify mass override replaces default when provided (tests MUST fail before implementation)
- [x] T027 [P] [US2] Write unit tests in `tests/PhysicsClient.Tests/GeneratorsTests.fs` — test stack positions are vertically spaced, test grid produces rows*cols bodies, test pyramid layer counts, test randomSpheres with same seed produces same results, test count=0 returns error (tests MUST fail before implementation)
- [x] T028 [US2] Implement `src/PhysicsClient/Bodies/Presets.fs` — each preset function creates a body with hardcoded shape/mass/size from data-model.md preset table. Delegates to SimulationCommands.addSphere/addBox. Optional position and mass overrides, defaults to preset values. Returns body ID
- [x] T029 [US2] Implement `src/PhysicsClient/Bodies/Generators.fs` — randomSpheres/randomBoxes/randomBodies use System.Random with optional seed for reproducibility. Randomize position (spread ±5 on X/Z, 1-10 on Y), radius (0.05-0.5), mass (0.1-50). stack: N boxes vertically spaced by box height. row: N spheres along X axis. grid: NxM boxes on ground plane. pyramid: layers decreasing by 1 each level. All return list of created body IDs. Validate count > 0
- [x] T030 [US2] Verify all tests pass

**Checkpoint**: User Story 2 complete — presets and generators create bodies with correct parameters

---

## Phase 5: User Story 3 — Body Steering and Motion Control (Priority: P2)

**Goal**: High-level steering functions — push, launch, spin, stop — translating intent to physics commands

**Independent Test**: Add a body, call `push` with direction East, verify impulse applied in +X direction

### Implementation for User Story 3

- [x] T031 [US3] Create `.fsi` signature file `src/PhysicsClient/Steering/Steering.fsi` per contracts/public-api.md — Direction DU, push, pushVec, launch, spin, stop
- [x] T032 [US3] Add Compile entries to `.fsproj` for Steering.fsi/.fs
- [x] T033 [P] [US3] Write unit tests in `tests/PhysicsClient.Tests/SteeringTests.fs` — test Direction-to-Vec3 mapping for all 6 directions, test launch vector calculation (normalized direction * speed), test stop counter-impulse calculation (-velocity * mass) (tests MUST fail before implementation)
- [x] T034 [US3] Implement `src/PhysicsClient/Steering/Steering.fs` — Direction DU maps to unit Vec3 (Up=+Y, Down=-Y, North=-Z, South=+Z, East=+X, West=-X). push: direction vector * magnitude → applyImpulse. pushVec: raw vector → applyImpulse. launch: get body position from cached state, compute normalized direction to target * speed → applyImpulse. spin: direction as axis * magnitude → applyTorque. stop: clearForces + get current velocity from cached state, apply counter-impulse (-velocity * mass)
- [x] T035 [US3] Verify all tests pass

**Checkpoint**: User Story 3 complete — steering functions produce correct force/impulse/torque vectors

---

## Phase 6: User Story 4 — State Display and Monitoring (Priority: P3)

**Goal**: Formatted state output via Spectre.Console — body table, inspection, status, live-watch

**Independent Test**: Add bodies, call `listBodies` — verify formatted table printed to stdout

### Implementation for User Story 4

- [x] T036 [P] [US4] Create `.fsi` signature file `src/PhysicsClient/Display/StateDisplay.fsi` per contracts/public-api.md — listBodies, inspect, status, snapshot
- [x] T037 [P] [US4] Create `.fsi` signature file `src/PhysicsClient/Display/LiveWatch.fsi` per contracts/public-api.md — watch with optional filters
- [x] T038 [US4] Add Compile entries to `.fsproj` for StateDisplay.fsi/.fs, LiveWatch.fsi/.fs
- [x] T039 [P] [US4] Write unit tests in `tests/PhysicsClient.Tests/StateDisplayTests.fs` — test Vec3 formatting helper, test velocity magnitude calculation for filter, test shape filter matching logic, test empty state produces "no bodies" output (tests MUST fail before implementation)
- [x] T040 [US4] Implement `src/PhysicsClient/Display/StateDisplay.fs` — listBodies: create Spectre.Console Table with columns (ID, Shape, Position, Velocity, Mass), iterate bodies from cached state, format Vec3 as "(x, y, z)" with 2 decimal places, render with AnsiConsole.Write. inspect: Panel with body details (all fields including angular velocity and orientation). status: Panel showing time, running/paused, body count. snapshot: return raw LatestState. Handle no-bodies case with MarkupLine "[yellow]No bodies in simulation[/]". If cached state is older than 5 seconds, display "[dim]Last updated: Xs ago[/]" below the table
- [x] T041 [US4] Implement `src/PhysicsClient/Display/LiveWatch.fs` — use AnsiConsole.Live() context. Loop: read cached state, apply filters (bodyIds list, shapeFilter string matching shape case name, minVelocity threshold on velocity magnitude), render filtered table, sleep 100ms. CancellationTokenSource linked to Console.CancelKeyPress for Ctrl+C cancellation. On cancel, restore console and return
- [x] T042 [US4] Verify all tests pass

**Checkpoint**: User Story 4 complete — tables, inspection, and live-watch all render correctly

---

## Phase 7: User Story 5 — Viewer Control from REPL (Priority: P3)

**Goal**: Camera, zoom, wireframe control functions forwarded to viewer via server

**Independent Test**: Call `setCamera`, verify ViewCommand sent to server

*Note: ViewCommands module was already implemented in Phase 3 (T015, T017) as part of US1 since it's a thin wrapper over SendViewCommand. This phase adds integration verification only.*

### Implementation for User Story 5

- [x] T043 [US5] Write integration test in `tests/PhysicsSandbox.Integration.Tests/` — connect client, send SetCamera/SetZoom/ToggleWireframe commands, verify CommandAck success via Aspire test builder
- [x] T044 [US5] Verify all tests pass

**Checkpoint**: User Story 5 verified — viewer commands flow through server correctly

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Surface area tests, .fsproj ordering, final validation

- [x] T045 [P] Write surface area baseline tests in `tests/PhysicsClient.Tests/SurfaceAreaTests.fs` — test public API for all 8 modules (Session, SimulationCommands, ViewCommands, Presets, Generators, Steering, StateDisplay, LiveWatch) matches expected member lists, following pattern from `tests/PhysicsSimulation.Tests/SurfaceAreaTests.fs`
- [x] T046 Verify final `.fsproj` Compile ordering is correct (IdGenerator → Session → SimulationCommands → ViewCommands → Presets → Generators → Steering → StateDisplay → LiveWatch → Program)
- [x] T047 Update `CLAUDE.md` — add PhysicsClient to Project Structure section, update test counts, add any gotchas discovered during implementation
- [x] T048 Run full solution build and test suite: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true && dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [x] T049 Verify `dotnet pack src/PhysicsClient/PhysicsClient.fsproj --no-build` succeeds (constitution: every library MUST be packable)
- [x] T050 Verify FSI loadability: run `dotnet fsi` with `#r` directives loading PhysicsClient.dll and Shared.Contracts.dll, execute `Session.connect` and confirm Result returned (validates FR-010)
- [x] T051 Validate quickstart.md examples work against running server (manual or scripted)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — MVP, do this first
- **US2 (Phase 4)**: Depends on Phase 3 (uses SimulationCommands.addSphere/addBox)
- **US3 (Phase 5)**: Depends on Phase 3 (uses SimulationCommands.applyImpulse/applyTorque/clearForces + Session cached state)
- **US4 (Phase 6)**: Depends on Phase 2 (reads Session cached state only), can run parallel with US2/US3
- **US5 (Phase 7)**: Depends on Phase 3 (ViewCommands already implemented)
- **Polish (Phase 8)**: Depends on all user stories complete

### User Story Dependencies

```
Phase 1 (Setup)
  └── Phase 2 (Foundation: Session + IdGenerator)
        ├── Phase 3 (US1: Commands) ← MVP
        │     ├── Phase 4 (US2: Presets + Generators)
        │     ├── Phase 5 (US3: Steering)
        │     └── Phase 7 (US5: Viewer verification)
        └── Phase 6 (US4: Display) ← can parallel with US2/US3
              └── Phase 8 (Polish)
```

### Within Each User Story

- .fsi signature files first
- Tests before implementation (TDD per constitution — tests MUST fail before implementation)
- .fsproj Compile entries before build verification

### Parallel Opportunities

- T006 + T008: IdGenerator.fsi and Session.fsi can be written in parallel
- T010 + T011: IdGenerator and Session tests in parallel
- T014 + T015: SimulationCommands.fsi and ViewCommands.fsi in parallel
- T023 + T024: Presets.fsi and Generators.fsi in parallel
- T028 + T029: Presets and Generators tests in parallel
- T036 + T037: StateDisplay.fsi and LiveWatch.fsi in parallel
- Phase 4 (US2) + Phase 5 (US3) + Phase 6 (US4): can run in parallel after US1

---

## Parallel Example: User Story 1

```text
# .fsi files in parallel:
Task T014: "Create SimulationCommands.fsi"
Task T015: "Create ViewCommands.fsi"

# Unit tests (before implementation):
Task T017: "Unit tests for SimulationCommands (must fail)"

# Then implementation + integration test
```

## Parallel Example: User Story 2

```text
# .fsi files in parallel:
Task T023: "Create Presets.fsi"
Task T024: "Create Generators.fsi"

# Tests in parallel (before implementation):
Task T026: "Unit tests for Presets (must fail)"
Task T027: "Unit tests for Generators (must fail)"

# Then implementation
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T005)
2. Complete Phase 2: Foundation — Session + IdGenerator (T006-T013)
3. Complete Phase 3: US1 — All simulation commands work (T014-T022)
4. **STOP and VALIDATE**: Connect in FSI, add bodies, play/pause/step
5. This alone delivers the core value: programmatic simulation control

### Incremental Delivery

1. Setup + Foundation → Connection works
2. US1 (Commands) → Full simulation control (MVP!)
3. US2 (Presets/Generators) → Quick scene building
4. US3 (Steering) → Intuitive motion control
5. US4 (Display) → Formatted state visibility
6. US5 (Viewer) → Camera scripting verified
7. Polish → Surface area tests, docs, final validation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Constitution requires .fsi for every public module and surface area baseline tests
- Use `fsgrpc-client` skill patterns for gRPC client setup (contract-first with standard Grpc.Net.Client)
- Use `Spectre.Console` for all formatted display (tables, panels, live context)
- All command functions return `Result<'T, string>` — catch RpcException, map to Error string
- Tuple `(float * float * float)` for user-facing vector args, convert to proto Vec3 internally
