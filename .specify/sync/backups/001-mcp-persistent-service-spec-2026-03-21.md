# Feature Specification: MCP Persistent Service

**Feature Branch**: `001-mcp-persistent-service`
**Created**: 2026-03-21
**Status**: Draft
**Input**: User description: "i want to change the mcp server to be a permanent service of the aspire apphost. for that it should most likely use socket connection not std pipes since it shuts down if no connection. it should get all messages that the server gets. it should also have access to the convenience functions and other libraries that the repl user has access to easily control the simulation state. it should also only be able to send messages to the server. it should be able to send all possible messages to easily test stuff. basically it should be able to create any legal application state by pretending to be any part of the simulation."

## User Scenarios & Testing

### User Story 1 - Persistent MCP Connection (Priority: P1)

An AI assistant connects to the MCP server via a network socket. The MCP server stays running permanently as part of the Aspire AppHost, regardless of whether any AI assistant is currently connected. The assistant can disconnect and reconnect at any time without the MCP server shutting down.

**Why this priority**: The MCP server currently uses stdio transport, which ties its lifecycle to the connecting process. Switching to a socket-based transport is the foundational change that enables all other features — without it, the MCP server cannot be a persistent service.

**Independent Test**: Start the AppHost, verify the MCP server is running, connect an AI assistant via socket, disconnect, and confirm the MCP server remains running and accepting new connections.

**Acceptance Scenarios**:

1. **Given** the AppHost is running, **When** no AI assistant is connected, **Then** the MCP server remains running and healthy.
2. **Given** an AI assistant is connected via socket, **When** the assistant disconnects, **Then** the MCP server continues running and can accept new connections.
3. **Given** the AppHost starts up, **When** all services initialize, **Then** the MCP server starts automatically and is reachable via its network endpoint.

---

### User Story 2 - Full Message Visibility (Priority: P1)

The MCP server receives all messages that flow through the PhysicsServer — simulation state updates, view commands, and a live feed of every command sent by any client. The AI assistant can observe the complete state of the system at any time, including what commands other participants (simulation, viewer, client) are sending in real time.

**Why this priority**: Equally critical to persistence — the MCP server's value as a debugging and testing tool depends on having full visibility into the system. Without this, the assistant is operating blind. Seeing raw commands (not just their effects on state) enables understanding causality and debugging command sequences.

**Independent Test**: Start the AppHost with all services, send commands from another client, and verify the MCP server receives both the state updates and the raw command feed showing what was sent.

**Acceptance Scenarios**:

1. **Given** the simulation is running and producing state updates, **When** the AI assistant queries state, **Then** it receives the current simulation state with all bodies, positions, velocities, and simulation time.
2. **Given** a client sends a simulation command, **When** the AI assistant observes the command feed, **Then** it sees the raw command that was sent (command type, parameters, sender).
3. **Given** view commands are sent by any participant, **When** the AI assistant observes the command feed, **Then** those view commands also appear in the feed.

---

### User Story 3 - Full Command Capability (Priority: P1)

The AI assistant can send any simulation command and view command that the system supports — adding bodies, applying forces/impulses/torques, controlling playback, setting gravity, managing the camera, and toggling rendering modes. The assistant has the same command surface as the REPL client.

**Why this priority**: The MCP server must be able to create any legal application state to be useful for testing and debugging. This is core to the feature's purpose.

**Independent Test**: Connect to the MCP server and execute each available command type, verifying the simulation state changes accordingly.

**Acceptance Scenarios**:

1. **Given** the AI assistant is connected, **When** it sends an "add body" command with specific parameters, **Then** the body appears in the simulation state.
2. **Given** bodies exist in the simulation, **When** the assistant sends force/impulse/torque commands, **Then** the affected bodies' velocities and positions change accordingly.
3. **Given** the simulation is running, **When** the assistant sends play/pause/step commands, **Then** the simulation playback state changes as expected.
4. **Given** the assistant sends view commands (camera, zoom, wireframe), **Then** the commands are delivered to the viewer.

---

### User Story 4 - Convenience Functions and Presets (Priority: P2)

The AI assistant has access to high-level convenience tools equivalent to the REPL client library — body presets (marble, bowling ball, crate, etc.), scene generators (random bodies, stacks, rows, grids, pyramids), and steering helpers (push, launch, spin, stop). These simplify common operations without requiring the assistant to manually specify low-level parameters.

**Why this priority**: While the full command set (P1) enables all operations, convenience functions dramatically improve the assistant's usability for common tasks. However, the system is fully functional without them.

**Independent Test**: Connect to the MCP server and use preset/generator/steering tools to build a scene, verifying that the high-level operations produce the expected bodies and forces.

**Acceptance Scenarios**:

1. **Given** the assistant is connected, **When** it uses a body preset tool (e.g., "add marble"), **Then** a body with the correct preset properties (size, mass) appears in the simulation.
2. **Given** the assistant is connected, **When** it uses a scene generator (e.g., "create pyramid of 10 bodies"), **Then** the specified arrangement of bodies appears in the simulation.
3. **Given** bodies exist, **When** the assistant uses steering tools (e.g., "push body north", "launch body to target"), **Then** the appropriate forces/impulses are applied.

---

### User Story 5 - Impersonation of System Participants (Priority: P2)

