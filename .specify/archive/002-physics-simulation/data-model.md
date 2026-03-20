# Data Model: Physics Simulation Service

**Branch**: `002-physics-simulation` | **Date**: 2026-03-20

## Entities

### SimulationWorld (singleton per service instance)

The top-level container for all simulation state.

| Field | Type | Description |
|-------|------|-------------|
| PhysicsWorld | BepuFSharp.PhysicsWorld | Underlying physics engine instance |
| Bodies | Map<string, BodyRecord> | Body ID → body tracking record |
| ActiveForces | Map<string, Vec3 list> | Body ID → list of persistent force vectors |
| Gravity | Vec3 | Current gravity vector (default: zero) |
| SimulationTime | float | Accumulated simulation time in seconds |
| Running | bool | Play/pause state (default: false/paused) |
| TimeStep | float32 | Fixed step duration (1/60 second) |

**State transitions**:
- `Paused` → `Playing` (PlayPause running=true)
- `Playing` → `Paused` (PlayPause running=false)
- `Paused` → `Paused` (StepSimulation: advance one step, remain paused)

### BodyRecord

Tracks the mapping between user-facing body IDs and BepuFSharp internal handles.

| Field | Type | Description |
|-------|------|-------------|
| Id | string | User-assigned unique identifier |
| BepuBodyId | BepuFSharp.BodyId | Opaque handle into BepuFSharp world |
| ShapeId | BepuFSharp.ShapeId | Opaque shape handle |
| Mass | float32 | Body mass in kg (>0, validated on add) |
| ShapeDesc | ShapeDescription | Original shape parameters for state reporting |

### ShapeDescription (discriminated union for state reporting)

| Variant | Fields | Description |
|---------|--------|-------------|
| Sphere | radius: float32 | Sphere shape |
| Box | halfExtents: Vec3 | Box shape (half-widths) |
| Plane | normal: Vec3 | Infinite static plane |

### SimulationState (proto message, emitted every step)

| Field | Type | Description |
|-------|------|-------------|
| bodies | repeated Body | All bodies with current state |
| time | double | Current simulation clock |
| running | bool | Whether simulation is playing |

### Body (proto message, per-body state)

| Field | Type | Description |
|-------|------|-------------|
| id | string | Unique body identifier |
| position | Vec3 | World-space position |
| velocity | Vec3 | Linear velocity |
| angular_velocity | Vec3 | Angular velocity (new field) |
| mass | double | Body mass |
| shape | Shape | Shape descriptor |
| orientation | Vec4 | Rotation quaternion (new field) |

## Relationships

```
SimulationWorld 1──* BodyRecord (Bodies map)
SimulationWorld 1──* Vec3 list (ActiveForces map, keyed by body ID)
BodyRecord 1──1 BepuFSharp.BodyId (engine handle)
BodyRecord 1──1 BepuFSharp.ShapeId (engine handle)
BodyRecord 1──1 ShapeDescription (for state reporting)
```

## Validation Rules

- **Body ID uniqueness**: AddBody MUST reject duplicate IDs (return error in CommandAck)
- **Positive mass**: AddBody MUST reject mass ≤ 0 (return error in CommandAck)
- **Body existence**: RemoveBody, ApplyForce, ApplyImpulse, ApplyTorque, ClearForces on non-existent body ID → no-op with success ack
- **Shape mapping**: Proto Sphere/Box/Plane map to BepuFSharp PhysicsShape.Sphere/Box. Plane maps to a large static box (BepuPhysics2 has no infinite plane; approximate with large thin box)

## Command → BepuFSharp API Mapping

| Proto Command | BepuFSharp Call | Notes |
|---------------|-----------------|-------|
| AddBody | `addShape` + `addBody` or `addStatic` | Static if plane, dynamic otherwise |
| RemoveBody | `removeBody` | Also clears from Bodies map and ActiveForces |
| ApplyForce | — | Stored in ActiveForces map, applied each step |
| ApplyImpulse | `applyLinearImpulse` | One-shot, applied immediately |
| ApplyTorque | `applyTorque` | Applied with dt parameter |
| ClearForces | — | Remove body's entry from ActiveForces map |
| SetGravity | Update Gravity field | Applied as force (mass × gravity) each step |
| StepSimulation | `step` | Uses provided dt or default TimeStep |
| PlayPause | Toggle Running flag | Controls simulation loop |
