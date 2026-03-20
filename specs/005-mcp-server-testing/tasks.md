# Tasks: MCP Server and Integration Testing

**Input**: Design documents from `/specs/005-mcp-server-testing/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the MCP server project and add solution references

- [x] T001 Create F# console project `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` with dependencies: ModelContextProtocol 1.1.0, Microsoft.Extensions.Hosting, Grpc.Net.Client 2.x, Google.Protobuf 3.x. Add project references to PhysicsSandbox.Shared.Contracts. Add to PhysicsSandbox.slnx solution file.
- [x] T002 Verify existing tests pass by running `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — confirm 118 unit tests + 5 integration tests all green before any changes.

---

## Phase 2: User Story 2 — Fix Known Connection Issues (Priority: P1)

**Goal**: Fix simulation SSL bypass and viewer DISPLAY env so the full Aspire stack works end-to-end with real physics data.

**Independent Test**: Start Aspire stack, verify simulation stays connected, send AddBody + Play, confirm state stream shows body with changing positions.

### Implementation for User Story 2

- [x] T003 [US2] Add SSL bypass and HTTP/2 channel creation to `src/PhysicsSimulation/Client/SimulationClient.fs`. Replace `GrpcChannel.ForAddress(serverAddress)` with a `createChannel` function matching the pattern from `src/PhysicsClient/Connection/Session.fs` lines 22-31: SocketsHttpHandler with `EnableMultipleHttp2Connections = true`, `RemoteCertificateValidationCallback` that returns true, and HTTP/2 version policy for plain HTTP addresses.
- [x] T004 [US2] Add exponential backoff reconnection loop to `src/PhysicsSimulation/Client/SimulationClient.fs`. Wrap the existing connection + main-loop logic inside an outer retry loop: on RpcException or connection failure, log warning, wait with exponential backoff (1s initial, 2x multiplier, 10s max), and reconnect using the same BepuPhysics world instance. Only exit on CancellationToken cancellation.
- [x] T005 [P] [US2] Add DISPLAY environment variable to viewer registration in `src/PhysicsSandbox.AppHost/AppHost.cs`. Add `.WithEnvironment("DISPLAY", Environment.GetEnvironmentVariable("DISPLAY") ?? ":0")` to the `builder.AddProject<Projects.PhysicsViewer>("viewer")` chain.
- [x] T006 [US2] Update SimulationClient.fsi signature file at `src/PhysicsSimulation/Client/SimulationClient.fsi` if the `run` function signature changes (e.g., if reconnection changes the return type or adds parameters). Ensure public API surface matches implementation.
- [x] T007 [US2] Run full test suite `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` to confirm zero regressions from bug fixes. All 118 unit + 5 integration tests must pass.

**Checkpoint**: Aspire stack starts with simulation maintaining a stable gRPC connection. Commands sent reach the simulation and produce real physics state updates.

---

## Phase 3: User Story 1 — MCP-Based Physics Exploration (Priority: P1) 🎯 MVP

**Goal**: Build an MCP server with ~15 fine-grained tools that wraps all PhysicsHub RPCs, enabling interactive physics debugging via AI assistants.

**Independent Test**: Start MCP server alongside Aspire stack, invoke each tool, verify gRPC calls reach server and return correct responses.

### Implementation for User Story 1

