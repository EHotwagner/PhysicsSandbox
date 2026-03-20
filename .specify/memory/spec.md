# PhysicsSandbox — Main Specification

**Last Updated**: 2026-03-20
**Revision**: Updated with 002-physics-simulation archival

## Overview

PhysicsSandbox is an Aspire-orchestrated distributed application for real-time physics simulation. Four services communicate through a central server hub via gRPC. Each service is added incrementally as a feature spec.

## User Stories

### US-001: Solution Foundation and Orchestrator (P1)
A developer runs the Aspire AppHost to launch the orchestration dashboard, confirming the foundation is operational. [Source: specs/001-server-hub]

### US-002: Shared Communication Contracts (P1)
A developer defines shared gRPC message contracts (SimulationCommand, ViewCommand, SimulationState) in a contracts project so all services agree on message structure. [Source: specs/001-server-hub]

### US-003: Server Hub with Message Routing (P1)
A developer creates the PhysicsServer — the central hub that accepts commands from clients, receives simulation state, caches latest state for late joiners, and fans out updates to all subscribers. Only one simulation source at a time. [Source: specs/001-server-hub]

### US-004: Shared Service Defaults (P2)
A developer sets up standardized health checks, OpenTelemetry, service discovery, and resilience patterns via a shared ServiceDefaults project. [Source: specs/001-server-hub]

### US-005: Simulation Lifecycle Control (P1)
An operator starts the simulation service, connects to the server hub, and controls it via play, pause, and single-step commands. State is streamed after every step. [Source: specs/002-physics-simulation]

### US-006: Body Management (P1)
An operator adds/removes rigid bodies (sphere, box, plane) with unique IDs, position, velocity, mass, and shape. Bodies appear in the streamed state. [Source: specs/002-physics-simulation]

### US-007: Force, Torque, and Impulse Application (P2)
An operator applies persistent forces, one-shot impulses, and torques to bodies by ID. Forces accumulate until cleared. Non-existent body targets are no-ops. [Source: specs/002-physics-simulation]

### US-008: Gravity Configuration (P2)
An operator sets and changes the global gravity vector at any time, affecting all bodies each step. [Source: specs/002-physics-simulation]

### US-009: Continuous State Streaming (P1)
Every simulation step streams the complete world state (all body poses, velocities, angular velocities, time, running flag) to the server for downstream fan-out. [Source: specs/002-physics-simulation]

## Functional Requirements

- **FR-001**: Solution structure with Aspire AppHost, shared contracts, service defaults, and server hub. [Source: specs/001-server-hub]
- **FR-002**: Contracts define `PhysicsHub` service (SendCommand, SendViewCommand, StreamState) and `SimulationLink` service (ConnectSimulation bidirectional stream). [Source: specs/001-server-hub]
- **FR-003**: `SimulationCommand` with variants: AddBody, ApplyForce, SetGravity, StepSimulation, PlayPause, RemoveBody, ApplyImpulse, ApplyTorque, ClearForces. [Source: specs/001-server-hub, specs/002-physics-simulation]
- **FR-004**: `ViewCommand` with variants: SetCamera, ToggleWireframe, SetZoom. [Source: specs/001-server-hub]
- **FR-005**: `SimulationState` with bodies (id, position, velocity, angular_velocity, mass, shape, orientation), time, running flag. [Source: specs/001-server-hub, specs/002-physics-simulation]
- **FR-006**: Server hub accepts simulation commands and forwards to simulation. [Source: specs/001-server-hub]
- **FR-007**: Server hub accepts view commands and forwards to viewer. [Source: specs/001-server-hub]
- **FR-008**: Server hub fans out simulation state to all subscribers. [Source: specs/001-server-hub]
- **FR-009**: Graceful handling when downstream services not connected (ack without error, drop commands). [Source: specs/001-server-hub]
- **FR-010**: AppHost registers server hub as first service. [Source: specs/001-server-hub]
- **FR-011**: ServiceDefaults provides health endpoints, OpenTelemetry, service discovery, resilience. [Source: specs/001-server-hub]
- **FR-012**: Server hub references ServiceDefaults for health and observability. [Source: specs/001-server-hub]
- **FR-013**: Server hub caches latest state and delivers it immediately to late-joining subscribers. [Source: specs/001-server-hub]
- **FR-014**: Single simulation source enforcement — reject second connection with ALREADY_EXISTS. [Source: specs/001-server-hub]
- **FR-015**: Simulation service connects to server hub on startup via SimulationLink protocol. [Source: specs/002-physics-simulation]
- **FR-016**: Simulation starts paused by default; supports play, pause, single-step lifecycle commands. [Source: specs/002-physics-simulation]
- **FR-017**: Simulation advances at 60Hz fixed time step when playing, streams state after each step. [Source: specs/002-physics-simulation]
- **FR-018**: Simulation supports adding dynamic bodies (sphere, box) with position, velocity, mass, shape; assigns unique ID. Plane shapes create statics. [Source: specs/002-physics-simulation]
- **FR-019**: Simulation supports removing bodies by identifier (idempotent). [Source: specs/002-physics-simulation]
- **FR-020**: Simulation supports persistent forces (accumulated per-body, applied each step until cleared). [Source: specs/002-physics-simulation]
- **FR-021**: Simulation supports one-shot impulses (immediate velocity change, not stored). [Source: specs/002-physics-simulation]
- **FR-022**: Simulation supports torques (rotational force on specific body). [Source: specs/002-physics-simulation]
- **FR-023**: Simulation supports clear-forces command (removes all persistent forces on a body). [Source: specs/002-physics-simulation]
- **FR-024**: Simulation supports global gravity vector, changeable at runtime, applied as mass*gravity force each step. [Source: specs/002-physics-simulation]
- **FR-025**: Streamed state includes each dynamic body's position, velocity, angular velocity, mass, shape, identifier, and orientation. Static bodies excluded. [Source: specs/002-physics-simulation]
- **FR-026**: Streamed state includes simulation time and running/paused flag. [Source: specs/002-physics-simulation]
- **FR-027**: Commands targeting non-existent body IDs handled gracefully (success ack, no-op). [Source: specs/002-physics-simulation]
- **FR-028**: Simulation handles server disconnection by logging and shutting down cleanly (no reconnect). [Source: specs/002-physics-simulation]
- **FR-029**: Simulation rejects bodies with zero or negative mass. [Source: specs/002-physics-simulation]
- **FR-030**: Simulation registered in Aspire AppHost with WithReference(server).WaitFor(server). [Source: specs/002-physics-simulation]
- **FR-031**: Proto contracts extended with RemoveBody, ApplyImpulse, ApplyTorque, ClearForces commands and Body angular_velocity/orientation fields. Backward compatible. [Source: specs/002-physics-simulation]

