# Feature Specification: MCP Server and Integration Testing

**Feature Branch**: `005-mcp-server-testing`
**Created**: 2026-03-20
**Status**: Completed
**Input**: User description: "Integrationtests and fixes. create a mcp server for the server that can send and gets everything so exploring the errorspace can be done faster. then generate a comprehensive test suite to find/fix errors and prevent regressions."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - MCP-Based Physics Exploration (Priority: P1)

A developer working in an AI-assisted environment (e.g., Claude Code) wants to interact with the running PhysicsSandbox system directly through MCP tool calls. They can send any simulation command (add bodies, apply forces, step, play/pause, set gravity, remove bodies, clear forces, apply impulses/torques), send view commands (camera, zoom, wireframe), read the current simulation state, and check simulation connection status — all without writing gRPC client code or FSI scripts.

**Why this priority**: This is the core deliverable. An MCP server that wraps all PhysicsHub and SimulationLink RPCs enables rapid interactive debugging and error-space exploration from any MCP-capable client. It eliminates the boilerplate barrier that currently slows down investigation of issues like the ones documented in the error report.

**Independent Test**: Can be tested by starting the MCP server alongside the Aspire stack, invoking each MCP tool, and verifying the corresponding gRPC call reaches the server and returns an appropriate response.

**Acceptance Scenarios**:

1. **Given** the PhysicsSandbox Aspire stack is running and the MCP server is connected to the PhysicsServer, **When** a user invokes the "add_body" tool with body parameters, **Then** the MCP server sends the command via gRPC and returns the server's acknowledgment (success/failure and message).
2. **Given** a simulation is connected and bodies exist, **When** a user invokes the "get_state" tool, **Then** the MCP server returns the latest simulation state including body positions, velocities, time, and running status in a human-readable format.
3. **Given** the MCP server is running, **When** a user invokes any of the available tools with invalid or missing parameters, **Then** the MCP server returns a clear error description without crashing.
4. **Given** no simulation is connected to the server, **When** a user sends a command via MCP, **Then** the response clearly indicates the command was accepted but dropped (no simulation connected).

---

### User Story 2 - Fix Known Connection Issues (Priority: P1)

A developer running the full Aspire stack expects the simulation to maintain a stable gRPC connection to the server. Currently, the simulation's gRPC channel lacks SSL certificate bypass for dev certificates, causing the bidirectional stream to fail silently. The viewer also fails to display because the DISPLAY environment variable is not propagated by Aspire.

**Why this priority**: Without fixing the simulation SSL issue, no commands reach the simulation and no real physics data streams back. This blocks all meaningful integration testing and demo execution. The viewer DISPLAY fix is lower effort and completes the "everything works out of the box" experience.

**Independent Test**: Can be tested by starting the Aspire stack, verifying the simulation's gRPC stream stays connected for at least 60 seconds, sending commands, and confirming bodies appear in the state stream with non-zero physics data (positions change over time).

**Acceptance Scenarios**:

1. **Given** the Aspire stack starts with simulation and server, **When** the simulation connects to the server's HTTPS endpoint, **Then** the SSL handshake succeeds and the bidirectional stream remains open.
2. **Given** the simulation is connected, **When** a user sends an AddBody command followed by a Play command, **Then** the state stream shows the body with changing position values over time (gravity is working).
3. **Given** the Aspire stack starts with the viewer on a system with a display server, **When** the viewer process launches, **Then** it receives the DISPLAY environment variable and renders a window.

---

### User Story 3 - Comprehensive Regression Test Suite (Priority: P2)

A developer making changes to any PhysicsSandbox service wants confidence that existing functionality still works. A comprehensive integration test suite exercises all gRPC RPCs end-to-end through the Aspire stack, covering command routing, state streaming, simulation connection lifecycle, error conditions, and concurrent client scenarios.

**Why this priority**: The existing integration tests cover only basic RPC connectivity (5 tests). A comprehensive suite that tests actual physics behavior, error conditions, and multi-client scenarios prevents regressions and documents expected system behavior. This is P2 because it depends on the fixes from Story 2 to test real simulation behavior.

**Independent Test**: Can be tested by running the test runner against the integration test project. All tests should pass with a fresh Aspire stack and no manual setup.

**Acceptance Scenarios**:

1. **Given** the full Aspire stack is running in the test harness, **When** the integration test suite executes, **Then** all tests pass within a reasonable timeout (under 5 minutes total).
2. **Given** a test sends an AddBody command and steps the simulation, **When** the state stream is read, **Then** the returned state contains the added body with updated physics values.
3. **Given** a test opens multiple concurrent state stream subscriptions, **When** simulation state updates, **Then** all subscribers receive the same state data.
4. **Given** a test sends a command while no simulation is connected, **When** the response is received, **Then** it indicates the command was dropped (not an error, but an informational message).

---

### User Story 4 - MCP Server Configuration and Discovery (Priority: P3)

A developer wants to add the PhysicsSandbox MCP server to their AI assistant's configuration with minimal effort. The MCP server should be runnable as a standalone process that connects to the PhysicsServer's gRPC endpoint, and its configuration should follow standard MCP conventions.

**Why this priority**: Usability and discoverability. The MCP server is only useful if developers can easily configure it in their tools. This is P3 because the core functionality (P1) must work first.