- [x] T008 [US1] Implement GrpcConnection module in `src/PhysicsSandbox.Mcp/GrpcConnection.fs`. Create a `createChannel` function with SSL bypass (same pattern as PhysicsClient Session.fs). Create a `GrpcConnection` class/type registered as DI singleton that holds: PhysicsHub.PhysicsHubClient, background StreamState subscription task with cached SimulationState option, LastUpdateTime DateTimeOffset, and StreamConnected bool. The background stream should use exponential backoff reconnection (1s → 10s). Accept server address from DI configuration (command-line arg or default `https://localhost:7180`).
- [x] T009 [US1] Write signature file `src/PhysicsSandbox.Mcp/GrpcConnection.fsi` exposing the public API of GrpcConnection: channel creation, latest state access, connection status, and the DI-registrable type.
- [x] T010 [US1] Implement SimulationTools in `src/PhysicsSandbox.Mcp/SimulationTools.fs`. Create an F# type with `[<McpServerToolType>]` containing 10 static methods with `[<McpServerTool>]` and `[<Description>]` attributes: add_body (shape, position, mass params), apply_force (body_id, x/y/z), apply_impulse (body_id, x/y/z), apply_torque (body_id, x/y/z), set_gravity (x/y/z), step (no params), play (no params), pause (no params), remove_body (body_id), clear_forces (body_id). Each tool receives GrpcConnection via DI, constructs the proto SimulationCommand, calls SendCommand, and returns the CommandAck as a formatted string.
- [x] T011 [P] [US1] Write signature file `src/PhysicsSandbox.Mcp/SimulationTools.fsi` exposing the SimulationTools type and all 10 tool methods.
- [x] T012 [P] [US1] Implement ViewTools in `src/PhysicsSandbox.Mcp/ViewTools.fs`. Create an F# type with `[<McpServerToolType>]` containing 3 static methods with `[<McpServerTool>]`: set_camera (pos_x/y/z, target_x/y/z), set_zoom (level), toggle_wireframe (no params). Each constructs a ViewCommand proto message, calls SendViewCommand, returns formatted ack.
- [x] T013 [P] [US1] Write signature file `src/PhysicsSandbox.Mcp/ViewTools.fsi` exposing the ViewTools type and all 3 tool methods.
- [x] T014 [P] [US1] Implement QueryTools in `src/PhysicsSandbox.Mcp/QueryTools.fs`. Create an F# type with `[<McpServerToolType>]` containing 2 static methods with `[<McpServerTool>]`: get_state (reads cached SimulationState from GrpcConnection, formats as human-readable table with body id/position/velocity/mass/shape, simulation time, running status, and staleness indicator showing seconds since last update), get_status (returns server address, stream connected bool, last update time).
- [x] T015 [P] [US1] Write signature file `src/PhysicsSandbox.Mcp/QueryTools.fsi` exposing the QueryTools type and both tool methods.
- [x] T016 [US1] Implement Program.fs entry point in `src/PhysicsSandbox.Mcp/Program.fs`. Use `Host.CreateApplicationBuilder(args)`, configure logging to stderr (`AddConsole` with `LogToStandardErrorThreshold = LogLevel.Trace`), parse server address from first command-line arg (default `https://localhost:7180`), register GrpcConnection as singleton in DI, call `.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()`, then `builder.Build().RunAsync()`.
- [x] T017 [US1] Set correct file ordering in `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` — F# requires source files in dependency order: GrpcConnection.fsi, GrpcConnection.fs, SimulationTools.fsi, SimulationTools.fs, ViewTools.fsi, ViewTools.fs, QueryTools.fsi, QueryTools.fs, Program.fs.
- [x] T018 [US1] Create surface-area baseline files for all public MCP modules. For each .fsi file (GrpcConnection.fsi, SimulationTools.fsi, ViewTools.fsi, QueryTools.fsi), generate a serialized public API snapshot in `src/PhysicsSandbox.Mcp/baselines/` matching the pattern used by other services. Add a build-time or test-time check that validates the baseline matches the current .fsi surface. (Constitution Principle V requirement.)
- [x] T019 [US1] Build and smoke-test the MCP server: `dotnet build src/PhysicsSandbox.Mcp` must succeed. Verify tool discovery works by starting the server and confirming it responds to MCP tool listing (or test via integration with Claude Code MCP config).

**Checkpoint**: MCP server builds, starts, discovers all ~15 tools, and can send commands to a running PhysicsServer.

---

## Phase 4: User Story 3 — Comprehensive Regression Test Suite (Priority: P2)

**Goal**: Expand integration tests from 5 to 20+ covering command routing, state streaming, simulation lifecycle, and error conditions with real physics data.

**Independent Test**: Run `dotnet test tests/PhysicsSandbox.Integration.Tests` — all tests pass in headless environment within 5 minutes.

**Depends on**: Phase 2 (bug fixes) for tests requiring real simulation data.

### Implementation for User Story 3

- [x] T020 [US3] Create `tests/PhysicsSandbox.Integration.Tests/SimulationConnectionTests.cs` with tests: (1) simulation connects and stays connected after Aspire startup, (2) simulation connection produces non-empty state in StreamState within 10s, (3) sending AddBody + StepSimulation produces state with body having updated physics values, (4) state stream shows running=true after Play command, (5) simulation maintains active connection for at least 30 seconds under normal operation — send periodic step commands and verify state updates continue arriving (validates SC-002 connection stability within CI-practical timeframe). Use existing test infrastructure pattern from ServerHubTests.cs (DistributedApplicationTestingBuilder, WaitForResourceHealthyAsync, SSL bypass handler). Wait for simulation resource to be healthy before asserting.
- [x] T021 [P] [US3] Create `tests/PhysicsSandbox.Integration.Tests/CommandRoutingTests.cs` with tests for all 9 simulation command types end-to-end: (1) AddBody creates a body visible in state, (2) ApplyForce on a body changes its velocity after step, (3) ApplyImpulse produces immediate velocity change, (4) ApplyTorque changes angular velocity, (5) SetGravity changes body trajectory, (6) StepSimulation advances time, (7) PlayPause toggles running flag, (8) RemoveBody removes from state, (9) ClearForces stops acceleration. Each test sends the command, steps/waits, then reads state to verify the effect.
- [x] T022 [P] [US3] Create `tests/PhysicsSandbox.Integration.Tests/StateStreamingTests.cs` with tests: (1) multiple concurrent StreamState subscriptions all receive the same state data, (2) late joiner receives cached state immediately on subscription, (3) state stream delivers updates within 1s of simulation step, (4) StreamViewCommands receives forwarded SetZoom command (extends existing test with more view command types).
- [x] T023 [P] [US3] Create `tests/PhysicsSandbox.Integration.Tests/ErrorConditionTests.cs` with tests: (1) SendCommand returns success with "dropped" message when no simulation connected (stop simulation resource first or test before simulation connects), (2) SendCommand with empty/default proto message returns appropriate response, (3) StreamState returns empty/cached state when simulation disconnects mid-stream, (4) multiple rapid commands don't crash or deadlock the server.
- [x] T024 [US3] Run the complete test suite `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` to verify all existing tests (118 unit + 5 integration) plus all new integration tests pass. Confirm total integration test count is at least 20 (5 existing + 15+ new).

