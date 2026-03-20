# Data Model: MCP Server and Integration Testing

**Date**: 2026-03-20 | **Feature**: 005-mcp-server-testing

## Entities

### MCP Tool Definitions

The MCP server exposes ~15 tools, each mapping to a gRPC operation. No new persistent entities are introduced — the MCP server is a stateless bridge (except for the cached state snapshot).

#### Simulation Command Tools (9 tools)

| Tool Name | Input Parameters | gRPC Mapping |
|-----------|-----------------|--------------|
| `add_body` | id (optional), shape (sphere/box), position (x,y,z), mass, velocity (x,y,z, optional) | `SendCommand(AddBody)` |
| `apply_force` | body_id, force (x,y,z) | `SendCommand(ApplyForce)` |
| `apply_impulse` | body_id, impulse (x,y,z) | `SendCommand(ApplyImpulse)` |
| `apply_torque` | body_id, torque (x,y,z) | `SendCommand(ApplyTorque)` |
| `set_gravity` | gravity (x,y,z) | `SendCommand(SetGravity)` |
| `step` | (none) | `SendCommand(StepSimulation)` |
| `play` | (none) | `SendCommand(PlayPause { play=true })` |
| `pause` | (none) | `SendCommand(PlayPause { play=false })` |
| `remove_body` | body_id | `SendCommand(RemoveBody)` |
| `clear_forces` | body_id | `SendCommand(ClearForces)` |

#### View Command Tools (3 tools)

| Tool Name | Input Parameters | gRPC Mapping |
|-----------|-----------------|--------------|
| `set_camera` | position (x,y,z), target (x,y,z) | `SendViewCommand(SetCamera)` |
| `set_zoom` | level (float) | `SendViewCommand(SetZoom)` |
| `toggle_wireframe` | (none) | `SendViewCommand(ToggleWireframe)` |

#### Query Tools (2 tools)

| Tool Name | Input Parameters | gRPC Mapping |
|-----------|-----------------|--------------|
| `get_state` | (none) | Returns cached `SimulationState` from background stream |
| `get_status` | (none) | Returns connection health: server reachable, state stream active, last update timestamp |

### Cached State

The MCP server maintains one piece of runtime state:

- **LatestState**: `SimulationState option` — the most recent state received from the background `StreamState` subscription.
- **LastUpdateTime**: `DateTimeOffset` — when the cached state was last updated. Used for staleness detection in `get_state` responses.
- **StreamConnected**: `bool` — whether the background state stream is currently active.

### Tool Response Format

All tools return human-readable text responses:

- **Command tools**: Return the `CommandAck` message: success/failure boolean and message string.
- **get_state**: Returns formatted state — body count, simulation time, running status, and a table of bodies (id, position, velocity, mass, shape).
- **get_status**: Returns connection summary — server address, stream status, last update time, staleness indicator.

## State Transitions

No new state machines. The MCP server is stateless except for the cached state snapshot, which is continuously overwritten by the background stream.

## Relationships

```
MCP Client (stdio) → MCP Server → (gRPC) → PhysicsServer → PhysicsSimulation
                                          → PhysicsViewer
```

The MCP server is a gRPC client of PhysicsServer, identical in role to PhysicsClient. It does not interact with PhysicsSimulation or PhysicsViewer directly.
