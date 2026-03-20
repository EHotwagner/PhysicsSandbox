# PhysicsSandbox — Main Specification

**Last Updated**: 2026-03-20
**Revision**: Updated with 003-3d-viewer archival

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

### US-010: View Live Simulation (P1)
A user launches the 3D viewer alongside the running simulation. The viewer connects to the server, subscribes to the state stream, and renders all physics bodies as colored 3D shapes (spheres blue, boxes orange) at correct positions and orientations. Updates in real time as simulation advances. [Source: specs/003-3d-viewer]

### US-011: Camera Control via Commands and Input (P2)
A user controls the camera via interactive mouse/keyboard input (click-drag orbit, scroll zoom, middle-click pan) and via precise REPL commands (SetCamera, SetZoom) forwarded through the server. REPL commands override the current camera state when received. [Source: specs/003-3d-viewer]

### US-012: Wireframe Toggle (P3)
A user toggles wireframe rendering mode from the REPL client. When enabled, all bodies are drawn as wireframe outlines instead of solid shapes. [Source: specs/003-3d-viewer]

### US-013: Simulation Status Display (P3)
The viewer displays simulation metadata — current time and running/paused status — as a text overlay. [Source: specs/003-3d-viewer]

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
- **FR-032**: Viewer connects to server and subscribes to simulation state stream on startup. [Source: specs/003-3d-viewer]
- **FR-033**: Viewer renders each body as a 3D shape (sphere, box) with shape-type-based colors (spheres blue, boxes orange, unknown red). [Source: specs/003-3d-viewer]
- **FR-034**: Viewer positions and orients bodies from proto Vec3 position and Vec4 quaternion orientation. [Source: specs/003-3d-viewer]
- **FR-035**: Viewer updates the rendered scene each time a new simulation state is received. [Source: specs/003-3d-viewer]
- **FR-036**: Viewer applies SetCamera commands by repositioning camera to specified position, target, and up vector. [Source: specs/003-3d-viewer]
- **FR-037**: Viewer applies SetZoom commands by scaling camera distance from target. [Source: specs/003-3d-viewer]
- **FR-038**: Viewer applies ToggleWireframe commands by switching between solid and flat materials. Entity recreation on toggle. [Source: specs/003-3d-viewer]
- **FR-039**: Viewer displays simulation time and running/paused indicator as a DebugText overlay. [Source: specs/003-3d-viewer]
- **FR-040**: Viewer provides default camera position (10,8,10) looking at origin on startup. [Source: specs/003-3d-viewer]
- **FR-041**: Viewer handles late-join gracefully — renders first state received without errors. [Source: specs/003-3d-viewer]
- **FR-042**: Viewer supports interactive mouse/keyboard camera: left-drag orbit, scroll zoom, middle-drag pan. [Source: specs/003-3d-viewer]
- **FR-043**: REPL camera commands override interactive camera state (applied after interactive input each frame). [Source: specs/003-3d-viewer]
- **FR-044**: Viewer displays ground reference grid at Y=0 via Add3DGround + AddGroundGizmo. [Source: specs/003-3d-viewer]
- **FR-045**: Viewer registered in Aspire AppHost with WithReference(server).WaitFor(server). [Source: specs/003-3d-viewer]
- **FR-046**: Viewer uses AddServiceDefaults via background host for OpenTelemetry and structured logging. [Source: specs/003-3d-viewer]
- **FR-047**: Proto extended with StreamViewCommands RPC on PhysicsHub service; server extended with readViewCommand and StreamViewCommands override. [Source: specs/003-3d-viewer]

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
- **SceneState**: Viewer's internal state — tracked body entities (Map<string, Entity>), simulation time, running flag, wireframe flag. [Source: specs/003-3d-viewer]
- **CameraState**: Camera parameters — position, target, up (Vector3), zoom level (float). [Source: specs/003-3d-viewer]
- **ShapeKind**: Discriminated union for visual classification — Sphere, Box, Unknown. Maps to colors and Stride PrimitiveModelType. [Source: specs/003-3d-viewer]

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
- Viewer starts before simulation sends state: shows empty scene, renders once state arrives. [Source: specs/003-3d-viewer]
- Viewer server connection drops: displays last known state, auto-reconnects with exponential backoff (1s→30s). [Source: specs/003-3d-viewer]
- Unknown or unset body shape type: rendered as small red sphere (fallback). [Source: specs/003-3d-viewer]
- Viewer receives state with zero bodies: displays empty scene (ground grid, skybox only). [Source: specs/003-3d-viewer]

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
- **SC-013**: All bodies visible in viewer within 1 second of state receipt. [Source: specs/003-3d-viewer]
- **SC-014**: Camera commands reflected in viewer within 1 second. [Source: specs/003-3d-viewer]
- **SC-015**: Viewer maintains responsive display during continuous state updates. [Source: specs/003-3d-viewer]
- **SC-016**: Wireframe toggle without viewer restart. [Source: specs/003-3d-viewer]
- **SC-017**: Viewer starts and displays scene within 5 seconds when server running. [Source: specs/003-3d-viewer]
- **SC-018**: Viewer renders 100 bodies simultaneously. [Source: specs/003-3d-viewer]
- **SC-019**: 66 total tests passing (16 viewer + 13 server + 37 simulation). [Source: specs/003-3d-viewer]