The AI assistant can send all command types available through the server's client-facing interface — all 9 simulation command types and all 3 view command types. This gives the assistant the full command surface to create any reachable application state by issuing the right sequence of commands, without needing to inject raw simulation state.

**Why this priority**: This is a power-user capability for advanced testing and debugging. It extends the basic command capability (P1) by giving the assistant explicit access to the complete command vocabulary, enabling it to drive the simulation into any state reachable through the command protocol.

**Independent Test**: Use the MCP server to issue every supported command type and verify each is accepted and produces the expected state change.

**Acceptance Scenarios**:

1. **Given** the assistant is connected, **When** it sends any of the 12 command types (9 simulation + 3 view), **Then** the server accepts and processes the command.
2. **Given** the assistant wants to test a specific application state, **When** it sends a sequence of commands, **Then** the resulting state matches what those commands would produce from any other client.

---

### Edge Cases

- What happens when multiple AI assistants connect simultaneously? All sessions share a single underlying server connection, state cache, and body ID counter — every assistant sees identical simulation state.
- What happens when the PhysicsServer is not yet ready? The MCP server should wait for the server (existing behavior) and report connection status to the assistant.
- What happens when the PhysicsServer goes down while the MCP server is running? The MCP server should remain running, report the disconnection, and attempt reconnection.
- What happens when an invalid command is sent? The system should return a clear error message without crashing.
- What happens when the socket port is already in use? The MCP server should report a clear startup failure.

## Requirements

### Functional Requirements

- **FR-001**: The MCP server MUST use a network socket transport instead of stdio, allowing it to persist independently of client connections.
- **FR-002**: The MCP server MUST start automatically as part of the Aspire AppHost and remain running for the lifetime of the AppHost.
- **FR-003**: The MCP server MUST accept multiple concurrent client connections, all sharing a single underlying server connection, state cache, and body ID counter.
- **FR-004**: The MCP server MUST receive all simulation state updates, all view commands, and a live audit feed of every command sent by any client, streamed from the PhysicsServer. This requires a new server-side command audit stream that broadcasts incoming commands to subscribers.
- **FR-005**: The MCP server MUST be able to send all simulation command types supported by the system protocol (add body, remove body, apply force, apply impulse, apply torque, clear forces, set gravity, step, play, pause).
- **FR-006**: The MCP server MUST be able to send all view command types (set camera, set zoom, toggle wireframe).
- **FR-007**: The MCP server MUST expose convenience tools for body presets (marble, bowling ball, beach ball, crate, brick, boulder, die) with configurable position, mass, and ID.
- **FR-008**: The MCP server MUST expose scene generator tools (random bodies, stacks, rows, grids, pyramids) with configurable parameters.
- **FR-009**: The MCP server MUST expose steering tools (push in direction, launch to target, spin around axis, stop body).
- **FR-010**: The MCP server MUST be able to send all 12 command types available through the server's client-facing interface (9 simulation commands + 3 view commands), enabling it to create any reachable application state. It does NOT inject raw simulation state via the simulation-to-server link.
- **FR-011**: The MCP server MUST report its connection status and the current simulation state through query tools.
- **FR-012**: The MCP server MUST handle PhysicsServer disconnection gracefully, continuing to run and attempting reconnection.

### Key Entities

- **MCP Session**: A connected AI assistant's session, including its socket connection and access to all tools.
- **System Message**: Any command or state update that flows through the PhysicsServer, observable by the MCP server.
- **Tool**: An MCP-exposed operation that an AI assistant can invoke, ranging from low-level commands to high-level convenience functions.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The MCP server remains running and accepting connections for the entire lifetime of the AppHost, with zero unplanned shutdowns due to client disconnections.
- **SC-002**: An AI assistant can connect, disconnect, and reconnect to the MCP server without any service interruption.
- **SC-003**: All simulation command types and view command types are executable through the MCP server, covering 100% of the system protocol's command surface.
- **SC-004**: Convenience tools (presets, generators, steering) are available and produce the same results as equivalent REPL client operations.
- **SC-005**: The MCP server can create any legal application state reachable through the system protocol within a single session.
- **SC-006**: State queries return current simulation data with staleness under 2 seconds during normal operation.

## Clarifications

### Session 2026-03-21

- Q: Should impersonation include pushing fake simulation state via SimulationLink, or only sending commands through PhysicsHub? → A: Commands-only via PhysicsHub (all 12 command types). No SimulationLink access.
- Q: Should message visibility include only existing streams (state + view), or also a new command audit stream? → A: Full visibility — add a new server-side command audit stream that broadcasts every incoming command to subscribers.
- Q: Should concurrent MCP sessions share or isolate their server connection and state? → A: Shared — single gRPC connection, state cache, and body ID counter across all sessions.

## Assumptions

- The MCP protocol library supports socket-based (SSE or streamable HTTP) transport as an alternative to stdio.
- The Aspire AppHost can expose network endpoints for the MCP server alongside existing services.
- The PhysicsClient library's modules can be referenced and used by the MCP server project without architectural conflicts.
- Multiple simultaneous MCP client connections are supported by the chosen transport mechanism.
- The MCP server communicates with the PhysicsServer exclusively through gRPC, same as all other components.
