# Feature Specification: Contracts and Server Hub

**Feature Branch**: `001-server-hub`
**Created**: 2026-03-20
**Status**: Completed
**Input**: User description: "Create a physics sandbox. The Aspire server is the first feature — it is the center of this distributed application. Services will be added later."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Solution Foundation and Orchestrator (Priority: P1)

A developer creates the Physics Sandbox solution with the Aspire orchestrator (AppHost) and shared infrastructure projects. Running the AppHost starts the orchestration dashboard and confirms the foundation is operational — even before any domain services exist.

**Why this priority**: Nothing else can be built without the solution structure and orchestrator in place. This is the foundation that all future services depend on.

**Independent Test**: Can be fully tested by running the AppHost and verifying the orchestration dashboard launches with no registered services. Delivers a working build/run cycle from day one.

**Acceptance Scenarios**:

1. **Given** the solution exists with the AppHost project, **When** a developer runs the AppHost, **Then** the orchestration dashboard launches and displays an empty resource list with no errors.
2. **Given** the AppHost is running, **When** a developer inspects the dashboard, **Then** it shows system health status and is ready to accept service registrations in the future.

---

### User Story 2 - Shared Communication Contracts (Priority: P1)

A developer defines the shared message contracts that all future services will use for communication. These contracts describe the commands, state, and view-control messages that flow through the server hub. They are defined once in a shared contracts project so that all services agree on message structure.

**Why this priority**: Contracts must exist before any service can send or receive messages. They are the API boundary between all services and must be defined up-front.

**Independent Test**: Can be fully tested by verifying the contracts project builds and that all message types, fields, and service methods are present and well-formed. Delivers a compilable, versionable contract artifact.

**Acceptance Scenarios**:

1. **Given** the contracts project exists, **When** a developer builds it, **Then** it compiles and generates typed message and service stubs for use by other projects.
2. **Given** the contracts define a `PhysicsHub` service, **When** a developer inspects the contract, **Then** it includes methods for sending simulation commands, sending view commands, and streaming simulation state.
3. **Given** the contracts define message types, **When** a developer inspects them, **Then** `SimulationCommand` supports adding bodies, applying forces, setting gravity, stepping, and play/pause; `ViewCommand` supports camera, wireframe, and zoom; and `SimulationState` describes bodies with position, velocity, mass, and shape.

---

### User Story 3 - Server Hub with Message Routing (Priority: P1)

A developer creates the Server (PhysicsServer) — the central hub through which all messages flow. The server accepts commands from clients and forwards them to the simulation. It receives simulation state and fans it out to all connected clients and viewers. It accepts view commands and forwards them to viewers.

**Why this priority**: The server hub is the central nervous system of the application. Without it, no service can communicate with any other. It must exist and be testable before any other service is built.

**Independent Test**: Can be fully tested by connecting a test client to the server, sending a command, and verifying the server acknowledges it. State streaming can be tested with a mock simulation source. Delivers a running, connectable hub service registered in the Aspire AppHost.

**Acceptance Scenarios**:

1. **Given** the server is running and registered in the AppHost, **When** a client sends a `SimulationCommand`, **Then** the server acknowledges receipt and makes the command available for forwarding to a simulation service (when one connects later).
2. **Given** the server is running, **When** a client sends a `ViewCommand`, **Then** the server acknowledges receipt and makes the command available for forwarding to a viewer service (when one connects later).
3. **Given** the server is receiving simulation state, **When** one or more clients are subscribed to the state stream, **Then** the server fans out the state to all subscribers.
4. **Given** no simulation or viewer services are connected yet, **When** a client sends a command, **Then** the server does not error — it acknowledges the command gracefully.
5. **Given** the server has received at least one state update, **When** a new client or viewer subscribes to the state stream, **Then** it immediately receives the most recent cached state snapshot before any subsequent live updates.

---

### User Story 4 - Shared Service Defaults (Priority: P2)

A developer sets up the shared service defaults project that all future services will reference. This project provides standardized health checks, observability (telemetry, logging, tracing), service discovery, and resilience patterns. Any service that references it automatically gets these capabilities.

**Why this priority**: Service defaults ensure consistency across all services. While the server hub can function without them initially, establishing them now means every future service starts with production-quality infrastructure from day one.

**Independent Test**: Can be tested by having the server hub reference the service defaults and verifying that health check endpoints (`/health`, `/alive`) become available and the dashboard shows health status.

**Acceptance Scenarios**:

1. **Given** a service references the service defaults project, **When** the service starts, **Then** health check endpoints are automatically registered and respond to requests.
2. **Given** a service references the service defaults project, **When** the service handles requests, **Then** structured logs and traces are emitted and visible in the Aspire dashboard.

---

### Edge Cases