**Checkpoint**: 20+ integration tests pass in headless CI. All scenarios from spec covered.

---

## Phase 5: User Story 4 — MCP Server Configuration and Discovery (Priority: P3)

**Goal**: Make the MCP server easily configurable in AI assistant tools with standard MCP conventions.

**Independent Test**: Add MCP server to Claude Code config, verify tools are discovered and invocable.

### Implementation for User Story 4

- [x] T025 [US4] Add MCP server configuration example to `src/PhysicsSandbox.Mcp/mcp-config.json` — a sample Claude Code MCP config snippet showing `command`, `args`, and optional `env` fields that users can copy into their `.claude/settings.local.json`.
- [x] T026 [US4] Update `CLAUDE.md` at project root to document the MCP server: add entry under Active Technologies for ModelContextProtocol 1.1.0, add PhysicsSandbox.Mcp to Project Structure, add build/run commands, and note the MCP configuration instructions.

**Checkpoint**: Developers can configure and use the MCP server by following documented instructions.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [x] T027 Run full solution build `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` to verify all projects compile cleanly.
- [x] T028 Run full test suite `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — final regression check: all unit tests (118+) and integration tests (20+) pass.
- [x] T029 Validate quickstart.md scenarios from `specs/005-mcp-server-testing/quickstart.md` — verify build, run, and configure instructions are accurate.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (US2: Bug Fixes)**: Depends on Phase 1 (solution must build)
- **Phase 3 (US1: MCP Server)**: Depends on Phase 1 only — can proceed in parallel with Phase 2
- **Phase 4 (US3: Integration Tests)**: Depends on Phase 2 (needs working simulation for real physics tests)
- **Phase 5 (US4: MCP Config)**: Depends on Phase 3 (MCP server must exist to document it)
- **Phase 6 (Polish)**: Depends on all previous phases

### User Story Dependencies

- **US2 (Bug Fixes, P1)**: Independent — no dependencies on other stories. BLOCKS US3.
- **US1 (MCP Server, P1)**: Independent — can proceed in parallel with US2. Blocked by US2 only for MCP tools that need real simulation data for testing.
- **US3 (Integration Tests, P2)**: Depends on US2 (simulation SSL fix required for tests with real physics data).
- **US4 (MCP Config, P3)**: Depends on US1 (MCP server must be built to document configuration).

### Parallel Opportunities

**Phase 2 + Phase 3 can proceed in parallel:**
- Developer A: US2 bug fixes (SimulationClient.fs, AppHost.cs)
- Developer B: US1 MCP server (new project, no file conflicts)

**Within Phase 3, multiple files can be written in parallel:**
- T011 + T012 + T013 + T014 + T015 (SimulationTools.fsi, ViewTools, QueryTools — different files)

**Within Phase 4, all test classes can be written in parallel:**
- T020 + T021 + T022 + T023 (different test files)

---

## Parallel Example: User Story 1 (MCP Server)

```bash
# After T008-T009 (GrpcConnection) completes, launch tool modules in parallel:
Task T010: "Implement SimulationTools in src/PhysicsSandbox.Mcp/SimulationTools.fs"
Task T011: "Write signature file src/PhysicsSandbox.Mcp/SimulationTools.fsi"
Task T012: "Implement ViewTools in src/PhysicsSandbox.Mcp/ViewTools.fs"
Task T013: "Write signature file src/PhysicsSandbox.Mcp/ViewTools.fsi"
Task T014: "Implement QueryTools in src/PhysicsSandbox.Mcp/QueryTools.fs"
Task T015: "Write signature file src/PhysicsSandbox.Mcp/QueryTools.fsi"
```

## Parallel Example: User Story 3 (Integration Tests)

```bash
# All test classes can be written in parallel (different files):
Task T020: "SimulationConnectionTests.cs"
Task T021: "CommandRoutingTests.cs"
Task T022: "StateStreamingTests.cs"
Task T023: "ErrorConditionTests.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: US2 Bug Fixes (T003-T007) — unblocks real simulation
3. Complete Phase 3: US1 MCP Server (T008-T019) — core deliverable
4. **STOP and VALIDATE**: MCP server works end-to-end with fixed simulation
5. Deploy/demo if ready

### Incremental Delivery

1. Phase 1 + 2 → Simulation works, viewer displays → Demo real physics
2. Phase 3 → MCP server → Demo interactive debugging via AI assistant
3. Phase 4 → Integration tests → CI confidence, regression prevention
4. Phase 5 → Config docs → Developer onboarding
5. Phase 6 → Final validation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Phase 2 (US2) and Phase 3 (US1) can proceed in parallel — no file conflicts
- Integration tests (Phase 4) must wait for bug fixes (Phase 2) to test real simulation data
- All test tasks are integration tests (explicitly requested in spec FR-008 through FR-012)
- Commit after each task or logical group