**Independent Test**: Can be tested by starting the MCP server with a server address argument, verifying it advertises its tools via the MCP protocol's tool listing, and confirming an MCP client can discover and invoke them.

**Acceptance Scenarios**:

1. **Given** the MCP server binary is built, **When** a developer adds it to their MCP client configuration with the server's gRPC address, **Then** the client discovers all available tools.
2. **Given** the MCP server is listed in a configuration file, **When** the MCP client starts, **Then** the MCP server connects to the PhysicsServer and is ready to accept tool invocations.

---

### Edge Cases

- What happens when the MCP server cannot reach the PhysicsServer? It should return clear connection-failure errors on each tool invocation, not crash.
- What happens when the simulation disconnects mid-stream? The state tool should return the last cached state with a staleness indicator.
- What happens when multiple MCP clients connect simultaneously? Each should function independently (they each create their own gRPC channel to the server).
- What happens when the server's command channel is full (100 capacity)? The MCP tool should relay the server's "dropped" response.
- What happens when integration tests run without GPU/display? Tests must not depend on the viewer being renderable; viewer tests should be limited to connection/communication, not rendering.
- What happens when the simulation process is alive but its gRPC stream has died? The simulation automatically reconnects with exponential backoff (1s → 10s max), preserving its current world state across reconnections.

## Requirements *(mandatory)*

### Functional Requirements

**MCP Server**:
- **FR-001**: System MUST expose an MCP server with fine-grained, individual tools — one per operation (~15 tools): add_body, apply_force, set_gravity, step, play, pause, remove_body, apply_impulse, apply_torque, clear_forces, set_camera, set_zoom, toggle_wireframe, get_state, get_status.
- **FR-002**: System MUST expose MCP tools for querying simulation connection status and system health. The get_state tool MUST return the latest cached simulation state instantly (via a background stream subscription), including a timestamp for staleness detection.
- **FR-003**: Each MCP tool MUST accept structured parameters matching the gRPC message schemas and return human-readable results.
- **FR-004**: The MCP server MUST communicate via stdio transport (standard MCP pattern for local tool servers).
- **FR-005**: The MCP server MUST handle gRPC connection failures gracefully and return descriptive error messages through the MCP protocol.

**Bug Fixes**:
- **FR-006**: The simulation service MUST establish and maintain a stable gRPC connection to the server over HTTPS with dev certificates, with automatic reconnection using exponential backoff (1s → 10s max) on stream failure.
- **FR-007**: The viewer service MUST receive the DISPLAY environment variable from the Aspire orchestrator on systems with a display server.

**Integration Tests**:
- **FR-008**: System MUST include integration tests covering all PhysicsHub RPCs end-to-end through the Aspire stack.
- **FR-009**: System MUST include integration tests that verify simulation connection, command delivery, and state streaming with real physics data.
- **FR-010**: System MUST include integration tests for error conditions: duplicate simulation connections, commands without simulation, stream reconnection.
- **FR-011**: System MUST include integration tests for concurrent state stream subscribers receiving consistent data.
- **FR-012**: All integration tests MUST run without manual setup, GPU, or display server (headless-compatible).

### Key Entities

- **MCP Tool**: A named operation exposed by the MCP server, corresponding to a gRPC RPC or a composition of RPCs. Each tool has a schema (input parameters) and returns a result.
- **Simulation State Snapshot**: A point-in-time capture of all bodies, simulation time, and running status — the primary data returned by the state tool.
- **Command Acknowledgment**: The server's response to a command, indicating whether it was processed, queued, or dropped.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All PhysicsHub and SimulationLink query operations are invocable from an MCP client without writing any gRPC code — 100% RPC coverage as MCP tools.
- **SC-002**: The simulation maintains a stable connection to the server for at least 10 minutes under normal operation after the connection fix.
- **SC-003**: Integration test suite covers at least 15 distinct scenarios across command routing, state streaming, simulation lifecycle, and error conditions.
- **SC-004**: All integration tests pass in a headless CI environment within 5 minutes total execution time.
- **SC-005**: Sending a command via MCP and reading the resulting state change completes within 5 seconds end-to-end (excluding simulation step time).
- **SC-006**: Zero regressions — all existing unit tests (118 total) and integration tests (5 existing) continue to pass after changes.

## Assumptions

- The MCP server is a standalone console application using stdio transport, run on-demand by developers — not an Aspire-managed service.
- SSL certificate bypass in development is acceptable for all gRPC client connections (matching the established pattern in PhysicsClient and integration tests).
- Integration tests use the existing Aspire testing infrastructure and do not require the viewer to render graphics.
- The viewer DISPLAY fix applies only to systems with a display server; headless environments are expected to not render.

## Clarifications

### Session 2026-03-20

- Q: Should the MCP server expose fine-grained individual tools (one per operation) or coarse-grained tools with command-type parameters? → A: Fine-grained — one tool per operation (~15 tools: add_body, apply_force, step, play, pause, etc.). Best discoverability for AI assistants and developers.
- Q: Should the simulation auto-reconnect on stream failure? → A: Yes, auto-reconnect with exponential backoff (1s → 10s max), matching the PhysicsClient pattern. Preserves world state across reconnections.
- Q: How should the MCP server access simulation state — background stream or on-demand? → A: Background stream. MCP server subscribes once at startup, caches latest state. get_state returns cached snapshot instantly with timestamp for staleness detection.
