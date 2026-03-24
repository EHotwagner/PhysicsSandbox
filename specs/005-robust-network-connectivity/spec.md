# Feature Specification: Robust Network Connectivity

**Feature Branch**: `005-robust-network-connectivity`
**Created**: 2026-03-24
**Status**: Draft
**Input**: User description: "Fix issues from NetworkProblems.md and camera-commands-debugging reports. Make all MCP/services/dashboard connections more robust. Document all findings. Podman container with exposed ports: 4173, 5000, 5001, 5137, 5173, 8080, 8081, 50051."

## Clarifications

### Session 2026-03-24

- Q: What is the container networking boundary? → A: Only the Aspire dashboard needs external access out of the container. All services (Server, Simulation, Viewer, Client, MCP) communicate internally via localhost.
- Q: When ViewCommands are sent before any viewer has connected, what happens? → A: Silently dropped — no buffering, no error. Commands sent with zero subscribers are discarded.
- Q: Which port serves the Aspire dashboard externally? → A: Port 18888 (added on next container rebuild). Use any fitting open port in the interim.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - ViewCommand Broadcast Delivery (Priority: P1)

A developer runs a demo script that sends rapid ViewCommands (narration, camera modes, smooth transitions). All commands arrive at the viewer reliably, regardless of how many viewer instances are connected or how fast commands are sent relative to the viewer's frame rate.

**Why this priority**: ViewCommand loss is the most impactful bug — it silently breaks all 42 demo scripts and any MCP-driven camera orchestration. The single-consumer channel architecture means a stale viewer process steals half the commands with no error or warning.

**Independent Test**: Run Demo22_CameraShowcase with two viewer processes connected simultaneously. Both viewers receive every command. Kill one viewer — the other continues receiving all commands without interruption.

**Acceptance Scenarios**:

1. **Given** a demo script sends 20 ViewCommands in rapid succession (~100ms apart), **When** one viewer is connected, **Then** the viewer receives and processes all 20 commands in order.
2. **Given** two viewer processes are connected to the same server, **When** a demo script sends a ViewCommand, **Then** both viewers receive the command (broadcast, not round-robin).
3. **Given** a viewer disconnects mid-stream, **When** the next ViewCommand is sent, **Then** the remaining connected viewers still receive it and no errors propagate to the sender.

---

### User Story 2 - MCP SSE Connectivity Through DCP Proxy (Priority: P2)

An AI assistant or developer running inside the container connects to the MCP server's SSE endpoint. The connection succeeds on the first attempt without needing to discover the dynamic port — the Aspire-published internal endpoint works directly for HTTP/1.1 SSE traffic. All MCP clients operate within the container; no external port exposure is needed for MCP.

**Why this priority**: The current MCP endpoint is unreachable via the DCP proxy port because the proxy enforces HTTP/2, rejecting HTTP/1.1 SSE requests. Developers must manually discover the dynamic port via `ss -tlnp`, which is fragile and undocumented.

**Independent Test**: After starting the Aspire stack, connect to the MCP SSE endpoint from within the container using the Aspire-advertised URL. The connection succeeds and receives events.

**Acceptance Scenarios**:

1. **Given** the Aspire stack is running, **When** an MCP client connects to the published MCP endpoint URL, **Then** the SSE handshake succeeds and the client receives the initial event stream.
2. **Given** the MCP server restarts, **When** the MCP client reconnects to the same endpoint URL, **Then** the connection is re-established without manual port discovery.
3. **Given** both gRPC services and the MCP SSE endpoint are running, **When** clients connect to their respective endpoints, **Then** each protocol works correctly on its designated endpoint without interference.

---

### User Story 3 - Reliable Process Cleanup (Priority: P2)

A developer runs `kill.sh` to stop all PhysicsSandbox processes before starting a fresh Aspire stack. All service processes are terminated, no stale processes remain, and the kill script never accidentally kills the developer's shell session, editor, or build process.

