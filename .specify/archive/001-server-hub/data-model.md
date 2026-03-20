# Data Model: Contracts and Server Hub

**Feature**: 001-server-hub | **Date**: 2026-03-20

## Entities

### SimulationCommand

A command from a user (via client) to control the physics simulation.

| Field | Type | Description |
|-------|------|-------------|
| command | oneof | Exactly one of the variants below |

**Variants**:

| Variant | Fields | Description |
|---------|--------|-------------|
| AddBody | id: string, position: Vec3, velocity: Vec3, mass: double, shape: Shape | Add a new body to the simulation |
| ApplyForce | body_id: string, force: Vec3 | Apply a force vector to an existing body |
| SetGravity | gravity: Vec3 | Set the global gravity vector |
| StepSimulation | delta_time: double | Advance the simulation by one time step |
| PlayPause | running: bool | Set the simulation to running or paused |

### ViewCommand

A command from a user (via client) to control the 3D viewer.

| Field | Type | Description |
|-------|------|-------------|
| command | oneof | Exactly one of the variants below |

**Variants**:

| Variant | Fields | Description |
|---------|--------|-------------|
| SetCamera | position: Vec3, target: Vec3, up: Vec3 | Set camera position, look-at target, and up vector |
| ToggleWireframe | enabled: bool | Enable or disable wireframe rendering |
| SetZoom | level: double | Set the zoom level |

### SimulationState

A snapshot of the physics world at a point in time.

| Field | Type | Description |
|-------|------|-------------|
| bodies | repeated Body | All bodies in the simulation |
| time | double | Current simulation time in seconds |
| running | bool | Whether the simulation is actively running |

### Body

A physical object in the simulation.

| Field | Type | Description |
|-------|------|-------------|
| id | string | Unique identifier |
| position | Vec3 | 3D position in world coordinates |
| velocity | Vec3 | 3D velocity vector |
| mass | double | Mass in kilograms |
| shape | Shape | Geometric shape descriptor |

### Vec3

A 3D vector used for positions, velocities, forces, and directions.

| Field | Type | Description |
|-------|------|-------------|
| x | double | X component |
| y | double | Y component |
| z | double | Z component |

### Shape

Geometric shape of a body.

| Field | Type | Description |
|-------|------|-------------|
| shape | oneof | Exactly one of the variants below |

**Variants**:

| Variant | Fields | Description |
|---------|--------|-------------|
| Sphere | radius: double | A sphere with given radius |
| Box | half_extents: Vec3 | A box defined by half-extents along each axis |
| Plane | normal: Vec3 | An infinite plane defined by its normal vector |

### CommandAck

Acknowledgment response for commands.

| Field | Type | Description |
|-------|------|-------------|
| success | bool | Whether the command was accepted |
| message | string | Human-readable status or error description |

### StateRequest

Request to subscribe to the state stream.

| Field | Type | Description |
|-------|------|-------------|
| (empty) | — | No parameters needed; subscribes the caller to state updates |

## Service Interfaces

### PhysicsHub (client/viewer-facing)

| Method | Request | Response | Pattern | Description |
|--------|---------|----------|---------|-------------|
| SendCommand | SimulationCommand | CommandAck | Unary | Client sends a simulation command |
| SendViewCommand | ViewCommand | CommandAck | Unary | Client sends a view command |
| StreamState | StateRequest | stream SimulationState | Server streaming | Subscribe to simulation state updates |

### SimulationLink (simulation-facing)

| Method | Request | Response | Pattern | Description |
|--------|---------|----------|---------|-------------|
| ConnectSimulation | stream SimulationState | stream SimulationCommand | Bidirectional streaming | Simulation pushes state, receives commands |

## Internal State (Server Hub — not persisted)

| State | Type | Lifecycle | Description |
|-------|------|-----------|-------------|
| Latest state cache | SimulationState (nullable) | In-memory, reset on restart | Most recent state for late-joining subscribers |
| Active simulation flag | boolean | In-memory | Whether a simulation is currently connected |
| State subscribers | Set of stream writers | In-memory, dynamic | Currently connected state stream consumers |
| Command buffer | Channel/queue | In-memory | Pending commands to forward to simulation |
| View command buffer | Channel/queue | In-memory | Pending view commands to forward to viewer |

## Relationships

```
Client/Viewer ──[SimulationCommand, ViewCommand]──▶ PhysicsHub (server)
Client/Viewer ◀──[stream SimulationState]────────── PhysicsHub (server)

Simulation ──[stream SimulationState]──▶ SimulationLink (server)
Simulation ◀──[stream SimulationCommand]── SimulationLink (server)
```

## Constraints

- Only one active simulation connection at a time (FR-014)
- Commands for disconnected consumers are dropped, not queued (Assumption)
- State cache holds exactly one snapshot (the most recent)
- No persistent storage — all state is in-memory and lost on restart