## Key Entities

- **SimulationCommand**: User command to control physics (add body, apply force, set gravity, step, play/pause). [Source: specs/001-server-hub]
- **ViewCommand**: User command to control 3D viewer (camera, wireframe, zoom). [Source: specs/001-server-hub]
- **SimulationState**: Snapshot of physics world — bodies, time, running flag. [Source: specs/001-server-hub]
- **Body**: Physical object — id, Vec3 position, Vec3 velocity, Vec3 angular_velocity, mass, Shape, Vec4 orientation. [Source: specs/001-server-hub, specs/002-physics-simulation]
- **Vec3**: 3D vector (x, y, z doubles). [Source: specs/001-server-hub]
- **Vec4**: 4D vector / quaternion (x, y, z, w doubles). [Source: specs/002-physics-simulation]
- **Shape**: Geometric descriptor — Sphere (radius), Box (half_extents), Plane (normal). [Source: specs/001-server-hub]
- **World**: Simulation environment — physics engine instance, body registry, active forces map, gravity, simulation time, running state. [Source: specs/002-physics-simulation]
- **Force**: Persistent 3D vector applied to a body each step until cleared. [Source: specs/002-physics-simulation]
- **Impulse**: One-shot velocity change applied once on next step. [Source: specs/002-physics-simulation]
- **Torque**: Rotational force vector applied to a body. [Source: specs/002-physics-simulation]
- **CommandAck**: Acknowledgment with success flag and message. [Source: specs/001-server-hub]
- **PhysicsHub**: Client/viewer-facing gRPC service. [Source: specs/001-server-hub]
- **SimulationLink**: Simulation-facing gRPC service (bidirectional streaming). [Source: specs/001-server-hub]

## Edge Cases

- Commands with no simulation connected: acknowledged, dropped gracefully. [Source: specs/001-server-hub]
- No state subscribers: state from simulation accepted but discarded. [Source: specs/001-server-hub]
- Server shutdown during streaming: streams terminated cleanly. [Source: specs/001-server-hub]
- Malformed commands: rejected with descriptive error. [Source: specs/001-server-hub]
- Second simulation connection: rejected with ALREADY_EXISTS. [Source: specs/001-server-hub]
- Server disconnects while simulation running: simulation logs event and shuts down cleanly, no reconnect. [Source: specs/002-physics-simulation]
- Body with zero or negative mass: rejected with error ack. [Source: specs/002-physics-simulation]
- Extremely large forces: simulation continues without crashing (results may be unrealistic). [Source: specs/002-physics-simulation]
- Empty world set to play: continues stepping and streaming empty state. [Source: specs/002-physics-simulation]
- Force/impulse/torque on non-existent body: success ack, no-op. [Source: specs/002-physics-simulation]

## Success Criteria

- **SC-001**: Clone, build, and run AppHost in under 2 minutes. [Source: specs/001-server-hub]
- **SC-002**: Dashboard shows server hub as healthy in Development mode. [Source: specs/001-server-hub]
- **SC-003**: Command acknowledgment within 1 second. [Source: specs/001-server-hub]
- **SC-004**: State stream updates with <100ms latency. [Source: specs/001-server-hub]
- **SC-005**: Health check endpoints (/health, /alive) operational. [Source: specs/001-server-hub]
- **SC-006**: Contracts buildable by any referencing project. [Source: specs/001-server-hub]
- **SC-007**: No errors when sending commands with no downstream. [Source: specs/001-server-hub]
- **SC-008**: Simulation connects to server and is ready within 5 seconds of startup. [Source: specs/002-physics-simulation]
- **SC-009**: All simulation commands produce expected physical result within one step. [Source: specs/002-physics-simulation]
- **SC-010**: Zero skipped steps in state streaming; backpressure paces rather than drops. [Source: specs/002-physics-simulation]
- **SC-011**: Stable operation with 100+ bodies simultaneously. [Source: specs/002-physics-simulation]
- **SC-012**: 37 unit tests + 10 existing tests pass. [Source: specs/002-physics-simulation]
