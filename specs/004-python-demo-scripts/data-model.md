# Data Model: Python Demo Scripts

**Feature**: 004-python-demo-scripts
**Date**: 2026-03-21

## Entities

### Session

Represents an active gRPC connection to the PhysicsServer.

| Field | Type | Description |
|-------|------|-------------|
| channel | grpc.Channel | The gRPC channel to the server |
| stub | PhysicsHubStub | The generated gRPC service stub |
| address | str | Server address used for connection |

**Lifecycle**: Created by `connect()`, destroyed by `disconnect()`. Passed to all command functions.

### Demo

Represents a single physics demo scenario.

| Field | Type | Description |
|-------|------|-------------|
| name | str | Human-readable demo name (e.g., "Hello Drop") |
| description | str | One-line description of the scenario |
| run | Callable[[Session], None] | Function that executes the demo given an active session |

**Usage**: Demos are registered in `all_demos.py` as a list. Runners iterate this list.

### Direction (Enum)

Cardinal directions for the `push()` steering command.

| Value | Impulse Vector (x, y, z) |
|-------|--------------------------|
| Up | (0, 1, 0) |
| Down | (0, -1, 0) |
| North | (0, 0, -1) |
| South | (0, 0, 1) |
| East | (1, 0, 0) |
| West | (-1, 0, 0) |

### ID Counters (Module State)

Global mutable state for auto-generating unique body IDs.

| Field | Type | Description |
|-------|------|-------------|
| _counters | dict[str, int] | Maps prefix (e.g., "sphere") to next counter value |

**Operations**: `next_id(prefix)` increments and returns `"{prefix}-{n}"`. `reset_ids()` clears all counters.

## Proto Message Usage

The Python demos use proto messages defined in `physics_hub.proto`. No new messages are defined — all communication uses the existing contract.

**Frequently constructed messages**:
- `Vec3(x=, y=, z=)` — position, force, impulse, torque vectors
- `Sphere(radius=)`, `Box(half_extents=Vec3(...))` — shape definitions
- `Shape(sphere=)` or `Shape(box=)` — shape wrapper
- `AddBody(id=, position=, mass=, shape=)` — body creation
- `ApplyImpulse(body_id=, impulse=)` — impulse application
- `ApplyTorque(body_id=, torque=)` — torque application
- `SimulationCommand(add_body=)` etc. — command wrapper (oneof)
- `ViewCommand(set_camera=)` etc. — view command wrapper (oneof)
- `PlayPause(running=)` — simulation control
- `SetGravity(gravity=)` — gravity control
- `BatchSimulationRequest(commands=[...])` — batch operations

## State Flow

```
Python Script → prelude.py helpers → gRPC stubs → PhysicsServer → PhysicsSimulation
                                                        ↓
                                                  PhysicsViewer (3D display)
```

All state lives server-side. Python scripts are stateless beyond the Session object and local ID counters.
