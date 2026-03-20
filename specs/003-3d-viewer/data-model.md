# Data Model: 003-3d-viewer

## Entities

### ViewerState

Internal state of the viewer, updated each frame.

| Field | Type | Description |
|-------|------|-------------|
| Bodies | `Map<string, RenderedBody>` | Currently rendered bodies, keyed by body ID |
| Camera | `CameraState` | Current camera position/target/zoom |
| Wireframe | `bool` | Whether wireframe mode is active |
| SimTime | `float` | Latest simulation time |
| SimRunning | `bool` | Whether the simulation is running or paused |
| Connected | `bool` | Whether gRPC connection to server is active |

### RenderedBody

Represents a single physics body mapped to a Stride3D entity.

| Field | Type | Description |
|-------|------|-------------|
| Id | `string` | Body identifier (matches proto Body.id) |
| Entity | `Entity` | Stride3D entity reference (mutable transform) |
| ShapeType | `ShapeKind` | Sphere, Box, or Unknown |

### CameraState

Camera parameters, set via REPL commands or interactive input.

| Field | Type | Description |
|-------|------|-------------|
| Position | `Vector3` | Camera position in world space |
| Target | `Vector3` | Look-at target point |
| Up | `Vector3` | Up vector (default: Y-axis) |
| ZoomLevel | `float` | Zoom level (affects FOV or distance) |

### ShapeKind

Discriminated union for supported shape types.

| Variant | Maps From | Stride Primitive | Color |
|---------|-----------|------------------|-------|
| Sphere | Proto `Sphere` | `PrimitiveModelType.Sphere` | Blue |
| Box | Proto `Box` | `PrimitiveModelType.Cube` | Orange |
| Unknown | Unset/unsupported | `PrimitiveModelType.Sphere` | Red |

## State Transitions

### Body Lifecycle

```
(absent) ──[body appears in state]──▶ Created (entity added to scene)
Created  ──[body in next state]──────▶ Updated (position/orientation changed)
Updated  ──[body absent from state]──▶ Removed (entity removed from scene)
```

### Camera State

```
Default ──[SetCamera cmd]──────▶ REPL-positioned
Default ──[mouse drag]─────────▶ User-positioned
*       ──[SetCamera cmd]──────▶ REPL-positioned (overrides any state)
*       ──[SetZoom cmd]────────▶ Zoom updated (position unchanged)
*       ──[mouse scroll]───────▶ Zoom updated
```

### Wireframe Mode

```
Solid ──[ToggleWireframe enabled=true]──▶ Wireframe
Wireframe ──[ToggleWireframe enabled=false]──▶ Solid
```

## Data Flow

```
gRPC StreamState ──▶ ConcurrentQueue<SimulationState> ──▶ Game Update Loop
                                                              │
                                                              ▼
                                                     SceneManager.applyState
                                                     (diff bodies, update entities)

gRPC StreamViewCommands ──▶ ConcurrentQueue<ViewCommand> ──▶ Game Update Loop
                                                                  │
                                                                  ▼
                                                         CameraController.applyCommand
                                                         (update camera/wireframe)

Mouse/Keyboard ──▶ Stride InputManager ──▶ Game Update Loop
                                                │
                                                ▼
                                       CameraController.applyInput
                                       (orbit/pan/zoom)
```