**Why this priority**: Stale processes cause silent command theft (duplicate viewers) and port conflicts. The previous `pkill -f` patterns with bare process names killed the developer's own shell, making `kill.sh && dotnet build` unusable.

**Independent Test**: Run `kill.sh` while a PhysicsSandbox stack is running, then verify no PhysicsSandbox processes remain and the calling shell is still alive.

**Acceptance Scenarios**:

1. **Given** a full Aspire stack is running (AppHost, Server, Simulation, Viewer, Client, MCP), **When** `kill.sh` is executed, **Then** all PhysicsSandbox processes are terminated within 5 seconds.
2. **Given** `kill.sh` is chained with another command (e.g., `./kill.sh && dotnet build`), **When** the kill script runs, **Then** the subsequent command executes successfully (exit code 0, shell not killed).
3. **Given** no PhysicsSandbox processes are running, **When** `kill.sh` is executed, **Then** it exits cleanly with no errors.

---

### User Story 4 - Consolidated Network Problem Documentation (Priority: P3)

All known network connectivity issues, root causes, and resolutions are documented in a single `reports/NetworkProblems.md` file. Each entry follows a structured format with context, error, root cause, resolution, and prevention guidance. The document serves as a living troubleshooting guide for the Podman container environment.

**Why this priority**: Network issues are the most time-consuming to debug because they manifest as silent failures. A comprehensive troubleshooting reference prevents re-investigation of known issues.

**Independent Test**: A new developer encountering a network error can search NetworkProblems.md and find the relevant entry with actionable resolution steps.

**Acceptance Scenarios**:

1. **Given** the debugging reports contain 6 documented issues, **When** the consolidation is complete, **Then** all issues appear in NetworkProblems.md with the structured entry format (Context, Error, Root Cause, Resolution, Prevention).
2. **Given** the Podman container environment has specific port mappings and networking constraints, **When** a developer reads NetworkProblems.md, **Then** they find a section documenting the container environment, exposed ports, and their typical uses.
3. **Given** a new network issue is discovered in the future, **When** a developer follows the documented format, **Then** they can add a new entry consistent with existing entries.

---

### User Story 5 - Body-Relative Camera Mode Resilience (Priority: P3)

A demo script sets a camera mode (Follow, Chase, Orbit, Frame) targeting a body that was just created. The camera holds its current position until the body appears in the simulation state, then begins tracking. The camera mode is never silently cancelled due to a timing race.

**Why this priority**: Camera modes targeting newly-created bodies were immediately cancelled because the body hadn't appeared in the simulation state yet. This made follow/chase/orbit demos appear broken.

**Independent Test**: Set CameraFollow on a body ID, then create the body. The camera waits and begins following once the body appears in the next sim state tick.

**Acceptance Scenarios**:

1. **Given** a CameraFollow command references a body ID not yet in the simulation state, **When** the next frame updates, **Then** the camera holds its current position (mode remains active, not cancelled).
2. **Given** a CameraOrbit command references a body that appears 2 frames later, **When** the body appears in the simulation state, **Then** the camera begins orbiting the body from its current position.
3. **Given** a camera mode references a body ID that never appears (invalid ID), **When** 10 seconds elapse, **Then** the mode remains active (no timeout implemented; body may appear later or mode can be cancelled explicitly).

---

### Edge Cases

