# Tasks: MCP Persistent Service

**Input**: Design documents from `/specs/001-mcp-persistent-service/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add package references, project references, and prepare the .fsproj for new files

- [x] T001 Add `ModelContextProtocol.AspNetCore` package reference (version 1.1.*) to `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`
- [x] T002 Add `PhysicsClient` project reference to `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` — `<ProjectReference Include="..\PhysicsClient\PhysicsClient.fsproj" />`
- [x] T002a Add `PhysicsSandbox.ServiceDefaults` project reference to `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` and configure health checks and structured logging in `Program.fs` WebApplication builder

---

## Phase 2: Foundational (Proto Contract + Server Audit Stream)

**Purpose**: Add the `CommandEvent` message and `StreamCommands` RPC to the proto contract, then implement the server-side audit stream broadcasting. This MUST be complete before US2 can consume the audit stream.

**CRITICAL**: No user story work on message visibility (US2) can begin until this phase is complete. US1 (transport switch) can proceed in parallel with this phase.

- [x] T004 Add `CommandEvent` message to `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — oneof wrapping `SimulationCommand` and `ViewCommand` per contracts/proto-changes.md
- [x] T005 Add `StreamCommands` RPC to the `PhysicsHub` service definition in `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — `rpc StreamCommands(StateRequest) returns (stream CommandEvent)`
- [x] T005a Add unit tests for `MessageRouter` command audit subscriber functions (`subscribeCommands`, `unsubscribeCommands`, `publishCommandEvent`) in `tests/PhysicsServer.Tests/`. Tests MUST fail before implementation in T006
- [x] T006 Add `CommandSubscribers` field (`ConcurrentDictionary<Guid, CommandEvent -> Task>`) to `MessageRouter` record type in `src/PhysicsServer/Hub/MessageRouter.fs`. Add `subscribeCommands`, `unsubscribeCommands`, and `publishCommandEvent` functions following the existing `Subscribers`/`publishState` pattern
- [x] T007 Update `src/PhysicsServer/Hub/MessageRouter.fsi` with new public signatures: `subscribeCommands`, `unsubscribeCommands`, `publishCommandEvent`
- [x] T008 Modify `submitCommand` in `src/PhysicsServer/Hub/MessageRouter.fs` to also call `publishCommandEvent` with a `CommandEvent` wrapping the `SimulationCommand` after writing to the command channel
- [x] T009 Modify `submitViewCommand` in `src/PhysicsServer/Hub/MessageRouter.fs` to also call `publishCommandEvent` with a `CommandEvent` wrapping the `ViewCommand` after writing to the view command channel
- [x] T010 Implement `StreamCommands` RPC in `src/PhysicsServer/Services/PhysicsHubService.fs` — subscriber-based pattern matching `StreamState`: register callback via `subscribeCommands`, await cancellation, unsubscribe on disconnect. No late-joiner backfill (commands are ephemeral)
- [x] T011 Initialize `CommandSubscribers` in the `MessageRouter` creation function in `src/PhysicsServer/Hub/MessageRouter.fs` (same place `Subscribers` is initialized) and wire `StreamCommands` in `src/PhysicsServer/Program.fs` service registration
- [x] T011a Update surface-area baseline file for `MessageRouter` module in `tests/PhysicsServer.Tests/` to include new `subscribeCommands`, `unsubscribeCommands`, `publishCommandEvent` signatures
- [x] T012 Build and verify proto compilation succeeds: `dotnet build src/PhysicsSandbox.Shared.Contracts/PhysicsSandbox.Shared.Contracts.csproj` and `dotnet build src/PhysicsServer/PhysicsServer.fsproj`

**Checkpoint**: Proto contract extended, server can broadcast command audit events. Existing clients unaffected (additive changes only).

---

## Phase 3: User Story 1 — Persistent MCP Connection (Priority: P1) MVP

**Goal**: Switch the MCP server from stdio transport to HTTP/SSE transport so it persists independently of client connections as part of the Aspire AppHost.

**Independent Test**: Start the AppHost, verify the MCP server is running and healthy with no clients connected. Connect via HTTP, disconnect, confirm MCP server stays running.

### Tests for User Story 1

- [x] T012a [US1] Add integration test in `tests/PhysicsSandbox.Integration.Tests/McpHttpTransportTests.cs` verifying MCP server starts as HTTP service, stays running without clients, and accepts HTTP connections. Test MUST fail before transport switch. Follow existing `McpOrchestrationTests.cs` patterns
- [x] T012b [US1] Add integration test in `tests/PhysicsSandbox.Integration.Tests/McpHttpTransportTests.cs` verifying two simultaneous MCP client connections both see identical shared simulation state (FR-003)

### Implementation for User Story 1

- [x] T013 [US1] Rewrite `src/PhysicsSandbox.Mcp/Program.fs` to use `WebApplication.CreateBuilder` instead of `Host.CreateApplicationBuilder`. Register `GrpcConnection` as singleton, add MCP server with `.AddMcpServer()`, configure HTTP/SSE transport via `ModelContextProtocol.AspNetCore` (replace `.WithStdioServerTransport()`), use `.WithToolsFromAssembly()`, and map the MCP endpoint route. Ensure Kestrel is configured with an HTTP endpoint for Aspire service discovery
- [x] T014 [US1] Update `src/PhysicsSandbox.AppHost/AppHost.cs` — change the MCP resource configuration to expose an HTTP endpoint instead of treating it as a stdio process. Ensure `.WithReference(server)` and `.WaitFor(server)` are preserved. The MCP server should be reachable via its HTTP endpoint in the Aspire dashboard
- [x] T015 [US1] Update `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` to add ASP.NET Core SDK properties if needed for the WebApplication host (e.g., `<Project Sdk="Microsoft.NET.Sdk.Web">` or appropriate SDK). Verify the project builds: `dotnet build src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`
- [x] T016 [US1] Update `.mcp.json` in the repository root to reflect the new HTTP/SSE connection URL instead of the stdio launch command. This is the client-side MCP configuration that AI assistants use to connect
- [x] T016a [US1] Verify `GrpcConnection` reconnection logic in `src/PhysicsSandbox.Mcp/GrpcConnection.fs` works correctly with the new WebApplication host — exponential backoff, IsConnected flag, and status reporting via `get_status` tool (FR-012)

**Checkpoint**: MCP server runs as a persistent HTTP service within Aspire AppHost. Clients connect via HTTP/SSE. Server survives client disconnection.

---

## Phase 4: User Story 2 — Full Message Visibility (Priority: P1)

**Goal**: MCP server subscribes to all three server streams (state, view commands, command audit) and exposes query tools for each.

**Independent Test**: Start AppHost, send commands from another client, query the MCP server's audit tools to see the raw command feed and current view state.

**Dependencies**: Phase 2 (proto + server audit stream) must be complete.

### Tests for User Story 2

- [x] T016b [US2] Add integration test in `tests/PhysicsSandbox.Integration.Tests/CommandAuditStreamTests.cs` verifying the `StreamCommands` audit stream receives command events when commands are sent via `SendCommand` and `SendViewCommand`. Test MUST fail before stream subscription implementation

### Implementation for User Story 2

- [x] T017 [US2] Extend `GrpcConnection` in `src/PhysicsSandbox.Mcp/GrpcConnection.fs` to add a `startViewCommandStream` background task that subscribes to `StreamViewCommands` RPC with the same exponential backoff reconnection pattern as the existing `startStateStream`. Cache the latest view command state
- [x] T018 [US2] Extend `GrpcConnection` in `src/PhysicsSandbox.Mcp/GrpcConnection.fs` to add a `startCommandAuditStream` background task that subscribes to the new `StreamCommands` RPC. Maintain a bounded circular buffer (`CommandLog`) of the most recent 100 `CommandEvent` entries
- [x] T019 [US2] Update `GrpcConnection` initialization in `src/PhysicsSandbox.Mcp/GrpcConnection.fs` to start all three background streaming tasks (state, view commands, command audit) on construction
- [x] T020 [US2] Update `src/PhysicsSandbox.Mcp/GrpcConnection.fsi` with new public members: `CommandLog` (recent command events list), any new state/view query accessors
- [x] T021 [US2] Create `src/PhysicsSandbox.Mcp/AuditTools.fsi` — signature file declaring MCP tool types for command audit queries (e.g., `get_command_log`, `get_recent_commands`)
- [x] T022 [US2] Create `src/PhysicsSandbox.Mcp/AuditTools.fs` — implement `[<McpServerToolType>]` tools: `get_command_log` (returns recent N commands from the bounded buffer with type, parameters, timestamp), `get_recent_commands` (filtered by command type). Use `[<McpServerTool>]` and `[<Description>]` attributes matching existing tool patterns
- [x] T022a [US2] Create surface-area baseline files for `AuditTools` module and update baseline for `GrpcConnection` module to include new `CommandLog` and stream accessors. Add compile entries for `AuditTools.fsi`/`.fs` in `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj`
- [x] T023 [US2] Update `src/PhysicsSandbox.Mcp/QueryTools.fs` to include view command state in the `get_status` tool output (connection status for all three streams)

**Checkpoint**: MCP server has full visibility into all messages. AI assistants can query state, view commands, and the raw command audit log.

---

## Phase 5: User Story 3 — Full Command Capability (Priority: P1)

**Goal**: Verify the MCP server can send all 12 command types (9 simulation + 3 view) through the new HTTP/SSE transport.

**Independent Test**: Connect to the MCP server via HTTP and execute each of the 12 command types, verifying state changes.

**Note**: The existing `SimulationTools.fs` (9 commands) and `ViewTools.fs` (3 commands) already cover all 12 command types. This phase is about verifying they work correctly on the new transport and confirming completeness.

### Implementation for User Story 3

- [x] T024 [US3] Review `src/PhysicsSandbox.Mcp/SimulationTools.fs` and verify all 9 simulation command types are exposed as MCP tools: add_body, remove_body, apply_force, apply_impulse, apply_torque, clear_forces, set_gravity, step, play, pause. Add any missing commands if found
- [x] T025 [US3] Review `src/PhysicsSandbox.Mcp/ViewTools.fs` and verify all 3 view command types are exposed as MCP tools: set_camera, set_zoom, toggle_wireframe. Add any missing commands if found
- [x] T026 [US3] Verify `GrpcConnection` in `src/PhysicsSandbox.Mcp/GrpcConnection.fs` correctly sends commands via `SendCommandAsync` and `SendViewCommandAsync` on the shared gRPC client. Ensure error handling returns descriptive messages matching the existing `CommandAck` pattern
- [x] T026a [US3] Verify invalid commands (bad body ID, malformed parameters) return clear error messages without crashing. Test at least one simulation command and one view command with invalid input

**Checkpoint**: All 12 command types verified accessible through MCP tools on HTTP/SSE transport.

---

## Phase 6: User Story 4 — Convenience Functions and Presets (Priority: P2)

**Goal**: Expose PhysicsClient library convenience functions as MCP tools — body presets, scene generators, and steering helpers.

**Independent Test**: Connect to MCP, use preset tools to add bodies (marble, crate), use generators to build a scene (pyramid), use steering to push a body. Verify state reflects expected results.

**Dependencies**: Phase 1 (PhysicsClient project reference) must be complete.

### Implementation for User Story 4

- [x] T027 [US4] Create an adapter module in `src/PhysicsSandbox.Mcp/ClientAdapter.fs` (and `.fsi`) that bridges MCP's `GrpcConnection` singleton with PhysicsClient library functions. PhysicsClient modules (Presets, Generators, Steering) expect a `Session` — the adapter should provide the gRPC client and body registry from `GrpcConnection` in the format these modules need, or call the underlying PhysicsClient.SimulationCommands functions directly using the shared gRPC client
- [x] T028 [P] [US4] Create `src/PhysicsSandbox.Mcp/PresetTools.fsi` — signature file declaring MCP tool types for body presets
- [x] T029 [P] [US4] Create `src/PhysicsSandbox.Mcp/PresetTools.fs` — implement `[<McpServerToolType>]` tools for all 7 presets: `add_marble`, `add_bowling_ball`, `add_beach_ball`, `add_crate`, `add_brick`, `add_boulder`, `add_die`. Each accepts optional position (x, y, z), mass override, and custom ID. Delegate to PhysicsClient.Presets module via the adapter
- [x] T030 [P] [US4] Create `src/PhysicsSandbox.Mcp/GeneratorTools.fsi` — signature file declaring MCP tool types for scene generators
- [x] T031 [P] [US4] Create `src/PhysicsSandbox.Mcp/GeneratorTools.fs` — implement `[<McpServerToolType>]` tools: `generate_random_bodies` (count, optional seed), `generate_stack` (count, body type, position), `generate_row` (count, spacing), `generate_grid` (rows, cols, spacing), `generate_pyramid` (layers). Delegate to PhysicsClient.Generators module via the adapter
- [x] T032 [P] [US4] Create `src/PhysicsSandbox.Mcp/SteeringTools.fsi` — signature file declaring MCP tool types for steering helpers
- [x] T033 [P] [US4] Create `src/PhysicsSandbox.Mcp/SteeringTools.fs` — implement `[<McpServerToolType>]` tools: `push_body` (body ID, direction: up/down/north/south/east/west, strength), `launch_body` (body ID, target x/y/z, speed), `spin_body` (body ID, axis x/y/z, strength), `stop_body` (body ID — applies opposing impulse). Delegate to PhysicsClient.Steering module via the adapter
- [x] T033a [P] [US4] Create surface-area baseline files for `PresetTools`, `GeneratorTools`, `SteeringTools`, and `ClientAdapter` modules
- [x] T034 [US4] Add compile entries for `ClientAdapter.fsi`/`.fs`, `PresetTools.fsi`/`.fs`, `GeneratorTools.fsi`/`.fs`, `SteeringTools.fsi`/`.fs` in `src/PhysicsSandbox.Mcp/PhysicsSandbox.Mcp.fsproj` (ClientAdapter must appear before the tool files that depend on it)

**Checkpoint**: All convenience tools available — 7 presets + 5 generators + 4 steering = 16 new tools. AI assistants can build complex scenes with simple commands.

---

## Phase 7: User Story 5 — Full Command Coverage Verification (Priority: P2)

**Goal**: Confirm the MCP server exposes all 12 command types and can drive the simulation into any reachable state.

**Independent Test**: Issue a sequence of all 12 command types through MCP and verify each produces the expected state change.

**Note**: This story is satisfied by the combination of US3 (12 base commands) and US4 (convenience wrappers). This phase verifies completeness and documents the full tool inventory.

### Implementation for User Story 5

- [x] T035 [US5] Review the complete MCP tool inventory across all tool modules (`SimulationTools.fs`, `ViewTools.fs`, `QueryTools.fs`, `AuditTools.fs`, `PresetTools.fs`, `GeneratorTools.fs`, `SteeringTools.fs`) and verify the full tool list covers all 12 command types. Document the complete tool count in a comment in `src/PhysicsSandbox.Mcp/Program.fs`
- [x] T036 [US5] Verify that `src/PhysicsSandbox.Mcp/SimulationTools.fs` includes `remove_body` and `clear_forces` tools (these are sometimes overlooked). If missing, add them following the existing tool patterns

**Checkpoint**: Complete tool inventory verified. All 12 base command types + convenience wrappers confirmed.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Build verification, integration testing, cleanup

- [x] T037 Full solution build verification: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — fix any compilation errors across all projects
- [x] T038 Run full test suite: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — ensure no regressions and all new tests pass (T005a, T012a, T012b, T016b)
- [x] T039 Update `CLAUDE.md` Recent Changes section with feature summary for `001-mcp-persistent-service`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup for proto compilation context. BLOCKS User Story 2
- **US1 (Phase 3)**: Depends on Setup (Phase 1) only. Can proceed in PARALLEL with Phase 2
- **US2 (Phase 4)**: Depends on Phase 2 (proto + server audit stream) AND Phase 3 (HTTP transport)
- **US3 (Phase 5)**: Depends on Phase 3 (HTTP transport) — verification only
- **US4 (Phase 6)**: Depends on Phase 1 (PhysicsClient ref) AND Phase 3 (HTTP transport)
- **US5 (Phase 7)**: Depends on Phase 5 + Phase 6 — verification only
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Independent — can start after Setup
- **US2 (P1)**: Requires Foundational phase + US1
- **US3 (P1)**: Requires US1 (verify on new transport)
- **US4 (P2)**: Requires Setup + US1 (can proceed in parallel with US2/US3)
- **US5 (P2)**: Requires US3 + US4 (verification only)

### Within Each User Story

- .fsi signature files before .fs implementation files
- GrpcConnection changes before tool modules that depend on them
- Adapter modules before tool modules that use them

### Parallel Opportunities

- **Phase 1**: T001, T002, T002a can run in parallel (different .fsproj sections)
- **Phase 2 + Phase 3**: Can run in parallel (server changes vs. MCP transport switch)
- **Phase 4**: T017, T018 can run in parallel (different stream subscriptions)
- **Phase 6**: T028-T033 can run in parallel (independent tool modules touching different files)

---

## Parallel Example: User Story 4 (Convenience Tools)

```text
# After adapter (T027) is complete, launch all tool modules in parallel:
Task T028+T029: "PresetTools.fsi + PresetTools.fs"
Task T030+T031: "GeneratorTools.fsi + GeneratorTools.fs"
Task T032+T033: "SteeringTools.fsi + SteeringTools.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001, T002, T002a)
2. Complete Phase 3: US1 — Tests first (T012a, T012b), then transport switch (T013-T016, T016a)
3. **STOP and VALIDATE**: Start AppHost, verify MCP server persists, connect/disconnect via HTTP, run tests
4. This alone delivers the core value: a persistent MCP server

### Incremental Delivery

1. Setup + US1 → Persistent MCP server (MVP)
2. Foundational + US2 → Full message visibility (audit stream)
3. US3 → Command coverage verified
4. US4 → Convenience tools (presets, generators, steering)
5. US5 → Full coverage verification
6. Polish → Tests, docs, cleanup

### Critical Path

Setup → US1 (transport switch) → US2 (audit streams) → Polish

The foundational phase (proto + server) can run in parallel with US1 to shorten the critical path.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US3 and US5 are lightweight verification phases — most work is in US1, US2, and US4
- Existing SimulationTools.fs and ViewTools.fs already cover all 12 command types — no reimplementation needed
- PhysicsClient modules are referenced, not copied — MCP delegates to them via an adapter
- Commit after each phase completion