- What happens when the server receives a command but no simulation service is connected? The server acknowledges the command without error. Commands sent before a simulation connects are not queued (they are dropped gracefully).
- What happens when no clients are subscribed to the state stream? The server continues to accept state from the simulation (when connected) but discards it if no subscribers exist.
- What happens if the server shuts down while clients are streaming? Open streams are terminated cleanly with appropriate status codes.
- What happens if a malformed or unknown command type is received? The server rejects it with a descriptive error response.
- What happens if a second simulation service attempts to connect while one is already active? The server supports only one simulation source at a time — it rejects the new connection or replaces the existing one.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a solution structure containing an Aspire AppHost orchestrator, a shared contracts project, a shared service defaults project, and the server hub service.
- **FR-002**: The shared contracts project MUST define a `PhysicsHub` service with methods for sending simulation commands, sending view commands, and streaming simulation state; and a `SimulationLink` service with a bidirectional streaming method for the simulation to push state and receive commands.
- **FR-003**: The contracts MUST define `SimulationCommand` with variants: add body, apply force, set gravity, step simulation, and play/pause.
- **FR-004**: The contracts MUST define `ViewCommand` with variants: set camera, toggle wireframe, and set zoom.
- **FR-005**: The contracts MUST define `SimulationState` containing a collection of bodies (each with id, position, velocity, mass, and shape), a simulation time, and a running/paused flag.
- **FR-006**: The server hub MUST accept simulation commands from connected clients and make them available for forwarding to a simulation service.
- **FR-007**: The server hub MUST accept view commands from connected clients and make them available for forwarding to a viewer service.
- **FR-008**: The server hub MUST accept a simulation state stream and fan out state updates to all subscribed clients and viewers.
- **FR-013**: The server hub MUST cache the most recent simulation state snapshot and deliver it immediately to any newly subscribing client or viewer.
- **FR-009**: The server hub MUST handle the case where downstream services (simulation, viewer) are not yet connected, acknowledging commands gracefully without errors.
- **FR-014**: The server hub MUST support only one connected simulation source at a time. If a second simulation attempts to connect, the server MUST either reject the new connection or replace the existing one.
- **FR-010**: The AppHost MUST register the server hub and start it as the first service, so that future services can declare a dependency on it.
- **FR-011**: The service defaults project MUST provide health check endpoints, structured observability (logging, tracing, metrics), service discovery configuration, and resilience patterns for outgoing calls.
- **FR-012**: The server hub MUST reference the service defaults so that health checks and observability are active from initial deployment.

### Key Entities

- **SimulationCommand**: A command sent by a user (via a client) to control the physics simulation. Variants include adding bodies, applying forces, changing gravity, stepping the simulation, and toggling play/pause.
- **ViewCommand**: A command sent by a user (via a client) to control the 3D viewer. Variants include camera positioning, wireframe toggling, and zoom adjustment.
- **SimulationState**: A snapshot of the physics world at a point in time. Contains a list of bodies, the current simulation time, and whether the simulation is running.
- **Body**: A physical object in the simulation. Has an identifier, 3D position, 3D velocity, mass, and a shape descriptor.
- **PhysicsHub**: The central routing service. Defines the communication interface between all services in the system.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can clone the repository, build the solution, and run the AppHost in under 2 minutes with a single command.
- **SC-002**: The orchestration dashboard is accessible and displays the server hub as a healthy, running resource in Development mode.
- **SC-003**: A test client can connect to the server hub, send a simulation command, and receive an acknowledgment within 1 second.
- **SC-004**: A test client can subscribe to the state stream and, when state is published, receive updates with less than 100 milliseconds latency.
- **SC-005**: The server hub exposes health check endpoints that confirm service readiness and liveness.
- **SC-006**: All message types defined in the contracts are buildable and usable by any project that references the contracts project — no additional configuration required.
- **SC-007**: The server hub handles commands sent when no downstream services are connected without producing errors in logs or returning failure responses.

## Clarifications

### Session 2026-03-20

- Q: Should late-joining subscribers receive the latest cached state immediately on connect, or only future updates? → A: Server caches latest state; new subscribers get it immediately on connect.
- Q: Should the server support one or multiple simultaneous simulation sources? → A: Single simulation only — reject or replace if a second one connects.

## Assumptions

- The AppHost orchestrator is written in C# (per design guidance), while service projects use F#.
- No persistent storage (database) is needed for this feature — the server hub is a stateless message router.
- Container runtime is Podman (rootless), configured via AppHost launch settings.
- The server hub does not queue commands for disconnected downstream services — commands are dropped if no consumer is connected. This is acceptable for a real-time physics simulation where stale commands have no value.
- The .NET solution name follows the "PhysicsSandbox" naming from the design documents.