- What happens when the ViewCommand broadcast subscriber list is modified while a command is being published (concurrent add/remove)?
- How does the system handle a viewer that connects but never reads from its stream (backpressure)?
- What happens when the MCP SSE endpoint receives an HTTP/2 connection attempt?
- What happens when `kill.sh` runs inside a CI environment where processes have different command-line patterns?
- What happens when a demo script sends ViewCommands before any viewer has connected? → Commands are silently dropped (no buffering).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The ViewCommand delivery system MUST broadcast each command to all connected subscribers (not round-robin to a single consumer).
- **FR-002**: The ViewCommand delivery system MUST preserve command ordering per subscriber — commands arrive in the order they were sent.
- **FR-003**: The system MUST handle subscriber disconnection gracefully — removing a subscriber does not affect delivery to remaining subscribers.
- **FR-004**: The ViewCommand delivery system MUST apply backpressure to slow subscribers — if a subscriber's queue exceeds a bounded capacity, the current command is skipped for that subscriber only (newest-drop).
- **FR-005**: The MCP SSE endpoint MUST be reachable via the Aspire-published URL without requiring manual dynamic port discovery.
- **FR-006**: The MCP endpoint MUST support HTTP/1.1 connections for SSE transport, regardless of how other services handle HTTP/2 for gRPC.
- **FR-007**: The process cleanup script MUST terminate all PhysicsSandbox service processes without killing the calling shell, editor, or build processes.
- **FR-008**: The process cleanup script MUST use process identification patterns that are specific enough to avoid false matches (e.g., matching DLL paths, not bare service names).
- **FR-009**: Body-relative camera modes (Follow, Chase, Orbit, Frame, LookAt) MUST hold their current position when the target body is not yet present in the simulation state, rather than cancelling the mode.
- **FR-010**: All known network connectivity issues MUST be documented in `reports/NetworkProblems.md` following the structured entry format defined in the project guidelines.
- **FR-011**: NetworkProblems.md MUST include an environment section documenting the Podman container configuration, exposed ports, and their typical uses.
- **FR-012**: The ViewCommand viewer queue MUST drain all pending commands each frame (not just one per frame) to handle bursts of rapid commands.
- **FR-013**: The ViewCommand broadcast system MUST silently discard commands when no subscribers are connected — no buffering, no error, no replay.

### Key Entities

- **ViewCommand Subscriber**: A connected viewer instance that receives broadcast ViewCommands. Has a unique ID, a bounded command queue, and a connection lifecycle (connect/disconnect).
- **Network Problem Entry**: A structured documentation record with Context, Error, Root Cause, Hypothesis, Resolution, and Prevention fields.
- **Container Port Mapping**: The set of host ports exposed by the Podman container and their designated protocol/service assignments.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 42 demo scripts complete with 100% ViewCommand delivery to connected viewers (zero silent command drops).
- **SC-002**: Two simultaneously connected viewers both receive every ViewCommand (broadcast verified).
- **SC-003**: MCP SSE clients can connect using the Aspire-published URL on the first attempt without manual port discovery.
- **SC-004**: `kill.sh` terminates all PhysicsSandbox processes and returns exit code 0 without killing the calling shell, verified by `./kill.sh && echo "alive"` printing "alive".
- **SC-005**: Camera modes targeting newly-created bodies begin tracking within 2 seconds of the body appearing in simulation state.
- **SC-006**: NetworkProblems.md contains structured entries for all 6+ known issues plus the container environment documentation.

## Assumptions

- All services (Server, Simulation, Viewer, Client, MCP) run inside the Podman container and communicate via localhost. Only the Aspire dashboard requires external access (port 18888, added on next container rebuild; use any open port in the interim).
- The Podman container exposes ports 4173, 5000, 5001, 5137, 5173, 8080, 8081, and 50051 for application services. Aspire DCP allocates dynamic ports internally behind these.
- The existing `ConcurrentQueue<ViewCommand>` drain loop in the viewer is retained — the broadcast change is at the server's channel/distribution layer.
- The MCP transport remains SSE (HTTP/1.1) for the current release; migration to Streamable HTTP is a future consideration. MCP clients operate within the container.
- Single-viewer is the primary use case; multi-viewer broadcast is a robustness improvement, not a user-facing feature.
- The body-not-found camera hold behavior (already partially implemented) is the correct default; no timeout-based cancellation is strictly required but is recommended.
